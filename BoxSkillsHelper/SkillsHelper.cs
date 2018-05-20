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

        public struct TranscriptCardEntry
        {
            public string Text { get; }
            public IList<TranscriptCardAppearance> Appearances { get; }

            public TranscriptCardEntry(string text, IList<TranscriptCardAppearance> appearances)
            {
                Text = text;
                Appearances = appearances;
            }
        }

        public struct TranscriptCardAppearance
        {
            public double Start { get; }
            public double End { get; }

            public TranscriptCardAppearance(double start, double end)
            {
                Start = start;
                End = end;
            }
        }
    }
}
