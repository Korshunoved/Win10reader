using System;
using System.Globalization;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public class PluralTextConverter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, string language)
		{
			if( value is int && parameter is string )
			{
				var valueInt = System.Convert.ToInt32( value );
				var forms = (( string ) parameter).Split( new Char[] { '|' } );

				if( forms.Length == 3 )
				{
					var mod10 = valueInt % 10;
					var mod100 = valueInt % 100;

					if( mod100 > 10 && mod100 < 20 )
					{
						return forms[2];
					}

					if( mod10 > 1 && mod10 < 5 )
					{
						return forms[1];
					}

					if( mod10 == 1 )
					{
						return forms[0];
					}

					return forms[2];

					//return mod100 > 4 && mod100 < 21 || mod10 > 4 || mod10 == 0 ? forms[2] : (mod10 == 1 ? forms[0] : forms[1]);
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
