using Microsoft.UI.Xaml;

namespace MyTikTokBackup.Desktop.Views
{
    public static class UIHelper
    {
        public static T GetDataContext<T>(object sender) where T : class
        {
            if(sender is T)
            {
                return sender as T;
            }
            var element = sender as FrameworkElement;
            return element.DataContext as T;
        }
    }
}
