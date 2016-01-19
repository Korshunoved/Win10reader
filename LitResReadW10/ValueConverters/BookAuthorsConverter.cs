using LitRes.Models;
using System.Collections;

namespace LitRes.ValueConverters
{
	public class BookAuthorsConverter : ConverterBase<Book, IEnumerable>
	{
		public override object Convert( Book book, object parameter, string language)
		{
			if( book.Description.Hidden.TitleInfo.Author != null && book.Description.Hidden.TitleInfo.Author.Length > 0 )
			{
				var authors = new Book.TitleInfo.AuthorInfo[book.Description.Hidden.TitleInfo.Author.Length];
				int i=0;

				foreach( var author in book.Description.Hidden.TitleInfo.Author )
				{
					authors[i] = author;

					if( string.IsNullOrEmpty( author.LastName )
						&& string.IsNullOrEmpty( author.FirstName )
						&& string.IsNullOrEmpty( author.MiddleName ) )
					{
						
						authors[i].LastName = book.Copyright;
					}

					i++;
				}

				return authors;
			}

			return book.Description.Hidden.TitleInfo.Author;
		}
	}
}
