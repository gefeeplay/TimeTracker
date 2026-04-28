using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using TimeTracker.ViewModels;

namespace TimeTracker.Converters;

public class PeriodToStyleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int current && parameter is string targetStr)
        {
            if (int.TryParse(targetStr, out int target))
            {
                return current == target
                    ? Application.Current.Resources["ActivePeriodButtonStyle"]
                    : Application.Current.Resources["InactivePeriodButtonStyle"];
            }
        }

        return Application.Current.Resources["InactivePeriodButtonStyle"];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}