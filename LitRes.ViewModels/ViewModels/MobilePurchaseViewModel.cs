using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Digillect;
using Digillect.Collections;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Exceptions;
using LitRes.Models;
using LitRes.Services;
using LitRes.Services.Connectivity;
using System.ComponentModel;
using System;

namespace LitRes.ViewModels
{
    public class MobilePurchaseViewModel : EntityViewModel<int, Book>
	{
		private readonly ICatalogProvider _catalogProvider;
		private readonly IBookProvider _bookProvider;
		private readonly INavigationService _navigationService;
        private readonly IProfileProvider _profileProvider;
        private readonly ICredentialsProvider _credentialsProvider;

		private bool _loaded;
        private UserInformation _userInformation;

		#region Public Properties
        public UserInformation UserInformation
        {
            get { return _userInformation; }
            private set { SetProperty(ref _userInformation, value, "UserInformation"); }
        }
		#endregion

		#region Constructors/Disposer
        public MobilePurchaseViewModel(ICatalogProvider catalogProvider, IBookProvider bookProvider, INavigationService navigationService, IProfileProvider profileProvider, ICredentialsProvider credentialsProvider)
		{
			_catalogProvider = catalogProvider;
			_bookProvider = bookProvider;
            _credentialsProvider = credentialsProvider;
			_navigationService = navigationService;
            _profileProvider = profileProvider;
		}

		#endregion	

        #region LoadEntity
        protected override Task LoadEntity(EntitySession<int> session)
        {
            return LoadBook(session);
        }
        #endregion

        #region LoadBook
        private async Task LoadBook(EntitySession<int> session)
        {
            var book = await _catalogProvider.GetBook(session.Id, session.Token);

            if (book == null)
            {
                System.Windows.MessageBox.Show(string.Concat("Невозможно получить информацию о книге Id=", Convert.ToString(session.Id)));
                (System.Windows.Application.Current.RootVisual as Microsoft.Phone.Controls.PhoneApplicationFrame).GoBack();
                return;
            }
            Entity = book;

            var userInfo = await _profileProvider.GetUserInfo(session.Token, true);
            if (_userInformation == null) _userInformation = userInfo;

            if (string.IsNullOrEmpty(UserInformation.Phone)) OnPropertyChanged(new PropertyChangedEventArgs("WithoutPhoneBlockEnable"));
            else OnPropertyChanged(new PropertyChangedEventArgs("WithPhoneBlockEnable"));

            _loaded = true;

        }
        #endregion

        public async Task RunMobileCommerceSaveNubmerAndStart(string number, bool save = false)
        {
            var userInfo = await _profileProvider.GetUserInfo(System.Threading.CancellationToken.None, true);
            userInfo.Phone = number;
            var cnt = await _credentialsProvider.ProvideCredentials(System.Threading.CancellationToken.None);
            if (cnt != null) userInfo.OldPassword = cnt.Password;

            if (save) await _profileProvider.ChangeUserInfo(userInfo, System.Threading.CancellationToken.None);

            var param = new Parameters() {  { "Id", Entity.Id },
                                            { "Operation", (int)AccountDepositViewModel.AccountDepositOperationType.AccountDepositOperationTypeMobile }                                               
                                         };
            _navigationService.Navigate("AccountDeposit", param);
        }
	}
}
