using System.Collections.Generic;
using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class StickersOnItem
    {
        [JsonProperty("stickerType")]
        public long StickerType { get; set; }

        [JsonProperty("stickerText")]
        public List<string> StickerText { get; set; }
    }
}
