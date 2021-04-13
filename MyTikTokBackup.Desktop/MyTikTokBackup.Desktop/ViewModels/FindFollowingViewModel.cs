using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using MvvmHelpers;
using Newtonsoft.Json;
using Serilog;
using MyTikTokBackup.Core.TikTok;

namespace MyTikTokBackup.Desktop.ViewModels
{
    public class FindFollowingViewModel : ObservableObject
    {
        private HttpClient _httpClient;
        private ApiClient _apiClient;

        public string CookieHeader { get; set; }

        public FindFollowingViewModel()
        {
            _apiClient = new ApiClient();
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            _httpClient = new HttpClient(handler);
            FindFollowingCommand = new AsyncRelayCommand((token) => FindFollowing(token), () => !FindFollowingCommand.IsRunning);
        }

        public AsyncRelayCommand FindFollowingCommand { get; }

        public ObservableRangeCollection<string> Users { get; } = new ObservableRangeCollection<string>();

        private async Task FindFollowing(CancellationToken cancellationToken)
        {
            var sessionId = _apiClient.GetSessionIdFromCookies(CookieHeader);
            _apiClient.SetSessionIdSs(sessionId);
            var following = await _apiClient.GetMyFollowing(cancellationToken);
            Users.ReplaceRange(following.Select(x => x.User.UniqueId).OrderBy(x => x));
        }

        private async Task FindFollowing2(CancellationToken cancellationToken)
        {
            Log.Information("");
            if (string.IsNullOrEmpty(CookieHeader)) return;
            try
            {
                int i = 0;
                while (i < 20)
                {
                    i++;
                    var url = "https://m.tiktok.com/api/following/item_list/?aid=1988&count=20&cursor=0&pullType=1&level=1";
                    var msg = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
                    msg.Headers.Add("cookie", CookieHeader);

                    var response = await _httpClient.SendAsync(msg, cancellationToken);
                    FollowingResponse result;
                    JsonSerializer serializer = new JsonSerializer();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (StreamReader sr = new StreamReader(stream))
                    using (JsonReader reader = new JsonTextReader(sr))
                    {
                        result = serializer.Deserialize<FollowingResponse>(reader);
                    }

                    var users = result.ItemList.Select(x => x.Author.UniqueId).Distinct();
                    var ordered = users.Concat(Users).Distinct().OrderBy(x => x).ToList();
                    Users.ReplaceRange(ordered);
                }
            }
            catch(Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        public void CancelFindFollowing()
        {
            Log.Information("");
            FindFollowingCommand.Cancel();
        }
    }
}
