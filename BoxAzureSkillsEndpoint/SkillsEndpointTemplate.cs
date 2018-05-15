
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using static Box.EnterpriseDevelopmentKit.Azure.Shared.Config;
using Box.V2.Auth;
using Box.V2.Config;
using Box.V2;
using System;
using Box.V2.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Box.EnterpriseDevelopmentKit.Azure
{
    //sample card format: https://cloud.app.box.com/s/b3y0z3y2w526d60pnxstvgie79yuby7v/file/290240418780

    public static class SkillsEndpointTemplate
    {
        public const string BOX_SKILLS_API_KEY_KEY = "BoxSkillsApiKey";
        public const string BOX_CONFIG_KEY = "BoxConfig";
        public const string BOX_FILE_CONTENT_URL_FORMAT_STRING = @"https://api.box.com/2.0/files/{0}/content?access_token={1}";
        public const string BOX_SKILL_TYPE = "skill_invocation";

        [FunctionName("BoxAzureSkillsTemplate")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req, TraceWriter log, ExecutionContext context)
        {
            var config = GetConfiguration(context);

            string requestBody = new StreamReader(req.Body).ReadToEnd();

            //if (!ValidateWebhookSignatures(req, config, requestBody))
            //{
            //    log.Error("Signature check for Box Skills webhook failed");
            //    return (ActionResult)new BadRequestResult();
            //}

            dynamic webhook = JsonConvert.DeserializeObject(requestBody);
            log.Info($"Received webhook");

            var formattedJson = JsonConvert.SerializeObject(webhook, Formatting.Indented);
            log.Info(formattedJson);

            string type = webhook.type;
            if (string.IsNullOrEmpty(type) || type != BOX_SKILL_TYPE)
            {
                return (ActionResult)new BadRequestObjectResult("Not a valid Box Skill payload");
            }

            string writeToken = webhook.token.write.access_token;
            string readToken = webhook.token.read.access_token;
            string sourceId = webhook.source.id;
            string sourceName = webhook.source.name;
            string downloadUrl = string.Format(BOX_FILE_CONTENT_URL_FORMAT_STRING, sourceId, readToken);

            var boxClient = GetBoxClientWithApiKeyAndToken(config[BOX_SKILLS_API_KEY_KEY], writeToken);

            var entries = new JArray
            {
                new JObject(
                    new JProperty("text", "Hello World!"),
                    new JProperty("appears",
                        new JArray() {
                            new JObject(
                                new JProperty("start", 9.95),
                                new JProperty("end", 14.8))
                        }
                    )
                ),
                new JObject(
                    new JProperty("text", "Goodbye World!"),
                    new JProperty("appears",
                        new JArray() {
                            new JObject(
                                new JProperty("start", 14.8),
                                new JProperty("end", 17.5))
                        }
                    )
                )
            };

            JObject transcriptCard = new JObject(
                            new JProperty("type", "skill_card"),
                            new JProperty("skill_card_type", "transcript"),
                            new JProperty("title", "transcript"),
                            new JProperty("skill",
                                new JObject(
                                    new JProperty("type", "service"),
                                    new JProperty("id", "chad-funky-ml"))),
                            new JProperty("invocation",
                                new JObject(
                                    new JProperty("type", "skill_invocation"),
                                    new JProperty("id", "123456789"))),
                            new JProperty("entries", entries));

            JArray cards = new JArray{transcriptCard};

            JObject md = new JObject(
                new JProperty("cards", cards)
            );

            var createdMD = await boxClient.MetadataManager.CreateFileMetadataAsync(sourceId, md.ToObject<Dictionary<string, object>>(), "global", "boxSkillsCards");

            return (ActionResult)new OkObjectResult(null);
        }
    }
}
