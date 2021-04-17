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
using Serilog.Core;

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

        private string mobileUserAgent = "Mozilla/5.0 (Linux; Android 8.0; Pixel 2 Build/OPD3.170816.012) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4324.150 Mobile Safari/537.36";
        private bool isLoaded = false;
        private async void Webview_Loaded(object sender, RoutedEventArgs e)
        {
            if (isLoaded) return;
            await webview.EnsureCoreWebView2Async();
            //TODO
            //webview.CoreWebView2.Settings.UserAgent = mobileUserAgent;
            webview.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
            webview.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            webview.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            webview.CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
            webview.Source = new Uri(ViewModel.StartAddress);
            isLoaded = true;
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
                ViewModel.FindFollowingVM.CookieHeader = GetHeaders(args.Request.Headers)
                    .FirstOrDefault(x => x.Name.ToLower() == "cookie")?.Value;
                Log.Information($"Set Cookie {ViewModel.FindFollowingVM.CookieHeader}");
            }

            if (uri.Contains("api/post/item_list"))
            {
                if (Flurl.Url.Parse(uri).QueryParams.TryGetFirst("cursor", out var cursor))
                {
                    ViewModel.FetchPostedVideosVM.VideosUrl= uri;
                    var headers = HeadersToString(args.Request.Headers);
                    Log.Information($"Posted {uri}");
                    Log.Information($"Posted {string.Join(" ", args.Request.Headers.Select(x => $"{x.Key}: {x.Value}"))}");
                    ViewModel.FetchPostedVideosVM.Headers = GetHeaders(args.Request.Headers);

                    var videos = await GetVideosFromResponseAsync(args.Response);
                    ViewModel.FetchPostedVideosVM.Videos.AddRange(videos);
                }
            }
            else if (uri.Contains("api/favorite/item_list"))
            {
                if (Flurl.Url.Parse(uri).QueryParams.TryGetFirst("cursor", out var cursor))
                {
                    ViewModel.FetchFavoriteVideosVM.VideosUrl = uri;
                    Log.Information($"Favorites {uri}");
                    Log.Information($"Favorites {string.Join(" ", args.Request.Headers.Select(x => $"{x.Key}: {x.Value}"))}");
                    ViewModel.FetchFavoriteVideosVM.Headers = GetHeaders(args.Request.Headers);

                    var videos = await GetVideosFromResponseAsync(args.Response);
                    ViewModel.FetchFavoriteVideosVM.Videos.AddRange(videos);
                }
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
                var content = await response.GetContentAsync();
                if (content != null)
                {
                    JsonSerializer serializer = new JsonSerializer();
                    using (StreamReader sr = new StreamReader(content.AsStreamForRead()))
                    using (JsonReader reader = new JsonTextReader(sr))
                    {
                        T result = serializer.Deserialize<T>(reader);
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return default(T);
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
    }
}
