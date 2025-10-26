using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Diagnostics;

namespace Avalonia.Desktop.Helpers;

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
/// <summary>
/// Converts bool to one of two strings
/// Strings separated by ;
/// </summary>
internal sealed class BoolToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var strings = ((string)parameter).Split(',');
        return (bool)value ? strings[0] : strings[1];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ThrowHelper.ThrowNotSupportedException<object>("ConvertBack method for BoolToStringConverter is not implemented.");
    }
}

/// <summary>
/// Reverses bool value
/// </summary>
internal sealed class ReverseBoolConverter : IValueConverter
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
internal sealed class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var pars = ((string)parameter).Split(';');

        return (bool)value ? new SolidColorBrush(Color.Parse(pars[0])) : new SolidColorBrush(Color.Parse(pars[1]));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ThrowHelper.ThrowNotSupportedException<object>("ConvertBack method for BoolToColorConverter is not implemented.");
    }
}

/// <summary>
/// Converts path to image to Bitmap
/// </summary>
internal sealed class ImagePathToBitmapConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!File.Exists((string)value))
        {
            return null;
        }

        return new Bitmap((string)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ThrowHelper.ThrowNotSupportedException<object>("ConvertBack method for ImagePathToBitmapConverter is not implemented.");
    }
}
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).

