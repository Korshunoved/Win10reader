using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public class SecondAuthorVisibleConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
        {
			if (value is Models.Book.TitleInfo.AuthorInfo[] && ((Models.Book.TitleInfo.AuthorInfo[])value).Length > 1)
			{
				return Visibility.Visible;
			}
			else
			{
				return Visibility.Collapsed;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
			throw new NotImplementedException();
		}
	}
}