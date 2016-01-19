using System;
using System.Threading.Tasks;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;

using LitRes.Models;
using LitRes.Services;
using LitRes.Services.Connectivity;

namespace LitRes.ViewModels
{
	public class RegistrationViewModel : ViewModel
	{
		public const string RegistrationPart = "Registration";
		public const string RegistrationParameter = "LoginCredentials";

		private readonly ICredentialsProvider _credentialsProvider;
		private readonly IProfileProvider _profileProvider;
        private readonly ICatalogProvider _catalogProvider;

		private CatalitCredentials _credential;

		#region Public Properties
		public CatalitCredentials Credential
		{
			get { return _credential; }
			private set { SetProperty( ref _credential, value, "Credential" ); }
		}
        public bool toCreateShopAccount { get; set; }
		#endregion

		#region Constructors/Disposer
        public RegistrationViewModel(ICredentialsProvider credentialsProvider, IProfileProvider profileProvider, INavigationService navigationService, ICatalogProvider catalogProvider)
		{
			_credentialsProvider = credentialsProvider;
			_profileProvider = profileProvider;
            _catalogProvider = catalogProvider;
            RegisterAction(RegistrationPart).AddPart(session => RegistrationProceed(session), session => session.Parameters.GetValue<CatalitCredentials>(RegistrationParameter, null) != null);
		}
		#endregion

		#region RegistrationRequest
		public async Task Register(CatalitCredentials credentials)
		{
			Session session = new Session( RegistrationPart );
			session.AddParameter(RegistrationParameter, credentials);
			await Load(session);
		}

		private async Task RegistrationProceed(Session session)
		{
			var credential = session.Parameters.GetValue<CatalitCredentials>(RegistrationParameter);
            var exitsCredentials =  _credentialsProvider.ProvideCredentials(session.Token);

            var userInfo = await _profileProvider.Register(credential, session.Token, exitsCredentials);

            if (userInfo != null && !string.IsNullOrEmpty(userInfo.SessionId))
			{
				//Merge accounts if exits not user			
                if (toCreateShopAccount || (exitsCredentials != null && !exitsCredentials.IsRealAccount))
				{
				    try
				    {
                        await _profileProvider.MergeAccounts(userInfo.SessionId, exitsCredentials, session.Token);
				    }
                    catch(Exception e) {}
				}

                credential.Sid = userInfo.SessionId;
				_credentialsProvider.RegisterCredentials(credential, session.Token);
				Credential = credential;
                _catalogProvider.CheckBooks();
			}
			//ToDo: What next? Go back?
		}

		#endregion
	}
}
