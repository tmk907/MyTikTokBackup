using System.Text.Json.Serialization;

namespace MyTikTokBackup.Core.TikTok
{
    public class TikTokOembed
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("author_url")]
        public string AuthorUrl { get; set; }

        [JsonPropertyName("author_name")]
        public string AuthorName { get; set; }

        [JsonPropertyName("width")]
        public string Width { get; set; }

        [JsonPropertyName("height")]
        public string Height { get; set; }

        [JsonPropertyName("html")]
        public string Html { get; set; }

        [JsonPropertyName("thumbnail_width")]
        public int ThumbnailWidth { get; set; }

        [JsonPropertyName("thumbnail_height")]
        public int ThumbnailHeight { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public string ThumbnailUrl { get; set; }

        [JsonPropertyName("provider_url")]
        public string ProviderUrl { get; set; }

        [JsonPropertyName("provider_name")]
        public string ProviderName { get; set; }
    }


}
