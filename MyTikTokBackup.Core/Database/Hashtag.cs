using System;
using System.Collections.Generic;

namespace MyTikTokBackup.Core.Database
{
    public class Hashtag
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ICollection<Video> Videos { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Hashtag hashtag &&
                   Id == hashtag.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }
    }
}
