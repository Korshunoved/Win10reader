using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Digillect.Mvvm;
using Digillect.Mvvm.UI;
using LitRes.ViewModels;
using LitResReadW10.Controls;

namespace LitRes.Views
{
	[View("GeneralSettings")]
    [ViewParameter("PDFBook", typeof(bool), Required = false)]
	public partial class GeneralSettings : SettingsFitting
	{
		private bool _loaded;

		#region Constructors/Disposer
		public GeneralSettings()
		{
			InitializeComponent();

			Loaded += Settings_Loaded;
		}
		#endregion

	    protected override Session CreateDataSession(DataLoadReason reason)
	    {
            ViewModel.IsDefaultSettings = !ViewParameters.GetValue<bool>("PDFBook");        
            return base.CreateDataSession(reason);
	    }

		void Settings_Loaded( object sender, RoutedEventArgs e )
		{
			_loaded = true;
            Analytics.Instance.sendMessage(Analytics.ViewSettingsReader);
		}

	    protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
	    {
            //await ViewModel.Save();
            base.OnNavigatingFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ControlPanel.Instance.TopBarTitle = "Настройки";
            base.OnNavigatedTo(e);
        }
    }

	public class GeneralSettingsFitting : ViewModelPage<ReaderSettingsViewModel>
	{
	}
}