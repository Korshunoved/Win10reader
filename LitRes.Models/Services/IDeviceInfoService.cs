namespace LitRes.Services
{
	public interface IDeviceInfoService
	{
		string DeviceManufacturer { get; }
		string DeviceId { get; }
        string DeviceModel { get; }
        string DeviceName { get; }
        bool IsSimCardDetected{ get; }
        string MobileOperator { get; }
		bool IsNokiaDevice { get; }
		int AppId { get; }
        int LibAppId { get; }
		int WinMobileRefId { get; }
        int LitresInnerRefId { get; }
        int LitresInAppRefId { get; }

		string Currency { get; }
	}
}
