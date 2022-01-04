using System;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using MyTikTokBackup.Core.Services;
using MyTikTokBackup.WindowsUWP.Helpers;
using Windows.ApplicationModel;

namespace MyTikTokBackup.Desktop.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private readonly IAppConfiguration _appConfiguration;
        private readonly ILocalVideosService _localVideosService;

        public SettingsViewModel(IAppConfiguration appConfiguration,
            ILocalVideosService localVideosService)
        {
            _appConfiguration = appConfiguration;
            _localVideosService = localVideosService;
            DownloadsFolderPath = appConfiguration.DownloadsFolder;
            ChooseDownloadsFolderCommand = new AsyncRelayCommand(ChooseDownloadsFolder);
        }


        private string downloadsFolderPath;
        public string DownloadsFolderPath
        {
            get { return downloadsFolderPath; }
            set { SetProperty(ref downloadsFolderPath, value); }
        }


        public IAsyncRelayCommand ChooseDownloadsFolderCommand { get; }

        private async Task ChooseDownloadsFolder()
        {
            var folder = await FilePickerHelper.PickFolder();
            if (folder != null)
            {
                _appConfiguration.DownloadsFolder = folder.Path;
                DownloadsFolderPath = _appConfiguration.DownloadsFolder;
                _localVideosService.Refresh();
            }
        }

        public string AppName => Package.Current.DisplayName;

        public string AppVersion => GetAppVersion();

        private string GetAppVersion()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            return string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }
    }
}
