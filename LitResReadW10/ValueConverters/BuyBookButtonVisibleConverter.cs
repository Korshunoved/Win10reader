using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public class BuyBookButtonVisibleConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
        {
			bool visible = value is bool && !( bool ) value;
			return visible ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
			throw new NotImplementedException();
		}
	}
}
