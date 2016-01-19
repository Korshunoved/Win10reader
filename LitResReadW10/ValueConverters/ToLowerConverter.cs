using System;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public class ToLowerConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
        {
			string valueStringLower = value.ToString().ToLower();

			if( valueStringLower.Contains( "nokia" ) )
			{
				return value;
			}

			return valueStringLower;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
			throw new NotImplementedException();
		}
	}
}
