using System.Collections.Generic;
using System.Globalization;
using LitRes.Models;

namespace LitRes.ValueConverters
{
	public class BookAuthorConverter : ConverterBase<Book, string>
	{
		public override object Convert( Book book, object parameter, string language)
		{
			string result = string.Empty;

			if( book.Description.Hidden.TitleInfo.Author != null && book.Description.Hidden.TitleInfo.Author.Length > 0 )
			{
				if( !string.IsNullOrEmpty( (string) parameter ) )
				{
					int index;
					int.TryParse( (string) parameter, out index );

					if( book.Description.Hidden.TitleInfo.Author.Length > index )
					{
						result = AuthorToShortStringConverter.GetAuthor( book.Description.Hidden.TitleInfo.Author[index] );
					}
				}
				else
				{
					var authorsList = new List<string>();

					foreach( var auth in book.Description.Hidden.TitleInfo.Author )
					{
						var author = AuthorToShortStringConverter.GetAuthor( auth );

						authorsList.Add( author );
					}

					result = string.Join( ", ", authorsList );
				}
			}

			if( result == string.Empty )
			{ 
				return book.Copyright;
			}

			return result;
		}
	}
}
