using Microsoft.UI.Xaml;

namespace MyTikTokBackup.Desktop.Views
{
    public static class UIHelper
    {
        public static T GetFromDataContext<T>(object sender) where T : class
        {
            var element = sender as FrameworkElement;
            return element.DataContext as T;
        }
    }
}
