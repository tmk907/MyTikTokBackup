using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class EffectSticker
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ID")]
        public string Id { get; set; }
    }
}
