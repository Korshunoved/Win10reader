using System.Threading;
using System.Threading.Tasks;
using LitRes.Models;

namespace LitRes.Services.Connectivity
{
	public interface ICredentialsProvider
	{
		Task<CatalitCredentials> ProvideCredentials(CancellationToken cancellationToken);
		void RegisterCredentials(CatalitCredentials credentials, CancellationToken cancellationToken);
        void ForgetCredentialsRebill(CatalitCredentials credentials, CancellationToken cancellationToken);
		Task<bool> ShouldRetryAuthentication(CatalitCredentials credentials, CancellationToken cancellationToken);
	}
}
