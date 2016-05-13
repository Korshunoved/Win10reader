using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Autofac;
using LitResReadW10;

namespace LitRes.ValueConverters
{
    public class IsFileExistsConverter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, string language)
        {
			bool isExists = false;
            var s1 = parameter as string;
            var parameters = s1?.Split('|');

            if ( value != null )
			{
                if (value.GetType() == typeof(Models.Book))
                {
                    var book = value as Models.Book;
                    var bookProvider = ((App)Application.Current).Scope.Resolve<Services.IBookProvider>();

                    if (parameters != null && parameters.Contains("price"))
                    {
                        if (book != null)
                        {
                            if ((book.IsMyBook && bookProvider.TrialBookExistsInLocalStorage(book.Id)) || !book.IsMyBook) isExists = true;
                        }
                    }
                    else if (parameters != null && parameters.Contains("trial"))
                    {
                        if (book != null && bookProvider.TrialBookExistsInLocalStorage(book.Id)) isExists = true;
                    }
                    else if (parameters != null && parameters.Contains("full"))
                    {
                        //if (book != null && bookProvider.FullBookExistsInLocalStorage(book.Id)) isExists = true;
                        if (book != null && book.IsMyBook && !book.IsFreeBook) isExists = true;
                    }
                    else
                    {
                        if (book != null && (!string.IsNullOrEmpty(book.SelfService) && book.SelfServiceMyRequest.Equals("1")))
                            isExists = true;
                        else if (book != null && book.IsExpiredBook) isExists = true;
                        else if (book != null && bookProvider.TrialBookExistsInLocalStorage(book.Id)) isExists = true;
                        else if (book != null && bookProvider.FullBookExistsInLocalStorage(book.Id)) isExists = true;
                    }
                }
			}


		    if (parameters == null) return isExists;

		    if (parameters.Contains("inverse"))
		    {
		        isExists = !isExists;
		    }
		    if (parameters.Contains("visibility"))
		    {
		        return isExists ? Visibility.Visible : Visibility.Collapsed;
		    }
		    return isExists;
		}

		public object ConvertBack( object value, Type targetType, object parameter, string language)
        {
			throw new NotImplementedException();
		}
	}
}
