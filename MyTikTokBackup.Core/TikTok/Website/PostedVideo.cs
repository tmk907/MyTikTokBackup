using Newtonsoft.Json;
using System.Collections.Generic;

namespace MyTikTokBackup.Core.TikTok.Website
{
    public class PostedVideo
    {
        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("authorId")]
        public string AuthorId { get; set; }

        [JsonProperty("authorSecId")]
        public string AuthorSecId { get; set; }

        [JsonProperty("authorStats")]
        public AuthorStats AuthorStats { get; set; }

        [JsonProperty("challenges")]
        public List<Challenge> Challenges { get; set; }

        [JsonProperty("comments")]
        public List<object> Comments { get; set; }

        [JsonProperty("createTime")]
        public string CreateTime { get; set; }

        [JsonProperty("desc")]
        public string Desc { get; set; }

        [JsonProperty("digged")]
        public bool Digged { get; set; }

        [JsonProperty("duetDisplay")]
        public long DuetDisplay { get; set; }

        [JsonProperty("duetEnabled")]
        public bool DuetEnabled { get; set; }

        [JsonProperty("duetInfo")]
        public DuetInfo DuetInfo { get; set; }

        [JsonProperty("effectStickers")]
        public List<object> EffectStickers { get; set; }

        [JsonProperty("forFriend")]
        public bool ForFriend { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("indexEnabled")]
        public bool IndexEnabled { get; set; }

        [JsonProperty("isAd")]
        public bool IsAd { get; set; }

        [JsonProperty("itemCommentStatus")]
        public long ItemCommentStatus { get; set; }

        [JsonProperty("itemMute")]
        public bool ItemMute { get; set; }

        [JsonProperty("music")]
        public Music Music { get; set; }

        [JsonProperty("nickname")]
        public string Nickname { get; set; }

        [JsonProperty("officalItem")]
        public bool OfficalItem { get; set; }

        [JsonProperty("originalItem")]
        public bool OriginalItem { get; set; }

        [JsonProperty("privateItem")]
        public bool PrivateItem { get; set; }

        [JsonProperty("scheduleTime")]
        public long ScheduleTime { get; set; }

        [JsonProperty("secret")]
        public bool Secret { get; set; }

        [JsonProperty("shareEnabled")]
        public bool ShareEnabled { get; set; }

        [JsonProperty("showNotPass")]
        public bool ShowNotPass { get; set; }

        [JsonProperty("stats")]
        public Stats Stats { get; set; }

        [JsonProperty("stickersOnItem")]
        public List<StickersOnItem> StickersOnItem { get; set; }

        [JsonProperty("stitchDisplay")]
        public long StitchDisplay { get; set; }

        [JsonProperty("stitchEnabled")]
        public bool StitchEnabled { get; set; }

        [JsonProperty("takeDown")]
        public long TakeDown { get; set; }

        [JsonProperty("textExtra")]
        public List<TextExtra> TextExtra { get; set; }

        [JsonProperty("video")]
        public Video Video { get; set; }

        [JsonProperty("vl1")]
        public bool Vl1 { get; set; }

        [JsonProperty("warnInfo")]
        public List<object> WarnInfo { get; set; }
    }


    public class AuthorStats
    {
        [JsonProperty("diggCount")]
        public long DiggCount { get; set; }

        [JsonProperty("followerCount")]
        public long FollowerCount { get; set; }

        [JsonProperty("followingCount")]
        public long FollowingCount { get; set; }

        [JsonProperty("heart")]
        public long Heart { get; set; }

        [JsonProperty("heartCount")]
        public long HeartCount { get; set; }

        [JsonProperty("videoCount")]
        public long VideoCount { get; set; }
    }

    public class Challenge
    {
        [JsonProperty("coverLarger")]
        public string CoverLarger { get; set; }

        [JsonProperty("coverMedium")]
        public string CoverMedium { get; set; }

        [JsonProperty("coverThumb")]
        public string CoverThumb { get; set; }

        [JsonProperty("desc")]
        public string Desc { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("isCommerce")]
        public bool IsCommerce { get; set; }

        [JsonProperty("profileLarger")]
        public string ProfileLarger { get; set; }

        [JsonProperty("profileMedium")]
        public string ProfileMedium { get; set; }

        [JsonProperty("profileThumb")]
        public string ProfileThumb { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }

    public class DuetInfo
    {
        [JsonProperty("duetFromId")]
        public string DuetFromId { get; set; }
    }

    public class Music
    {
        [JsonProperty("album")]
        public string Album { get; set; }

        [JsonProperty("authorName")]
        public string AuthorName { get; set; }

        [JsonProperty("coverLarge")]
        public string CoverLarge { get; set; }

        [JsonProperty("coverMedium")]
        public string CoverMedium { get; set; }

        [JsonProperty("coverThumb")]
        public string CoverThumb { get; set; }

        [JsonProperty("duration")]
        public long Duration { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("original")]
        public bool Original { get; set; }

        [JsonProperty("playUrl")]
        public string PlayUrl { get; set; }

        [JsonProperty("scheduleSearchTime")]
        public long ScheduleSearchTime { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }

    public class Stats
    {
        [JsonProperty("commentCount")]
        public long CommentCount { get; set; }

        [JsonProperty("diggCount")]
        public long DiggCount { get; set; }

        [JsonProperty("playCount")]
        public long PlayCount { get; set; }

        [JsonProperty("shareCount")]
        public long ShareCount { get; set; }
    }

    public class StickersOnItem
    {
        [JsonProperty("stickerText")]
        public List<string> StickerText { get; set; }

        [JsonProperty("stickerType")]
        public long StickerType { get; set; }
    }

    public class TextExtra
    {
        [JsonProperty("awemeId")]
        public string AwemeId { get; set; }

        [JsonProperty("end")]
        public long End { get; set; }

        [JsonProperty("hashtagId")]
        public string HashtagId { get; set; }

        [JsonProperty("hashtagName")]
        public string HashtagName { get; set; }

        [JsonProperty("isCommerce")]
        public bool IsCommerce { get; set; }

        [JsonProperty("secUid")]
        public string SecUid { get; set; }

        [JsonProperty("start")]
        public long Start { get; set; }

        [JsonProperty("subType")]
        public long SubType { get; set; }

        [JsonProperty("type")]
        public long Type { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("userUniqueId")]
        public string UserUniqueId { get; set; }
    }

    public class Video
    {
        [JsonProperty("bitrate")]
        public long Bitrate { get; set; }

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

        [JsonProperty("reflowCover")]
        public string ReflowCover { get; set; }

        [JsonProperty("shareCover")]
        public List<string> ShareCover { get; set; }

        [JsonProperty("videoQuality")]
        public string VideoQuality { get; set; }

        [JsonProperty("width")]
        public long Width { get; set; }
    }
}
