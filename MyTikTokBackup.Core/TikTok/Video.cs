using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class Video
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("height")]
        public long Height { get; set; }

        [JsonProperty("width")]
        public long Width { get; set; }

        [JsonProperty("duration")]
        public long Duration { get; set; }

        [JsonProperty("ratio")]
        public string Ratio { get; set; }

        [JsonProperty("cover")]
        public string Cover { get; set; }

        [JsonProperty("originCover")]
        public string OriginCover { get; set; }

        [JsonProperty("dynamicCover")]
        public string DynamicCover { get; set; }

        [JsonProperty("playAddr")]
        public string PlayAddr { get; set; }

        [JsonProperty("downloadAddr")]
        public string DownloadAddr { get; set; }

        [JsonProperty("shareCover")]
        public List<string> ShareCover { get; set; }

        [JsonProperty("reflowCover")]
        public string ReflowCover { get; set; }
    }
}
