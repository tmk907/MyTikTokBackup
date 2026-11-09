using System;

namespace MyTikTokBackup.Core.Database
{
    public enum AiLabelStatus
    {
        Suggested = 0,
        Rejected = 1,
    }

    /// <summary>
    /// Category suggested by the AI labeling process. Rejected suggestions are kept
    /// so they are not suggested again.
    /// </summary>
    public class VideoAiLabel
    {
        public int Id { get; set; }

        /// <summary><see cref="Database.Video.Id"/> (database id, not the TikTok video id).</summary>
        public int VideoId { get; set; }

        /// <summary><see cref="Category.Name"/>. No FK - cleaned up explicitly by CategoriesService.</summary>
        public string CategoryName { get; set; }

        public float Score { get; set; }
        public AiLabelStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
