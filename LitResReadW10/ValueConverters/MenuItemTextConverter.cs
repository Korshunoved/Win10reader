using System;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public class MenuItemTextConverter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, string language)
        {
			string text = "";
			string type = "(null)";
			string annotation = string.Empty;

			if( value != null )
			{
				Type valueType = value.GetType();

				type = valueType.Name;

                if (valueType == typeof(Models.Book))
                {
                    var book = value as Models.Book;
                    if (!book.IsMyBook) text = "удалить";
                    else if (book.IsMyBook) text = "удалить файл";
                }
			}

			if( parameter is string )
			{
				annotation = (string) parameter;

                if (annotation == "inverse")
                {
                }
			}
            return text;
		}

		public object ConvertBack( object value, Type targetType, object parameter, string language)
        {
            return null;
		}
	}
}
