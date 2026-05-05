using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace TimeTracker.Converters;

public class BoolToGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (bool)value ? "\uE96D" : "\uE970"; // hide / show
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) 
        => throw new NotImplementedException();
}
