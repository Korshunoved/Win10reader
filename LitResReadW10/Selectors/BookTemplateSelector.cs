using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using LitRes.Models;

namespace LitRes.Selectors
{
    public class BookTemplateSelector : DataTemplateSelector
	{
		public DataTemplate Book { get; set; }
		public DataTemplate Empty { get; set; }

		public DataTemplate SelectTemplate( object item, DependencyObject container )
		{
			var book = item as Book;

			if( book != null && !book.IsEmptyElement )
			{
				return Book;
			}

			return Empty;
		}

	    protected override DataTemplate SelectTemplateCore(object item)
	    {
            var book = item as Book;

            if (book != null && !book.IsEmptyElement)
            {
                return Book;
            }

            return Empty;
        }
	}
}
