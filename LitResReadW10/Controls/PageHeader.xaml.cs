using System;
using Digillect.Mvvm.Services;
using Autofac;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using LitResReadW10;

namespace LitRes.Controls
{
	public partial class PageHeader : UserControl
	{
		private IDataExchangeService dataExchangeService;

		public PageHeader()
		{
			InitializeComponent();

			this.Loaded += PageHeader_Loaded;
			this.Unloaded += PageHeader_UnLoaded;
		}

		#region Public Properties

        

		public bool NetworkAvailability
		{
			set { networkAvailability.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
		}
		#endregion

		private void PageHeader_Loaded(object sender, RoutedEventArgs e)
		{
			this.dataExchangeService = ((App) App.Current).Scope.Resolve<IDataExchangeService>();

			this.dataExchangeService.DataExchangeStarted += UpdateProgressIndicator;
			this.dataExchangeService.DataExchangeComplete +=  UpdateProgressIndicator;

			UpdateProgressIndicator(sender, null);
		}

		private void PageHeader_UnLoaded(object sender, RoutedEventArgs e)
		{
			this.dataExchangeService = ((App)Application.Current).Scope.Resolve<IDataExchangeService>();

			this.dataExchangeService.DataExchangeStarted -= UpdateProgressIndicator;
			this.dataExchangeService.DataExchangeComplete -= UpdateProgressIndicator;

			UpdateProgressIndicator(sender, null);
		}

		private void UpdateProgressIndicator( object sender, EventArgs eventArgs )
		{
			if( Dispatcher.HasThreadAccess )
			{
				loadingProgress.IsIndeterminate = this.dataExchangeService.DataExchangeCount > 0;
				loadingProgress.Visibility = loadingProgress.IsIndeterminate ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		//private void Header_Tap(object sender, GestureEventArgs e)
		//{
		//	((App) App.Current).Scope.Resolve<INavigationService>().Navigate( "Main" );
		//}
	}
}
