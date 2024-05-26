using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class SubtitleInfo
    {
        [JsonProperty("Format")]
        public string Format { get; set; }

        [JsonProperty("LanguageCodeName")]
        public string LanguageCodeName { get; set; }

        [JsonProperty("LanguageID")]
        public string LanguageID { get; set; }

        [JsonProperty("Size")]
        public long Size { get; set; }

        [JsonProperty("Source")]
        public string Source { get; set; }

        [JsonProperty("Url")]
        public string Url { get; set; }

        [JsonProperty("UrlExpire")]
        public long UrlExpire { get; set; }

        [JsonProperty("Version")]
        public string Version { get; set; }
    }
}
