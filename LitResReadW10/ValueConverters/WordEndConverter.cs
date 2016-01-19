using System;
using System.Collections;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public class WordEndConverter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, string language)
		{
			var parameters = string.Empty;
			var enumerable = value as IList;

			if( enumerable != null && parameter is string )
			{
				parameters = ( string ) parameter;

				if( enumerable.Count > 1 )
				{
					return parameters.Replace( "|", string.Empty );
				}
				else
				{
					return parameters.Split( new Char[] { '|' } )[0];
				}
			}

			return string.Empty;
		}

		public object ConvertBack( object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
