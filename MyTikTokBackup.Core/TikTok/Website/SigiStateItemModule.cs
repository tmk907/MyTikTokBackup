using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MyTikTokBackup.Core.TikTok.Website
{
    public class SigiStateItemModule
    {
        [JsonPropertyName("ItemModule")]
        public Dictionary<string, PostedVideo> ItemModule { get; set; }
    }
}
