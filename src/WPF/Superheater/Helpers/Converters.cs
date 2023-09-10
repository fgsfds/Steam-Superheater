﻿using System;
using System.Windows.Data;

namespace SteamFD.Helpers
{
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var strings = ((string)parameter).Split(",");
            return (bool)value ? strings[0] : strings[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack method for BoolToStringConverter is not implemented.");
        }
    }

    public class ReverseBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(bool)value;
        }
    }
}