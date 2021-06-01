using System;

namespace MyTikTokBackup.Core.Database
{
    class Author
    {
        public string Id { get; set; }
        public string Nickname { get; set; }
        public string Signature { get; set; }
        public string UniqueId { get; set; }
        public AuthorStats Stats { get; set; }
    }

    class AuthorStats
    {
        public int DiggCount { get; set; }
        public int FollowerCount { get; set; }
        public int FollowingCount { get; set; }
        public int HeartCount { get; set; }
        public int VideoCount { get; set; }

        public DateTimeOffset LastUpdated { get; set; }

        public static AuthorStats Create(int digg, int follower, int following, int heart, int video)
        {
            return new AuthorStats
            {
                DiggCount = digg,
                FollowerCount = follower,
                FollowingCount = following,
                HeartCount = heart,
                VideoCount = video,
                LastUpdated = DateTimeOffset.Now
            };
        }
    }
}
