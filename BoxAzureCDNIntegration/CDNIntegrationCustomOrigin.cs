using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using static Box.EnterpriseDevelopmentKit.Azure.Shared.Config;
using static Box.EnterpriseDevelopmentKit.Azure.BoxAzureCDNIntegration.TableStorage;
using static Box.EnterpriseDevelopmentKit.Azure.BoxAzureCDNIntegration.Config;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Box.EnterpriseDevelopmentKit.Azure
{
    public static class CDNIntegrationCustomOrigin
    {
        [FunctionName("BoxAzureCDNIntegrationCustomOrigin")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "BoxAzureCDNIntegrationCustomOrigin/{guid}")]HttpRequest req, string guid, TraceWriter log, ExecutionContext context)
        {
            var config = GetConfiguration(context);

            if (UseStorageOrigin(config))
            {
                log.Error($"Received request at custom origin but currently configured to use storage origin");
                return new UnauthorizedResult();
            }

            CloudTable fileTable = null;
            CloudTable guidTable = null;
            string fileId;
            string ownerId;

            try
            {
                //Azure Table Storage setup
                (fileTable, guidTable) = await SetupTables(config);

                var boxCDNGuidEntity = await RetrieveBoxCDNGuidEntity(guidTable, guid, config, log);
                fileId = boxCDNGuidEntity.FileId;
                ownerId = boxCDNGuidEntity.OwnerId;
            }
            catch
            {
                log.Warning($"Requested guid not found in Table (guid={guid})");
                return new BadRequestResult();
            }

            try
            {
                var box = GetBoxUserClient(config, ownerId);
                var downloadUrl = await box.FilesManager.GetDownloadUriAsync(fileId);

                log.Info($"Redirecting to direct link in Box (fileId={fileId})");

                var redirect = new RedirectResult(downloadUrl.ToString());
                return redirect;
            }
            catch
            {
                log.Error($"Error retrieving download URI for file (fileId={fileId})");
                return new StatusCodeResult(500);
            }
        }
    }
}
