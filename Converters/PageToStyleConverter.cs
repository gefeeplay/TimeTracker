using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace TimeTracker.Converters;

public class PageToStyleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var currentPage = value as string;
        var buttonPage = parameter as string;

        if (currentPage == buttonPage)
            return Application.Current.Resources["ActiveNavigationButtonStyle"];

        return Application.Current.Resources["NavigationButtonStyle"];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
