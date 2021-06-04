using System;
using System.Collections.Generic;

namespace MyTikTokBackup.Core.Database
{
    public class Video
    {
        public int Id { get; set; }

        public string VideoId { get; set; }
        public string Description { get; set; }
        public string DuetFromId { get; set; }
        public TimeSpan Duration { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public string Ratio { get; set; }
        public int MyProperty { get; set; }
        public VideoStats Stats { get; set; }

        public Author Author { get; set; }
        public Music Music { get; set; }
        public ICollection<Hashtag> Hashtags { get; set; }
    }

    public class VideoStats
    {
        public long CommentCount { get; set; }
        public long DiggCount { get; set; }
        public long PlayCount { get; set; }
        public long ShareCount { get; set; }
        public DateTime LastUpdated { get; set; }

        public static VideoStats Create(long comment, long digg, long play, long share)
        {
            return new VideoStats
            {
                CommentCount = comment,
                DiggCount = digg,
                PlayCount = play,
                ShareCount = share,
                LastUpdated = DateTime.Now
            };
        }
    }
}
