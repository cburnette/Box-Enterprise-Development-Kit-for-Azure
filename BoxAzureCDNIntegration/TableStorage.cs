using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;
using static Box.EnterpriseDevelopmentKit.Azure.BoxAzureCDNIntegration.Config;

namespace Box.EnterpriseDevelopmentKit.Azure.BoxAzureCDNIntegration
{
    static class TableStorage
    {
        public static async void StoreBoxCDNInfo(string guid, string fileId, string ownerId, CloudTable fileTable, CloudTable guidTable, IConfigurationRoot config, TraceWriter log)
        {
            var boxCDNGuidEntity = new BoxCDNGuidEntity(guid)
            {
                FileId = fileId,
                OwnerId = ownerId
            };
            var insertOperation = TableOperation.InsertOrReplace(boxCDNGuidEntity);
            await guidTable.ExecuteAsync(insertOperation);
            log.Info($"Stored BoxCDNGuidEntity in Table (guid={guid})");

            var boxCDNFileEntity = new BoxCDNFileEntity(fileId)
            {
                Guid = guid,
                OwnerId = ownerId
            };
            insertOperation = TableOperation.InsertOrReplace(boxCDNFileEntity);
            await fileTable.ExecuteAsync(insertOperation);
            log.Info($"Stored BoxCDNFileEntity in Table (fileId={fileId})");
        }

        public static async Task<BoxCDNGuidEntity> RetrieveBoxCDNGuidEntity(CloudTable table, string guid, IConfigurationRoot config, TraceWriter log)
        {
            var retrieveOp = TableOperation.Retrieve<BoxCDNGuidEntity>(guid, string.Empty);
            var retrievedRes = await table.ExecuteAsync(retrieveOp);
            var boxCDNGuidEntity = (BoxCDNGuidEntity)retrievedRes.Result;
            log.Info($"Retrieved BoxCDNGuidEntity from Table (guid={guid})");
            return boxCDNGuidEntity;
        }

        public static async Task<BoxCDNFileEntity> RetrieveBoxCDNFileEntity(CloudTable table, string fileId, IConfigurationRoot config, TraceWriter log)
        {
            var retrieveOp = TableOperation.Retrieve<BoxCDNFileEntity>(fileId, string.Empty);
            var retrievedRes = await table.ExecuteAsync(retrieveOp);
            var boxCDNFileEntity = (BoxCDNFileEntity)retrievedRes.Result;
            log.Info($"Retrieved BoxCDNFileEntity from Table (fileId={fileId})");
            return boxCDNFileEntity;
        }

        public static async Task<(CloudTable, CloudTable)> SetupTables(IConfigurationRoot config)
        {
            var storageConnectionString = config[TABLE_STORAGE_CONNECTION_STRING_KEY];
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();

            var fileTable = tableClient.GetTableReference(CDN_FILE_TABLE_NAME);
            await fileTable.CreateIfNotExistsAsync();

            var guidTable = tableClient.GetTableReference(CDN_GUID_TABLE_NAME);
            await guidTable.CreateIfNotExistsAsync();

            return (fileTable, guidTable);
        }
    }

    public class BoxCDNGuidEntity : TableEntity
    {
        public BoxCDNGuidEntity(string guid)
        {
            this.PartitionKey = guid;
            this.RowKey = string.Empty;
        }

        public BoxCDNGuidEntity() { }

        public string FileId { get; set; }
        public string OwnerId { get; set; }
    }

    public class BoxCDNFileEntity : TableEntity
    {
        public BoxCDNFileEntity(string fileId)
        {
            this.PartitionKey = fileId;
            this.RowKey = string.Empty;
        }

        public BoxCDNFileEntity() { }

        public string Guid { get; set; }
        public string OwnerId { get; set; }
    }
}
