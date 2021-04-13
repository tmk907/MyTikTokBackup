using System.ComponentModel;

namespace MyTikTokBackup.Core.Models
{
    public enum DownloadStatus
    {
        [Description("")]
        NotDownloaded,
        [Description("Downloading")]
        Downloading,
        [Description("Downloaded")]
        Downloaded,
        [Description("Error")]
        Error
    }
}
