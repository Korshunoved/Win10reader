using System;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using LitResReadW10;

namespace LitRes.ValueConverters
{
	public class EnableBrushConverter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, string language)
		{
			return value is bool && (bool) value ? new SolidColorBrush( Colors.Gray ) : App.Current.Resources["LitResForegroundBrush"];
		}

		public object ConvertBack( object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
		
	}
}
