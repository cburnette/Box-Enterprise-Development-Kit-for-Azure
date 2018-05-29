
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Box.V2;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using Box.V2.Managers;
using Microsoft.WindowsAzure.Storage.Queue;
using static Box.EnterpriseDevelopmentKit.Azure.Config;
using static Box.EnterpriseDevelopmentKit.Azure.Shared.Config;
using Microsoft.WindowsAzure.Storage.Table;
using System.Security.Cryptography;

namespace Box.EnterpriseDevelopmentKit.Azure
{
    //Box CLI command to create webhook:
    //box webhooks create 48060867550 folder FILE.UPLOADED,FILE.DELETED,FILE.TRASHED https://cburnette.ngrok.io/api/BoxAzureCDNIntegration --as-user 3419749388

    //Ngrok command:
    //ngrok http -subdomain=cburnette 7071

    //skills url
    //https://cburnette.ngrok.io/api/BoxAzureSkillsTemplate

    public static class CDNIntegration
    {
        [FunctionName("BoxAzureCDNIntegration")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req, TraceWriter log, ExecutionContext context)
        {
            var config = GetConfiguration(context);

            string requestBody = new StreamReader(req.Body).ReadToEnd();

            if(!ValidateWebhookSignatures(req, config, requestBody))
            {
                log.Warning("Signature check for Box webhook failed");
                return (ActionResult)new BadRequestResult();
            }

            bool useStorageOrigin = false;

            string containerName = null;
            string cdnEndpointName = null;
            string storageConnectionString = null;
            CloudTable fileTable = null;
            CloudTable guidTable = null;

            if (useStorageOrigin)
            {
                containerName = config[CDN_STORAGE_CONTAINER_NAME_KEY];
                cdnEndpointName = config[CDN_ENDPOINT_NAME_KEY];
                storageConnectionString = config[CDN_STORAGE_CONNECTION_STRING_KEY];
            }
            else
            {
                //Azure Table setup
                (fileTable, guidTable) = await SetupTables(config);
            }

            dynamic webhook = JsonConvert.DeserializeObject(requestBody);
            string trigger = webhook.trigger;
            string fileId = webhook.source.id;

            if (trigger == "FILE.UPLOADED")
            {
                string filename = webhook.source.name;
                string ownerId = webhook.source.owned_by.id;
                BoxClient box = GetBoxUserClient(config, ownerId);
                
                string cdnUrl = null;

                if (useStorageOrigin)
                {
                    var storageAccount = CloudStorageAccount.Parse(storageConnectionString);

                    var fileStream = await box.FilesManager.DownloadStreamAsync(fileId, timeout: TimeSpan.FromMinutes(10));
                    log.Info($"Retrieved stream from Box for fileId '{fileId}'");

                    var blobName = string.Format(STORAGE_CONTAINER_FILENAME_FORMAT_STRING, fileId, filename);
                    var cloudBlobClient = storageAccount.CreateCloudBlobClient();

                    var cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
                    var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
                    await cloudBlockBlob.UploadFromStreamAsync(fileStream);
                    log.Info($"Uploaded fileId '{fileId}' to storage container '{containerName}'");

                    cdnUrl = string.Format(CDN_URL_FORMAT_STRING, cdnEndpointName, containerName, blobName);
                }     

                //set the CDN URL of the file in box metadata          
                var templateName = config[METADATA_TEMPLATE_NAME_KEY];
                try
                {
                    var fetchedMD = await box.MetadataManager.GetFileMetadataAsync(fileId, METADATA_SCOPE, templateName);
                    log.Info($"Found CDN metadata for fileId '{fileId}'");

                    //invalidate this cached file in the CDN because a new version was uploaded to Box
                    PurgeFileFromCDN(fileId, log, config);
                }
                catch //exception means metadata hasn't been created yet so we know this is a new file
                {
                    if (!useStorageOrigin)
                    {
                        //we are using a custom origin so create a guid, format url, and store in table storage; only need to do this the first time, not for each new version
                        var cryptoProvider = new RNGCryptoServiceProvider();
                        var byteArray = new byte[32];
                        cryptoProvider.GetBytes(byteArray);
                        var guid = BitConverter.ToString(byteArray).Replace("-", string.Empty).ToLower();

                        cdnUrl = string.Format(CDN_URL_FORMAT_STRING_CUSTOM_ORIGIN, cdnEndpointName, guid);
                        StoreBoxCDNInfo(guid, fileId, ownerId, fileTable, guidTable, config, log);
                    }

                    var md = new Dictionary<string, object>() { { "url", cdnUrl } };
                    var createdMD = await box.MetadataManager.CreateFileMetadataAsync(fileId, md, METADATA_SCOPE, templateName);
                    log.Info($"Created CDN metadata for fileId '{fileId}'");
                }

                return (ActionResult)new OkObjectResult(null);
            }
            else if (trigger == "FILE.DELETED" || trigger == "FILE.TRASHED")
            {
                if (useStorageOrigin)
                {
                    var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
                    var cloudBlobClient = storageAccount.CreateCloudBlobClient();
                    var cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);

                    var virtualDir = cloudBlobContainer.GetDirectoryReference(fileId);
                    BlobContinuationToken bct = new BlobContinuationToken();
                    var blobs = await virtualDir.ListBlobsSegmentedAsync(bct);
                    foreach (var blob in blobs.Results)
                    {
                        await ((CloudBlob)blob).DeleteIfExistsAsync();
                        log.Info($"Deleted fileId '{fileId}' from storage container '{containerName}'");
                    }
                }
                else //using custom origin function
                {
                    //need to remove the table entries
                    var fileEntity = await RetrieveBoxCDNFileEntity(fileTable, fileId, config, log);
                    var guidEntity = await RetrieveBoxCDNGuidEntity(guidTable, fileEntity.Guid, config, log);

                    TableOperation.Delete(fileEntity);
                    TableOperation.Delete(guidEntity);
                }

                PurgeFileFromCDN(fileId, log, config);

                return (ActionResult)new OkObjectResult(null);
            }
            else
            {
                log.Warning($"Invalid Box webhook type: '{trigger}'");
                return (ActionResult)new BadRequestResult();
            }
        }

        static async void PurgeFileFromCDN(string fileId, TraceWriter log, IConfigurationRoot config)
        {
            var forcePurge = config[CDN_FORCE_PURGE_KEY];        
            if (forcePurge != null && forcePurge == "true")
            {
                //add message to the purge queue; will cause CDN purge function to execute
                var connectionString = config[CDN_STORAGE_QUEUE_CONNECTION_STRING_KEY];
                var storageAccount = CloudStorageAccount.Parse(connectionString);

                var queueClient = storageAccount.CreateCloudQueueClient();
                var queue = queueClient.GetQueueReference(CDN_PURGE_QUEUE_NAME);
                await queue.CreateIfNotExistsAsync();

                var message = new CloudQueueMessage(fileId);
                await queue.AddMessageAsync(message);
                var items = queue.ApproximateMessageCount;

                log.Info($"Added message to queue to purge fileId '{fileId}' from CDN");
            }     
        }

        static async void StoreBoxCDNInfo(string guid, string fileId, string ownerId, CloudTable fileTable, CloudTable guidTable, IConfigurationRoot config, TraceWriter log)
        {
            var boxCDNGuidEntity = new BoxCDNGuidEntity(guid)
            {
                FileId = fileId,
                OwnerId = ownerId
            };
            var insertOperation = TableOperation.InsertOrReplace(boxCDNGuidEntity);
            await guidTable.ExecuteAsync(insertOperation);
            log.Info($"Stored BoxCDNFileInfo in Table (guid={guid})");

            var boxCDNFileEntity = new BoxCDNFileEntity(fileId)
            {
                Guid = guid,
                OwnerId = ownerId
            };
            insertOperation = TableOperation.InsertOrReplace(boxCDNFileEntity);
            await fileTable.ExecuteAsync(insertOperation);
            log.Info($"Stored BoxCDNGuidInfo in Table (guid={guid})");
        }

        static async Task<BoxCDNGuidEntity> RetrieveBoxCDNGuidEntity(CloudTable table, string guid, IConfigurationRoot config, TraceWriter log)
        {
            var retrieveOp = TableOperation.Retrieve<BoxCDNGuidEntity>(guid, string.Empty);
            var retrievedRes = await table.ExecuteAsync(retrieveOp);
            var boxCDNFileInfoEntity = (BoxCDNGuidEntity)retrievedRes.Result;
            log.Info($"Retrieved BoxCDNFileInfo from Table (guid={guid})");
            return boxCDNFileInfoEntity;
        }

        static async Task<BoxCDNFileEntity> RetrieveBoxCDNFileEntity(CloudTable table, string fileId, IConfigurationRoot config, TraceWriter log)
        {
            var retrieveOp = TableOperation.Retrieve<BoxCDNFileEntity>(fileId, string.Empty);
            var retrievedRes = await table.ExecuteAsync(retrieveOp);
            var boxCDNGuidEntity = (BoxCDNFileEntity)retrievedRes.Result;
            log.Info($"Retrieved BoxCDNGuidInfo from Table (fileId={fileId})");
            return boxCDNGuidEntity;
        }

        static async Task<(CloudTable, CloudTable)> SetupTables(IConfigurationRoot config)
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
