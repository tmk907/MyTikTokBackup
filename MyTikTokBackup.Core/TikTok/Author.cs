using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class Author
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("uniqueId")]
        public string UniqueId { get; set; }

        [JsonProperty("nickname")]
        public string Nickname { get; set; }

        [JsonProperty("avatarThumb")]
        public string AvatarThumb { get; set; }

        [JsonProperty("avatarMedium")]
        public string AvatarMedium { get; set; }

        [JsonProperty("avatarLarger")]
        public string AvatarLarger { get; set; }

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
