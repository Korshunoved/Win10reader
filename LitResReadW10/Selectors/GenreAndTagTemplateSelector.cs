using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using LitRes.Models;
using LitRes.Models.JsonModels;

namespace LitRes.Selectors
{
    public class GenreAndTagTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Genre { get; set; }
        public DataTemplate Tag { get; set; }

        public DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is Tag) return Tag;

            return Genre;
        }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is Tag) return Tag;

            return Genre;
        }
    }
}
