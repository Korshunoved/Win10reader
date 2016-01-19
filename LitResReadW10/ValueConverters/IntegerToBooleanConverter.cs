using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public class IntegerToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!(value is int)) return DependencyProperty.UnsetValue;
            int valueInt = (int)value;
            if (parameter == null) return valueInt != 0;

            int parameterValue = 0;

            if (parameter is int)
            {
                parameterValue = (int)parameter;
            }
            else if (parameter is string)
            {
                int.TryParse((string)parameter, out parameterValue);
            }

            return valueInt == parameterValue;
		}

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (!(value is int)) return value;
            int valueInt = (int)value;
            if ( parameter == null ) return DependencyProperty.UnsetValue;

            if (parameter is int) return (int)parameter;

            if (parameter is string)
            {
                int parameterValue = 0;

                if (int.TryParse((string)parameter, out parameterValue))
                {
                    return parameterValue;
                }
            }
            
		return DependencyProperty.UnsetValue;
		}
	}
}
