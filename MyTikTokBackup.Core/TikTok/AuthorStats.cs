using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace MyTikTokBackup.Core.TikTok
{
    public class AuthorStats
    {
        [JsonProperty("diggCount")]
        public long DiggCount { get; set; }

        [JsonProperty("followerCount")]
        public long FollowerCount { get; set; }

        [JsonProperty("followingCount")]
        public long FollowingCount { get; set; }

        [JsonProperty("friendCount")]
        public long FriendCount { get; set; }

        [JsonProperty("heart")]
        public long Heart { get; set; }

        [JsonProperty("heartCount")]
        public long HeartCount { get; set; }

        [JsonProperty("videoCount")]
        public long VideoCount { get; set; }
    }
}
