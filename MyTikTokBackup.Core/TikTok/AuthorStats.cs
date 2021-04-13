using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class AuthorStats
    {
        [JsonProperty("followingCount")]
        public long FollowingCount { get; set; }

        [JsonProperty("followerCount")]
        public long FollowerCount { get; set; }

        [JsonProperty("heartCount")]
        public long HeartCount { get; set; }

        [JsonProperty("videoCount")]
        public long VideoCount { get; set; }

        [JsonProperty("diggCount")]
        public long DiggCount { get; set; }

        [JsonProperty("heart")]
        public long Heart { get; set; }
    }
}
