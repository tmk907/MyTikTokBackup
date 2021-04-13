using System;
using System.Collections.Generic;

namespace MyTikTokBackup.Core.Repositories
{
    public class UserVideo
    {
        public string VideoId { get; set; }
        public List<Category> Categories { get; set; } = new List<Category>();

        public override bool Equals(object obj)
        {
            return obj is UserVideo video &&
                   VideoId == video.VideoId;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(VideoId);
        }
    }
}
