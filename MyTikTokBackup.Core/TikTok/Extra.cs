using Newtonsoft.Json;
using System.Collections.Generic;

namespace MyTikTokBackup.Core.TikTok
{
    public class Extra
    {
        [JsonProperty("fatal_item_ids")]
        public List<object> FatalItemIds { get; set; }

        [JsonProperty("logid")]
        public string Logid { get; set; }

        [JsonProperty("now")]
        public long Now { get; set; }
    }

    public class ExtraInfo
    {
        [JsonProperty("subtype")]
        public string Subtype { get; set; }
    }

    public class Icon
    {
        [JsonProperty("urlList")]
        public List<string> UrlList { get; set; }
    }

    public class Image
    {
        [JsonProperty("imageHeight")]
        public int ImageHeight { get; set; }

        [JsonProperty("imageURL")]
        public ImageURL ImageURL { get; set; }

        [JsonProperty("imageWidth")]
        public int ImageWidth { get; set; }
    }

    public class ImagePost
    {
        [JsonProperty("cover")]
        public Cover Cover { get; set; }

        [JsonProperty("images")]
        public List<Image> Images { get; set; }

        [JsonProperty("shareCover")]
        public ShareCover ShareCover { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }

    public class ImageURL
    {
        [JsonProperty("urlList")]
        public List<string> UrlList { get; set; }
    }

    public class ShareCover
    {
        [JsonProperty("imageHeight")]
        public int ImageHeight { get; set; }

        [JsonProperty("imageURL")]
        public ImageURL ImageURL { get; set; }

        [JsonProperty("imageWidth")]
        public int ImageWidth { get; set; }
    }

    public class LogPb
    {
        [JsonProperty("impr_id")]
        public string ImprId { get; set; }
    }

}
