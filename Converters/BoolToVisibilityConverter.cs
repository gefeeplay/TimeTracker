using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace TimeTracker.Converters;
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (bool)value ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return (Visibility)value == Visibility.Visible;
    }
}