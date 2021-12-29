using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MyTikTokBackup.Core.TikTok.Website
{
    public class PostedVideo
    {
        [JsonPropertyName("author")]
        public string Author { get; set; }

        [JsonPropertyName("authorId")]
        public string AuthorId { get; set; }

        [JsonPropertyName("authorSecId")]
        public string AuthorSecId { get; set; }

        [JsonPropertyName("authorStats")]
        public AuthorStats AuthorStats { get; set; }

        [JsonPropertyName("challenges")]
        public List<Challenge> Challenges { get; set; }

        [JsonPropertyName("comments")]
        public List<object> Comments { get; set; }

        [JsonPropertyName("createTime")]
        public string CreateTime { get; set; }

        [JsonPropertyName("desc")]
        public string Desc { get; set; }

        [JsonPropertyName("digged")]
        public bool Digged { get; set; }

        [JsonPropertyName("duetDisplay")]
        public long DuetDisplay { get; set; }

        [JsonPropertyName("duetEnabled")]
        public bool DuetEnabled { get; set; }

        [JsonPropertyName("duetInfo")]
        public DuetInfo DuetInfo { get; set; }

        [JsonPropertyName("effectStickers")]
        public List<object> EffectStickers { get; set; }

        [JsonPropertyName("forFriend")]
        public bool ForFriend { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("indexEnabled")]
        public bool IndexEnabled { get; set; }

        [JsonPropertyName("isAd")]
        public bool IsAd { get; set; }

        [JsonPropertyName("itemCommentStatus")]
        public long ItemCommentStatus { get; set; }

        [JsonPropertyName("itemMute")]
        public bool ItemMute { get; set; }

        [JsonPropertyName("music")]
        public Music Music { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("officalItem")]
        public bool OfficalItem { get; set; }

        [JsonPropertyName("originalItem")]
        public bool OriginalItem { get; set; }

        [JsonPropertyName("privateItem")]
        public bool PrivateItem { get; set; }

        [JsonPropertyName("scheduleTime")]
        public long ScheduleTime { get; set; }

        [JsonPropertyName("secret")]
        public bool Secret { get; set; }

        [JsonPropertyName("shareEnabled")]
        public bool ShareEnabled { get; set; }

        [JsonPropertyName("showNotPass")]
        public bool ShowNotPass { get; set; }

        [JsonPropertyName("stats")]
        public Stats Stats { get; set; }

        [JsonPropertyName("stickersOnItem")]
        public List<StickersOnItem> StickersOnItem { get; set; }

        [JsonPropertyName("stitchDisplay")]
        public long StitchDisplay { get; set; }

        [JsonPropertyName("stitchEnabled")]
        public bool StitchEnabled { get; set; }

        [JsonPropertyName("takeDown")]
        public long TakeDown { get; set; }

        [JsonPropertyName("textExtra")]
        public List<TextExtra> TextExtra { get; set; }

        [JsonPropertyName("video")]
        public Video Video { get; set; }

        [JsonPropertyName("vl1")]
        public bool Vl1 { get; set; }

        [JsonPropertyName("warnInfo")]
        public List<object> WarnInfo { get; set; }
    }


    public class AuthorStats
    {
        [JsonPropertyName("diggCount")]
        public long DiggCount { get; set; }

        [JsonPropertyName("followerCount")]
        public long FollowerCount { get; set; }

        [JsonPropertyName("followingCount")]
        public long FollowingCount { get; set; }

        [JsonPropertyName("heart")]
        public long Heart { get; set; }

        [JsonPropertyName("heartCount")]
        public long HeartCount { get; set; }

        [JsonPropertyName("videoCount")]
        public long VideoCount { get; set; }
    }

    public class Challenge
    {
        [JsonPropertyName("coverLarger")]
        public string CoverLarger { get; set; }

        [JsonPropertyName("coverMedium")]
        public string CoverMedium { get; set; }

        [JsonPropertyName("coverThumb")]
        public string CoverThumb { get; set; }

        [JsonPropertyName("desc")]
        public string Desc { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("isCommerce")]
        public bool IsCommerce { get; set; }

        [JsonPropertyName("profileLarger")]
        public string ProfileLarger { get; set; }

        [JsonPropertyName("profileMedium")]
        public string ProfileMedium { get; set; }

        [JsonPropertyName("profileThumb")]
        public string ProfileThumb { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }
    }

    public class DuetInfo
    {
        [JsonPropertyName("duetFromId")]
        public string DuetFromId { get; set; }
    }

    public class Music
    {
        [JsonPropertyName("album")]
        public string Album { get; set; }

        [JsonPropertyName("authorName")]
        public string AuthorName { get; set; }

        [JsonPropertyName("coverLarge")]
        public string CoverLarge { get; set; }

        [JsonPropertyName("coverMedium")]
        public string CoverMedium { get; set; }

        [JsonPropertyName("coverThumb")]
        public string CoverThumb { get; set; }

        [JsonPropertyName("duration")]
        public long Duration { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("original")]
        public bool Original { get; set; }

        [JsonPropertyName("playUrl")]
        public string PlayUrl { get; set; }

        [JsonPropertyName("scheduleSearchTime")]
        public long ScheduleSearchTime { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }
    }

    public class Stats
    {
        [JsonPropertyName("commentCount")]
        public long CommentCount { get; set; }

        [JsonPropertyName("diggCount")]
        public long DiggCount { get; set; }

        [JsonPropertyName("playCount")]
        public long PlayCount { get; set; }

        [JsonPropertyName("shareCount")]
        public long ShareCount { get; set; }
    }

    public class StickersOnItem
    {
        [JsonPropertyName("stickerText")]
        public List<string> StickerText { get; set; }

        [JsonPropertyName("stickerType")]
        public long StickerType { get; set; }
    }

    public class TextExtra
    {
        [JsonPropertyName("awemeId")]
        public string AwemeId { get; set; }

        [JsonPropertyName("end")]
        public long End { get; set; }

        [JsonPropertyName("hashtagId")]
        public string HashtagId { get; set; }

        [JsonPropertyName("hashtagName")]
        public string HashtagName { get; set; }

        [JsonPropertyName("isCommerce")]
        public bool IsCommerce { get; set; }

        [JsonPropertyName("secUid")]
        public string SecUid { get; set; }

        [JsonPropertyName("start")]
        public long Start { get; set; }

        [JsonPropertyName("subType")]
        public long SubType { get; set; }

        [JsonPropertyName("type")]
        public long Type { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("userUniqueId")]
        public string UserUniqueId { get; set; }
    }

    public class Video
    {
        [JsonPropertyName("bitrate")]
        public long Bitrate { get; set; }

        [JsonPropertyName("codecType")]
        public string CodecType { get; set; }

        [JsonPropertyName("cover")]
        public string Cover { get; set; }

        [JsonPropertyName("definition")]
        public string Definition { get; set; }

        [JsonPropertyName("downloadAddr")]
        public string DownloadAddr { get; set; }

        [JsonPropertyName("duration")]
        public long Duration { get; set; }

        [JsonPropertyName("dynamicCover")]
        public string DynamicCover { get; set; }

        [JsonPropertyName("encodeUserTag")]
        public string EncodeUserTag { get; set; }

        [JsonPropertyName("encodedType")]
        public string EncodedType { get; set; }

        [JsonPropertyName("format")]
        public string Format { get; set; }

        [JsonPropertyName("height")]
        public long Height { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("originCover")]
        public string OriginCover { get; set; }

        [JsonPropertyName("playAddr")]
        public string PlayAddr { get; set; }

        [JsonPropertyName("ratio")]
        public string Ratio { get; set; }

        [JsonPropertyName("reflowCover")]
        public string ReflowCover { get; set; }

        [JsonPropertyName("shareCover")]
        public List<string> ShareCover { get; set; }

        [JsonPropertyName("videoQuality")]
        public string VideoQuality { get; set; }

        [JsonPropertyName("width")]
        public long Width { get; set; }
    }
}
