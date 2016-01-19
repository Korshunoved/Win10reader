using System;
using Windows.UI.Xaml.Data;
using LitRes.Models;

namespace LitRes.ValueConverters
{
	public class BookmarksTextCoverter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, string language)
		{
			var bookmark = value as Bookmark;

			if( bookmark != null)
			{
                if (bookmark.NoteText != null && !string.IsNullOrEmpty(bookmark.NoteText.Text))
			    {
			        return bookmark.NoteText.Text;
			    }

			    if (bookmark.ExtractInfo != null)
			    {
			        if (!string.IsNullOrEmpty(bookmark.ExtractInfo.SelectionText))
			        {
			            return bookmark.ExtractInfo.SelectionText;
			        }

			        //if( !string.IsNullOrEmpty( bookmark.ExtractInfo.ExtractText ) )
			        //{
			        //	return Regex.Replace( bookmark.ExtractInfo.ExtractText, @"<[^>]*>", string.Empty );
			        //}
			        if (bookmark.ExtractInfo.ExtractText != null && bookmark.ExtractInfo.ExtractText.Count > 0 &&
			            !string.IsNullOrEmpty(bookmark.ExtractInfo.ExtractText[0].Text))
			        {
			            return bookmark.ExtractInfo.ExtractText[0].Text;
			        }
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
