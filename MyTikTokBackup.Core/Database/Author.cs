using System;

namespace MyTikTokBackup.Core.Database
{
    public class Author
    {
        public string Id { get; set; }
        public string Nickname { get; set; }
        public string Signature { get; set; }
        public string UniqueId { get; set; }
        public AuthorStats Stats { get; set; }
    }

    public class AuthorStats
    {
        public long DiggCount { get; set; }
        public long FollowerCount { get; set; }
        public long FollowingCount { get; set; }
        public long HeartCount { get; set; }
        public long VideoCount { get; set; }

        public DateTime LastUpdated { get; set; }

        public static AuthorStats Create(long digg, long follower, long following, long heart, long video)
        {
            return new AuthorStats
            {
                DiggCount = digg,
                FollowerCount = follower,
                FollowingCount = following,
                HeartCount = heart,
                VideoCount = video,
                LastUpdated = DateTime.Now
            };
        }
    }
}
