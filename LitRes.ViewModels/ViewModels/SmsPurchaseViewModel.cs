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
using Microsoft.Phone.Tasks;
using System.Threading;
using System.Text;
using Windows.ApplicationModel.Chat;

namespace LitRes.ViewModels
{
    public class SmsPurchaseViewModel : EntityViewModel<int, Book>
	{
        private const string GetSmsPaymentInfoPart       = "GetSmsPaymentInfoPart";      

		private readonly ICatalogProvider _catalogProvider;
		private readonly IBookProvider _bookProvider;
		private readonly INavigationService _navigationService;
        private readonly ILitresPurchaseService _litresPurchaseService;
        private readonly IDeviceInfoService _deviceInfoService;
        private readonly IProfileProvider _profileProvider;

		private bool _loaded;
        private XCollection<Country> _countries;

		#region Public Properties        
        public XSubRangeCollection<Country> Countries { get; private set; }
        public string getMobileOperator { 
            get { 
                return _deviceInfoService.MobileOperator; 
            } 
        }

		#endregion

		#region Constructors/Disposer
        public SmsPurchaseViewModel(ICatalogProvider catalogProvider, IBookProvider bookProvider, INavigationService navigationService, ILitresPurchaseService litresPurchaseService, IDeviceInfoService deviceInfoService, IProfileProvider profileProvider)
		{
			_catalogProvider = catalogProvider;
			_bookProvider = bookProvider;
			_navigationService = navigationService;
            _litresPurchaseService = litresPurchaseService;
            _deviceInfoService = deviceInfoService;
            _profileProvider = profileProvider;

            RegisterPart(GetSmsPaymentInfoPart, (session, part) => GetSmsPaymentInfoAsync(session), (session, part) => true, false);          

            _countries = new XCollection<Country>();
            Countries = new XSubRangeCollection<Country>(_countries, 0, 100);
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
            GetSmsPaymentInfo();
            _loaded = true;

        }
        #endregion

        private async void GetSmsPaymentInfo()
        {
            await Load(new Session(GetSmsPaymentInfoPart));
        }

        private async Task GetSmsPaymentInfoAsync(Session session)
        {
            var smsInfo = await _litresPurchaseService.GetSmsPaymentInfo(session.Token);
            _countries.Clear();
            _countries.Update(smsInfo.Countries);
            OnPropertyChanged(new PropertyChangedEventArgs("SmsLoaded"));
        }

        public async Task RunSmsLauncher(Number number)
        {
            var userInfo = await _profileProvider.GetUserInfo(CancellationToken.None, true);
            var msg = new StringBuilder();
            if (!string.IsNullOrEmpty(number.Subprefix))
            {
                msg.Append(number.Subprefix);
                msg.Append(" ");
            }
            msg.Append(number.Prefix);
            msg.Append(" ");
            msg.Append(userInfo.UserId);
            await ChatMessageManager.ShowComposeSmsMessageAsync(new ChatMessage
            {
                Recipients = { number.PhoneNumber.ToString() },
                Body = msg.ToString()
            });

            //System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            //{
            //    var sms = new SmsComposeTask();
            //    sms.To = number.PhoneNumber.ToString();
            //    var msg = new StringBuilder();
            //    if (!string.IsNullOrEmpty(number.Subprefix))
            //    {
            //        msg.Append(number.Subprefix);
            //        msg.Append(" ");
            //    }
            //    msg.Append(number.Prefix);
            //    msg.Append(" ");
            //    msg.Append(userInfo.UserId);
            //    sms.Body = msg.ToString();
            //    sms.Show();
            //});
           
        }

        public void ShowSmsProcessView()
        {

            var param = new Parameters() {  { "Id", Entity.Id },
                                            { "Operation", (int)AccountDepositViewModel.AccountDepositOperationType.AccountDepositOperationTypeSms }
                                        };
            
            _navigationService.Navigate("AccountDeposit", param);
        }
	}
}
