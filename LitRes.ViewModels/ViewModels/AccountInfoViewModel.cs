using System.Diagnostics;
using System.Threading.Tasks;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;

using LitRes.Models;
using LitRes.Services;
using LitRes.Services.Connectivity;
using System.ComponentModel;
using System.Collections.Generic;

namespace LitRes.ViewModels
{
	public class AccountInfoViewModel : ViewModel
	{
        public class Deposit
	    {
            public string LitresPrice { get; set; }
            public string RealPrice { get; set; }            
	        public override string ToString()
	        {
	            return string.Format("{0} за {1}", LitresPrice, RealPrice);
	        }
	    }
	        
		public const string LoadUserInfoPart = "LoadUserInfo";
        public const string ClearUserInfoPart = "ClearUserInfoPart";
        public const string AddToDepositPart = "AddToDepositPart";
		public const string ChangeUserInfoParameter = "ChangeUserInfo";

		private readonly IProfileProvider _profileProvider;
		private readonly INavigationService _navigationService;
		private readonly ICredentialsProvider _credentialsProvider;
		private readonly IDataCacheService _dataCacheService;
		private readonly IFileCacheService _fileCacheService;
        private readonly IBookProvider _bookProvider;
        private readonly ICatalitClient _client;

		private readonly INotificationsProvider _notificationsProvider;
		private readonly IBookmarksProvider _bookmarksProvider;
		private readonly ICatalogProvider _catalogProvider;
	    private readonly ILitresPurchaseService _purchaseService;

        public RelayCommand ClearData { get; private set; }
        public RelayCommand ReloadInfo { get; private set; }
        public RelayCommand<DepositType> AddToDeposit { get; private set; }

		private UserInformation _userInformation;

        public List<Deposit> Deposits { get; private set; }

		#region Public Properties
		public UserInformation UserInformation
		{
			get { return _userInformation; }
			set { SetProperty( ref _userInformation, value, "UserInformation" ); }
		}

        public string LastNumbers;

		#endregion

		#region Constructors/Disposer
        public AccountInfoViewModel(IProfileProvider profileProvider, ICredentialsProvider credentialsProvider, IDataCacheService dataCacheService, IFileCacheService fileCacheService, INavigationService navigationService,
            INotificationsProvider notificationsProvider, IBookmarksProvider bookmarksProvider, ICatalogProvider catalogProvider, IBookProvider bookProvider, ICatalitClient client, ILitresPurchaseService purchaseService)
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
            _client = client;
            _purchaseService = purchaseService;
            RegisterAction(ClearUserInfoPart).AddPart(ClearUserInfoProceed, session => true);
            RegisterAction(AddToDepositPart).AddPart(AddToDepositProceed, session => true);
            RegisterAction(LoadUserInfoPart).AddPart(LoadUserInfoProceed, session => session.Parameters.GetValue<UserInformation>(ChangeUserInfoParameter, null) == null);

            ClearData = new RelayCommand(ClearUserInfo);

            ReloadInfo = new RelayCommand(ReloadUserInfo);

            AddToDeposit = new RelayCommand<DepositType>(dt => AddToDepositFunc(dt));


            Deposits = new List<Deposit> { 
                                       new Deposit { LitresPrice = "105 руб.", RealPrice = "149 руб." }, 
                                       new Deposit { LitresPrice = "189 руб.", RealPrice = "269 руб." }, 
                                       new Deposit { LitresPrice = "304 руб.", RealPrice = "434 руб." }};
		}
		#endregion

        public async void ClearUserInfo()
        {
            await Load(new Session(ClearUserInfoPart));
        }

        private async void ReloadUserInfo()
        {
            _userInformation = null;
            await Load(new Session(LoadUserInfoPart));
        }

        private async void AddToDepositFunc(DepositType dt)
        {
            var session = new Session(AddToDepositPart);
            session.AddParameter("depositType", dt);
            await Load(session);
        }

	    private async Task AddToDepositProceed(Session session)
	    {
	        var dt = session.Parameters.GetValue<DepositType>("depositType");
	        await _purchaseService.Deposit(dt, session.Token);
	    }

		private async Task ClearUserInfoProceed(Session session)
		{
            OnPropertyChanged(new PropertyChangedEventArgs("LockClearDataButton"));
            var userInfo = await _profileProvider.GetUserInfo(session.Token);
            var cnt = _credentialsProvider.ProvideCredentials(session.Token);
            if (cnt != null) userInfo.OldPassword = cnt.Password;
            userInfo.Phone = string.Empty;
            userInfo.CanRebill = "0";            
            
            await _profileProvider.ChangeUserInfo(userInfo, session.Token);
            _credentialsProvider.ForgetCredentialsRebill(cnt, session.Token);

		    var purgeRebilsResponse = await _client.PurgeRebils(session.Token);
            Debug.WriteLine("Purge Response Result = {0}", purgeRebilsResponse.Result);
            OnPropertyChanged(new PropertyChangedEventArgs("UserInfoCleared")); 
		}

		#region LoadUserInfoProceed
		private async Task LoadUserInfoProceed(Session session)
		{
			var creds = _credentialsProvider.ProvideCredentials( session.Token );
			var userInfo = await _profileProvider.GetUserInfo( session.Token, true );
			UserInformation = userInfo;
            LastNumbers = "xxxx";
            if (creds != null)
            {
                LastNumbers = creds.CreditCardLastNumbers;
                UserInformation.CanRebill = creds.CanRebill;
            }
            OnPropertyChanged(new PropertyChangedEventArgs("UserInfoLoaded"));
		}
		#endregion
	}
}
