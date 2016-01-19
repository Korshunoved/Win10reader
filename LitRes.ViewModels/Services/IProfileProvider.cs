using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LitRes.Models;

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
	}
}
