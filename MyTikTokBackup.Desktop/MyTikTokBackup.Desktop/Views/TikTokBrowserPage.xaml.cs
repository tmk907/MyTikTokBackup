using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Serilog;
using MyTikTokBackup.Core.TikTok;
using MyTikTokBackup.Desktop.ViewModels;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using MyTikTokBackup.Core.TikTok.Website;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MyTikTokBackup.Desktop.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TikTokBrowserPage : Page
    {
        public TikTokBrowserViewModel ViewModel { get; }

        public TikTokBrowserPage()
        {
            ViewModel = Ioc.Default.GetService<TikTokBrowserViewModel>();
            DataContext = ViewModel;
            this.InitializeComponent();
            webview.Loaded += Webview_Loaded;
        }

        private string mobileUserAgent = "Mozilla/5.0 (Linux; Android 8.0; Pixel 2 Build/OPD3.170816.012) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.93 Mobile Safari/537.36";
        // Pixel2
        // "Mozilla/5.0 (Linux; Android 8.0; Pixel 2 Build/OPD3.170816.012) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.93 Mobile Safari/537.36";
        private bool isLoaded = false;
        private async void Webview_Loaded(object sender, RoutedEventArgs e)
        {
            if (isLoaded) return;
            await webview.EnsureCoreWebView2Async();
            webview.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
            webview.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            webview.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            webview.CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
            webview.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

            //webview.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
            //webview.CoreWebView2.Settings.IsWebMessageEnabled = true;
            //webview.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            //webview.CoreWebView2.CookieManager.DeleteAllCookies();

            webview.Source = new Uri(ViewModel.StartAddress);
            isLoaded = true;
        }

        private async void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            Log.Information("WebMessageReceived");
            if (System.Text.Json.JsonSerializer.Deserialize<string>(args.WebMessageAsJson) == "favClicked")
            {
                await TryParseUserVideos();
            }
        }

        private async void CoreWebView2_DOMContentLoaded(CoreWebView2 sender, CoreWebView2DOMContentLoadedEventArgs args)
        {
            Log.Information("DOMContentLoaded");
            var notifyWhenLikedTabClickedJS = @"
            var tabs = document.evaluate( ""//*[@data-e2e='liked-tab']"", document, null, XPathResult.ANY_TYPE, null);
            var tab = tabs.iterateNext();
            tab.addEventListener('click', function(){ window.chrome.webview.postMessage('favClicked'); }, false);";
            await webview.CoreWebView2.ExecuteScriptAsync(notifyWhenLikedTabClickedJS);
        }

        private async void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            Log.Information("NavigationCompleted");
            await TryParseUserVideos();
        }

        private async Task TryParseUserVideos()
        {
            try
            {
                var getPreloadedPostedVideosJS = "window['SIGI_STATE'].ItemList['user-post']['list']";
                var videos = await FindVideos(getPreloadedPostedVideosJS);
                ViewModel.FetchPostedVideosVM.Videos
                    .AddRange(videos
                        .Select(x => Helper.ToItemInfo(x))
                        .Except(ViewModel.FetchPostedVideosVM.Videos));

                //var getPreloadedFavoriteVideosJS = "window['SIGI_STATE'].ItemList['user-favorite']['list']";
                //videos = await FindVideos(getPreloadedFavoriteVideosJS);
                //ViewModel.FetchFavoriteVideosVM.Videos
                //    .AddRange(videos
                //        .Select(x => _mapper.Map<ItemInfo>(x))
                //        .Except(ViewModel.FetchFavoriteVideosVM.Videos));
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        private async Task<List<PostedVideo>> FindVideos(string js)
        {
            var videos = new List<PostedVideo>();

            try
            {
                var idsJson = await webview.CoreWebView2.ExecuteScriptAsync(js);
                var ids = System.Text.Json.JsonSerializer.Deserialize<string[]>(idsJson);

                foreach (var id in ids)
                {
                    var getVideoJsonJS = $"window['SIGI_STATE'].ItemModule['{id}']";
                    var data = await webview.CoreWebView2.ExecuteScriptAsync(getVideoJsonJS);
                    var item = System.Text.Json.JsonSerializer.Deserialize<PostedVideo>(data);
                    videos.Add(item);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

            return videos;
        }

        private void CoreWebView2_WebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
        {
            if (ViewModel.UseMobileVersion)
            {
                args.Request.Headers.SetHeader("User-Agent", mobileUserAgent);
            }
        }

        private void CoreWebView2_HistoryChanged(CoreWebView2 sender, object args)
        {
            Log.Information($"History {sender.Source}");
            var url = webview.Source.ToString();
            var user = BookmarksViewModel.GetUserFromUrl(url);
            WeakReferenceMessenger.Default.Send(new UserChangedMessage(user));
            WeakReferenceMessenger.Default.Send(new AddressChangedMessage(user));
        }

        string lastUser = "";
        private List<ItemInfo> pageVideos = new List<ItemInfo>();

        private async void CoreWebView2_WebResourceResponseReceived(CoreWebView2 sender, CoreWebView2WebResourceResponseReceivedEventArgs args)
        {
            var uri = args.Request.Uri;

            //Log.Information($"Url {uri}");

            if (uri.Contains("tiktok.com/music/original-sound-"))
            {
                Log.Information($"Music {uri}");
            }

            if (uri.Contains("tiktok.com"))
            {
                var headers = GetHeaders(args.Request.Headers);
                ViewModel.FindFollowingVM.CookieHeader = headers
                    .FirstOrDefault(x => x.Name.ToLower() == "cookie")?.Value;

                ViewModel.FetchPostedVideosVM.Headers = headers.ToList();
                ViewModel.FetchFavoriteVideosVM.Headers = headers.ToList();
                //Log.Information($"Set Cookie {ViewModel.FindFollowingVM.CookieHeader}");
            }

            if (uri.Contains("tiktok.com/@") && args.Response.StatusCode == 200)
            {
                Log.Information("Html content");
                Log.Information(uri);
                try
                {
                    var contentStream = await args.Response.GetContentAsync();
                    using (StreamReader sr = new StreamReader(contentStream.AsStreamForRead()))
                    {
                        var text = await sr.ReadToEndAsync();
                        var indexOfVideoData = text.IndexOf("\"videoData\":");
                        var prev = text.Substring(indexOfVideoData, 100);
                        if (indexOfVideoData > 0)
                        {
                            var firstBracket = text.IndexOf('[', indexOfVideoData);
                            int count = 1;
                            int lastBracket = 0;
                            for(int i=firstBracket+1; i < text.Length; i++)
                            {
                                if(text[i] == '[')
                                {
                                    count++;
                                }
                                if(text[i] == ']')
                                {
                                    count--;
                                }
                                if (count == 0)
                                {
                                    lastBracket = i;
                                    break;
                                }
                            }
                            if (lastBracket != 0)
                            {
                                var videoData = text.Substring(firstBracket, lastBracket - firstBracket + 1);
                                pageVideos = JsonConvert.DeserializeObject<List<ItemInfo>>(videoData);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }

            if (uri.Contains("api/post/item_list"))
            {
                Log.Information($"Posted {uri}");
                Log.Information($"Posted {string.Join(" ", args.Request.Headers.Select(x => $"{x.Key}: {x.Value}"))}");

                ViewModel.FetchPostedVideosVM.Headers = GetHeaders(args.Request.Headers);
                ViewModel.FetchPostedVideosVM.Videos.AddRange(pageVideos.Except(ViewModel.FetchPostedVideosVM.Videos));
                pageVideos.Clear();
                var videos = await GetVideosFromResponseAsync(args.Response);
                ViewModel.FetchPostedVideosVM.Videos.AddRange(videos.Except(ViewModel.FetchPostedVideosVM.Videos));
            }
            else if (uri.Contains("api/favorite/item_list"))
            {
                Log.Information($"Favorites {uri}");
                Log.Information($"Favorites {string.Join(" ", args.Request.Headers.Select(x => $"{x.Key}: {x.Value}"))}");
                    
                ViewModel.FetchFavoriteVideosVM.Headers = GetHeaders(args.Request.Headers);
                ViewModel.FetchFavoriteVideosVM.Videos.AddRange(pageVideos.Except(ViewModel.FetchFavoriteVideosVM.Videos));
                pageVideos.Clear();
                var videos = await GetVideosFromResponseAsync(args.Response);
                ViewModel.FetchFavoriteVideosVM.Videos.AddRange(videos.Except(ViewModel.FetchFavoriteVideosVM.Videos));
            }
        }

        private async Task<List<ItemInfo>> GetVideosFromResponseAsync(CoreWebView2WebResourceResponseView response)
        {
            var content = await DeserializeContentAsync<VideosReponse>(response);
            if (content?.ItemList == null) return new List<ItemInfo>();
            return content.ItemList;
        }

        private async Task<T> DeserializeContentAsync<T>(CoreWebView2WebResourceResponseView response)
        {
            try
            {
                var contentStream = await response.GetContentAsync();
                if (contentStream != null)
                {
                    return DeserializeFromStream<T>(contentStream);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return default(T);
        }

        private T DeserializeFromStream<T>(IRandomAccessStream stream)
        {
            JsonSerializer serializer = new JsonSerializer();
            using (StreamReader sr = new StreamReader(stream.AsStreamForRead()))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                T result = serializer.Deserialize<T>(reader);
                return result;
            }
        }

        private IEnumerable<Header> GetHeaders(CoreWebView2HttpRequestHeaders headers)
        {
            return headers
                .Where(x => x.Key.ToLower() == "cookie" || x.Key.ToLower() == "referer")
                .Select(x => new MyTikTokBackup.Core.TikTok.Header
                {
                    Name = x.Key,
                    Value = x.Value
                }).ToList();
        }

        private string HeadersToString(CoreWebView2HttpRequestHeaders headers)
        {
            return string.Join(' ', headers.Select(x => x.Key));
        }

        private string HeadersToString(CoreWebView2HttpResponseHeaders headers)
        {
            return string.Join(' ', headers.Select(x => x.Key));
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (webview.CanGoBack)
            {
                webview.GoBack();
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (webview.CanGoForward)
            {
                webview.GoForward();
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            webview.Reload();
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            SearchUser(args.QueryText);
        }

        private void SearchUser(string user)
        {
            var url = $"https://www.tiktok.com/search?q={user}";
            webview.Source = new Uri(url);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = e.AddedItems?.FirstOrDefault();
            if (selected is Bookmark bookmark)
            {
                Log.Information($"Selected {bookmark.User}");
                ViewModel.BookmarksVM.SelectedBookmark = bookmark;
                try
                {
                    webview.Source = new Uri(bookmark.Url);
                    ViewModel.StartAddress = bookmark.Url;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }

        private void FollowingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = e.AddedItems?.FirstOrDefault() as string;
            if (!string.IsNullOrEmpty(selected))
            {
                var url = $"https://www.tiktok.com/@{selected}";
                webview.Source = new Uri(url);
            }
        }

        private void ClearCookies_Click(object sender, RoutedEventArgs e)
        {
            webview.CoreWebView2.CookieManager.DeleteAllCookies();
        }
    }
}
