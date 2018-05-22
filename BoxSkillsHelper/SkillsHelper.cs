using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Box.EnterpriseDevelopmentKit.Azure
{
    public class SkillsHelper
    {
        public static Dictionary<string, object> CreateCardMetadata(params JObject[] cards)
        {
            var cardMetadata = new JObject(
                new JProperty("cards", cards)
            );

            return cardMetadata.ToObject<Dictionary<string, object>>();
        }

        public static JObject CreateTranscriptCard(string title, string serviceName, string skillInvocationId, IList<SkillCardEntry> entries, double? duration = null)
        {
            return CreateSkillCard("transcript", title, serviceName, skillInvocationId, entries, duration);
        }

        public static JObject CreateKeywordCard(string title, string serviceName, string skillInvocationId, IList<SkillCardEntry> entries, double? duration = null)
        {
            return CreateSkillCard("keyword", title, serviceName, skillInvocationId, entries, duration);
        }

        public static JObject CreateTimelineCard(string title, string serviceName, string skillInvocationId, IList<SkillCardEntry> entries, double? duration = null)
        {
            return CreateSkillCard("timeline", title, serviceName, skillInvocationId, entries, duration);
        } 

        public class TranscriptCardEntry : SkillCardEntry
        {
            public TranscriptCardEntry(string text, IList<EntryAppearance> appearances)
            {
                _type = "text";
                _text = text;
                _appearances = appearances;
            }
        }

        public class KeywordCardEntry : SkillCardEntry
        {
            public KeywordCardEntry(string text, IList<EntryAppearance> appearances)
            {
                _type = "text";
                _text = text;
                _appearances = appearances;
            }
        }

        public class TimelineCardEntry : SkillCardEntry
        {
            public TimelineCardEntry(string text, string imageUrl, IList<EntryAppearance> appearances)
            {
                _type = "image";
                _text = text;
                _imageUrl = imageUrl;
                _appearances = appearances;
            }
        }

        public class EntryAppearance
        {
            public double Start { get; }
            public double End { get; }

            public EntryAppearance(double start, double end)
            {
                Start = start;
                End = end;
            }
        }

        public abstract class SkillCardEntry
        {
            protected string _type;
            public string Type { get { return _type; } }

            protected string _text;
            public string Text { get { return _text; } }

            protected string _imageUrl;
            public string ImageUrl { get { return _imageUrl; } }

            protected IList<EntryAppearance> _appearances;
            public IList<EntryAppearance> Appearances { get { return _appearances; } }

            public JObject EntryJson { get
                {
                    var appearancesJson = new JArray();
                    foreach (var appearance in Appearances)
                    {
                        var appearanceJson = new JObject(
                            new JProperty("start", appearance.Start),
                            new JProperty("end", appearance.End));

                        appearancesJson.Add(appearanceJson);
                    }

                    var newEntry = new JObject(
                        new JProperty("type", _type),
                        new JProperty("text", _text),
                        new JProperty("appears", appearancesJson));

                    if(!string.IsNullOrEmpty(_imageUrl))
                    {
                        newEntry.Add("image_url", _imageUrl);
                    }

                    return newEntry;
                }
            }
        }

        private static JObject CreateSkillCard(string skillCardType, string title, string serviceName, string skillInvocationId, IList<SkillCardEntry> entries, double? duration = null)
        {
            var entriesJson = new JArray();
            foreach (var entry in entries)
            {
                entriesJson.Add(entry.EntryJson);
            }

            JObject transcriptCard = new JObject(
                            new JProperty("type", "skill_card"),
                            new JProperty("skill_card_type", skillCardType),
                            new JProperty("title", title),
                            new JProperty("skill",
                                new JObject(
                                    new JProperty("type", "service"),
                                    new JProperty("id", serviceName))),
                            new JProperty("invocation",
                                new JObject(
                                    new JProperty("type", "skill_invocation"),
                                    new JProperty("id", skillInvocationId))),
                            new JProperty("entries", entriesJson));

            if (duration.HasValue)
            {
                transcriptCard.Add("duration", duration.Value);
            }

            return transcriptCard;
        }
    }
}
