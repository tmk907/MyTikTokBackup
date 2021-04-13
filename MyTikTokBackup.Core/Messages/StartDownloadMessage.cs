using MyTikTokBackup.Core.Models;

namespace MyTikTokBackup.Core.Messages
{
    public record DownloadStatusChanged(string Id, DownloadStatus DownloadStatus, string FilePath);

    public class FilterByCategoryChangeMessage
    {

    }
}
