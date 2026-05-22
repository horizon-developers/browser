using Microsoft.UI.Xaml.Data;

namespace Horizon.Core
{
    /// <summary>
    /// Converts between Boolean and Visibility values. 
    /// </summary>
    internal partial class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts true values to Visibility.Visible and false values to
        /// Visibility.Collapsed, or the reverse if the parameter is "Reverse".
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        /// <summary>
        /// Converts Visibility.Visible values to true and Visibility.Collapsed 
        /// values to false, or the reverse if the parameter is "Reverse".
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibilityValue)
            {
                bool isReverse = parameter is string paramStr && paramStr.Equals("Reverse", StringComparison.OrdinalIgnoreCase);

                if (visibilityValue == Visibility.Visible)
                {
                    // If Visible: returns false if reversed, true otherwise
                    return !isReverse;
                }
                // If Collapsed/Hidden: returns true if reversed, false otherwise
                return isReverse;
            }

            return false;
        }
    }
}
