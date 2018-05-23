
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using static Box.EnterpriseDevelopmentKit.Azure.Config;
using static Box.EnterpriseDevelopmentKit.Azure.Shared.Config;
using System.Threading.Tasks;
using Box.V2;

namespace BoxAzureCDNIntegration
{
    public static class CDNIntegrationOrigin
    {
        [FunctionName("BoxAzureCDNIntegrationOrigin")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "BoxAzureCDNIntegrationOrigin/{guid}")]HttpRequest req, string guid, TraceWriter log, ExecutionContext context)
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
