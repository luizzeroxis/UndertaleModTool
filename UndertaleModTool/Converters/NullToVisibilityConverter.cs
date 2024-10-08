﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UndertaleModTool
{
    public sealed class NullToVisibilityConverter : IValueConverter
    {
        public Visibility nullValue { get; set; }
        public Visibility notNullValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? nullValue : notNullValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
