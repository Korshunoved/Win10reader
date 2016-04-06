using System;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Digillect.Mvvm;
using Digillect.Mvvm.UI;
using LitRes.Services;
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

		    SystemTileSwitcher.IsOn = ViewModel.SystemTile;
            SystemTileSwitcher.Toggled -= SystemTileSwitcherOnToggled;
            SystemTileSwitcher.Toggled += SystemTileSwitcherOnToggled;

		    PushNotificationSwitcher.IsOn = ViewModel.PushNotifications;
            PushNotificationSwitcher.Toggled -= PushNotificationSwitcherOnToggled;
            PushNotificationSwitcher.Toggled += PushNotificationSwitcherOnToggled;		    
		}

	    private async void PushNotificationSwitcherOnToggled(object sender, RoutedEventArgs routedEventArgs)
	    {
	        var value = ViewModel.PushNotifications = PushNotificationSwitcher.IsOn;
            var push = PushNotificationsService.Instance;

	        if (value)
	        {
	            if (push.Channel == null)
	            {
                    await push.AcquirePushChannel();
	            }
	        }
	        else
	        {
	            push.Channel?.Close();
	        }
	    }

	    private void SystemTileSwitcherOnToggled(object sender, RoutedEventArgs routedEventArgs)
	    {
            var value = ViewModel.SystemTile = SystemTileSwitcher.IsOn;

	        var src = value ? "ms-appx:///Assets/Tiles/FlipCycleTileMediumOp.png" : "ms-appx:///Assets/Tiles/FlipCycleTileMedium.png";
            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            updater.EnableNotificationQueue(true);
            updater.Clear();
            var tileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare150x150Image);
            XmlNodeList imageAttribute = tileXml.GetElementsByTagName("image");
            ((XmlElement)imageAttribute[0]).SetAttribute("src", src);
            // Create a new tile notification. 
            updater.Update(new TileNotification(tileXml));
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