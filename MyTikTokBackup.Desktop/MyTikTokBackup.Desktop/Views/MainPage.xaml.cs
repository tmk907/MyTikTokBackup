using System.IO;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using MyTikTokBackup.Desktop.ViewModels;
using Windows.Storage;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MyTikTokBackup.Desktop.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; }

        public MainPage()
        {
            ViewModel = Ioc.Default.GetService<MainViewModel>();
            this.InitializeComponent();
            //Asd();
        }

        public async void Asd()
        {
            try
            {
                string fname = @"Assets\Fonts\fa-solid-900.otf";
                StorageFolder InstallationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                StorageFile file = await InstallationFolder.GetFileAsync(fname);
                if (File.Exists(file.Path))
                {
                    var contents = File.ReadAllBytes(file.Path);
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
