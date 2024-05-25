using Newtonsoft.Json;
using System.Collections.Generic;

namespace MyTikTokBackup.Core.TikTok
{
    public class Content
    {
        [JsonProperty("desc")]
        public string Desc { get; set; }

        [JsonProperty("textExtra")]
        public List<TextExtra> TextExtra { get; set; }
    }
}
