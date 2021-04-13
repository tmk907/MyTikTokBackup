using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyTikTokBackup.Desktop.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MyTikTokBackup.Desktop.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DownloadsPage : Page
    {
        public DownloadsViewModel ViewModel { get; }
        public DownloadsPage()
        {
            ViewModel = Ioc.Default.GetService<DownloadsViewModel>();
            this.InitializeComponent();

            Loaded += DownloadsPage_Loaded;
            Unloaded += DownloadsPage_Unloaded;
        }

        private async void DownloadsPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.Init();
        }

        private async void DownloadsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.UnInit();
        }
    }
}
