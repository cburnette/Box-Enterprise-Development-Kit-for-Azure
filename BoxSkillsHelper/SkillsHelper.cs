using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Box.EnterpriseDevelopmentKit.Azure
{
    public class SkillsHelper
    {
        public static JObject CreateTranscriptCard(string title, string serviceName, string skillInvocationId, IList<TranscriptCardEntry> entries, double? duration=null)
        {
            var entriesJson = new JArray();
            foreach (var entry in entries)
            {
                var appearances = new JArray();
                foreach(var appearance in entry.Appearances)
                {
                    var appearanceJson = new JObject(
                        new JProperty("start", appearance.Start),
                        new JProperty("end", appearance.End));

                    appearances.Add(appearanceJson);
                }

                var newEntry = new JObject(
                    new JProperty("text", entry.Text),
                    new JProperty("appears", appearances));

                entriesJson.Add(newEntry);
            }

            JObject transcriptCard = new JObject(
                            new JProperty("type", "skill_card"),
                            new JProperty("skill_card_type", "transcript"),
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

        public static JObject CreateKeywordCard(string title, string serviceName, string skillInvocationId, IList<KeywordCardEntry> entries, double? duration = null)
        {
            var entriesJson = new JArray();
            foreach (var entry in entries)
            {
                var appearances = new JArray();
                foreach (var appearance in entry.Appearances)
                {
                    var appearanceJson = new JObject(
                        new JProperty("start", appearance.Start),
                        new JProperty("end", appearance.End));

                    appearances.Add(appearanceJson);
                }

                var newEntry = new JObject(
                    new JProperty("text", entry.Text),
                    new JProperty("appears", appearances));

                entriesJson.Add(newEntry);
            }

            JObject keywordCard = new JObject(
                            new JProperty("type", "skill_card"),
                            new JProperty("skill_card_type", "keyword"),
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
                keywordCard.Add("duration", duration.Value);
            }

            return keywordCard;
        }

        public static JObject CreateTimelineCard(string title, string serviceName, string skillInvocationId, IList<TimelineCardEntry> entries, double? duration = null)
        {
            var entriesJson = new JArray();
            foreach (var entry in entries)
            {
                var appearances = new JArray();
                foreach (var appearance in entry.Appearances)
                {
                    var appearanceJson = new JObject(
                        new JProperty("start", appearance.Start),
                        new JProperty("end", appearance.End));

                    appearances.Add(appearanceJson);
                }

                var newEntry = new JObject(
                    new JProperty("type", "image"),
                    new JProperty("text", entry.Text),
                    new JProperty("image_url", entry.ImageUrl),
                    new JProperty("appears", appearances));

                entriesJson.Add(newEntry);
            }

            JObject timelineCard = new JObject(
                            new JProperty("type", "skill_card"),
                            new JProperty("skill_card_type", "timeline"),
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
                timelineCard.Add("duration", duration.Value);
            }

            return timelineCard;
        }

        public struct TranscriptCardEntry
        {
            public string Text { get; }
            public IList<SkillsCardEntryAppearance> Appearances { get; }

            public TranscriptCardEntry(string text, IList<SkillsCardEntryAppearance> appearances)
            {
                Text = text;
                Appearances = appearances;
            }
        }

        public struct KeywordCardEntry
        {
            public string Text { get; }
            public IList<SkillsCardEntryAppearance> Appearances { get; }

            public KeywordCardEntry(string text, IList<SkillsCardEntryAppearance> appearances)
            {
                Text = text;
                Appearances = appearances;
            }
        }

        public struct TimelineCardEntry
        {
            public string Text { get; }
            public string ImageUrl { get; }
            public IList<SkillsCardEntryAppearance> Appearances { get; }

            public TimelineCardEntry(string text, string imageUrl, IList<SkillsCardEntryAppearance> appearances)
            {
                Text = text;
                ImageUrl = imageUrl;
                Appearances = appearances;
            }
        }

        public struct SkillsCardEntryAppearance
        {
            public double Start { get; }
            public double End { get; }

            public SkillsCardEntryAppearance(double start, double end)
            {
                Start = start;
                End = end;
            }
        }
    }
}
