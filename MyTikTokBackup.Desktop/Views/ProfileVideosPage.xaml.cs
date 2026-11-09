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
using Microsoft.UI.Xaml.Media.Imaging;
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
            PreviewKeyDown += ProfileVideosPage_PreviewKeyDown;
        }

        private static bool IsKeyPressed(VirtualKey key) =>
            Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);

        private void ProfileVideosPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (IsKeyPressed(VirtualKey.Control) && e.Key == VirtualKey.Space)
            {
                TogglePlayPause();
            }
        }

        /// <summary>
        /// Up/Down always move to the previous/next video, wherever focus is. Handled in the tunneling
        /// (preview) phase so the focused control (e.g. the ListView) doesn't also act on the key.
        /// </summary>
        private void ProfileVideosPage_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key is not (VirtualKey.Up or VirtualKey.Down)) return;
            if (IsKeyPressed(VirtualKey.Menu) || IsKeyPressed(VirtualKey.Shift)) return;

            e.Handled = true;
            MoveSelection(e.Key == VirtualKey.Down ? 1 : -1);
        }

        /// <summary>Selects the video <paramref name="offset"/> positions away from the current one, if it exists.</summary>
        private void MoveSelection(int offset)
        {
            if (VM.Videos.Count == 0) return;

            var current = VM.Videos.IndexOf(VM.SelectedVideo);
            // Nothing selected (or the selected video was filtered out): Down starts at the first item.
            var target = current < 0
                ? (offset > 0 ? 0 : -1)
                : current + offset;
            if (target < 0 || target >= VM.Videos.Count) return;

            // Selecting in the ListView raises SelectionChanged, which loads the video.
            VideosList.SelectedIndex = target;
            VideosList.ScrollIntoView(VM.Videos[target]);
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

        public static Visibility InvertBoolToVisibility(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

        private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            // Raised on a background thread.
            var width = sender.PlaybackSession.NaturalVideoWidth;
            var height = sender.PlaybackSession.NaturalVideoHeight;
            DispatcherQueue.TryEnqueue(() =>
            {
                if (VM.SelectedVideo?.IsImagePost == true) return;
                SetMediaSize(width, height);
            });
        }

        private void MediaPlayerElementContainer_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateVideoHostSize();

        private void SetMediaSize(double width, double height)
        {
            _mediaWidth = width;
            _mediaHeight = height;
            UpdateVideoHostSize();
        }

        /// <summary>Fits the video surface (and its overlays) to the media's aspect ratio inside the available area.</summary>
        private void UpdateVideoHostSize()
        {
            var availableWidth = MediaPlayerElementContainer.ActualWidth;
            var availableHeight = MediaPlayerElementContainer.ActualHeight;
            if (availableWidth <= 0 || availableHeight <= 0) return;

            var videoWidth = _mediaWidth;
            var videoHeight = _mediaHeight;

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

            VM.Activate();
            await VM.ShowPostedVideos();
        }

        private void ProfileVideosPage_Unloaded(object sender, RoutedEventArgs e)
        {
            VM.Deactivate();
            CleanUpMediaPlayer();
        }

        private DispatcherQueueTimer _progressTimer;
        private bool _isSeeking;
        private double _mediaWidth;
        private double _mediaHeight;
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

        /// <summary>
        /// Loads photo post images through StorageFile (file names often contain '#', which breaks file URIs).
        /// </summary>
        private async Task ShowImagesAsync(VideoUI video)
        {
            var images = new List<BitmapImage>();
            foreach (var path in video.ImagePaths)
            {
                try
                {
                    var file = await StorageFile.GetFileFromPathAsync(path);
                    using var stream = await file.OpenAsync(FileAccessMode.Read);
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(stream);
                    images.Add(bitmap);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to load image {Path}", path);
                }
            }

            // Selection may have changed while images were loading.
            if (VM.SelectedVideo != video) return;

            if (images.Count == 0) return;

            // Reset the index before changing the page count. NumberOfPages is never set to 0:
            // PipsPager fails natively when the count drops below the selected index.
            ImagesPipsPager.SelectedPageIndex = 0;
            ImagesPipsPager.NumberOfPages = images.Count;
            ImagesFlipView.ItemsSource = images;
            SetMediaSize(images[0].PixelWidth, images[0].PixelHeight);
        }

        // FlipView <-> PipsPager are synced in code rather than with a TwoWay binding,
        // so an emptied FlipView (SelectedIndex -1) never reaches PipsPager.
        private void ImagesFlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var index = ImagesFlipView.SelectedIndex;
            if (index >= 0 && index < ImagesPipsPager.NumberOfPages && ImagesPipsPager.SelectedPageIndex != index)
            {
                ImagesPipsPager.SelectedPageIndex = index;
            }
        }

        private void ImagesPipsPager_SelectedIndexChanged(PipsPager sender, PipsPagerSelectedIndexChangedEventArgs args)
        {
            var index = sender.SelectedPageIndex;
            if (index >= 0 && index < ImagesFlipView.Items.Count && ImagesFlipView.SelectedIndex != index)
            {
                ImagesFlipView.SelectedIndex = index;
            }
        }

        private async Task SelectVideo(VideoUI video)
        {
            VM.SelectedVideo = video;
            ProgressFillScale.ScaleX = 0;
            ImagesFlipView.ItemsSource = null;
            SetMediaSize(video.Video.Width, video.Video.Height);

            if (video.IsImagePost)
            {
                MediaPlayerElement.MediaPlayer.Source = null;
                await ShowImagesAsync(video);
            }
            else if (!string.IsNullOrEmpty(video.FilePath))
            {
                MediaPlayerElement.MediaPlayer.AutoPlay = videoPivot.SelectedIndex == 0;
                var file = await StorageFile.GetFileFromPathAsync(video.FilePath);
                MediaPlayerElement.MediaPlayer.Source = MediaSource.CreateFromStorageFile(file);
            }
            else
            {
                MediaPlayerElement.MediaPlayer.Source = null;
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

        private async void VideoCategories_AiLabelRejected(object sender, Controls.CategoryItem category)
        {
            await VM.RejectAiLabelAsync(category);
        }
    }
}
