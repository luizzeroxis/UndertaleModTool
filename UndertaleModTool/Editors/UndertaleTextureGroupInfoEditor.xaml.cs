﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using UndertaleModLib;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleTextureGroupInfoEditor.xaml
    /// </summary>
    public partial class UndertaleTextureGroupInfoEditor : DataUserControl
    {
        public UndertaleTextureGroupInfoEditor()
        {
            InitializeComponent();
        }
    }

    public class IsGM2023Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not UndertaleData data)
                return Visibility.Visible;

            return data.IsVersionAtLeast(2023, 1) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
