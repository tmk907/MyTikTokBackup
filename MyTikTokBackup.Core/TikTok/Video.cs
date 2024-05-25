using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class Video
    {
        [JsonProperty("bitrate")]
        public long Bitrate { get; set; }

        [JsonProperty("bitrateInfo")]
        public List<BitrateInfo> BitrateInfo { get; set; }

        [JsonProperty("codecType")]
        public string CodecType { get; set; }

        [JsonProperty("cover")]
        public string Cover { get; set; }

        [JsonProperty("definition")]
        public string Definition { get; set; }

        [JsonProperty("downloadAddr")]
        public string DownloadAddr { get; set; }

        [JsonProperty("duration")]
        public long Duration { get; set; }

        [JsonProperty("dynamicCover")]
        public string DynamicCover { get; set; }

        [JsonProperty("encodeUserTag")]
        public string EncodeUserTag { get; set; }

        [JsonProperty("encodedType")]
        public string EncodedType { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("height")]
        public long Height { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("originCover")]
        public string OriginCover { get; set; }

        [JsonProperty("playAddr")]
        public string PlayAddr { get; set; }

        [JsonProperty("ratio")]
        public string Ratio { get; set; }

        [JsonProperty("subtitleInfos")]
        public List<SubtitleInfo> SubtitleInfos { get; set; }

        [JsonProperty("videoQuality")]
        public string VideoQuality { get; set; }

        [JsonProperty("volumeInfo")]
        public VolumeInfo VolumeInfo { get; set; }

        [JsonProperty("width")]
        public long Width { get; set; }

        [JsonProperty("zoomCover")]
        public ZoomCover ZoomCover { get; set; }
    }

    public class VideoSuggestWordsList
    {
        [JsonProperty("video_suggest_words_struct")]
        public List<VideoSuggestWordsStruct> VideoSuggestWordsStruct { get; set; }
    }

    public class VideoSuggestWordsStruct
    {
        [JsonProperty("hlong_text")]
        public string HlongText { get; set; }

        [JsonProperty("scene")]
        public string Scene { get; set; }

        [JsonProperty("words")]
        public List<Word> Words { get; set; }
    }

    public class VolumeInfo
    {
        [JsonProperty("Loudness")]
        public double Loudness { get; set; }

        [JsonProperty("Peak")]
        public double Peak { get; set; }
    }

    public class Word
    {
        [JsonProperty("word")]
        public string WordValue { get; set; }

        [JsonProperty("word_id")]
        public string WordId { get; set; }
    }

    public class ZoomCover
    {
        [JsonProperty("240")]
        public string _240 { get; set; }

        [JsonProperty("480")]
        public string _480 { get; set; }

        [JsonProperty("720")]
        public string _720 { get; set; }

        [JsonProperty("960")]
        public string _960 { get; set; }
    }
}
