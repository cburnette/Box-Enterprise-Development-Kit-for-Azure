using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using static Box.EnterpriseDevelopmentKit.Azure.Shared.Config;

namespace Box.EnterpriseDevelopmentKit.Azure
{
    public static class WebhookEndpointTemplate
    {
        public const string EXPECTED_TRIGGER = "FILE.PREVIEWED";

        [FunctionName("BoxWebhookEndpointTemplate")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req, TraceWriter log, ExecutionContext context)
        {
            var config = GetConfiguration(context);

            string requestBody = new StreamReader(req.Body).ReadToEnd();

            if (!ValidateWebhookSignatures(req, config, requestBody))
            {
                log.Error("Signature check for Box webhook failed");
                return (ActionResult)new UnauthorizedResult();
            }

            dynamic webhook = JsonConvert.DeserializeObject(requestBody);
            string trigger = webhook.trigger;
            string sourceId = webhook.source.id;

            if (trigger == EXPECTED_TRIGGER)
            {
                log.Info($"Box webhook function processed a request (trigger=({trigger}), sourceId=({sourceId})");

                //do something interesting here

                return (ActionResult)new OkObjectResult(null);
            }
            else
            {
                log.Error($"Box webhook function received unexpected trigger '{trigger}' for source Id '{sourceId}'");
                return (ActionResult)new BadRequestObjectResult(null);
            }
        }
    }
}
