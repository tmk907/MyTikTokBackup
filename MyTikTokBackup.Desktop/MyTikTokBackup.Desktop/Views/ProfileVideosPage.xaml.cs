using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using MyTikTokBackup.Core.TikTok;
using MyTikTokBackup.Desktop.ViewModels;
using Serilog;
using MediaSource = Microsoft.UI.Media.Core.MediaSource;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MyTikTokBackup.Desktop.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ProfileVideosPage : Page
    {
        public ProfileVideosViewModel VM { get; }

        public MediaPlayerElement MediaPlayerElement { get; } = new() 
        { 
            AreTransportControlsEnabled = false
        };

        public ProfileVideosPage()
        {
            this.InitializeComponent();
            VM = Ioc.Default.GetService<ProfileVideosViewModel>();
            DataContext = VM;
            this.InitializeComponent();
            this.Loaded += ProfileVideosPage_Loaded;
            this.Unloaded += ProfileVideosPage_Unloaded;
            webView.Loaded += WebView_Loaded;

            MediaPlayerElementContainer.Children.Add(MediaPlayerElement);
        }

        private void Play() => MediaPlayerElement.MediaPlayer?.Play();

        private void Pause() => MediaPlayerElement.MediaPlayer?.Pause();

        private void CleanUpMediaPlayer() => MediaPlayerElement.SetMediaPlayer(null!);

        private void ShowTransportControls(bool show) => MediaPlayerElement.AreTransportControlsEnabled = show;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var args = e.Parameter as Dictionary<string, string>;
            VM.UserUniqueId = args["user"];
        }

        private async void ProfileVideosPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (MediaPlayerElement.MediaPlayer is null) MediaPlayerElement.SetMediaPlayer(new());
            MediaPlayerElement.MediaPlayer.AutoPlay = true;
            MediaPlayerElement.MediaPlayer.IsLoopingEnabled = true;

            await VM.LoadPostedVideos();
        }

        private void ProfileVideosPage_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanUpMediaPlayer();
        }

        private bool loaded = false;
        private string mobileUserAgent = "Mozilla/5.0 (Linux; Android 8.0; Pixel 2 Build/OPD3.170816.012) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.93 Mobile Safari/537.36";

        private async void WebView_Loaded(object sender, RoutedEventArgs e)
        {
            if (loaded) return;
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            if (VM.SelectedVideo != null)
            {
                webView.CoreWebView2.Navigate(VM.SelectedVideo.Url);
            }
            loaded = true;
        }

        private void CoreWebView2_WebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
        {
            args.Request.Headers.SetHeader("User-Agent", mobileUserAgent);
        }

        private void Videos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var video = (VideoUI)e.AddedItems.FirstOrDefault();
            if (video == null) return;

            VM.SelectedVideo = video;

            if (!string.IsNullOrEmpty(video.FilePath))
            {
                var fileStream = File.OpenRead(video.FilePath);

                MediaPlayerElement.MediaPlayer.AutoPlay = videoPivot.SelectedIndex == 0;
                MediaPlayerElement.MediaPlayer.Source = MediaSource.CreateFromStream(fileStream.AsRandomAccessStream(), "video/mp4");
            }
            if (videoPivot.SelectedIndex == 1 && loaded)
            {
                webView.CoreWebView2.Navigate(VM.SelectedVideo.Url);
            }
        }

        private async void Posted_Click(object sender, RoutedEventArgs e)
        {
            await VM.LoadPostedVideos();
        }

        private async void Liked_Click(object sender, RoutedEventArgs e)
        {
            await VM.LoadLikedVideos();
        }

        private async void Bookmarked_Click(object sender, RoutedEventArgs e)
        {
            await VM.LoadBookmarkedVideos();
        }

        private void MediaPlayerElementContainer_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var position = e.GetPosition(MediaPlayerElementContainer);
            if (position.Y < MediaPlayerElementContainer.ActualHeight - 90)
            {
                var session = MediaPlayerElement.MediaPlayer.PlaybackSession;
                if (session.PlaybackState == Microsoft.UI.Media.Playback.MediaPlaybackState.Playing)
                {
                    Pause();
                }
                else
                {
                    Play();
                }
            }
        }

        private void MediaPlayerElementContainer_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ShowTransportControls(true);
        }

        private void MediaPlayerElementContainer_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ShowTransportControls(false);
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(videoPivot.SelectedIndex == 0)
            {
                Play();
            }
            else
            {
                Pause();
            }

            if (!loaded) return;
            if (videoPivot.SelectedIndex == 1)
            {
                webView.CoreWebView2.Navigate(VM.SelectedVideo.Url);
            }
            else
            {
                var html = @$"
<html>
	<body>
	</body>
</html>";
                webView.NavigateToString(html);
            }
        }

        private async Task ShowEmbedVideo(string videoUrl)
        {
            var response = await $"https://www.tiktok.com/oembed?url={videoUrl}".GetJsonAsync<TikTokOembed>();
            if (response.Html is not null)
            {
                var html = @$"
<html>
	<body>
{response.Html}
	</body>
</html>";
                webView.NavigateToString(html);
            }
        }
    }
}
