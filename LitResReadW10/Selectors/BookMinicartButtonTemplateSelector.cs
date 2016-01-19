using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Autofac;
using LitRes.Models;
using LitRes.Services;
using LitResReadW10;

namespace LitRes.Selectors
{
    public class BookMinicartButtonTemplateSelector : DataTemplateSelector
	{
		public DataTemplate Free { get; set; }
		public DataTemplate Fragment { get; set; }
		public DataTemplate Buy { get; set; }
		public DataTemplate Download { get; set; }
		public DataTemplate Read { get; set; }

        public DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var book = item as Book;

            if (book != null && !book.IsEmptyElement)
            {
                var bookProvider = ((App)Application.Current).Scope.Resolve<IBookProvider>();
                var isFullBookExists = bookProvider.FullBookExistsInLocalStorage(book.Id);
                // var isTrialBookExists = _bookProvider.TrialBookExistsInLocalStorage(book.Id);

                if (bookProvider.FullBookExistsInLocalStorage(book.Id)) return Read;
                if (!book.isFragment && book.IsMyBook && !isFullBookExists) return Download;
                if (!book.IsMyBook && book.isFreeBook) return Free;
                if (book.isFragment && book.IsMyBook) return Fragment;
            }

            return Buy;
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject obj)
	    {
            var book = item as Book;

            if (book != null && !book.IsEmptyElement)
            {
                var bookProvider = ((App)Application.Current).Scope.Resolve<IBookProvider>();
                var isFullBookExists = bookProvider.FullBookExistsInLocalStorage(book.Id);
               // var isTrialBookExists = _bookProvider.TrialBookExistsInLocalStorage(book.Id);
                
                if (bookProvider.FullBookExistsInLocalStorage(book.Id)) return Read;
                if (!book.isFragment && book.IsMyBook && !isFullBookExists) return Download;
                if (!book.IsMyBook && book.isFreeBook) return Free;
                if (book.isFragment) return Fragment;
            }

            return Buy;
        }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            var book = item as Book;

            if (book != null && !book.IsEmptyElement)
            {
                var bookProvider = ((App)Application.Current).Scope.Resolve<IBookProvider>();
                var isFullBookExists = bookProvider.FullBookExistsInLocalStorage(book.Id);
                // var isTrialBookExists = _bookProvider.TrialBookExistsInLocalStorage(book.Id);

                if (bookProvider.FullBookExistsInLocalStorage(book.Id)) return Read;
                if (!book.isFragment && book.IsMyBook && !isFullBookExists) return Download;
                if (!book.IsMyBook && book.isFreeBook) return Free;
                if (book.isFragment
                    && book.IsMyBook) return Fragment;
            }

            return Buy;
        }
    }
}
