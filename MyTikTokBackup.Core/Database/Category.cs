using System;
using System.Collections.Generic;

namespace MyTikTokBackup.Core.Database
{
    public class Category
    {
        public string Name { get; set; }
        public string Color { get; set; }

        public ICollection<Video> Videos { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Category category &&
                   Name == category.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name);
        }
    }
}
