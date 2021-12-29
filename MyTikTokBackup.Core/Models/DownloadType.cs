using System.ComponentModel;

namespace MyTikTokBackup.Core.Models
{
    public enum DownloadType
    {
        [Description("Favorite")]
        Favorite,
        [Description("Posted")]
        Posted,
        [Description("Other")]
        Other,
        [Description("Bookmarks")]
        Bookmarks
    }
}
