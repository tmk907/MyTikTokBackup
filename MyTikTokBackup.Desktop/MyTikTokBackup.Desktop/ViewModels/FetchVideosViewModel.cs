using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using MvvmHelpers;
using Newtonsoft.Json;
using Serilog;
using MyTikTokBackup.Core.Models;
using MyTikTokBackup.Core.Services;
using MyTikTokBackup.Core.TikTok;
using MyTikTokBackup.Desktop.Services;

namespace MyTikTokBackup.Desktop.ViewModels
{
    public abstract class FetchVideosViewModel : ObservableRecipient, IRecipient<UserChangedMessage>
    {
        private readonly INavigationService _navigationService;
        private readonly IDownloadsManager _downloadsManager;

        public IEnumerable<Header> Headers { get; set; }
        public string VideosUrl { get; set; }

        private HttpClient _httpClient;
        protected DownloadType type = DownloadType.Other;
        
        public FetchVideosViewModel(INavigationService navigationService, IDownloadsManager downloadsManager)
        {
            _navigationService = navigationService;
            _downloadsManager = downloadsManager;
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            _httpClient = new HttpClient(handler);
            FetchVideosCommand = new AsyncRelayCommand((token) => FetchVideos(token), () => !FetchVideosCommand.IsRunning);
            DownloadVideosCommand = new RelayCommand(() => AddVideosToDownloadQueue(), () => !FetchVideosCommand.IsRunning);
            IsActive = true;
        }

        private string user;
        public string User
        {
            get { return user; }
            set { SetProperty(ref user, value); }
        }
        
        public ObservableRangeCollection<ItemInfo> Videos { get; } = new ObservableRangeCollection<ItemInfo>();
        public AsyncRelayCommand FetchVideosCommand { get; }
        public RelayCommand DownloadVideosCommand { get; }


        private int maxItemsToFetch;
        public int MaxItemsToFetch
        {
            get { return maxItemsToFetch; }
            set { SetProperty(ref maxItemsToFetch, value); }
        }

        public void CancelFetchingVideos()
        {
            SerilogHelper.LogInfo("");
            FetchVideosCommand.Cancel();
        }

        public void Receive(UserChangedMessage message)
        {
            User = message.User;
            VideosUrl = "";
            Videos.Clear();
        }

        private async Task FetchVideos(CancellationToken cancellationToken)
        {
            SerilogHelper.LogInfo("");
            var url = VideosUrl;
            if (!string.IsNullOrEmpty(url))
            {
                SerilogHelper.LogInfo("");
                Videos.Clear();
                url = url.SetQueryParam("cursor", "0");
                bool hasMore = true;
                try
                {
                    while (hasMore && CanFetchMoreItems)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            SerilogHelper.LogInfo("Cancellation requested");
                            break;
                        }

                        var msg = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
                        foreach (var header in Headers)
                        {
                            msg.Headers.Add(header.Name, header.Value);
                        }
                        var response = await _httpClient.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                        response.EnsureSuccessStatusCode();
                        VideosReponse result;
                        JsonSerializer serializer = new JsonSerializer();
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (StreamReader sr = new StreamReader(stream))
                        using (JsonReader reader = new JsonTextReader(sr))
                        {
                            result = serializer.Deserialize<VideosReponse>(reader);
                        }

                        //var result = await url.GetJsonAsync<VideosReponse>(cancellationToken);
                        if (result.ItemList is not null) Videos.AddRange(result.ItemList.Take(ItemsToTake));
                        hasMore = result.HasMore;
                        url = url.SetQueryParam("cursor", result.Cursor);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }

        private bool CanFetchMoreItems => Videos.Count < MaxItemsToFetch || MaxItemsToFetch <= 0;

        private int ItemsToTake => MaxItemsToFetch <= 0 ? 1000 : MaxItemsToFetch - Videos.Count;

        protected IEnumerable<ItemInfo> GetItemsWithHeaders()
        {
            var headers = Headers.ToList();
            var items = Videos;
            foreach (var item in items)
            {
                item.Headers = headers;
            }
            return items;
        }

        private void AddVideosToDownloadQueue()
        {
            if (Videos.Count == 0) return;
            AddToDownloadQueue(User, type, GetItemsWithHeaders());
        }

        protected void AddToDownloadQueue(string user, DownloadType type, IEnumerable<ItemInfo> items)
        {
            _downloadsManager.QueueVideos(user, type, items);
            _navigationService.GoToNew(nameof(DownloadsViewModel));
        }
    }

    public class FetchPostedVideosViewModel : FetchVideosViewModel
    {
        public FetchPostedVideosViewModel(INavigationService navigationService, IDownloadsManager downloadsManager)
            : base(navigationService, downloadsManager)
        {
            type = DownloadType.Posted;
        }
    }

    public class FetchFavoriteVideosViewModel: FetchVideosViewModel
    {
        public FetchFavoriteVideosViewModel(INavigationService navigationService, IDownloadsManager downloadsManager)
            : base(navigationService, downloadsManager)
        {
            type = DownloadType.Favorite;
        }
    }
}
