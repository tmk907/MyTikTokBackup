namespace MyTikTokBackup.Core.Database
{
    public class VideoCategory
    {
        public int Id { get; set; }

        /// <summary>Foreign key to <see cref="Category.Name"/>.</summary>
        public string CategoryName { get; set; }
        public Category Category { get; set; }

        /// <summary>Foreign key to <see cref="Database.Video.Id"/> (database id, not the TikTok video id).</summary>
        public int VideoId { get; set; }
        public Video Video { get; set; }
    }
}
