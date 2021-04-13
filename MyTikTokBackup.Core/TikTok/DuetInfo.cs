using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class DuetInfo
    {
        [JsonProperty("duetFromId")]
        public string DuetFromId { get; set; }
    }
}
