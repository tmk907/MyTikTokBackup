using System;

namespace MyTikTokBackup.Core.Database
{
    public class Music
    {
        public string Id { get; set; }
        public string Album { get; set; }
        public string AuthorName { get; set; }
        public TimeSpan Duration { get; set; }
        public string Title { get; set; }
    }
}
