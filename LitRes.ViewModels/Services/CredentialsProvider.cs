using System.Threading;
using System.Threading.Tasks;
using LitRes.Models;
using LitRes.Services.Connectivity;

namespace LitRes.Services
{
	internal class CredentialsProvider : ICredentialsProvider
	{
		const string CacheItemName = "credentials";
		private IDataCacheService _dataCacheService;

		#region Constructors/Disposer
		public CredentialsProvider(IDataCacheService dataCacheService)
		{
			_dataCacheService = dataCacheService;
		}
		#endregion

		public CatalitCredentials ProvideCredentials(CancellationToken cancellationToken)
		{
			return  _dataCacheService.GetItem<CatalitCredentials>(CacheItemName);
		}

        public void ForgetCredentialsRebill(CatalitCredentials credentials, CancellationToken cancellationToken)
        {
            credentials.CanRebill = "0";
            credentials.CreditCardLastNumbers = string.Empty;
            credentials.UserId = string.Empty;
            _dataCacheService.PutItem(credentials, CacheItemName, cancellationToken);
        }

		public void RegisterCredentials(CatalitCredentials credentials, CancellationToken cancellationToken)
		{
			_dataCacheService.PutItem( credentials, CacheItemName, cancellationToken );
		}

		public Task<bool> ShouldRetryAuthentication(CatalitCredentials credentials, CancellationToken cancellationToken)
		{
			return Task.FromResult(false);
		}
	}
}
