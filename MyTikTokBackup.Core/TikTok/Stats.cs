using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class Stats
    {
        [JsonProperty("collectCount")]
        public long CollectCount { get; set; }

        [JsonProperty("commentCount")]
        public long CommentCount { get; set; }

        [JsonProperty("diggCount")]
        public long DiggCount { get; set; }

        [JsonProperty("playCount")]
        public long PlayCount { get; set; }

        [JsonProperty("shareCount")]
        public long ShareCount { get; set; }
    }

    public class StatsV2
    {
        [JsonProperty("collectCount")]
        public string CollectCount { get; set; }

        [JsonProperty("commentCount")]
        public string CommentCount { get; set; }

        [JsonProperty("diggCount")]
        public string DiggCount { get; set; }

        [JsonProperty("playCount")]
        public string PlayCount { get; set; }

        [JsonProperty("repostCount")]
        public string RepostCount { get; set; }

        [JsonProperty("shareCount")]
        public string ShareCount { get; set; }
    }
}
