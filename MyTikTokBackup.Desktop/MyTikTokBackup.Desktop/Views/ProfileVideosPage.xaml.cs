using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32.SafeHandles;
using MyTikTokBackup.Core.Models;
using MyTikTokBackup.Core.Services;
using MyTikTokBackup.Desktop.ViewModels;
using Serilog;
using Windows.Foundation;
using Windows.Foundation.Collections;
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

            MediaPlayerElementContainer.Children.Add(MediaPlayerElement);
        }

        private void Play() => MediaPlayerElement.MediaPlayer?.Play();

        private void Pause() => MediaPlayerElement.MediaPlayer?.Pause();

        private void CleanUpMediaPlayer() => MediaPlayerElement.SetMediaPlayer(null!);

        private void ToggleTransportControls() => MediaPlayerElement.AreTransportControlsEnabled = !MediaPlayerElement.AreTransportControlsEnabled;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var args = e.Parameter as Dictionary<string, string>;
            VM.UserUniqueId = args["user"];
        }

        private async void ProfileVideosPage_Loaded(object sender, RoutedEventArgs e)
        {
            await VM.LoadPostedVideos();
        }

        private void Videos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var video = (VideoUI)e.AddedItems.FirstOrDefault();
            if (video == null) return;

            if (MediaPlayerElement.MediaPlayer is null) MediaPlayerElement.SetMediaPlayer(new());

            MediaPlayerElement.MediaPlayer.AutoPlay = true;
            MediaPlayerElement.MediaPlayer.IsLoopingEnabled = true;

            var fileStream = File.OpenRead(video.FilePath);
            MediaPlayerElement.MediaPlayer.Source = MediaSource.CreateFromStream(fileStream.AsRandomAccessStream(), "video/mp4");
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
            var session = MediaPlayerElement.MediaPlayer.PlaybackSession;
            if (session.PlaybackState == Microsoft.UI.Media.Playback.MediaPlaybackState.Playing)
            {
                MediaPlayerElement.MediaPlayer.Pause();
            }
            else
            {
                MediaPlayerElement.MediaPlayer.Play();
            }
            Log.Debug($"Video size width:{session.NaturalVideoWidth} height:{session.NaturalVideoHeight}");
            Log.Debug($"Container size width:{MediaPlayerElementContainer.ActualWidth} height:{MediaPlayerElementContainer.ActualHeight}");

            
        }
    }
}
