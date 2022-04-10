using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using MyTikTokBackup.Core.Models;
using MyTikTokBackup.Core.Services;
using MyTikTokBackup.Core.TikTok;
using MyTikTokBackup.Core.TikTok.UserData;
using MyTikTokBackup.Core.TikTok.Website;
using MyTikTokBackup.WindowsUWP.Helpers;
using Serilog;

namespace MyTikTokBackup.Desktop.ViewModels
{
    public class UserDataViewModel : ObservableObject
    {
        private readonly IAppConfiguration _appConfiguration;
        private readonly IDownloadsManager _downloadsManager;
        private readonly IFlurlClient _flurlClient;
        private readonly IFlurlClient _flurlClient2;

        private const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.110 Safari/537.36 OPR/82.0.4227.43";

        public UserDataViewModel(IAppConfiguration appConfiguration, IDownloadsManager downloadsManager)
        {
            _flurlClient = new FlurlClient()
                .WithAutoRedirect(false)
                .AllowHttpStatus(new[] { System.Net.HttpStatusCode.Redirect, System.Net.HttpStatusCode.Moved });

            _flurlClient2 = new FlurlClient()
                .WithHeader("User-Agent", userAgent)
                .WithHeader("Connection", "keep-alive")
                .WithHeader("Accept", "*/*")
                .WithHeader("Accept-Encoding", "gzip");
            _appConfiguration = appConfiguration;
            _downloadsManager = downloadsManager;
            ImportUserDataFileCommand = new AsyncRelayCommand(ImportUserDataFile);
            DownloadFavoriteVideosCommand = new AsyncRelayCommand(DownloadFavoriteVideos);
        }

        private UserData _userData;
        public ObservableCollection<PostedVideo> FavoriteVideos { get; } = new ObservableCollection<PostedVideo>();
        public ObservableCollection<string> Urls { get; } = new ObservableCollection<string>();

        public IAsyncRelayCommand ImportUserDataFileCommand { get; }
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

        private int downloadedCount;
        public int DownloadedCount
        {
            get { return downloadedCount; }
            set { SetProperty(ref downloadedCount, value); }
        }

        private string userName;
        public string UserName
        {
            get { return userName; }
            set { SetProperty(ref userName, value); }
        }

        private RechargeableCancellationTokenSource _cts = new RechargeableCancellationTokenSource();

        private async Task ImportUserDataFile()
        {
            var file = await FilePickerHelper.PickFile(new[] {".json"});
            if (file != null)
            {
                _cts.Cancel();

                Urls.Clear();
                FavoriteVideos.Clear();
                DownloadedCount = 0;

                try
                {
                    using FileStream openStream = File.OpenRead(file.Path);
                    _userData = await JsonSerializer.DeserializeAsync<UserData>(openStream);
                }
                catch (Exception ex)
                {
                    Log.Error("Can't deserialize user data {0}", ex);
                    return;
                }

                try
                {
                    FavoriteCount = _userData.Activity.FavoriteVideos.FavoriteVideoList.Count;
                    LikedCount = _userData.Activity.LikeList.ItemFavoriteList.Count;
                    HistoryCount = _userData.Activity.VideoBrowsingHistory.VideoList.Count;
                    UserName = $"@{_userData.Profile.ProfileInformation.ProfileMap.UserName}";

                    Log.Information("{0} FavoriteCount {1}", nameof(UserDataViewModel), FavoriteCount);

                    await GetUrlsAfterRedirects(_userData.Activity.FavoriteVideos.FavoriteVideoList.Select(x => x.Link).ToList(), _cts.Token);
                    Log.Information("{0} Found {1} Urls", nameof(UserDataViewModel), Urls.Count);

                    var urls = ExcludeAlreadyDownloadedVideos(Urls);
                    Log.Information("{0} Excluded Already Downloaded Videos {1}", nameof(UserDataViewModel), Urls.Count);
                    await FindFavoriteVideos(urls, _cts.Token);
                    Log.Information("{0} Found Favorite Videos", nameof(UserDataViewModel));
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }

        private List<string> ExcludeAlreadyDownloadedVideos(IEnumerable<string> urls)
        {
            var folderPath = Path.Combine(_appConfiguration.DownloadsFolder, UserName, DownloadType.Bookmarks.ToString());
            var files = Directory.GetFiles(folderPath);
            var regexIdFile = new Regex(@"\[(\d+)\]", RegexOptions.Compiled);
            var regexIdUrl = new Regex(@"video\/(\d+)", RegexOptions.Compiled);
            //https://www.tiktok.com/@username/video/6935994246793006341

            var idToUrl = new Dictionary<string, string>();
            foreach(var url in urls)
            {
                var match = regexIdUrl.Match(url);
                if(TryGetCapture(match, out var id))
                {
                    idToUrl.Add(id, url);
                }
            }

            foreach (var file in files)
            {
                var match = regexIdFile.Match(Path.GetFileName(file));
                if (TryGetCapture(match, out var videoId) && idToUrl.ContainsKey(videoId))
                {
                    idToUrl.Remove(videoId);
                    DownloadedCount++;
                }
            }
            return idToUrl.Select(kv => kv.Value).ToList();
        }

        private bool TryGetCapture(Match match, out string data)
        {
            data = null;
            if (match.Success)
            {
                data = match.Groups.Values.LastOrDefault()?.Value;
            }
            return data != null;
        }


        private async Task FindFavoriteVideos(List<string> urls, CancellationToken token)
        {
            FavoriteVideos.Clear();
            var a = urls.ToList();

            foreach (var chunk in urls.Chunk(4))
            {
                var tasks = chunk.Select(x => GetFavoriteVideoFromUrl(x, token)).ToList();
                await Task.WhenAll(tasks);
                await Task.Delay(1000);
            }
        }

        private async Task DownloadFavoriteVideos()
        {
            var type = DownloadType.Bookmarks;
            await _downloadsManager.QueueVideos(UserName, type, FavoriteVideos.Select(x => Helper.ToItemInfo(x)).ToList());
        }

        private async Task GetFavoriteVideoFromUrl(string url, CancellationToken token)
        {
            try
            {
                var content = await _flurlClient2.Request(url).GetStringAsync();
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



        private async Task<IEnumerable<string>> GetUrlsAfterRedirects(List<string> urls, CancellationToken token)
        {          
            Urls.Clear();

            foreach (var chunk in urls.Chunk(10))
            {
                if (token.IsCancellationRequested) break;
                var tasks = chunk.Select(x => GetFinalUrl(x, token)).ToList();
                await Task.WhenAll(tasks);
                await Task.Delay(1000);
            }
            Log.Information("{0} Found all urls", nameof(UserDataViewModel));

            return Urls;
        }

        private async Task GetFinalUrl(string url, CancellationToken token)
        {
            try
            {
                if (url.Contains("tiktokv.com/share"))
                {
                    var response = await _flurlClient.Request(url).GetAsync(token);
                    response.Headers.TryGetFirst("Location", out url);
                }
                if (url.Contains("tiktok.com/share"))
                {
                    var response = await $"https://www.tiktok.com/oembed?url={url}".GetJsonAsync<TikTokOembed>();
                    //var id = url.Replace("https://www.tiktok.com/share/video/", "").Replace("/", "");
                    //url = $"{response.AuthorName}/{id}";
                    if (response.Html is not null)
                    {
                        var start = response.Html.IndexOf(@"cite=""") + @"cite=""".Length;
                        var end = response.Html.IndexOf(@"""", start);
                        url = response.Html.Substring(start, end - start);
                        if (url.Contains("/@"))
                        {
                            Log.Information($"Url found {url}");
                            Urls.Add(url);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }

    public class RechargeableCancellationTokenSource : IDisposable
    {
        private CancellationTokenSource state = new CancellationTokenSource();

        public CancellationTokenSource State => state;

        public CancellationToken Token => State.Token;

        public bool IsCancellationRequested => State.IsCancellationRequested;

        public void Cancel()
        {
            this.state?.Cancel();
            this.state?.Dispose();
            this.state = new CancellationTokenSource();
        }

        public void Dispose()
        {
            this.state?.Dispose();
            this.state = null;
        }
    }
}
