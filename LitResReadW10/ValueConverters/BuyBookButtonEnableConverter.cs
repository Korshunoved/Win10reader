using System;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public class BuyBookButtonEnableConverter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, string language)
		{
			return value is bool && !( bool ) value;
		}

		public object ConvertBack( object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
