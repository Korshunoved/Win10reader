using System;
using Windows.UI.Xaml.Data;
using LitRes.Models;

namespace LitRes.ValueConverters
{
	public class StringCharTrimConverter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, string language)
		{
			var valueString = value as string;
			var charParameter = parameter as string;

			if( !string.IsNullOrEmpty( valueString ) && !string.IsNullOrEmpty( charParameter ) && !valueString.StartsWith( "@" ) )
			{
				return valueString.Split( new Char[] { charParameter[0] } )[0];
			}

			return valueString;
		}

		public object ConvertBack( object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
