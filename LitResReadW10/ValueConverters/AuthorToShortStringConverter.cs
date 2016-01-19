using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LitRes.Models;

namespace LitRes.ValueConverters
{
	public class AuthorToShortStringConverter : ConverterBase<Book.TitleInfo.AuthorInfo, string>
	{
		public override object Convert( Book.TitleInfo.AuthorInfo authorInfo, object parameter, string language)
		{
			return GetAuthor( authorInfo );
		}

		public static string GetAuthor( Book.TitleInfo.AuthorInfo authorInfo )
		{
			var author = new StringBuilder();

            if (!string.IsNullOrEmpty(authorInfo.FirstName))
            {
                author.AppendFormat("{0} ", authorInfo.FirstName);
            }

			if( !string.IsNullOrEmpty( authorInfo.LastName ) )
			{
				author.AppendFormat( "{0} ", authorInfo.LastName );
			}

            //if( !string.IsNullOrEmpty( authorInfo.FirstName ) )
            //{
            //    author.AppendFormat( "{0}. ", authorInfo.FirstName[0] );
            //}

            //if( !string.IsNullOrEmpty( authorInfo.MiddleName ) )
            //{
            //    author.AppendFormat( "{0}. ", authorInfo.MiddleName[0] );
            //}

			var authorString = author.ToString().Trim();

			//if( authorString == string.Empty )
			//{ 
			//	return string.Empty;
			//}

			return authorString;
		}
	}
}
