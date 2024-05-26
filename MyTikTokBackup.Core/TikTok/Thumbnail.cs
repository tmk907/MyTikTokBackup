using Newtonsoft.Json;
using System.Collections.Generic;

namespace MyTikTokBackup.Core.TikTok
{
    public class Thumbnail
    {
        [JsonProperty("height")]
        public long Height { get; set; }

        [JsonProperty("urlList")]
        public List<string> UrlList { get; set; }

        [JsonProperty("width")]
        public long Width { get; set; }
    }
}
