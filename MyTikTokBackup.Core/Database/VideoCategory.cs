namespace MyTikTokBackup.Core.Database
{
    public class VideoCategory
    {
        public int Id { get; set; }
        public Category Category { get; set; }
        public Video Video { get; set; }
    }
}
