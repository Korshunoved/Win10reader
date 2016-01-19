using System;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public class CoverConverter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, string language)
		{
			var valueString = value as string;

			if( string.IsNullOrEmpty( valueString ) )
			{
				return "/Assets/Stub.png";
			}

			return value;
		}

		public object ConvertBack( object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
