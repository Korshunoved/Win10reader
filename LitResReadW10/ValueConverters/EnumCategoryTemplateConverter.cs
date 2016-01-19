using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using LitRes.ViewModels;
using LitResReadW10;
using LitResReadW10.Helpers;

namespace LitRes.ValueConverters
{
	public class EnumCategoryTemplateConverter : ConverterBase<int, DataTemplateSelector>
	{
		public override object Convert( int value, object parameter, string language)
		{
		    
			//if( DeviceStatus.DeviceManufacturer.ToLower().Contains( "nokia" ) && (BooksByCategoryViewModel.BooksViewModelTypeEnum)value ==  BooksByCategoryViewModel.BooksViewModelTypeEnum.NokiaCollection  )
			//if(SystemInfoHelper.GetDeviceManufacturerAsync().Result.ToLower().Contains( "nokia" ) && (BooksByCategoryViewModel.BooksViewModelTypeEnum)value ==  BooksByCategoryViewModel.BooksViewModelTypeEnum.NokiaCollection  )
			//{
			//	return App.Current.Resources["ListBoxBookNokiaTemplate"];
			//}
   //         else if(value.Equals( BooksByCategoryViewModel.BooksViewModelTypeEnum.FreeBooks ))
   //         {
   //             return App.Current.Resources["ListBoxFreeBookTemplate"];
   //         }

			return App.Current.Resources["ListBoxBookTemplate"];
		}
	}
}