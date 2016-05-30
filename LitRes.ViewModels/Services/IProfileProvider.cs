using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LitRes.Models;
using LitRes.Models.Models;

namespace LitRes.Services
{
	public interface IProfileProvider
	{
		Task<UserInformation> Authorize( CatalitCredentials credentials, CancellationToken cancellationToken );
		Task<UserInformation> GetUserInfo( CancellationToken cancellationToken, bool deffaultFromTheWeb = false );
        Task<UserInformation> Register(CatalitCredentials credentials, CancellationToken cancellationToken, CatalitCredentials oldCredentials = null, bool additionalParams = true);
		Task<CatalitCredentials> RegisterDefault( CancellationToken cancellationToken );
		Task ChangeUserInfo( UserInformation userInfo, CancellationToken cancellationToken );
		Task<UserInformation> MergeAccounts(string userAccountSid, CatalitCredentials mergedAccount, CancellationToken cancellationToken);
	    Task<OffersResponse> GetOffers(CancellationToken cancellationToken);
	}
}
