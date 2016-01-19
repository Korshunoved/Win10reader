using System;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public class AuthorToFullStringConverter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, string language)
		{
			if( value is Models.Person )
			{
				var author = (Models.Person) value;

				return author.FirstName + " " +
						author.LastName;
			}

			if( value is Models.Person[] )
			{
				int index;
				int.TryParse( (string) parameter, out index );

				var authors = ((Models.Person[]) value);

				if( authors.Length > index )
				{
					return authors[index].FirstName + " " +
							authors[index].LastName;
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
