using System.Threading;
using System.Threading.Tasks;
using LitRes.Models;

namespace LitRes.Services.Connectivity
{
	public interface ICredentialsProvider
	{
	    Task<CatalitCredentials> MigrateFromWp8ToWp10();
        CatalitCredentials ProvideCredentials(CancellationToken cancellationToken);
		void RegisterCredentials(CatalitCredentials credentials, CancellationToken cancellationToken);
        void ForgetCredentialsRebill(CatalitCredentials credentials, CancellationToken cancellationToken);
		Task<bool> ShouldRetryAuthentication(CatalitCredentials credentials, CancellationToken cancellationToken);
	}
}
