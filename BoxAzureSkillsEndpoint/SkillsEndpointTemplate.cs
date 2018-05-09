
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace Box.EnterpriseDevelopmentKit.Azure
{
    public static class SkillsEndpointTemplate
    {
        [FunctionName("BoxAzureSkillsTemplate")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req, TraceWriter log, ExecutionContext context)
        {
            string requestBody = new StreamReader(req.Body).ReadToEnd();

            //if (!ValidateWebhookSignatures(req, config, requestBody))
            //{
            //    log.Error("Signature check for Box webhook failed");
            //    return (ActionResult)new BadRequestResult();
            //}

            dynamic webhook = JsonConvert.DeserializeObject(requestBody);

            return (ActionResult)new OkObjectResult(null);
        }
    }
}
