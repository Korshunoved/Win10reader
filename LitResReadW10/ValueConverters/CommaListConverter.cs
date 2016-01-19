using System;
using System.Linq;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public class CommaListConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
        {
		    if (value == null) return string.Empty;
			var elements = value.ToString().Split( new Char[] { ',' } ).ToList();
			elements.ForEach( element => element = element.Trim() );

			return string.Join( ", ", elements );
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
			throw new NotImplementedException();
		}
	}
}
