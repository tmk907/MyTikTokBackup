using System;
using System.Linq;
using System.Threading.Tasks;
using Flurl;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using MyTikTokBackup.Core.Models;
using MyTikTokBackup.Core.Services;
using MyTikTokBackup.Desktop.ViewModels;
using Windows.Media.Core;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MyTikTokBackup.Desktop.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LibraryPage : Page
    {
        public LibraryViewModel ViewModel { get; }

        public LibraryPage()
        {
            ViewModel = Ioc.Default.GetService<LibraryViewModel>();
            DataContext = ViewModel;
            this.InitializeComponent();
            Loaded += LibraryPage_Loaded;
            Unloaded += LibraryPage_Unloaded;
            //mediaPlayerElement.SetMediaPlayer(new Windows.Media.Playback.MediaPlayer());
            //mediaPlayerElement.MediaPlayer.AutoPlay = true;
            //mediaPlayerElement.MediaPlayer.IsLoopingEnabled = true;
            webview.Loaded += Webview_Loaded;
        }

        string hostName = "downloadedvideos.tiktok";
        string downloadsFolderPath;
        private bool isLoaded = false;

        private async void Webview_Loaded(object sender, RoutedEventArgs e)
        {
            if (isLoaded) return;
            await webview.EnsureCoreWebView2Async();
            downloadsFolderPath = Ioc.Default.GetService<IAppConfiguration>().DownloadsFolder;
            webview.CoreWebView2
                .SetVirtualHostNameToFolderMapping(hostName, downloadsFolderPath, CoreWebView2HostResourceAccessKind.Allow);
            isLoaded = true;
        }

        private async void LibraryPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.Init();
        }

        private async void LibraryPage_Unloaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.UnInit();
        }

        private void Videos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var video = (TikTokVideo)e.AddedItems.FirstOrDefault();
            if (video == null) return;
            var s1 = video.FilePath.Replace(downloadsFolderPath, "").Replace("\\", "");
            var videoUrl = Uri.EscapeDataString(s1);
            //string videoUrl = .Replace("#", "%23").Replace(" ", "%20"); ;
            var address = $"http://{hostName}/{videoUrl}";
            var a = new Uri(address);

            var html = @$"
<html>
	<body>
		<video id='videoElement' style='width:100%; height: 100%; background: black;' loop controls autoplay>
			<source id='videoSource' src='{address}'>
		</video>
	</body>
</html>
<style>
	body {{
		margin: 0px;
	}}
</style>";

            webview.NavigateToString(html);

            //var file = await StorageFile.GetFileFromPathAsync(video.FilePath);
            //var mediaSource = MediaSource.CreateFromStorageFile(file);
            ////var stream = File.OpenRead(video.FilePath);
            ////var mediaSource = MediaSource.CreateFromStream(stream.AsRandomAccessStream(), "video/mp4");
            //mediaPlayerElement.Source = mediaSource;
            //mediaPlayerElement.MediaPlayer.AutoPlay = true;
            //mediaPlayerElement.MediaPlayer.IsLoopingEnabled = true;
            ViewModel.ChangeVideo(video);
        }

        private void Flyout_Opened(object sender, object e)
        {
            var flyout = sender as Flyout;
            var content = flyout.Content as FrameworkElement;
            var video = content.DataContext as TikTokVideo;
            if (video is null)
            {
                System.Diagnostics.Debug.WriteLine($"Flyout_Opened DataContext null");
            }
            else
            {
                ViewModel.PrepareVideoCategories(video);
            }
        }

        private void Flyout_Closed(object sender, object e)
        {
            ViewModel.SaveVideoCategoriesCommand.Execute(null);
        }

        private async void mediaPlayerElement_Loaded(object sender, RoutedEventArgs e)
        {
            //try
            //{
            //    await Task.Delay(1000);
            //    mediaPlayerElement.SetMediaPlayer(ViewModel.MediaPlayer);
            //}
            //catch (Exception ex)
            //{

            //}
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string val;
                richEditBox.TextDocument.GetText(Microsoft.UI.Text.TextGetOptions.None, out val);
                webview.Source = new Uri(val);
            }
            catch (Exception ex)
            {
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            webview.CoreWebView2.Stop();
            webview = null;
            base.OnNavigatingFrom(e);
        }
    }
}
