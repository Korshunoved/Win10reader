using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace LitRes.Controls
{
	public class AssociationUriMapper : UriMapperBase
	{
		private string _tempUri;

	    private const string FIND_DIGIT_PTRN = @"\b(\d+)";
        private const string FIND_AUTHOR_ID_PTRN = "[0-9a-z-]*$";
        private const string LITRESREAD_OPEN_BOOK_CARD_PTRN = @"/?litresread://content/b/\d*$";
        private const string LITRESREAD_OPEN_BOOK_FOR_READ_PTRN = @"/?litresread://content/b/\d*/open";
        private const string LITRESREAD_AUTHOR_BOOK_CARD_PTRN = @"/?litresread://content/a/*";
        private const string LITRESREAD_COLLECTION_PTRN = @"/?litresread://content/c/\d*$";
        private const string LITRESREAD_OPEN_BOOK_FROM_WEB_PTRN = @"/?litresread://pin/b/\d*/open/\d*$";

	    public override Uri MapUri(Uri uri)
	    {
	        _tempUri = WebUtility.UrlDecode(uri.ToString());
	        // URI association launch for my app detected
            
	        if (_tempUri.Contains("litressappuri:Main?bookdescr="))
	        {
	            // Get the book (after "book=").
                const string bookParam = "bookdescr=";
                int bookIndex = _tempUri.IndexOf(bookParam, StringComparison.Ordinal) + bookParam.Length;
	            string book = _tempUri.Substring(bookIndex);
	            // Redirect to the Main.xaml with the proper category to be displayed
	            return new Uri("/Views/Main.xaml?bookdescr=" + book, UriKind.Relative);
	        }
            
            if (_tempUri.Contains("bookdescr_hidden="))
	        {
                const string bookParam = "bookdescr_hidden=";
                int bookIndex = _tempUri.IndexOf(bookParam, StringComparison.Ordinal) + bookParam.Length;
                string book = _tempUri.Substring(bookIndex);
                // Redirect to the Main.xaml with the proper category to be displayed
                return new Uri(string.Format("/Views/Main.xaml?{0}{1}&hidden=1", bookParam, book), UriKind.Relative);
            }

           // var matchesDig = Regex.Matches(_tempUri, FIND_DIGIT_PTRN, RegexOptions.IgnoreCase);

            if (Regex.Match(_tempUri, LITRESREAD_OPEN_BOOK_CARD_PTRN, RegexOptions.IgnoreCase).Success)
            {
                var matchDig = Regex.Match(_tempUri, FIND_DIGIT_PTRN, RegexOptions.IgnoreCase);
                if (matchDig.Success){
                    return new Uri("/Views/Main.xaml?bookdescr=" + matchDig.Value, UriKind.Relative);
                }
            }

            if (Regex.Match(_tempUri, LITRESREAD_OPEN_BOOK_FOR_READ_PTRN, RegexOptions.IgnoreCase).Success)
            {
                var matchDig = Regex.Match(_tempUri, FIND_DIGIT_PTRN, RegexOptions.IgnoreCase);
                if (matchDig.Success){
                    return new Uri("/Views/Main.xaml?book=" + matchDig.Value, UriKind.Relative);
                }
            }

            if (Regex.Match(_tempUri, LITRESREAD_AUTHOR_BOOK_CARD_PTRN, RegexOptions.IgnoreCase).Success)
            {
                var matchDig = Regex.Match(_tempUri, FIND_AUTHOR_ID_PTRN, RegexOptions.IgnoreCase);
                if (matchDig.Success){
                    return new Uri("/Views/Main.xaml?authorId=" + matchDig.Value, UriKind.Relative);
                }
            }

            if (Regex.Match(_tempUri, LITRESREAD_COLLECTION_PTRN, RegexOptions.IgnoreCase).Success)
            {
                var matchDig = Regex.Match(_tempUri, FIND_DIGIT_PTRN, RegexOptions.IgnoreCase);
                if (matchDig.Success)
                {
                    return new Uri("/Views/Main.xaml?collection=" + matchDig.Value, UriKind.Relative);
                }
            }

            if (Regex.Match(_tempUri, LITRESREAD_OPEN_BOOK_FROM_WEB_PTRN, RegexOptions.IgnoreCase).Success)
            {
                var matchesDig = Regex.Matches(_tempUri, FIND_DIGIT_PTRN, RegexOptions.IgnoreCase);
                if (matchesDig.Count == 2)
                {

                    return new Uri(string.Format("/Views/Main.xaml?hubBookId={0}&userId={1}", matchesDig[0].Value, matchesDig[1].Value), UriKind.Relative);
                }
            }

            
         
            if (_tempUri.Contains("litressappuri:Main")) return new Uri("/Views/Main.xaml", UriKind.Relative);

            if (_tempUri.Contains("/FileTypeAssociation"))
            {
                // Get the file ID (after "fileToken=").
                int fileIDIndex = _tempUri.IndexOf("fileToken=") + 10;
                string fileID = _tempUri.Substring(fileIDIndex);

                // Get the file name.
                string incomingFileName =
                    SharedStorageAccessManager.GetSharedFileName(fileID);

                // Get the file extension.
                string incomingFileType = Path.GetExtension(incomingFileName);

                // Map the .sdkTest1 and .sdkTest2 files to different pages.
                switch (incomingFileType)
                {
                    case ".fb2":
                        return new Uri("/Views/Main.xaml?filetoken=" + fileID, UriKind.Relative);
                    default:
                        return new Uri("/Views/Main.xaml", UriKind.Relative);
                }
            }

            if (_tempUri.Contains("litresread://")) return new Uri("/Views/Main.xaml", UriKind.Relative);

	    // Otherwise perform normal launch.
			return uri;
		}
	}
}
