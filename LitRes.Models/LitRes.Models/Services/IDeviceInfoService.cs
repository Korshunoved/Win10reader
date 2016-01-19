namespace LitRes.Services
{
	public interface IDeviceInfoService
	{
		string DeviceManufacturer { get; }
		string DeviceId { get; }
        string DeviceFamily { get; }
        string OsVersion { get; }
        string SystemArchitecture { get;}
        string ApplicationName { get;}
        string ApplicationVersion { get; }
        string DeviceModel { get; }
       
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
