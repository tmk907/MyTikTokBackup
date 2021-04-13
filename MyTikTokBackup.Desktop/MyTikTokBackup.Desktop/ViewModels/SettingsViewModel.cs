using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using MyTikTokBackup.Core.Services;
using MyTikTokBackup.WindowsUWP.Helpers;

namespace MyTikTokBackup.Desktop.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private readonly IImportService _importService;
        private readonly IAppConfiguration _appConfiguration;
        private readonly ILocalVideosService _localVideosService;

        public SettingsViewModel(IImportService importService, IAppConfiguration appConfiguration,
            ILocalVideosService localVideosService)
        {
            _importService = importService;
            _appConfiguration = appConfiguration;
            _localVideosService = localVideosService;
            DownloadsFolderPath = appConfiguration.DownloadsFolder;
            ImportFavoriteVideosCommand = new AsyncRelayCommand(ImportFavoriteVideos);
            ChooseDownloadsFolderCommand = new AsyncRelayCommand(ChooseDownloadsFolder);
        }


        private string downloadsFolderPath;
        public string DownloadsFolderPath
        {
            get { return downloadsFolderPath; }
            set { SetProperty(ref downloadsFolderPath, value); }
        }


        public IAsyncRelayCommand ImportFavoriteVideosCommand { get; }
        public IAsyncRelayCommand ChooseDownloadsFolderCommand { get; }

        private async Task ImportFavoriteVideos()
        {
            var file = await FilePickerHelper.PickFile(new List<string>() { ".json", ".har" });
            if (file != null)
            {
                await _importService.ImportFavoriteVideos(file.Path);
            }
        }

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
    }
}
