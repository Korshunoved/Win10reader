using System.Threading.Tasks;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;

using LitRes.Models;
using LitRes.Services;
using LitRes.Services.Connectivity;
using Digillect.Collections;
using System.ComponentModel;
using Digillect;

namespace LitRes.ViewModels
{
	public class ChangeUserInfoViewModel : ViewModel
	{
		public const string LoadUserInfoPart = "LoadUserInfo";
		public const string LogoutPart = "Logout";
		public const string ChangeUserInfoPart = "ChangeUserInfo";
		public const string ChangeUserInfoParameter = "ChangeUserInfo";

		private readonly IProfileProvider _profileProvider;
		private readonly INavigationService _navigationService;
		private readonly ICredentialsProvider _credentialsProvider;
		private readonly IDataCacheService _dataCacheService;
		private readonly IFileCacheService _fileCacheService;
        private readonly IBookProvider _bookProvider;

		private readonly INotificationsProvider _notificationsProvider;
		private readonly IBookmarksProvider _bookmarksProvider;
		private readonly ICatalogProvider _catalogProvider;

		private UserInformation _userInformation;
		private string _login;

		#region Public Properties
		public UserInformation UserInformation
		{
			get { return _userInformation; }
			set { SetProperty( ref _userInformation, value, "UserInformation" ); }
		}

		public string Login
		{
			get { return _login; }
			set { SetProperty( ref _login, value, "Login" ); }
		}

		public RelayCommand<bool> ShowChangePassword { get; private set; }
        public RelayCommand ToRegistration { get; private set; }
		#endregion

		#region Constructors/Disposer
		public ChangeUserInfoViewModel(IProfileProvider profileProvider, ICredentialsProvider credentialsProvider, IDataCacheService dataCacheService, IFileCacheService fileCacheService, INavigationService navigationService,
            INotificationsProvider notificationsProvider, IBookmarksProvider bookmarksProvider, ICatalogProvider catalogProvider, IBookProvider bookProvider)
		{
			_navigationService = navigationService;
			_profileProvider = profileProvider;
			_credentialsProvider = credentialsProvider;
			_dataCacheService = dataCacheService;
			_fileCacheService = fileCacheService;

			_notificationsProvider = notificationsProvider;
			_bookmarksProvider = bookmarksProvider;
			_catalogProvider = catalogProvider;

            _bookProvider = bookProvider;

            RegisterAction(ChangeUserInfoPart).AddPart(session => ChangeUserInfoProceed(session), session => session.Parameters.GetValue<UserInformation>(ChangeUserInfoParameter, null) != null);
            RegisterAction(LoadUserInfoPart).AddPart(session => LoadUserInfoProceed(session), (session) =>
            {
                return session.Parameters.GetValue<UserInformation>(ChangeUserInfoParameter, null) == null;
            }
            );
            RegisterAction(LogoutPart).AddPart(session => LogoutProceed(session), session => session.Parameters.GetValue<UserInformation>(ChangeUserInfoParameter, null) == null);

			ShowChangePassword = new RelayCommand<bool>((isDesktop) => _navigationService.Navigate("ChangePassword", isDesktop));
            ToRegistration = new RelayCommand(() => _navigationService.Navigate("Registration", XParameters.Create("toCreateShopAccount", true)));
		}
		#endregion

		#region Logout
		public async Task Logout()
		{
			await Load( new Session( LogoutPart ) );
		}
		#endregion

		#region ChangeUserInfo
		public async Task ChangeUserInfo(UserInformation information)
		{
			Session session = new Session( ChangeUserInfoPart );
			session.AddParameter( ChangeUserInfoParameter, information );
			await Load( session );
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
            await _bookProvider.ClearLibrariesBooks(session.Token);
		    _catalogProvider.ClearBooksCollections();
            OnPropertyChanged(new PropertyChangedEventArgs("GetOutOfHere"));
        }
		#endregion

		#region ChangeUserInfoProceed
		private async Task ChangeUserInfoProceed(Session session)
		{
            var user = session.Parameters.GetValue<UserInformation>(ChangeUserInfoParameter);
            var cnt =  _credentialsProvider.ProvideCredentials(System.Threading.CancellationToken.None);
            if(cnt != null) user.OldPassword = cnt.Password;
			await _profileProvider.ChangeUserInfo( user, session.Token );
			//ToDo: What next?
		}
        #endregion

        #region LoadUserInfoProceed

	    public async void LoadUserInfoProceed()
	    {
	        await Load(new Session(LoadUserInfoPart));
	    }

        private async Task LoadUserInfoProceed(Session session)
		{
			var creds =  _credentialsProvider.ProvideCredentials( session.Token );
			Login = creds.Login;

			var userInfo = await _profileProvider.GetUserInfo( session.Token );
			UserInformation = userInfo;
            OnPropertyChanged(new PropertyChangedEventArgs("UserInfoLoaded"));
		}
		#endregion
	}
}
