
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Box.V2.Managers;
using static Box.EnterpriseDevelopmentKit.Azure.Shared.Config;

namespace Box.EnterpriseDevelopmentKit.Azure
{
    public static class WebhookEndpointTemplate
    {
        public const string BOX_CONFIG_KEY = "BoxConfig";
        public const string BOX_DELIVERY_TIMESTAMP_HEADER = "BOX-DELIVERY-TIMESTAMP";
        public const string BOX_SIGNATURE_PRIMARY_HEADER = "BOX-SIGNATURE-PRIMARY";
        public const string BOX_SIGNATURE_SECONDARY_HEADER = "BOX-SIGNATURE-SECONDARY";
        public const string BOX_WEBHOOK_PRIMARY_KEY_KEY = "BoxWebhookPrimaryKey";
        public const string BOX_WEBHOOK_SECONDARY_KEY_KEY = "BoxWebhookSecondaryKey";
        public const string EXPECTED_TRIGGER = "FILE.PREVIEWED";

        [FunctionName("BoxWebhookEndpointTemplate")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req, TraceWriter log, ExecutionContext context)
        {
            var config = GetConfiguration(context);

            string requestBody = new StreamReader(req.Body).ReadToEnd();

            if (!ValidateWebhookSignatures(req, config, requestBody))
            {
                log.Error("Signature check for Box webhook failed");
                return (ActionResult)new BadRequestResult();
            }

            dynamic webhook = JsonConvert.DeserializeObject(requestBody);
            string trigger = webhook.trigger;
            string sourceId = webhook.source.id;

            if (trigger == EXPECTED_TRIGGER)
            {
                log.Info($"Box webhook function processed a request for the trigger '{trigger}' for source Id '{sourceId}'");

                //do something interesting here

                return (ActionResult)new OkObjectResult(null);
            }
            else
            {
                log.Error($"Box webhook function received unexpected trigger '{trigger}' for source Id '{sourceId}'");
                return (ActionResult)new BadRequestObjectResult(null);
            }
        }

        static bool ValidateWebhookSignatures(HttpRequest req, IConfigurationRoot config, string requestBody)
        {
            var deliveryTimestamp = req.Headers[BOX_DELIVERY_TIMESTAMP_HEADER];
            var signaturePrimary = req.Headers[BOX_SIGNATURE_PRIMARY_HEADER];
            var signatureSecondary = req.Headers[BOX_SIGNATURE_SECONDARY_HEADER];
            var primaryKey = config[BOX_WEBHOOK_PRIMARY_KEY_KEY];
            var secondaryKey = config[BOX_WEBHOOK_SECONDARY_KEY_KEY];
            return BoxWebhooksManager.VerifyWebhook(deliveryTimestamp, signaturePrimary, signatureSecondary, requestBody, primaryKey, secondaryKey);
        }
    }
}
