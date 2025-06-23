using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PersonaDesk.Converter
{
    public class MessageToBubbleColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var message = value as string;
            if (message.StartsWith("You"))
                return Brushes.LightBlue; // User
            return Brushes.LightGray; // AI
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class MessageToBubbleAlignConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var message = value as string;
            if (message.StartsWith(">"))
                return HorizontalAlignment.Right; // User
            return HorizontalAlignment.Left; // AI
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
