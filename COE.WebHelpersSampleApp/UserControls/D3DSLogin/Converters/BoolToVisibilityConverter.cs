using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace COE.WebHelpersSampleApp.UserControls.D3DSLogin.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = (bool)value;
            bool inverse = parameter?.ToString()?.ToLower() == "inverse";

            if (inverse)
                isVisible = !isVisible;

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
