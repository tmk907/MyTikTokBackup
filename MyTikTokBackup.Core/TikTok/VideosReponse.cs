using System.Collections.Generic;
using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class VideosReponse
    {

        [JsonProperty("statusCode")]
        public long StatusCode { get; set; }

        [JsonProperty("itemList")]
        public List<ItemInfo> ItemList { get; set; }
        
        [JsonProperty("cursor")]
        public string Cursor { get; set; }

        [JsonProperty("extra")]
        public Extra Extra { get; set; }

        [JsonProperty("hasMore")]
        public bool HasMore { get; set; }

        [JsonProperty("log_pb")]
        public LogPb LogPb { get; set; }

        [JsonProperty("status_msg")]
        public string StatusMsg { get; set; }
    }
}
