using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MyTikTokBackup.Core.Database
{
    class Video
    {
        public int Id { get; set; }

        public string VideoId { get; set; }
        public string Description { get; set; }
        public string DuetFromId { get; set; }
        public int Duration { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public string Ratio { get; set; }
        public int MyProperty { get; set; }
        public VideoStats Stats { get; set; }

        public Author Author { get; set; }
        public Music Music { get; set; }
        public IEnumerable<Hashtag> Hashtags { get; set; }
    }

    class VideoStats
    {
        public int CommentCount { get; set; }
        public int DiggCount { get; set; }
        public int PlayCount { get; set; }
        public int ShareCount { get; set; }
        public DateTimeOffset LastUpdated { get; set; }

        public static VideoStats Create(int comment, int digg, int play, int share)
        {
            return new VideoStats
            {
                CommentCount = comment,
                DiggCount = digg,
                PlayCount = play,
                ShareCount = share,
                LastUpdated = DateTimeOffset.Now
            };
        }
    }
}
