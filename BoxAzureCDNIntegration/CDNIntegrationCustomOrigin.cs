
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using static Box.EnterpriseDevelopmentKit.Azure.Shared.Config;
using System.Threading.Tasks;
using Box.V2;

namespace Box.EnterpriseDevelopmentKit.Azure
{
    public static class CDNIntegrationCustomOrigin
    {
        [FunctionName("BoxAzureCDNIntegrationCustomOrigin")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "BoxAzureCDNIntegrationCustomOrigin/{guid}")]HttpRequest req, string guid, TraceWriter log, ExecutionContext context)
        {
            var config = GetConfiguration(context);

            var blah = guid;

            var fileId = "294115226716";
            var userId = "3419749388";

            BoxClient box = GetBoxUserClient(config, userId);

            var downloadUrl = await box.FilesManager.GetDownloadUriAsync(fileId);

            RedirectResult redirect = new RedirectResult(downloadUrl.ToString());

            return redirect;
        }
    }
}
