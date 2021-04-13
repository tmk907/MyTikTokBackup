using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class Stats
    {
        [JsonProperty("diggCount")]
        public long DiggCount { get; set; }

        [JsonProperty("shareCount")]
        public long ShareCount { get; set; }

        [JsonProperty("commentCount")]
        public long CommentCount { get; set; }

        [JsonProperty("playCount")]
        public long PlayCount { get; set; }
    }
}
