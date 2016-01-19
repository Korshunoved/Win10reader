
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace LitRes.Selectors
{
	public abstract class DataTemplateSelector : ContentControl
	{
		public virtual DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			return null;
		}

        protected virtual DataTemplate SelectTemplateCore(object item)
        {
            return null;
        }

        protected override void OnContentChanged(object oldContent, object newContent)
		{
			base.OnContentChanged(oldContent, newContent);

			ContentTemplate = SelectTemplate(newContent, this);
		}
	}
}
