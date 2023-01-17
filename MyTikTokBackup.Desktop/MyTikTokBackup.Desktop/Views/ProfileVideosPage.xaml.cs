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
using Windows.Media.Core;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;

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

        public MediaPlayerElement MediaPlayerElement => mediaPlayerElement;

        public ProfileVideosPage()
        {
            this.InitializeComponent();
            VM = Ioc.Default.GetService<ProfileVideosViewModel>();
            DataContext = VM;
            this.InitializeComponent();
            this.Loaded += ProfileVideosPage_Loaded;
            this.Unloaded += ProfileVideosPage_Unloaded;
            webView.Loaded += WebView_Loaded;
            AddHandler(KeyDownEvent, new KeyEventHandler(ProfileVideosPage_KeyDown), true);
        }

        private static bool IsCtrlKeyPressed()
        {
            var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
            return ctrlState == CoreVirtualKeyStates.Down;
        }

        private async void ProfileVideosPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            try
            {
                if (IsCtrlKeyPressed())
                {
                    switch (e.Key)
                    {
                        case Windows.System.VirtualKey.Up:
                            await SelectPreviousVideo();
                            break;
                        case Windows.System.VirtualKey.Down:
                            await SelectNextVideo();
                            break;
                        case Windows.System.VirtualKey.Space:
                            TogglePlayPause();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        private void Play() => MediaPlayerElement.MediaPlayer?.Play();

        private void Pause() => MediaPlayerElement.MediaPlayer?.Pause();

        private void TogglePlayPause()
        {
            if (MediaPlayerElement.MediaPlayer?.CurrentState == Windows.Media.Playback.MediaPlayerState.Playing)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        private void CleanUpMediaPlayer()
        {
            mediaPlayerElement.MediaPlayer.Pause();
            MediaPlayerElement.SetMediaPlayer(null!);
        }

        private void ShowTransportControls(bool show) => MediaPlayerElement.AreTransportControlsEnabled = show;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var args = e.Parameter as Dictionary<string, string>;
            VM.UserUniqueId = args["user"];
        }

        private async void ProfileVideosPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (MediaPlayerElement.MediaPlayer is null)
            {
                MediaPlayerElement.SetMediaPlayer(new());
            }

            MediaPlayerElement.MediaPlayer.AutoPlay = true;
            MediaPlayerElement.MediaPlayer.IsLoopingEnabled = true;

            await VM.ShowPostedVideos();
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

        private async void Videos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var video = (VideoUI)e.AddedItems.FirstOrDefault();
            if (video == null) return;

            await SelectVideo(video);
        }

        private async Task SelectPreviousVideo()
        {
            var index = VM.Videos.IndexOf(VM.SelectedVideo);
            if (index == 0) return;
            index--;
            await SelectVideo(VM.Videos[index]);
        }

        private async Task SelectNextVideo()
        {
            var index = VM.Videos.IndexOf(VM.SelectedVideo);
            if (index == VM.Videos.Count - 1) return;
            index++;
            await SelectVideo(VM.Videos[index]);
        }

        private async Task SelectVideo(VideoUI video)
        {
            VM.SelectedVideo = video;

            if (!string.IsNullOrEmpty(video.FilePath))
            {
                //var fileStream = File.OpenRead(video.FilePath);

                MediaPlayerElement.MediaPlayer.AutoPlay = videoPivot.SelectedIndex == 0;
                var file = await StorageFile.GetFileFromPathAsync(video.FilePath);
                MediaPlayerElement.MediaPlayer.Source = MediaSource.CreateFromStorageFile(file);
            }
            if (videoPivot.SelectedIndex == 1 && loaded)
            {
                webView.CoreWebView2.Navigate(VM.SelectedVideo.Url);
            }
        }

        private async void Posted_Click(object sender, RoutedEventArgs e)
        {
            await VM.ShowPostedVideos();
        }

        private async void Liked_Click(object sender, RoutedEventArgs e)
        {
            await VM.ShowLikedVideos();
        }

        private async void Bookmarked_Click(object sender, RoutedEventArgs e)
        {
            await VM.ShowBookmarkedVideos();
        }

        private void MediaPlayerElementContainer_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Log.Information("MediaPlayerElementContainer_Tapped");
            var position = e.GetPosition(MediaPlayerElementContainer);
            if (position.Y < MediaPlayerElementContainer.ActualHeight - 90)
            {
                var session = MediaPlayerElement.MediaPlayer.PlaybackSession;
                if (session.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing)
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
            Log.Information("MediaPlayerElementContainer_PointerEntered");
            ShowTransportControls(true);
        }

        private void MediaPlayerElementContainer_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Log.Information("MediaPlayerElementContainer_PointerExited");
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

        private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            await VM.RefreshVideos();
        }

        private async  void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            await VM.RefreshVideos();
        }

        private async void CategoriesFilterControl_CategorySelectionChanged(object sender, Controls.CategorySelectionChangedEventArgs e)
        {
            VM.SelectedCategories.ReplaceRange(e.SelectedCategories.Select(x => x.Name));
            await VM.RefreshVideos();
        }
    }
}
