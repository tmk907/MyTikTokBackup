﻿using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class TextExtra
    {
        [JsonProperty("awemeId")]
        public string AwemeId { get; set; }

        [JsonProperty("end")]
        public long End { get; set; }

        [JsonProperty("hashtagName")]
        public string HashtagName { get; set; }

        [JsonProperty("isCommerce")]
        public bool IsCommerce { get; set; }

        [JsonProperty("start")]
        public long Start { get; set; }

        [JsonProperty("subType")]
        public long SubType { get; set; }

        [JsonProperty("type")]
        public long Type { get; set; }

        [JsonProperty("secUid")]
        public string SecUid { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("userUniqueId")]
        public string UserUniqueId { get; set; }
    }
}
