using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using MyTikTokBackup.Core.Models;
using MyTikTokBackup.Core.Services;
using MyTikTokBackup.Core.TikTok;
using MyTikTokBackup.Core.TikTok.UserData;
using MyTikTokBackup.Core.TikTok.Website;
using MyTikTokBackup.WindowsUWP.Helpers;
using Serilog;
using Windows.ApplicationModel;

namespace MyTikTokBackup.Desktop.ViewModels
{
    public class UserDataViewModel : ObservableObject
    {
        private readonly IAppConfiguration _appConfiguration;
        private readonly IDownloadsManager _downloadsManager;
        private readonly HttpClient _httpClient;

        public UserDataViewModel(IAppConfiguration appConfiguration, IDownloadsManager downloadsManager)
        {
            _httpClient = new HttpClient();
            _appConfiguration = appConfiguration;
            _downloadsManager = downloadsManager;
            ImportUserDataFileCommand = new AsyncRelayCommand(ImportUserDataFile);
            FindFavoriteVideosCommand = new AsyncRelayCommand(FindFavoriteVideos);
            DownloadFavoriteVideosCommand = new AsyncRelayCommand(DownloadFavoriteVideos);
        }

        private UserData _userData;
        public ObservableCollection<PostedVideo> FavoriteVideos { get; } = new ObservableCollection<PostedVideo>();

        public IAsyncRelayCommand ImportUserDataFileCommand { get; }
        public IAsyncRelayCommand FindFavoriteVideosCommand { get; }
        public IAsyncRelayCommand DownloadFavoriteVideosCommand { get; }


        private int favoriteCount;
        public int FavoriteCount
        {
            get { return favoriteCount; }
            set { SetProperty(ref favoriteCount, value); }
        }

        private int likedCount;
        public int LikedCount
        {
            get { return likedCount; }
            set { SetProperty(ref likedCount, value); }
        }

        private int historyCount;
        public int HistoryCount
        {
            get { return historyCount; }
            set { SetProperty(ref historyCount, value); }
        }


        private string userName;
        public string UserName
        {
            get { return userName; }
            set { SetProperty(ref userName, value); }
        }


        private async Task ImportUserDataFile()
        {
            var file = await FilePickerHelper.PickFile(new[] {".json"});
            if (file != null)
            {
                using FileStream openStream = File.OpenRead(file.Path);
                _userData = await JsonSerializer.DeserializeAsync<UserData>(openStream);

                FavoriteCount = _userData.Activity.FavoriteVideos.FavoriteVideoList.Count;
                LikedCount = _userData.Activity.LikeList.ItemFavoriteList.Count;
                HistoryCount = _userData.Activity.VideoBrowsingHistory.VideoList.Count;
                UserName = $"@{_userData.Profile.ProfileInformation.ProfileMap.UserName}";

                await FindFavoriteVideos();
            }
        }

        private async Task FindFavoriteVideos()
        {
            FavoriteVideos.Clear();
            foreach(var chunk in _userData.Activity.FavoriteVideos.FavoriteVideoList.Chunk(4))
            {
                var tasks = chunk.Select(x => GetFavoriteVideoFromUrl(x.Link)).ToList();
                await Task.WhenAll(tasks);
            }
        }

        private async Task DownloadFavoriteVideos()
        {
            var type = DownloadType.Bookmarks;
            await _downloadsManager.QueueVideos(UserName, type, FavoriteVideos.Select(x => Helper.ToItemInfo(x)).ToList());
        }

        private async Task GetFavoriteVideoFromUrl(string url)
        {
            try
            {
                var content = await _httpClient.GetStringAsync(url);
                var sigiState = GetSigiStateFromHtml(content);
                var item = JsonSerializer.Deserialize<SigiStateItemModule>(sigiState).ItemModule.FirstOrDefault().Value;
                FavoriteVideos.Add(item);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        private string GetSigiStateFromHtml(string content)
        {
            var tokenStart = "window['SIGI_STATE']=";
            var tokenEnd = ";window['SIGI_RETRY']";
            var sigiState = "";
            int startIndex = content.IndexOf(tokenStart) + tokenStart.Length;
            if (startIndex > tokenStart.Length)
            {
                int endIndex = content.IndexOf(tokenEnd, startIndex);
                sigiState = content.Substring(startIndex, endIndex - startIndex);
            }
            return sigiState;
        }
    }
}
