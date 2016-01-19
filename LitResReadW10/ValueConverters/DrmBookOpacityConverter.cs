using System;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public class DrmBookOpacityConverter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, string language)
		{
            return (value is bool && (bool)value == true) ? 0.5 : 1.0;
		}

		public object ConvertBack( object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
		
	}
}
