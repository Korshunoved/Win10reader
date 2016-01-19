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
using System;
using System.ComponentModel;
using System.Text;
using Windows.UI.Popups;

namespace LitRes.ViewModels
{
    public class CreditCardPurchaseViewModel : EntityViewModel<Book>
	{

		private readonly ICatalogProvider _catalogProvider;
		private readonly IBookProvider _bookProvider;
		private readonly INavigationService _navigationService;
        private readonly IProfileProvider _profileProvider;

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
        public CreditCardPurchaseViewModel(ICatalogProvider catalogProvider, IBookProvider bookProvider, INavigationService navigationService, IProfileProvider profileProvider)
		{
			_catalogProvider = catalogProvider;
			_bookProvider = bookProvider;
			_navigationService = navigationService;
            _profileProvider = profileProvider;
		}
		#endregion	

        #region LoadEntity
 
        protected override Task LoadEntity(Session session)
        {
            return LoadBook(session);
        }
        #endregion

        #region LoadBook
        private async Task LoadBook(Session session)
        {
            var book = session.Parameters.GetValue<Book>("BookEntity");


            if (book == null)
            {
                await new MessageDialog("Невозможно получить информацию о книге").ShowAsync();
                _navigationService.GoBack();
                return;
            }
            Entity = book;

            var userInfo = await _profileProvider.GetUserInfo(session.Token, true);
            if (_userInformation == null) _userInformation = userInfo;

            if (string.IsNullOrEmpty(UserInformation.Email)) OnPropertyChanged(new PropertyChangedEventArgs("CreditEmailBlockShow"));

            OnPropertyChanged(new PropertyChangedEventArgs("PriceShow"));
            _loaded = true;

        }
        #endregion

        public void ShowProcessView(Dictionary<string, object> parameters)
        {
            var param = XParameters.Empty.ToBuilder()
                .AddValue("BookEntity", Entity)
                .AddValue("Id", Entity.Id)
                .AddValue("Operation",
                    (int) AccountDepositViewModel.AccountDepositOperationType.AccountDepositOperationTypeCreditCard)
                .AddValue("ParametersDictionary", ModelsUtils.DictionaryToString(parameters));
     
            _navigationService.Navigate("AccountDeposit", param.ToImmutable());
        }
            
	}
}
