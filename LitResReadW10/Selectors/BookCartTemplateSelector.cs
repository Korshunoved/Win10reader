using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using LitRes.Models;

namespace LitRes.Selectors
{
    public class BookCartTemplateSelector : DataTemplateSelector
	{
		public DataTemplate LocalBook { get; set; }
		public DataTemplate NormalBook { get; set; }

		public DataTemplate SelectTemplate( object item, DependencyObject container )
		{
		    if (item.GetType() == typeof (Book) && (item as Book).IsLocal) return LocalBook;
			return NormalBook;
		}

        protected override DataTemplate SelectTemplateCore(object item)
        {
            var book = item as Book;
            if (book != null && (item.GetType() == typeof(Book) && book.IsLocal)) return LocalBook;
            return NormalBook;
        }
    }
}
