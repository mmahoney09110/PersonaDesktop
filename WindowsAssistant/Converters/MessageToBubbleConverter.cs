using System;
using System.Globalization;
using System.Runtime;
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
            var message = (value as string)?.TrimStart();    // trim leading space
            if (message != null && message.StartsWith("You", StringComparison.OrdinalIgnoreCase))
            {
                return HorizontalAlignment.Right;            // User on left
            }
            return HorizontalAlignment.Left;               // AI on right
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class LoadingSpinnerVisibilityConverter : IValueConverter
    {
        private SettingsModel _settings = SettingsService.LoadSettings();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            _settings = SettingsService.LoadSettings();
            return (value as string)?.Trim() == _settings.AssistantName ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class LoadingTextVisibilityConverter : IValueConverter
    {
        private SettingsModel _settings = SettingsService.LoadSettings();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            _settings = SettingsService.LoadSettings();
            return (value as string)?.Trim() == _settings.AssistantName ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    public class InverseBoolToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b) ? !b : false;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }


}
