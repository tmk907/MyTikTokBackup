using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class Anchor
    {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("extraInfo")]
        public ExtraInfo ExtraInfo { get; set; }

        [JsonProperty("icon")]
        public Icon Icon { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("keyword")]
        public string Keyword { get; set; }

        [JsonProperty("logExtra")]
        public string LogExtra { get; set; }

        [JsonProperty("schema")]
        public string Schema { get; set; }

        [JsonProperty("thumbnail")]
        public Thumbnail Thumbnail { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }
    }
}
