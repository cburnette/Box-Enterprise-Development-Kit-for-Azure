using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using static Box.EnterpriseDevelopmentKit.Azure.Shared.Config;

namespace Box.EnterpriseDevelopmentKit.Azure
{
    public static class CDNCloudServiceIntegration
    {
        //Box CLI command to create webhook:
        //box webhooks create 49678690931 folder FILE.UPLOADED,FILE.DELETED,FILE.TRASHED https://cburnette.ngrok.io/api/BoxAzureCDNCloudServiceIntegration --as-user 3419749388

        [FunctionName("BoxAzureCDNCloudServiceIntegration")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "BoxAzureCDNCloudServiceIntegration/{guid}")]HttpRequest req, string guid, TraceWriter log, ExecutionContext context)
        {
            var config = GetConfiguration(context);

            var blah = guid;

            return (ActionResult)new OkObjectResult(blah);
        }
    }
}
