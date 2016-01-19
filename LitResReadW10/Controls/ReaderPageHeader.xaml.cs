using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace LitRes.Controls
{
	public partial class ReaderPageHeader : UserControl
	{
		public ReaderPageHeader()
		{
			InitializeComponent();
		}

		#region Public Properties
		public bool ProgressIndicatorVisible
		{
			set
			{
				UpdateProgressIndicator( value );
			}
		}
		#endregion

		private void UpdateProgressIndicator( bool visible )
		{
			if( !Dispatcher.HasThreadAccess)
			{
               Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High,
                            () => UpdateProgressIndicator(visible));
                //Dispatcher.RunAsync(CoreDispatcherPriority.Normal, (() => UpdateProgressIndicator(visible)));//.BeginInvoke( ( Action<bool> ) UpdateProgressIndicator, visible );
			}
			else
			{
				loadingProgress.IsIndeterminate = visible;
				Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
			}
		}
	}
}
