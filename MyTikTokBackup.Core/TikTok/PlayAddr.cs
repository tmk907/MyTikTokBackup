using Newtonsoft.Json;
using System.Collections.Generic;

namespace MyTikTokBackup.Core.TikTok
{
    public class PlayAddr
    {
        [JsonProperty("DataSize")]
        public int DataSize { get; set; }

        [JsonProperty("FileCs")]
        public string FileCs { get; set; }

        [JsonProperty("FileHash")]
        public string FileHash { get; set; }

        [JsonProperty("Uri")]
        public string Uri { get; set; }

        [JsonProperty("UrlKey")]
        public string UrlKey { get; set; }

        [JsonProperty("UrlList")]
        public List<string> UrlList { get; set; }
    }
}
