using Digillect.Collections;
using System.Collections.Generic;
using LitRes.Models;

namespace LitRes.ValueConverters
{
	public class TranslatorsConverter : ConverterBase<XCollection<LitRes.Models.Book.TitleInfo.AuthorInfo>, string>
	{
		public override object Convert(XCollection<LitRes.Models.Book.TitleInfo.AuthorInfo> translators, object parameter, string language)
        {
			if ( translators != null && translators.Count > 0 )
			{
				return string.Join(", ", AuthorsToStrings(translators));
			}

			return string.Empty;
		}

		private static IEnumerable<string> AuthorsToStrings(XCollection<LitRes.Models.Book.TitleInfo.AuthorInfo> translators)
		{
			foreach ( var author in translators )
			{
				yield return (string.IsNullOrEmpty(author.FirstName) ? string.Empty : author.FirstName[0] + ". ") +
						(string.IsNullOrEmpty(author.MiddleName) ? string.Empty : author.MiddleName[0] + ". ") +
						(string.IsNullOrEmpty(author.LastName) ? string.Empty : author.LastName);
			}
		}
    }
}
