using System.Threading.Tasks;
using BookParser;
using Digillect.Mvvm;
using LitRes.Models;

namespace LitRes.ViewModels
{
	public class ReaderSettingsViewModel : ViewModel
	{
		private const string LoadSettingsPart = "Load";
		private const string SaveSettingsPart = "Save";

	    private readonly SettingsStorage _settingsStorage = new SettingsStorage();

        public enum DeffaultSettingsType
        {
            DeffaultSettingsTypeNormal = 0,
            DeffaultSettingsTypeHd
        }


		#region Constructors/Disposer
		public ReaderSettingsViewModel()
		{
            RegisterAction(LoadSettingsPart).AddPart(LoadSettings, session => true);
            RegisterAction(SaveSettingsPart).AddPart(SaveSettings, session => true);
           
            Settings = new ReaderSettings();

		    Task.Run(async () => await Load(new Session(LoadSettingsPart)));
		}
		#endregion

		#region Properties
		public ReaderSettings Settings { get; }

	    public bool IsDefaultSettings { get; set; }

        public bool SystemTile
        {
            get { return _settingsStorage.GetValueWithDefault("SystemTile", false); }
            set { _settingsStorage.SetValue("SystemTile", value); }
        }

	    public bool PushNotifications
	    {
            get { return _settingsStorage.GetValueWithDefault("PushNotifications", true); }
            set { _settingsStorage.SetValue("PushNotifications", value); }
        }

	    #endregion

		#region Save
		public async Task Save()
		{
			await Load( new Session( SaveSettingsPart ) );
		}
        #endregion

        #region Load
        public async Task Load()
        {
            await Load(new Session(LoadSettingsPart));
        }
        #endregion

        #region LoadSettings
        private async Task LoadSettings( Session session )
		{
          
           /* var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            //rsModel.LastUpdate = DateTime.Parse(localSettings.Values["LastUpdate"].ToString()); 
            try
            {
                _brightness = (float)localSettings.Values["Brightness"];
                _animate = (bool)localSettings.Values["AnimationMoveToPage"];
            }
            catch (Exception)
            {
                _autorotate = false;
                _brightness = 0;
                _animate = false;
            }*/
        }
        #endregion

        #region SaveSettings
        private async Task SaveSettings( Session session )
		{
          /*  var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            try
            {
                localSettings.Values["Brightness"] = _brightness;
                localSettings.Values["AnimationMoveToPage"] = _animate;
            }
            catch (Exception)
            {
                // ignored
            }*/
        }
        #endregion
    }
}
