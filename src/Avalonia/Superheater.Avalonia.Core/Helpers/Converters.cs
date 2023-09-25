using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;

namespace Superheater.Avalonia.Core.Helpers
{
    /// <summary>
    /// Converts bool to one of two strings
    /// Strings separated by ;
    /// </summary>
    internal class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var strings = ((string)parameter).Split(",");
            return (bool)value ? strings[0] : strings[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack method for BoolToStringConverter is not implemented.");
        }
    }

    /// <summary>
    /// Reverses bool value
    /// </summary>
    internal class ReverseBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
    }

    /// <summary>
    /// Converts bool to color from a string of two colors separated by ;
    /// </summary>
    internal class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var pars = ((string)parameter).Split(";");

            return (bool)value ? new SolidColorBrush(Color.Parse(pars[0])) : new SolidColorBrush(Color.Parse(pars[1]));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack method for BoolToColorConverter is not implemented.");
        }
    }

    /// <summary>
    /// Converts path to image to Bitmap
    /// </summary>
    internal class ImagePathToBitmapConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new Bitmap((string)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ConvertBack method for ImagePathToBitmapConverter is not implemented.");
        }
    }
}
