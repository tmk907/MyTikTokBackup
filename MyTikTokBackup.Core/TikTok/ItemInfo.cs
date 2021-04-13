using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class ItemInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("desc")]
        public string Desc { get; set; }

        [JsonProperty("createTime")]
        public long CreateTime { get; set; }

        [JsonProperty("video")]
        public Video Video { get; set; }

        [JsonProperty("author")]
        public Author Author { get; set; }

        [JsonProperty("music")]
        public Music Music { get; set; }

        [JsonProperty("challenges", NullValueHandling = NullValueHandling.Ignore)]
        public List<Challenge> Challenges { get; set; }

        [JsonProperty("stats")]
        public Stats Stats { get; set; }

        [JsonProperty("duetInfo")]
        public DuetInfo DuetInfo { get; set; }

        [JsonProperty("originalItem")]
        public bool OriginalItem { get; set; }

        [JsonProperty("officalItem")]
        public bool OfficalItem { get; set; }

        [JsonProperty("textExtra", NullValueHandling = NullValueHandling.Ignore)]
        public List<TextExtra> TextExtra { get; set; }

        [JsonProperty("secret")]
        public bool Secret { get; set; }

        [JsonProperty("forFriend")]
        public bool ForFriend { get; set; }

        [JsonProperty("digged")]
        public bool Digged { get; set; }

        [JsonProperty("itemCommentStatus")]
        public long ItemCommentStatus { get; set; }

        [JsonProperty("showNotPass")]
        public bool ShowNotPass { get; set; }

        [JsonProperty("vl1")]
        public bool Vl1 { get; set; }

        [JsonProperty("itemMute")]
        public bool ItemMute { get; set; }

        [JsonProperty("authorStats")]
        public AuthorStats AuthorStats { get; set; }

        [JsonProperty("privateItem")]
        public bool PrivateItem { get; set; }

        [JsonProperty("duetEnabled")]
        public bool DuetEnabled { get; set; }

        [JsonProperty("stitchEnabled")]
        public bool StitchEnabled { get; set; }

        [JsonProperty("shareEnabled")]
        public bool ShareEnabled { get; set; }

        [JsonProperty("isAd")]
        public bool IsAd { get; set; }

        [JsonProperty("effectStickers", NullValueHandling = NullValueHandling.Ignore)]
        public List<EffectSticker> EffectStickers { get; set; }

        [JsonProperty("stickersOnItem", NullValueHandling = NullValueHandling.Ignore)]
        public List<StickersOnItem> StickersOnItem { get; set; }

        public List<Header> Headers { get; set; } = new List<Header>();

        public override bool Equals(object obj)
        {
            return obj is ItemInfo item &&
                   Id == item.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }
    }
}
