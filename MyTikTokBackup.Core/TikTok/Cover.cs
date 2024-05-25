using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class Cover
    {
        [JsonProperty("imageHeight")]
        public int ImageHeight { get; set; }

        [JsonProperty("imageURL")]
        public ImageURL ImageURL { get; set; }

        [JsonProperty("imageWidth")]
        public int ImageWidth { get; set; }
    }
}
