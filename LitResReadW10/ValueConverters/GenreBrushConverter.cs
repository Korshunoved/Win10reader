using System;
using Windows.UI.Xaml.Data;
using LitResReadW10;

namespace LitRes.ValueConverters
{
	public class GenreBrushConverter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, string language)
		{

			return value.ToString() == "-1" ? App.Current.Resources["LitResOrangeBrush"] : App.Current.Resources["LitResForegroundBrush"];
		}

		public object ConvertBack( object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
