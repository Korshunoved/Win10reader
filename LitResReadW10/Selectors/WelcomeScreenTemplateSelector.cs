using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
namespace LitRes.Selectors
{
    public class WelcomeScreenTemplateSelector : DataTemplateSelector
	{
		public DataTemplate Nokia { get; set; }
		public DataTemplate Other { get; set; }

		public DataTemplate SelectTemplate( object item, DependencyObject container )
		{
            //var deviceInfo = ((App) App.Current).Scope.Resolve<IDeviceInfoService>();            
            //if (deviceInfo.IsNokiaDevice) return Nokia;
			return Other;
		}

        protected override DataTemplate SelectTemplateCore(object item)
        {
            //var deviceInfo = ((App) App.Current).Scope.Resolve<IDeviceInfoService>();            
            //if (deviceInfo.IsNokiaDevice) return Nokia;
            return Other;
        }
    }
}
