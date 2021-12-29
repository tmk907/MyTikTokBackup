using System;
using Microsoft.UI.Xaml;

namespace MyTikTokBackup.Desktop.Converters
{
    public class NumberToVisibilityConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int number = (int)value;
            return number > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
