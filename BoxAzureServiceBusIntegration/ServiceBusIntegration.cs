using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.ServiceBus;
using System.Text;
using Box.V2.Models;
using Box.V2.Converter;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using static Box.EnterpriseDevelopmentKit.Azure.Shared.Config;

namespace Box.EnterpriseDevelopmentKit.Azure
{
    public static class ServiceBusIntegration
    {
        public const int INITIAL_LOOKBACK_MINUTES = 60;
        public const string BOX_CONFIG_KEY = "BoxConfig";
        public const string STORAGE_CONNECTION_STRING_KEY = "AzureWebJobsStorage";
        public const string STREAM_POSITION_TABLE_NAME_KEY = "StreamPositionTableName";
        public const string ROW_KEY_VALUE = "BoxStreamPosition";
        public const string SERVICE_BUS_CONNECTION_STRING_KEY = "ServiceBusConnectionString";
        public const string SERVICE_BUS_TOPIC_NAME_KEY = "ServiceBusTopicName";

        //commentary on why Service Bus instead of Storage Queue:  
        //https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-azure-and-service-bus-queues-compared-contrasted

        static bool resetNextStreamPosition_DebugOnly = false;  //used for debugging; clears NextStreamPosition causing fetch process to reset

        [FunctionName("BoxAzureServiceBusIntegration")]
        public static async Task Run([TimerTrigger("%TimerScheduleExpression%")]TimerInfo myTimer, TraceWriter log, ExecutionContext context)
        {
            var config = GetConfiguration(context);

            //Box setup
            var box = GetBoxAdminClient(config);

            //Azure Table setup
            var table = await SetupTable(config);      

            string nextStreamPosition = null;

            if (resetNextStreamPosition_DebugOnly)
            {
                log.Warning("Resetting Table to re-initiate stream position lookback");

                var nextStreamPositionEntityToDelete = await RetrieveNextStreamPosition(table, config);
                if (nextStreamPositionEntityToDelete != null)
                {
                    var deleteOperation = TableOperation.Delete(nextStreamPositionEntityToDelete);
                    await table.ExecuteAsync(deleteOperation);
                    log.Info("Deleted existing BoxStreamPositionEntity");
                }
                else
                {
                    log.Info("Could not retrieve existing BoxStreamPositionEntity");
                }

                resetNextStreamPosition_DebugOnly = false;
            }

            var nextStreamPositionEntity = await RetrieveNextStreamPosition(table, config);

            if (nextStreamPositionEntity == null)
            {
                log.Warning("NextStreamPosition not found in Table; Performing lookback to establish stream position");
                var startDate = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(INITIAL_LOOKBACK_MINUTES));
                var events = await box.EventsManager.EnterpriseEventsAsync(createdAfter: startDate, createdBefore: null);

                if (events.NextStreamPosition == "0") //this means that no events occurred in the lookback period; you need to generate an event in Box that will be picked up during the next attempt
                {
                    log.Warning("NextStreamPosition was returned as '0'.  Sleeping until next invocation.  Generate at least one event in Box to start fetching via stream position.  Note that events from Box are delayed by up to one minute.");
                    return;
                }
                else
                {
                    events.Entries.ForEach(e => log.Info($"Box Event Received (Type='{e.EventType}', Id={e.EventId})"));
                    nextStreamPosition = events.NextStreamPosition;

                    //create initial stream position in Table
                    StoreNextStreamPosition(nextStreamPosition, table, config, log);
                }
            }
            else
            {
                nextStreamPosition = nextStreamPositionEntity.NextStreamPosition;
                log.Info($"NextStreamPosition found in Table: '{nextStreamPosition}'");
            }

            //Service Bus setup
            //https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-how-to-use-topics-subscriptions
            var serviceBusConnectionString = config[SERVICE_BUS_CONNECTION_STRING_KEY];
            var topicName = config[SERVICE_BUS_TOPIC_NAME_KEY];
            ITopicClient topicClient = new TopicClient(serviceBusConnectionString, topicName);

            var currFetch = 0;
            while (currFetch++ < 4)
            {
                log.Info($"Fetching batch {currFetch} of events using stream position '{nextStreamPosition}'");

                var events = await box.EventsManager.EnterpriseEventsAsync(streamPosition: nextStreamPosition);
                nextStreamPosition = events.NextStreamPosition;

                if (events.Entries.Count == 0)
                {
                    //Store stream position even though no events were returned
                    StoreNextStreamPosition(nextStreamPosition, table, config, log);

                    log.Info("No more event entries found. Exiting this invocation.");
                    break;
                }

                try
                {
                    BoxJsonConverter bjc = new BoxJsonConverter();
                    foreach (var e in events.Entries)
                    {
                        log.Info($"Box Event Received (Type='{e.EventType}', Id={e.EventId})");
                        var eventJson = bjc.Serialize<BoxEnterpriseEvent>(e);
                        var messageId = e.EventId; //this is the value that is used by Service Bus for deduplication
                        var message = new Message(Encoding.UTF8.GetBytes(eventJson))
                        {
                            ContentType = "application/json",
                            MessageId = messageId,
                        };

                        await topicClient.SendAsync(message);
                        log.Info($"Sent message with Id '{messageId}' to Service Bus topic '{topicName}'");
                    }

                    //Successful propogation of all events to Service Bus. Store the stream position in Table
                    StoreNextStreamPosition(nextStreamPosition, table, config, log);
                }
                catch (Exception ex)
                {
                    log.Error($"Error while propogating events to Service Bus.  Error Message: {ex.Message}");
                    break;
                }
            }

            await topicClient.CloseAsync();
        }

        static string BuildPartitionKey(string topicName)
        {
            return $"BoxEvents-{topicName}";
        }

        static async Task<CloudTable> SetupTable(IConfigurationRoot config)
        {
            var storageConnectionString = config[STORAGE_CONNECTION_STRING_KEY];
            var tableName = config[STREAM_POSITION_TABLE_NAME_KEY];
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync();
            return table;
        }

        static async Task<BoxStreamPositionEntity> RetrieveNextStreamPosition(CloudTable table, IConfigurationRoot config)
        {
            var retrieveOp = TableOperation.Retrieve<BoxStreamPositionEntity>(BuildPartitionKey(config[SERVICE_BUS_TOPIC_NAME_KEY]), ROW_KEY_VALUE);
            var retrievedRes = await table.ExecuteAsync(retrieveOp);
            var nextStreamPositionEntity = (BoxStreamPositionEntity)retrievedRes.Result;
            return nextStreamPositionEntity;
        }

        static async void StoreNextStreamPosition(string nextStreamPosition, CloudTable table, IConfigurationRoot config, TraceWriter log)
        {
            var nextStreamPositionEntry = new BoxStreamPositionEntity(BuildPartitionKey(config[SERVICE_BUS_TOPIC_NAME_KEY]), ROW_KEY_VALUE)
            {
                NextStreamPosition = nextStreamPosition
            };
            var insertOperation = TableOperation.InsertOrReplace(nextStreamPositionEntry);
            await table.ExecuteAsync(insertOperation);
            log.Info($"Stored NextStreamPosition '{nextStreamPosition}' in Table");
        }

       
    }

    public class BoxStreamPositionEntity : TableEntity
    {
        public BoxStreamPositionEntity(string partitionkey, string rowKey)
        {
            this.PartitionKey = partitionkey;
            this.RowKey = rowKey;
        }

        public BoxStreamPositionEntity() { }

        public string NextStreamPosition { get; set; }
    }
}
