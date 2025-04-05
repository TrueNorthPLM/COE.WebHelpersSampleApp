using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace COE.WebHelpersSampleApp.Converters
{
    /// <summary>
    /// A value converter that converts a boolean value to a <see cref="Visibility"/> value.
    /// This converter is used in data binding to show or hide UI elements based on a boolean value.
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to a <see cref="Visibility"/> value.
        /// </summary>
        /// <param name="value">The boolean value to convert (true or false).</param>
        /// <param name="targetType">The type of the target property (should be <see cref="Visibility"/>).</param>
        /// <param name="parameter">An optional parameter that can be used for additional logic (not used in this case).</param>
        /// <param name="culture">The culture information (not used in this case).</param>
        /// <returns>
        /// <see cref="Visibility.Visible"/> if the value is true; otherwise, <see cref="Visibility.Collapsed"/>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when the value is not a boolean.</exception>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Ensure the input value is of type bool
            if (value is bool booleanValue)
            {
                // Return Visibility.Visible if true, otherwise Visibility.Collapsed
                return booleanValue ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                throw new ArgumentException("The value must be a boolean.", nameof(value));
            }
        }

        /// <summary>
        /// Converts a <see cref="Visibility"/> value back to a boolean value.
        /// </summary>
        /// <param name="value">The <see cref="Visibility"/> value to convert.</param>
        /// <param name="targetType">The type of the target property (should be boolean).</param>
        /// <param name="parameter">An optional parameter that can be used for additional logic (not used in this case).</param>
        /// <param name="culture">The culture information (not used in this case).</param>
        /// <returns>
        /// True if the <see cref="Visibility"/> is <see cref="Visibility.Visible"/>; otherwise, false.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when the value is not of type <see cref="Visibility"/>.</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Ensure the input value is of type Visibility
            if (value is Visibility visibility)
            {
                // Return true if Visibility is Visible, otherwise false
                return visibility == Visibility.Visible;
            }
            else
            {
                throw new ArgumentException("The value must be of type Visibility.", nameof(value));
            }
        }
    }
}