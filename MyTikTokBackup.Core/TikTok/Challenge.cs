﻿using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class Challenge
    {
        [JsonProperty("coverLarger")]
        public string CoverLarger { get; set; }

        [JsonProperty("coverMedium")]
        public string CoverMedium { get; set; }

        [JsonProperty("coverThumb")]
        public string CoverThumb { get; set; }

        [JsonProperty("desc")]
        public string Desc { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("profileLarger")]
        public string ProfileLarger { get; set; }

        [JsonProperty("profileMedium")]
        public string ProfileMedium { get; set; }

        [JsonProperty("profileThumb")]
        public string ProfileThumb { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("isCommerce")]
        public bool? IsCommerce { get; set; }
    }
}
