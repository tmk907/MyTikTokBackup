using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using MyTikTokBackup.Core.TikTok;
using MyTikTokBackup.Desktop.ViewModels;
using Serilog;
using Windows.Media.Core;
using Windows.Media.Playback;
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
            var player = MediaPlayerElement.MediaPlayer;
            if (player?.Source == null) return;

            var isPlaying = player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;
            if (isPlaying)
            {
                Pause();
            }
            else
            {
                Play();
            }
            ShowPlayPauseIndicator(isPlaying: !isPlaying);
        }

        private void ShowPlayPauseIndicator(bool isPlaying)
        {
            PlayPauseIcon.Symbol = isPlaying ? Symbol.Play : Symbol.Pause;
            PlayPauseIndicatorStoryboard.Stop();
            PlayPauseIndicatorStoryboard.Begin();
        }

        private void CleanUpMediaPlayer()
        {
            _progressTimer?.Stop();
            var player = mediaPlayerElement.MediaPlayer;
            if (player is null) return;
            player.MediaOpened -= MediaPlayer_MediaOpened;
            player.Pause();
            MediaPlayerElement.SetMediaPlayer(null!);
        }

        public static Visibility BoolToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

        private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            // Raised on a background thread.
            DispatcherQueue.TryEnqueue(UpdateVideoHostSize);
        }

        private void MediaPlayerElementContainer_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateVideoHostSize();

        /// <summary>Fits the video surface (and its overlays) to the video's aspect ratio inside the available area.</summary>
        private void UpdateVideoHostSize()
        {
            var availableWidth = MediaPlayerElementContainer.ActualWidth;
            var availableHeight = MediaPlayerElementContainer.ActualHeight;
            if (availableWidth <= 0 || availableHeight <= 0) return;

            var session = MediaPlayerElement.MediaPlayer?.PlaybackSession;
            double videoWidth = session?.NaturalVideoWidth ?? 0;
            double videoHeight = session?.NaturalVideoHeight ?? 0;
            if (videoWidth <= 0 || videoHeight <= 0)
            {
                videoWidth = VM.SelectedVideo?.Video.Width ?? 0;
                videoHeight = VM.SelectedVideo?.Video.Height ?? 0;
            }

            if (videoWidth <= 0 || videoHeight <= 0)
            {
                VideoHost.Width = availableWidth;
                VideoHost.Height = availableHeight;
                return;
            }

            var aspect = videoWidth / videoHeight;
            var width = Math.Min(availableWidth, availableHeight * aspect);
            VideoHost.Width = width;
            VideoHost.Height = width / aspect;
        }

        private void ProgressTimer_Tick(DispatcherQueueTimer sender, object args)
        {
            var session = MediaPlayerElement.MediaPlayer?.PlaybackSession;
            var duration = session?.NaturalDuration.TotalMilliseconds ?? 0;
            ProgressFillScale.ScaleX = duration > 0
                ? Math.Clamp(session.Position.TotalMilliseconds / duration, 0, 1)
                : 0;
        }

        private void SeekTo(double x)
        {
            var session = MediaPlayerElement.MediaPlayer?.PlaybackSession;
            if (session is null || session.NaturalDuration <= TimeSpan.Zero || ProgressArea.ActualWidth <= 0) return;

            var fraction = Math.Clamp(x / ProgressArea.ActualWidth, 0, 1);
            session.Position = session.NaturalDuration * fraction;
            ProgressFillScale.ScaleX = fraction;
        }

        private void ProgressArea_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isSeeking = ProgressArea.CapturePointer(e.Pointer);
            SeekTo(e.GetCurrentPoint(ProgressArea).Position.X);
            e.Handled = true;
        }

        private void ProgressArea_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isSeeking) return;
            SeekTo(e.GetCurrentPoint(ProgressArea).Position.X);
            e.Handled = true;
        }

        private void ProgressArea_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isSeeking = false;
            ProgressArea.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }

        private void ProgressArea_PointerCaptureLost(object sender, PointerRoutedEventArgs e) => _isSeeking = false;

        // Keep clicks on the progress bar from also toggling play/pause on the video.
        private void ProgressArea_Tapped(object sender, TappedRoutedEventArgs e) => e.Handled = true;

        private void VideoHost_Tapped(object sender, TappedRoutedEventArgs e) => TogglePlayPause();

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
                MediaPlayerElement.MediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            }

            MediaPlayerElement.MediaPlayer.AutoPlay = true;
            MediaPlayerElement.MediaPlayer.IsLoopingEnabled = true;

            if (_progressTimer is null)
            {
                _progressTimer = DispatcherQueue.CreateTimer();
                _progressTimer.Interval = TimeSpan.FromMilliseconds(50);
                _progressTimer.Tick += ProgressTimer_Tick;
            }
            _progressTimer.Start();

            await VM.ShowPostedVideos();
        }

        private void ProfileVideosPage_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanUpMediaPlayer();
        }

        private DispatcherQueueTimer _progressTimer;
        private bool _isSeeking;
        private bool loaded = false;
        private string mobileUserAgent = "Mozilla/5.0 (Linux; Android 8.0; Pixel 2 Build/OPD3.170816.012) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.93 Mobile Safari/537.36";

        private async void WebView_Loaded(object sender, RoutedEventArgs e)
        {
            if (loaded) return;
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            if (VM.SelectedVideo != null)
            {
                if (VM.SelectedVideo.Url == null)
                {
                    Log.Warning("SelectedVideo Url is null. VideoId: {0}", VM.SelectedVideo.Video.VideoId);
                }
                else
                {
                    webView.CoreWebView2.Navigate(VM.SelectedVideo.Url);
                }
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
            ProgressFillScale.ScaleX = 0;
            UpdateVideoHostSize();

            if (!string.IsNullOrEmpty(video.FilePath))
            {
                //var fileStream = File.OpenRead(video.FilePath);

                MediaPlayerElement.MediaPlayer.AutoPlay = videoPivot.SelectedIndex == 0;
                var file = await StorageFile.GetFileFromPathAsync(video.FilePath);
                MediaPlayerElement.MediaPlayer.Source = MediaSource.CreateFromStorageFile(file);
            }
            if (videoPivot.SelectedIndex == 1 && loaded)
            {
                if (VM.SelectedVideo.Url == null)
                {
                    Log.Warning("SelectedVideo Url is null. VideoId: {0}", VM.SelectedVideo.Video.VideoId);
                }
                else
                {
                    webView.CoreWebView2.Navigate(VM.SelectedVideo.Url);
                }
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
                if (VM.SelectedVideo.Url == null)
                {
                    Log.Warning("SelectedVideo Url is null. VideoId: {0}", VM.SelectedVideo.Video.VideoId);
                }
                else
                {
                    webView.CoreWebView2.Navigate(VM.SelectedVideo.Url);
                }
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

        private async void CategoriesFilter_FilterChanged(object sender, EventArgs e)
        {
            await VM.RefreshVideos();
        }

        private async void CategoriesFilter_AddRequested(object sender, string name)
        {
            await VM.AddCategoryAsync(name);
        }

        private async void CategoriesFilter_RenameRequested(object sender, Controls.CategoryRenameRequest e)
        {
            await VM.RenameCategoryAsync(e.Category, e.NewName);
        }

        private async void CategoriesFilter_DeleteRequested(object sender, Controls.CategoryItem category)
        {
            await VM.DeleteCategoryAsync(category);
        }

        private async void VideoCategories_CategoryToggled(object sender, Controls.CategoryToggledEventArgs e)
        {
            await VM.SetVideoCategoryAsync(e.Category, e.IsChecked);
        }
    }
}
