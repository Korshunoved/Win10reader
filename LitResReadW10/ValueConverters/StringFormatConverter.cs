using System;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public class StringFormatConverter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, string language)
        {
			if( parameter != null )
			{
				string formatString = parameter.ToString();

				if( !string.IsNullOrEmpty( formatString ) )
				{
					return string.Format(formatString, value );
				}
			}

			return value.ToString();
		}

		public object ConvertBack( object value, Type targetType, object parameter, string language)
        {
			throw new NotImplementedException();
		}
	}
}
