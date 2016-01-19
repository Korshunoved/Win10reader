using System.Collections.Generic;
using System.Threading.Tasks;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Models;
using LitRes.Services;
using System.ComponentModel;
using System;
using System.Threading;
using Windows.UI.Popups;

namespace LitRes.ViewModels
{
    public class AccountDepositViewModel : EntityViewModel<Book>
	{
        private const string MobileCommercePart = "MobileCommercePart";
        private const string CreditCardPaymentAsyncPart = "CreditCardPaymentAsyncPart";
        private const string StartSmsPaymentListenerPart = "StartSmsPaymentListenerPart";

        public enum AccountDepositOperationType
        {
            AccountDepositOperationTypeMobile = 1,
            AccountDepositOperationTypeSms = 2,
            AccountDepositOperationTypeCreditCard = 3,
            AccountDepositOperationType3ds = 4
        }

		private readonly ICatalogProvider _catalogProvider;
		private readonly IBookProvider _bookProvider;
		private readonly INavigationService _navigationService;
        private readonly IProfileProvider _profileProvider;
        private readonly ILitresPurchaseService _litresPurchaseService;
        private CancellationTokenSource cancellationTokenSource;
		private bool _loaded;
        private UserInformation _userInformation;
        private Dictionary<string, object> _parametersDictionary;

        public AccountDepositOperationType AccountDepositOperation;

		#region Public Properties
        public UserInformation UserInformation
        {
            get { return _userInformation; }
            private set { SetProperty(ref _userInformation, value, "UserInformation"); }
        }

        public string TermUrl
        {
            get { return ((LitresPurchaseService) _litresPurchaseService).TermUrl; }
        }

        public ProcessingResponse ProcessingData
        {
            get { return ((LitresPurchaseService)_litresPurchaseService).ProcessingData; }
        }

        public Dictionary<string, object> ParametersDictionary
        {
            get { return _parametersDictionary; }
            set 
            {
                if (!object.Equals(_parametersDictionary, value))
                {
                    OnPropertyChanging(_parametersDictionary, value, "ParametersDictionary");
                    _parametersDictionary = value;
                    OnPropertyChanged("ParametersDictionary");
                }
            }
        }
        
		#endregion

		#region Constructors/Disposer
        public AccountDepositViewModel(ICatalogProvider catalogProvider, IBookProvider bookProvider, INavigationService navigationService, IProfileProvider profileProvider, ILitresPurchaseService litresPurchaseService)
		{
			_catalogProvider = catalogProvider;
			_bookProvider = bookProvider;
			_navigationService = navigationService;
            _profileProvider = profileProvider;
            _litresPurchaseService = litresPurchaseService;
		}

		#endregion	

        #region LoadEntity
        protected override Task LoadEntity(Session session)
        {
            Entity = session.Parameters.GetValue<Models.Book>("BookEntity");
            session.AddParameter("LoadBookSessison", Entity);
            return LoadBook(session);
        }
        #endregion

        #region ShouldLoadSession
        protected override bool ShouldLoadEntity(Session session)
        {
            if (session.Parameters.Contains("LoadBookSession"))
            {
                return !_loaded;
            }
            return true;
        }
        #endregion

        #region LoadBook
        private async Task LoadBook(Session session)
        {
            var book = await _catalogProvider.GetBook(Entity.Id, session.Token);

            if (book == null)
            {
                await new MessageDialog(string.Format("Невозможно получить информацию о книге Id={0}", Entity.Id)).ShowAsync();
                _navigationService.GoBack();
                return;
            }
            Entity = book;

            var userInfo = await _profileProvider.GetUserInfo(session.Token);
            if (_userInformation == null) _userInformation = userInfo;
                        
            _loaded = true;
            RunPaymentProcessing();
        }
        #endregion

        public void RunPaymentProcessing()
        {
            new Task(async () =>
            {
                try
                {
                    bool _res = await StartProcess();
                    if (_res)
                    {
                        OnPropertyChanged(new PropertyChangedEventArgs("LetsGetOutOfHere"));
                    }
                    else
                    {
                        OnPropertyChanged(new PropertyChangedEventArgs("Start3dsAuthorization"));
                    }
                }
                catch (Exception ex)
                {
                    OnPropertyChanged(new PropertyChangedEventArgs("LetsGetOutOfHereWithError"));
                }
            }).Start();
        }

        private async Task<bool> StartProcess()
        {
            Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            switch (AccountDepositOperation)
            {

                case AccountDepositOperationType.AccountDepositOperationTypeMobile:
                    {                        
                        await RunMobileCommerce(cancellationTokenSource.Token, Entity);
                        return true;
                    }
                case AccountDepositOperationType.AccountDepositOperationTypeSms:
                    {
                        await SmsListenerAsync(cancellationTokenSource.Token, Entity);
                        return true;
                    }
                case AccountDepositOperationType.AccountDepositOperationTypeCreditCard:
                    {
                        if (ParametersDictionary != null)
                        {
                            return await CreditCardPaymentRuner(ParametersDictionary);
                        }
                        break;
                    }
                case AccountDepositOperationType.AccountDepositOperationType3ds:
                {
                    await _litresPurchaseService.Process3ds(cancellationTokenSource.Token);
                    break;
                }
            }
            return true;
        }

        private async Task RunMobileCommerce(CancellationToken token, Book book)
        {
            var userInfo = await _profileProvider.GetUserInfo(token);
            if (!string.IsNullOrEmpty(userInfo.Phone))
            {
                string phone = userInfo.Phone;
                if (phone.IndexOf('+') == 0 && phone.Length > 2) phone = phone.Substring(2);
                userInfo = await _profileProvider.GetUserInfo(token, true);
                if (string.IsNullOrEmpty(userInfo.Phone)) userInfo.Phone = phone;
                _userInformation = userInfo;
                double sum = Math.Ceiling(book.Price - userInfo.Account);                
                if (sum < 10) sum = 10;
                
                await _litresPurchaseService.MobileCommerceInit(sum, phone, book, token);
            }
        }

        private async Task<bool> CreditCardPaymentRuner(Dictionary<string, object> param)
        {
            var session = new Session(CreditCardPaymentAsyncPart);                       
            session.AddParameter("params", param["params"]);
            session.AddParameter("isSave", param["isSave"]);
            session.AddParameter("isAuth", param["isAuth"]);
            return await CreditCardPaymentAsync(session);
        }

        private async Task<bool> CreditCardPaymentAsync(Session session)
        {
            bool isSave = bool.Parse(session.Parameters.GetValue<string>("isSave"));
            var param = session.Parameters.GetValue<Dictionary<string, object>>("params");
            bool isAuth = bool.Parse(session.Parameters.GetValue<string>("isAuth"));

            if (_userInformation == null)
            {
                var userInfo = await _profileProvider.GetUserInfo(cancellationTokenSource.Token, true);
                _userInformation = userInfo;
            }
            double sum = Entity.Price - _userInformation.Account;
            if (sum < 10) sum = 10;
            return await _litresPurchaseService.CreditCardPayment(Entity, sum, isAuth, isSave, param, cancellationTokenSource.Token);            
        }
  
        private async Task SmsListenerAsync(CancellationToken token, Book book)
        {
            await _litresPurchaseService.StartSmsPaymentListener(book, token);
        }

        public void Cancel()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }
	}
}
