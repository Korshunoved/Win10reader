using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel;
using Windows.Networking.NetworkOperators;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.System.Profile;

namespace LitRes.Services
{
	public class DeviceInfoService : IDeviceInfoService
	{
	    private readonly Dictionary<string, Dictionary<string, int>> _devicesInnerRefs =
	        new Dictionary<string, Dictionary<string, int>> { { "fly", new Dictionary<string, int> { {"IQ400W", 43356846}, {"IQ500W", 43356846} } } };

        private readonly Dictionary<string, Dictionary<string, int>> _devicesInAppRefs =
            new Dictionary<string, Dictionary<string, int>> { { "fly", new Dictionary<string, int> { {"IQ400W", 43356841}, {"IQ500W", 43356841} }  } };

	    public DeviceInfoService()
	    {
            AnalyticsVersionInfo ai = AnalyticsInfo.VersionInfo;
            DeviceFamily = ai.DeviceFamily;

            // get the system version number
            string sv = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
            ulong v = ulong.Parse(sv);
            ulong v1 = (v & 0xFFFF000000000000L) >> 48;
            ulong v2 = (v & 0x0000FFFF00000000L) >> 32;
            ulong v3 = (v & 0x00000000FFFF0000L) >> 16;
            ulong v4 = (v & 0x000000000000FFFFL);
            OsVersion = $"{v1}.{v2}.{v3}.{v4}";

            // get the package architecure
            Package package = Package.Current;
            SystemArchitecture = package.Id.Architecture.ToString();

            // get the user friendly app name
            ApplicationName = package.DisplayName;

            // get the app version
            PackageVersion pv = package.Id.Version;
            ApplicationVersion = $"{pv.Major}.{pv.Minor}.{pv.Build}.{pv.Revision}";

            // get the device manufacturer and model name
            EasClientDeviceInformation eas = new EasClientDeviceInformation();
            DeviceManufacturer = eas.SystemManufacturer;
            DeviceModel = eas.SystemProductName;
	        OsVersion = eas.OperatingSystem + " " + OsVersion;
	        DeviceId = eas.Id.ToString();
	    }

		public string DeviceManufacturer { get; private set; }

		public string DeviceId { get; private set; }

        public bool IsSimCardDetected => !(string.IsNullOrEmpty(MobileOperator));

	    public string MobileOperator
        {
            get
            {
                try
                {
                    var deviceAccountId = MobileBroadbandAccount.AvailableNetworkAccountIds;
                    if (deviceAccountId != null && deviceAccountId.Count != 0)
                    {
                        var accountId = MobileBroadbandAccount.AvailableNetworkAccountIds.First();
                        var mobileBroadbandAccount = MobileBroadbandAccount.CreateFromNetworkAccountId(accountId);
                        var currentNetwork = mobileBroadbandAccount.CurrentNetwork;
                        return currentNetwork.RegisteredProviderName;
                    }
                }
                catch (Exception ex)
                {
                    ex = ex;
                }

                return null;
            }
        }

		public int AppId => 6;

	    public int LibAppId => 14;

	    public int WinMobileRefId => 14808722;

	    public int LitresInnerRefId
        {
            get
            {
                if (IsNokiaDevice)
                {
                    //return 43438891;
                    return 217176046;
                }
                else if (IsFlyDevice)
                {
                    if (_devicesInnerRefs["fly"].ContainsKey(DeviceModel))
                    {
                        return _devicesInnerRefs["fly"][DeviceModel];
                    }
                }                
                return 217176046;
            }
        }

        public int LitresInAppRefId
        {
            get
            {
                if (IsNokiaDevice)
                {
                    return 217176042;
                    //return 43438887;
                }
                else if (IsFlyDevice)
                {
                    if (_devicesInnerRefs["fly"].ContainsKey(DeviceModel))
                    {
                        return _devicesInAppRefs["fly"][DeviceModel];
                    }
                }
                return 217176042;
            }
        }

		public string Currency 
		{ 
			get { return "rub"; }
		}

        public string DeviceFamily { get; private set; }

        public string OsVersion { get; private set; }

        public string SystemArchitecture { get; private set; }

        public string ApplicationName { get; private set; }

        public string ApplicationVersion { get; private set; }

        public string DeviceModel { get; private set; }

		public bool IsNokiaDevice 
		{
			get
			{
#if DEBUG
				return true;
#else
                return DeviceManufacturer.ToLower().Contains( "nokia" );
#endif
            }
        }

        public bool IsFlyDevice
        {
            get
            {
#if DEBUG
                return true;
#else
				return DeviceManufacturer.ToLower().Contains( "fly" );
#endif
            }
        }
	}
}
