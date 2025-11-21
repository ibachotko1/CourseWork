using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SmartGreenhouse.App
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string colors)
            {
                var colorParts = colors.Split('|');
                if (colorParts.Length == 2)
                {
                    return boolValue ?
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorParts[0])) :
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorParts[1]));
                }
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string texts)
            {
                var textParts = texts.Split('|');
                if (textParts.Length == 2)
                {
                    return boolValue ? textParts[1] : textParts[0];
                }
            }
            return value?.ToString() ?? "Нет";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (parameter?.ToString() == "reverse")
                    return boolValue ? "Нет" : "Да";
                return boolValue ? "Да" : "Нет";
            }
            return "Нет данных";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}