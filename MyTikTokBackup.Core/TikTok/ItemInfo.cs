using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class ItemInfo
    {
        [JsonProperty("AIGCDescription")]
        public string AIGCDescription { get; set; }

        [JsonProperty("anchors")]
        public List<Anchor> Anchors { get; set; }

        [JsonProperty("author")]
        public Author Author { get; set; }

        [JsonProperty("challenges")]
        public List<Challenge> Challenges { get; set; }

        [JsonProperty("collected")]
        public bool Collected { get; set; }

        [JsonProperty("contents")]
        public List<Content> Contents { get; set; }

        [JsonProperty("createTime")]
        public long CreateTime { get; set; }

        [JsonProperty("desc")]
        public string Desc { get; set; }

        [JsonProperty("digged")]
        public bool Digged { get; set; }

        [JsonProperty("diversificationId")]
        public long DiversificationId { get; set; }

        [JsonProperty("duetDisplay")]
        public long DuetDisplay { get; set; }

        [JsonProperty("duetEnabled")]
        public bool DuetEnabled { get; set; }

        [JsonProperty("forFriend")]
        public bool ForFriend { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("itemCommentStatus")]
        public long ItemCommentStatus { get; set; }

        [JsonProperty("item_control")]
        public ItemControl ItemControl { get; set; }

        [JsonProperty("music")]
        public Music Music { get; set; }

        [JsonProperty("officalItem")]
        public bool OfficalItem { get; set; }

        [JsonProperty("originalItem")]
        public bool OriginalItem { get; set; }

        [JsonProperty("privateItem")]
        public bool PrivateItem { get; set; }

        [JsonProperty("secret")]
        public bool Secret { get; set; }

        [JsonProperty("shareEnabled")]
        public bool ShareEnabled { get; set; }

        [JsonProperty("stats")]
        public Stats Stats { get; set; }

        [JsonProperty("statsV2")]
        public StatsV2 StatsV2 { get; set; }

        [JsonProperty("stitchDisplay")]
        public long StitchDisplay { get; set; }

        [JsonProperty("stitchEnabled")]
        public bool StitchEnabled { get; set; }

        [JsonProperty("textExtra")]
        public List<TextExtra> TextExtra { get; set; }

        [JsonProperty("video")]
        public Video Video { get; set; }

        [JsonProperty("imagePost")]
        public ImagePost ImagePost { get; set; }

        [JsonProperty("itemMute")]
        public bool? ItemMute { get; set; }

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

    public class ItemControl
    {
    }
}
