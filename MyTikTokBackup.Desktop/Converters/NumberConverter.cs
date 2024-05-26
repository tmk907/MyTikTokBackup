using System;
using Microsoft.UI.Xaml.Data;

namespace MyTikTokBackup.Desktop.Converters
{
    public class NumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var number = (long)value;
            if(number > 1000000)
            {
                return $"{(number / 100000.0).ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}M";
            }
            else if (number > 10000)
            {
                return $"{(number / 1000.0).ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}K";
            }
            else
            {
                return $"{number}";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
