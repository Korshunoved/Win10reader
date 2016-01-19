using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using LitRes.Helpers;

namespace LitRes.Selectors
{
    public class SliderTemplateSelector : DataTemplateSelector
	{
		public DataTemplate HDSlider { get; set; }
		public DataTemplate Normal { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (ResolutionHelper.isFullHD) return HDSlider;
            return Normal;
        }
    }
}
