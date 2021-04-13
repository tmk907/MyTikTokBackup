using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class MyFollowing
    {
        [JsonProperty("statusCode")]
        public long StatusCode { get; set; }

        [JsonProperty("userList")]
        public List<Following> UserList { get; set; }

        [JsonProperty("maxCursor")]
        public long MaxCursor { get; set; }

        [JsonProperty("minCursor")]
        public long MinCursor { get; set; }
    }

    public class Following
    {
        [JsonProperty("user")]
        public FollowingUser User { get; set; }

        [JsonProperty("stats")]
        public FollowingStats Stats { get; set; }
    }

    public class FollowingStats
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

    public class FollowingUser
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("uniqueId")]
        public string UniqueId { get; set; }

        [JsonProperty("nickname")]
        public string Nickname { get; set; }

        [JsonProperty("avatarThumb")]
        public Uri AvatarThumb { get; set; }

        [JsonProperty("avatarMedium")]
        public Uri AvatarMedium { get; set; }

        [JsonProperty("avatarLarger")]
        public Uri AvatarLarger { get; set; }

        [JsonProperty("signature")]
        public string Signature { get; set; }

        [JsonProperty("verified")]
        public bool Verified { get; set; }

        [JsonProperty("secUid")]
        public string SecUid { get; set; }

        [JsonProperty("secret")]
        public bool Secret { get; set; }

        [JsonProperty("ftc")]
        public bool Ftc { get; set; }

        [JsonProperty("relation")]
        public long Relation { get; set; }

        [JsonProperty("openFavorite")]
        public bool OpenFavorite { get; set; }

        [JsonProperty("commentSetting")]
        public long CommentSetting { get; set; }

        [JsonProperty("duetSetting")]
        public long DuetSetting { get; set; }

        [JsonProperty("stitchSetting")]
        public long StitchSetting { get; set; }

        [JsonProperty("privateAccount")]
        public bool PrivateAccount { get; set; }
    }
}
