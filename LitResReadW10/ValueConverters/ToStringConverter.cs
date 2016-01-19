using System;
using System.Linq;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public class ToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
            return System.Convert.ToString(value);
        }

		public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
			throw new NotImplementedException();
		}
	}
}
