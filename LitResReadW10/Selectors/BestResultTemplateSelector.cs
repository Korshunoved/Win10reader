using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using LitRes.Models;
using LitRes.Models.JsonModels;
using Genre = LitRes.Models.JsonModels.Genre;

namespace LitRes.Selectors
{
    public class BestResultTemplateSelector : DataTemplateSelector
	{
		public DataTemplate Book { get; set; }
		public DataTemplate Person { get; set; }
		public DataTemplate Tag { get; set; }
		public DataTemplate Genre { get; set; }
		public DataTemplate Sequense { get; set; }
		public DataTemplate Empty { get; set; }

        public DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is Person) return Person;
            if (item is Book.SequenceInfo) return Sequense;
            if (item is Tag) return Tag;
            if (item is Genre) return Genre;

            return Empty;
        }

        protected override DataTemplate SelectTemplateCore(object item)
	    {
            if (item is Person) return Person;
            if (item is Book.SequenceInfo) return Sequense;
            if (item is Tag) return Tag;
            if (item is Genre) return Genre;
            if (item is Book) return Book;

            return Empty;
        }
	}
}
