using System;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Digillect.Mvvm;
using Digillect.Mvvm.UI;
using LitRes.ViewModels;

namespace LitRes.Views
{
	[View( "Settings" )]
	public partial class Settings : SettingsFitting
	{
		private bool _loaded;

		#region Constructors/Disposer
		public Settings()
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
            await ViewModel.Save();
            base.OnNavigatingFrom(e);
        }

        private void SliderManipulationCompleted( object sender, ManipulationCompletedEventArgs e )
		{
			( ( Slider ) sender ).Value = Math.Round( ( ( Slider ) sender ).Value );
		}
	}

	public class SettingsFitting : ViewModelPage<ReaderSettingsViewModel>
	{
	}
}