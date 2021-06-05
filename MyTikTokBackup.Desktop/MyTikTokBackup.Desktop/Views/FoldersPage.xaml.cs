using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MyTikTokBackup.Desktop.Services;
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
    public sealed partial class FoldersPage : Page
    {
        public FoldersViewModel VM { get; }
        public FoldersPage()
        {
            VM = Ioc.Default.GetService<FoldersViewModel>();
            DataContext = VM;
            this.InitializeComponent();
            this.Loaded += FoldersPage_Loaded;
        }

        private async void FoldersPage_Loaded(object sender, RoutedEventArgs e)
        {
            await VM.LoadAsync();
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var nav = Ioc.Default.GetService<INavigationService>();
            var uniqueId = UIHelper.GetFromDataContext<UserFolder>(e.ClickedItem).UniqueId;
            nav.GoToNew(nameof(ProfileVideosViewModel), new Dictionary<string, string>() { { "user", uniqueId } });
        }
    }
}
