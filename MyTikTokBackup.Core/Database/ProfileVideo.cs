namespace MyTikTokBackup.Core.Database
{
    public class ProfileVideo
    {
        public int Id { get; set; }

        public string UserUniqueId { get; set; }
        public string VideoId { get; set; }
        public FeedType FeedType { get; set; }
        public int Index { get; set; }
    }

    public enum FeedType
    {
        Posted,
        Liked
    }
}
