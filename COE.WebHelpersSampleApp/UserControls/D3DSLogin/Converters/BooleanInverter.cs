using System.Globalization;
using System.Windows.Data;

namespace COE.WebHelpersSampleApp.UserControls.D3DSLogin.Converters
{
    /// <summary>
    /// A value converter that inverts a boolean value. 
    /// It supports both nullable and non-nullable booleans.
    /// </summary>
    public class BooleanInverter : IValueConverter
    {
        /// <summary>
        /// Converts the input value (a boolean or nullable boolean) to its inverted value.
        /// </summary>
        /// <param name="value">The value to be converted (expected to be a boolean or nullable boolean).</param>
        /// <param name="targetType">The type to convert to (usually the type of the target property in the binding).</param>
        /// <param name="parameter">An optional parameter for customizing the conversion (not used here).</param>
        /// <param name="culture">The culture information (not used here).</param>
        /// <returns>
        /// The inverted boolean value. If the input value is null or not a boolean, false is returned by default.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // First, check if the value is a nullable boolean (bool?).
            if (value is bool?)
            {
                // Cast the value to a nullable boolean (bool?).
                bool? booleanValue = (bool?)value;

                // If the boolean value is not null, invert it. 
                // If the value is null, return false by default.
                return booleanValue.HasValue ? !booleanValue.Value : false;
            }

            // If the value is not a nullable boolean, return false by default.
            return false;
        }

        /// <summary>
        /// Converts the inverted value back to the original boolean value.
        /// This method supports two-way data binding scenarios in WPF.
        /// </summary>
        /// <param name="value">The value to convert back to the original form (expected to be a boolean or nullable boolean).</param>
        /// <param name="targetType">The target type (not used here, but typically the type of the source property in the binding).</param>
        /// <param name="parameter">An optional parameter for customizing the conversion (not used here).</param>
        /// <param name="culture">The culture information (not used here).</param>
        /// <returns>
        /// The inverted boolean value. If the value is null or not a boolean, false is returned by default.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Apply the same logic for ConvertBack to maintain consistency in two-way binding.
            if (value is bool?)
            {
                // Cast the value to a nullable boolean (bool?).
                bool? booleanValue = (bool?)value;

                // If the boolean value is not null, invert it.
                // If the value is null, return false by default.
                return booleanValue.HasValue ? !booleanValue.Value : false;
            }

            // If the value is not a nullable boolean, return false by default.
            return false;
        }
    }
}
