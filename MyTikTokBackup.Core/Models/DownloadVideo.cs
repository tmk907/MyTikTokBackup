using Microsoft.Toolkit.Mvvm.ComponentModel;
using EnumsNET;

namespace MyTikTokBackup.Core.Models
{
    public class DownloadVideo : ObservableObject
    {
        public string Id { get; init; }
        public string Title { get; init; }
        public string FilePath { get; init; }


        private DownloadStatus downloadStatus;
        public DownloadStatus DownloadStatus
        {
            get { return downloadStatus; }
            set { SetProperty(ref downloadStatus, value); OnPropertyChanged(nameof(Status)); }
        }

        public string Status => downloadStatus.AsString(EnumFormat.Description);
    }
}
