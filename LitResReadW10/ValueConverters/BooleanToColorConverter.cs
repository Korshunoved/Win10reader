using System;
using System.Globalization;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace LitRes.ValueConverters
{
	public class BooleanToColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is bool && ( bool ) value)
			{
				return new SolidColorBrush( Colors.White );
			}
			else
			{
				return new SolidColorBrush( Colors.Gray );
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
