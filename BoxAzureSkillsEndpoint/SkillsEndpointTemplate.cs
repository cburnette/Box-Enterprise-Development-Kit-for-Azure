
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using static Box.EnterpriseDevelopmentKit.Azure.Shared.Config;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

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

            var transcriptCard = CreateTranscriptCard();
            var keywordCard = CreateKeywordCard();
            var timelineCard = CreateTimelineCard();

            var cards = new JArray { transcriptCard, keywordCard, timelineCard };

            var md = new JObject(
                new JProperty("cards", cards)
            );

            var createdMD = await boxClient.MetadataManager.CreateFileMetadataAsync(sourceId, md.ToObject<Dictionary<string, object>>(), "global", "boxSkillsCards");

            return (ActionResult)new OkObjectResult(null);
        }

        private static JObject CreateTranscriptCard()
        {
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
                            new JProperty("title", "Transcript"),
                            new JProperty("skill",
                                new JObject(
                                    new JProperty("type", "service"),
                                    new JProperty("id", "chad-funky-ml"))),
                            new JProperty("invocation",
                                new JObject(
                                    new JProperty("type", "skill_invocation"),
                                    new JProperty("id", "123456789"))),
                            new JProperty("duration", 28),
                            new JProperty("entries", entries));
            return transcriptCard;
        }

        private static JObject CreateKeywordCard()
        {
            var entries = new JArray
            {
                new JObject(
                    new JProperty("type", "text"),
                    new JProperty("text", "work platform"),
                    new JProperty("appears",
                        new JArray() {
                            new JObject(
                                new JProperty("start", 9.95),
                                new JProperty("end", 0)),
                            new JObject(
                                new JProperty("start", 14.8),
                                new JProperty("end", 0))
                        }
                    )
                )
            };

            JObject keywordCard = new JObject(
                            new JProperty("type", "skill_card"),
                            new JProperty("skill_card_type", "keyword"),
                            new JProperty("title", "Topics"),
                            new JProperty("skill",
                                new JObject(
                                    new JProperty("type", "service"),
                                    new JProperty("id", "chad-funky-ml"))),
                            new JProperty("invocation",
                                new JObject(
                                    new JProperty("type", "skill_invocation"),
                                    new JProperty("id", "123456789"))),
                            new JProperty("duration", 28),
                            new JProperty("entries", entries));
            return keywordCard;
        }

        private static JObject CreateTimelineCard()
        {
            var entries = new JArray
            {
                new JObject(
                    new JProperty("type", "image"),
                    new JProperty("text", "Unknown #1"),
                    new JProperty("image_url", "data:image/jpeg;base64,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"),
                    new JProperty("appears",
                        new JArray() {
                            new JObject(
                                new JProperty("start", 9.95),
                                new JProperty("end", 14.8)),
                            new JObject(
                                new JProperty("start", 14.8),
                                new JProperty("end", 17.5))
                        }
                    )
                )
            };

            JObject timelineCard = new JObject(
                            new JProperty("type", "skill_card"),
                            new JProperty("skill_card_type", "timeline"),
                            new JProperty("title", "Faces"),
                            new JProperty("skill",
                                new JObject(
                                    new JProperty("type", "service"),
                                    new JProperty("id", "chad-funky-ml"))),
                            new JProperty("invocation",
                                new JObject(
                                    new JProperty("type", "skill_invocation"),
                                    new JProperty("id", "123456789"))),
                            new JProperty("duration", 28),
                            new JProperty("entries", entries));
            return timelineCard;
        }
    }
}
