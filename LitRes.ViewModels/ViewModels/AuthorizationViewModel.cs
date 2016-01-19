using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Exceptions;
using LitRes.Models;
using LitRes.Services;
using LitRes.Services.Connectivity;

namespace LitRes.ViewModels
{
	public class AuthorizationViewModel : ViewModel
	{
		public const string LoginParameter = "LoginCredentials";

		public const string MainPart = "Main";
		public const string LoginPart = "Login";
		private readonly ICredentialsProvider _credentialsProvider;
		private readonly IProfileProvider _profileProvider;
		private readonly INavigationService _navigationService;
		private readonly INotificationsProvider _notificationsProvider;
		private readonly IDataCacheService _dataCacheService;
		private readonly IFileCacheService _fileCacheService;
		private readonly IBookmarksProvider _bookmarksProvider;
		private readonly ICatalogProvider _catalogProvider;
	    private readonly IBookProvider _bookProvider;

		private CatalitCredentials _credential;
		private UserInformation _userInformation;
		private string _accountName;

		#region Public Properties
		public string AccountName
		{
			get { return _accountName; }
			set { SetProperty( ref _accountName, value, "AccountName" ); }
		}

		public CatalitCredentials Credential
		{
			get { return _credential; }
			private set { SetProperty( ref _credential, value, "Credential" ); }
		}
		public UserInformation UserInformation
		{
			get { return _userInformation; }
			private set { SetProperty( ref _userInformation, value, "UserInformation" ); }
		}
		#endregion

		#region Constructors/Disposer
		public AuthorizationViewModel( ICredentialsProvider credentialsProvider, IProfileProvider profileProvider, INavigationService navigationService, INotificationsProvider notificationsProvider,
			IDataCacheService dataCacheService, IFileCacheService fileCacheService, IBookmarksProvider bookmarksProvider, ICatalogProvider catalogProvider, IBookProvider bookProvider)
		{
			_credentialsProvider = credentialsProvider;
			_navigationService = navigationService;
			_profileProvider = profileProvider;
			_notificationsProvider = notificationsProvider;
			_dataCacheService = dataCacheService;
			_fileCacheService = fileCacheService;
			_bookmarksProvider = bookmarksProvider;
			_catalogProvider = catalogProvider;
		    _bookProvider = bookProvider;

            RegisterAction(MainPart).AddPart(session => LoadCredential(session), session => true);
            RegisterAction(LoginPart).AddPart(session => LoginProceed(session), session => true);

		}
		#endregion

		#region Login
		public async Task Login(CatalitCredentials credentials)
		{
			Session session = new Session( LoginPart );
			session.AddParameter( LoginParameter, credentials );
			await Load( session );
		}
		#endregion

		#region LoadCredential
		private async Task LoadCredential(Session session)
		{
			var credential = _credentialsProvider.ProvideCredentials( session.Token );
			if(credential != null)
			{
				Credential = credential;
				if(!credential.IsRealAccount)
				{
					AccountName = credential.Login;
				}
			}
		}
		#endregion
		
		#region LoginProceed
		private async Task LoginProceed(Session session)
		{
			var credential = session.Parameters.GetValue<CatalitCredentials>( LoginParameter );

			var userInfo = await _profileProvider.Authorize( credential, session.Token );
			if (userInfo != null)
			{
				//Merge accounts if exits not user
				var exitsCredentials =  _credentialsProvider.ProvideCredentials( session.Token );                
				if (exitsCredentials != null && !exitsCredentials.IsRealAccount)
				{
                    var userBooks = _catalogProvider.GetMyBooksByTimeLocal();                   
                    if (userInfo.AccountType == (int)AccountTypeEnum.AccountTypeLibraryAndShop || 
                        (userBooks != null && userBooks.Count > 0 && userInfo.AccountType == (int)AccountTypeEnum.AccountTypeLibrary))
                    {
                        try
                        {
                            userInfo =
                                await
                                    _profileProvider.MergeAccounts(userInfo.SessionId, exitsCredentials, session.Token);
                        }
                        catch (Exception ex)
                        {
                            ex = ex;
                            Debug.WriteLine(ex.Message);
                        }
                    }                    
				}

                //Clear caches
			    try
			    {
			        await LogoutProceed(session);
			    }
			    catch (Exception ex)
			    {
			        ex = ex;
			    }
			    credential.IsRealAccount = true;
			    credential.Sid = userInfo.SessionId;
				_credentialsProvider.RegisterCredentials( credential, session.Token );
				Credential = credential;
				UserInformation = userInfo;

				await _notificationsProvider.RefreshNotifications( session.Token );//Load profile notifications
                
                _catalogProvider.ClearBooksCollections();
			}
		}

		#endregion

		#region LogoutProceed
		private async Task LogoutProceed( Session session )
		{
		    await _bookProvider.ClearNotLoadedBooks(session.Token);
			_dataCacheService.ClearCache( session.Token );
			_fileCacheService.ClearStorage( session.Token );
			_notificationsProvider.Clear();
			_bookmarksProvider.Clear();
			_catalogProvider.Clear();		    
		}
		#endregion

		
	}
}
