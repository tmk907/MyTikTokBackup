using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using MyTikTokBackup.Desktop.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MyTikTokBackup.Desktop.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UserDataPage : Page
    {
        public UserDataViewModel ViewModel { get; }

        public UserDataPage()
        {
            ViewModel = Ioc.Default.GetService<UserDataViewModel>();
            this.InitializeComponent();
        }
    }
}
