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
using MyTikTokBackup.Core.Services;
using MyTikTokBackup.Desktop.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;

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
        public ProfileVideosPage()
        {
            this.InitializeComponent();
            VM = Ioc.Default.GetService<ProfileVideosViewModel>();
            DataContext = VM;
            this.InitializeComponent();
            this.Loaded += ProfileVideosPage_Loaded;
            webView.Loaded += WebView_Loaded;
            this.Unloaded += ProfileVideosPage_Unloaded;
        }

        private void ProfileVideosPage_Unloaded(object sender, RoutedEventArgs e)
        {
            var html = @$"
<html>
	<body>
	</body>
</html>";
            webView.NavigateToString(html);
        }

        private bool loaded = false;
        string downloadsFolderPath;
        string hostName = "downloadedvideos.tiktok";

        private async void WebView_Loaded(object sender, RoutedEventArgs e)
        {
            if (loaded) return;
            await webView.EnsureCoreWebView2Async();
            downloadsFolderPath = Ioc.Default.GetService<IAppConfiguration>().DownloadsFolder;
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(hostName, downloadsFolderPath, CoreWebView2HostResourceAccessKind.Allow);
            loaded = true;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var args = e.Parameter as Dictionary<string, string>;
            VM.UserUniqueId = args["user"];
        }

        private async void ProfileVideosPage_Loaded(object sender, RoutedEventArgs e)
        {
            await VM.LoadAllVideos();
        }

        private void PostedVideos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var video = (VideoUI)e.AddedItems.FirstOrDefault();
            if (video == null) return;
            var address = FilePathToAddress(video.FilePath, "Posted");
            VideoChanged(address);
        }


        private void LikedVideos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var video = (VideoUI)e.AddedItems.FirstOrDefault();
            if (video == null) return;
            var address = FilePathToAddress(video.FilePath, "Favorite");
            VideoChanged(address);
        }

        private string FilePathToAddress(string filePath, string type)
        {
            var fileName = Path.GetFileName(filePath);
            var videoUrl = Uri.EscapeDataString(fileName);
            //string videoUrl = .Replace("#", "%23").Replace(" ", "%20"); ;
            var address = $"http://{hostName}/{VM.UserUniqueId}/{type}/{videoUrl}";
            return address;
        }

        private void VideoChanged(string address)
        {
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

            webView.NavigateToString(html);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            likedLV.Visibility = Visibility.Collapsed;
            postedLV.Visibility = Visibility.Visible;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            postedLV.Visibility = Visibility.Collapsed;
            likedLV.Visibility = Visibility.Visible;
        }
    }
}
