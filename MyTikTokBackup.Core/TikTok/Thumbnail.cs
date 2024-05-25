using Newtonsoft.Json;
using System.Collections.Generic;

namespace MyTikTokBackup.Core.TikTok
{
    public class Thumbnail
    {
        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("urlList")]
        public List<string> UrlList { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }
    }
}
