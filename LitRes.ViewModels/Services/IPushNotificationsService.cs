using System.Threading.Tasks;

namespace LitRes.Services
{
	public interface IPushNotificationsService
	{
		Task AcquirePushChannel();
	}
}
