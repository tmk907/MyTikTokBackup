using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class BitrateInfo
    {
        [JsonProperty("Bitrate")]
        public int Bitrate { get; set; }

        [JsonProperty("CodecType")]
        public string CodecType { get; set; }

        [JsonProperty("GearName")]
        public string GearName { get; set; }

        [JsonProperty("PlayAddr")]
        public PlayAddr PlayAddr { get; set; }

        [JsonProperty("QualityType")]
        public int QualityType { get; set; }
    }
}
