using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public class StringToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!(value is string)) return DependencyProperty.UnsetValue;
            string valueString = (string)value;

            double result = 0;
            double.TryParse(valueString, out result);
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            
            
		return DependencyProperty.UnsetValue;
		}
	}
}
