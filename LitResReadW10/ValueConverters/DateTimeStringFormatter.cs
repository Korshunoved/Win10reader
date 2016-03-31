using System;
using System.Globalization;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public class DateTimeStringFormatter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, string language)
		{
            return new StringFormatConverter().Convert(System.Convert.ToDateTime(value), targetType, parameter, language);
		}

		public object ConvertBack( object value, Type targetType, object parameter, string language)
        {
			throw new NotImplementedException();
		}
	}
}
