using System.Windows;
using LitRes.Models;

namespace LitRes.Selectors
{
	public class NokiaBookTemplateSelector : DataTemplateSelector
	{
		public DataTemplate NokiaBook { get; set; }
		public DataTemplate FreeBook { get; set; }

		public override DataTemplate SelectTemplate( object item, DependencyObject container )
		{
			var book = item as Book;

		    if (book != null)
		    {
		        if ((!string.IsNullOrEmpty(book.InGifts) && book.InGifts == "1") ||
		            (string.IsNullOrEmpty(book.StoreProductPurchaseValue)))
		        {
		            return FreeBook;
		        }
		    }
		    return NokiaBook;
		}
	}
}
