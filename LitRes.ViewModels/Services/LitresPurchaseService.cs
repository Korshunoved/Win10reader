using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using LitRes.Exceptions;
using LitRes.Models;
using LitRes.Services.Connectivity;
using Windows.UI.Popups;
using LitRes.ViewModels;

namespace LitRes.Services
{
	internal class LitresPurchaseService : ILitresPurchaseService
	{
        
		private readonly ICatalitClient _client;
		private readonly IInAppPurchaseService _inAppPurchaseService;
		private readonly ICatalogProvider _catalogProvider;
		private readonly ICredentialsProvider _credentialsProvider;
		private readonly IProfileProvider _profileProvider;
		private readonly IPurchaseServiceDecorator _purchaseServiceDecorator;
		private readonly IDeviceInfoService _deviceInfoService;
        private SmsResponse smsResponse;
	    private string _termUrl;
	    private ProcessingResponse _processingCenterResponse;
	    private Book _3dsBook;
	    private string _3dsOrderId;

        #region Properties

	    public string TermUrl
	    {
	        get
	        {
	            return _termUrl;
	        }
	    }

	    public ProcessingResponse ProcessingData
	    {
	        get
	        {
	            return _processingCenterResponse;
	        }
	    }
        #endregion

        #region Constructors/Disposer
        public LitresPurchaseService( ICatalogProvider catalogProvider, ICredentialsProvider credentialsProvider, IProfileProvider profileProvider, IInAppPurchaseService inAppPurchaseSevice, ICatalitClient client, IPurchaseServiceDecorator purchaseServiceDecorator, IDeviceInfoService deviceInfoService)
		{
			_inAppPurchaseService = inAppPurchaseSevice;
			_client = client;
			_catalogProvider = catalogProvider;
			_credentialsProvider = credentialsProvider;
			_profileProvider = profileProvider;
			_purchaseServiceDecorator = purchaseServiceDecorator;
			_deviceInfoService = deviceInfoService;
		}
		#endregion

        public Task BuyBookFromLitres(Book book, CancellationToken cancellationToken)
        {
            var task = new Task(async () =>
            {
                //If acoount not exits - create
                CatalitCredentials creds = _credentialsProvider.ProvideCredentials(cancellationToken);
                if (creds == null)
                {
                    creds = await _profileProvider.RegisterDefault(cancellationToken);                    
                    _credentialsProvider.RegisterCredentials(creds, cancellationToken);
                }

                bool isNokiaBook = false;
                if (_deviceInfoService.IsNokiaDevice && !string.IsNullOrEmpty(book.InGifts) && book.InGifts.Equals("1"))
                {
                    var nokiaBook = await _catalogProvider.GetBookByCollection((int)BooksByCategoryViewModel.BooksViewModelTypeEnum.NokiaCollection, book.Id, cancellationToken);
                    if (nokiaBook != null) isNokiaBook = true;
                }

                if (isNokiaBook)
                {
                    await _catalogProvider.TakeBookFromCollectionBySubscription(book.Id, cancellationToken);
                    UpdateBook(book);
                }
                else
                {
                    var parameters = new Dictionary<string, object>
				    {
					    { "art", book.Id }, 
                        { "lfrom", _deviceInfoService.LitresInnerRefId},
                        { "pin", _deviceInfoService.DeviceModel}
				    };

                    LitresPurchaseResponse purchase = null;
                    try
                    {
                        purchase = await _client.LitresPurchaseBook(parameters, cancellationToken, book.isHiddenBook);
                    }
                    catch (CatalitPurchaseException e)
                    {
                        if (((CatalitPurchaseException) e).ErrorCode == 3)
                        {
                            _inAppPurchaseService.CheckProductIsUsed(book.InappName);
                        }
                        purchase = new LitresPurchaseResponse { Account = "-1", Art = "-1" };
                    }

                    if (purchase.Account == "-1" && purchase.Art == "-1") UpdateBookFailed(book);
                    else
                    {
                        _inAppPurchaseService.CheckProductIsUsed(book.InappName);
                        UpdateBook(book);
                    }
                }
            });        
            task.Start();
            return task;
        }

		public async Task BuyBook( Book book, CancellationToken cancellationToken )
		{
			//If acoount not exits - create
			CatalitCredentials creds = _credentialsProvider.ProvideCredentials( cancellationToken );
			if (creds == null)
			{
				creds = await _profileProvider.RegisterDefault( cancellationToken );
				_credentialsProvider.RegisterCredentials( creds, cancellationToken );
			}

			bool isNokiaBook = false;
            if (_deviceInfoService.IsNokiaDevice && !string.IsNullOrEmpty(book.InGifts) && book.InGifts.Equals("1"))
			{
                var nokiaBook = await _catalogProvider.GetBookByCollection((int)BooksByCategoryViewModel.BooksViewModelTypeEnum.NokiaCollection, book.Id, cancellationToken);
                if (nokiaBook != null) isNokiaBook = true;
			}

		    if (isNokiaBook)
		    {
		        await _catalogProvider.TakeBookFromCollectionBySubscription(book.Id, cancellationToken);
		        UpdateBook(book);
		    }
		    else
		    {
		        try
		        {
		            Purchase newPurchase = await _inAppPurchaseService.BuyBook(book);

		            //purchase cancelled or not exist
		            if (newPurchase == null) return;

		            var parameters = new Dictionary<string, object>
		            {
		                {"inapp_data", newPurchase.Win8Inapp},
		                {"lfrom", _deviceInfoService.LitresInAppRefId},
		                {"art", book.Id}
		            };

		            PurchaseResponse purchase = null;
		            try
		            {
		                purchase = await _client.PurchaseBook(parameters, cancellationToken, book.isHiddenBook);
		            }
		            catch (CatalitInappProcessingFailedException e)
		            {
		                if (e.ErrorCode == 309)
		                {
		                    _inAppPurchaseService.CheckProductIsUsed(book.InappName);
		                }
		                purchase = new PurchaseResponse {State = "failed"};
		            }

		            switch (purchase.State)
		            {
		                case "pending":
		                    break;
		                case "unknown":
		                case "closed_ok_book_failed":
		                case "failed":
		                    UpdateBookFailed(book);
		                    break;
		                case "closed_ok":
		                    //ToDo: Do something
		                    _inAppPurchaseService.CheckProductIsUsed(book.InappName);
		                    UpdateBook(book);
		                    break;
		            }
		        }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
		}

	    public async Task Deposit(DepositType depositType, CancellationToken cancellationToken)
	    {
            CatalitCredentials creds = _credentialsProvider.ProvideCredentials(cancellationToken);
            if (creds == null)
            {
                creds = await _profileProvider.RegisterDefault(cancellationToken);
                _credentialsProvider.RegisterCredentials(creds, cancellationToken);
            }

	        Purchase newPurchase = null;
	        try
	        {
	            newPurchase = await _inAppPurchaseService.AddToDeposit(depositType);
	        }
	        catch (Exception ex)
	        {
	            return;
	        }
	        //purchase cancelled or not exist
            if (newPurchase == null) return;

            var parameters = new Dictionary<string, object>
			{
				{ "inapp_data", newPurchase.Win8Inapp },
			};

            PurchaseResponse purchase = null;
            try
            {
                purchase = await _client.PurchaseBook(parameters, cancellationToken, false);
            }
            catch (CatalitInappProcessingFailedException e)
            {
                if (((CatalitInappProcessingFailedException) e).ErrorCode == 309)
                {
                    _inAppPurchaseService.CheckProductIsUsed(depositType);
                }
                purchase = new PurchaseResponse { State = "failed" };               
            }

	        switch (purchase.State)
	        {
	            case "pending":
	                break;
	            case "unknown":
	            case "closed_ok_book_failed":
	            case "failed":
	                UpdateDepositFailed();
	                break;
	            case "closed_ok":
	                //ToDo: Do something
                    {
	                    _inAppPurchaseService.CheckProductIsUsed(depositType);
	                    UpdateDeposit();
	                }
	            break;
            }
            
	    }
        public async Task MobileCommerceInit(double sum, string phone,Book book, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<string, object>
				{
					{ "sum", sum },
					{ "phone", phone }
				};

            var response = await _client.MobileCommerceInit(parameters, cancellationToken);

            if (response.State.Equals("success"))
            {
                await ProcessPurchase(book, response.OrderId, cancellationToken);
            }
            else if (response.State.Equals("failed"))
            {
                string messageBoxBody;
                switch (response.Error)
                {
                    case 1:
                        messageBoxBody = "заказ или пользователь не найдены";
                        break;
                    case 2:
                        messageBoxBody = "указан неправильный номер телефона";
                        break;
                    case 3:
                        messageBoxBody = "указана недопустимая сумма";
                        break;
                    case 4:
                        messageBoxBody = "таймаут платежей";
                        break;
                    case 5:
                        messageBoxBody = "внутренняя ошибка";
                        break;
                    default:
                        messageBoxBody = string.Empty;
                        break;
                }

                if (!string.IsNullOrEmpty(messageBoxBody))
                {
                    await new MessageDialog(messageBoxBody).ShowAsync();
                    //Deployment.Current.Dispatcher.BeginInvoke(() => MessageBox.Show(messageBoxBody));
                } 
            }
        }

        public async Task StartSmsPaymentListener(Book book, CancellationToken cancellationToken)
        {
             var startTimeout = DateTime.Now;
             var startSecond = startTimeout;
             do
             {
                 if (cancellationToken.IsCancellationRequested)
                 {
                     //System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                     //{
                     //    MessageBox.Show("Совершение платежа отменено.");
                     //});
                     return;
                 }

                 if ((DateTime.Now - startSecond).Seconds >= 15)
                 {
                     var parameters = new Dictionary<string, object>{{ "art", 0 }};
                     LitresPurchaseResponse purchase = null;
                     try
                     {
                         purchase = await _client.LitresPurchaseBook(parameters, cancellationToken, book.isHiddenBook);
                     }
                     catch (CatalitPurchaseException e) {}


                     if (purchase!=null && Convert.ToDouble(purchase.Account) >= book.Price)
                     {
                         await BuyBookFromLitres(book, cancellationToken);
                         break;
                     }
                     startSecond = DateTime.Now;
                 }

             } while ((DateTime.Now - startTimeout).Minutes < 5);
        }

        public async Task<SmsResponse> GetSmsPaymentInfo(CancellationToken cancellationToken)
        {
            if (smsResponse == null) smsResponse = await _client.GetSmsPaymentInfo(null, cancellationToken);
            return smsResponse;
        }

        public async Task<CreditCardInitResponse> CreditCardInit(double sum, bool isAuth, bool preventrebil, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<string, object> { { "sum", sum } };
            if (isAuth) parameters.Add("operation", "auth");
            else parameters.Add("operation", "rebill");
            if (!preventrebil) parameters.Add("preventrebill", 1);
            return await _client.CreditCardInit(parameters, cancellationToken);
        }

        public async Task<bool> CreditCardPayment(Book book, double sum, bool isAuth, bool preventrebil, IDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            CreditCardInitResponse response = null;
            string orderId = null;
            Dictionary<string, object> param = null;
            try
            {
                response = await CreditCardInit(sum, isAuth, preventrebil, cancellationToken);
                _termUrl = response.TermUrl;
                param = new Dictionary<string, object>();
                foreach (var _param in response.Params)
                {
                    if (_param.Substitute != null && parameters != null && parameters.ContainsKey(_param.Substitute)) param.Add(_param.Name, parameters[_param.Substitute]);
                    else param.Add(_param.Name, _param.Value);
                    if (_param.Name.Equals("OrderID")) orderId = _param.Value;
                }
                param.Add("ContentType", "xml");
            }
            catch (Exception ex)
            {
                await new MessageDialog("Ошибка инициализации карты.").ShowAsync();
                //Deployment.Current.Dispatcher.BeginInvoke(() => MessageBox.Show("Ошибка инициализации карты."));
                return true;
            }

            ProcessingResponse processingCenterResponse = null;
            try
            {
                processingCenterResponse = await _client.ProcessingServerRequest(response.Url, response.Method, param, cancellationToken);
                if (processingCenterResponse != null)
                {
                    if (processingCenterResponse.Result.Equals("Error"))
                    {
                        if (processingCenterResponse.Code == 6001)
                        {
                            _processingCenterResponse = processingCenterResponse;
                            _3dsBook = book;
                            _3dsOrderId = orderId;
                            return false;
                        }

                        var errorMessage = "Произошла ошибка во время оплаты.";
                        if (processingCenterResponse.Code == 6100 && processingCenterResponse.ErrorCode == 2 && processingCenterResponse.Status.Equals("Declined"))
                        {
                            errorMessage = "Невозможно совершить платеж из Вашего региона.";
                        }

                        await new MessageDialog(errorMessage).ShowAsync();
                        //Deployment.Current.Dispatcher.BeginInvoke(() => MessageBox.Show(errorMessage));

                        return true;
                    }

                    if (orderId != null)
                    {
                        try
                        {
                            var cred = _credentialsProvider.ProvideCredentials(cancellationToken) ?? new CatalitCredentials();
                            var user = await _profileProvider.GetUserInfo(cancellationToken, true);
                            if (user != null)
                            {
                                cred.UserId = user.UserId;
                                cred.CanRebill = "1";
                                if (isAuth && !preventrebil) cred.CanRebill = "0";
                                if (parameters!=null && parameters.ContainsKey("number"))
                                {
                                    var numbers = parameters["number"].ToString();
                                    if (numbers.Length >= 4)
                                    {
                                        cred.CreditCardLastNumbers = numbers.Substring(numbers.Length - 4, 4);
                                    }
                                    numbers = null;
                                }
                                _credentialsProvider.RegisterCredentials(cred, cancellationToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }

                        try
                        {
                            await ProcessPurchase(book, orderId, cancellationToken, 3);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (processingCenterResponse != null && processingCenterResponse.Code == 6001)
                {
                    _processingCenterResponse = processingCenterResponse;
                    _3dsBook = book;
                    _3dsOrderId = orderId;
                    return false;
                }

                if (processingCenterResponse != null &&
                    processingCenterResponse.Code == 6100 &&
                    processingCenterResponse.ErrorCode == 2 &&
                    processingCenterResponse.Status.Equals("Declined") &&
                    processingCenterResponse.Result.Equals("Error"))
                {
                    await new MessageDialog("Невозможно совершить платеж из Вашего региона.").ShowAsync();
                    //Deployment.Current.Dispatcher.BeginInvoke(() => MessageBox.Show("Невозможно совершить платеж из Вашего региона."));   
                }
                else
                {
                    await new MessageDialog("Произошла ошибка во время оплаты.").ShowAsync();
                    //Deployment.Current.Dispatcher.BeginInvoke(() => MessageBox.Show("Произошла ошибка во время оплаты."));   
                }

                return true;
            }
            return true;
        }

        private async Task ProcessPurchase(Book book, string orderId, CancellationToken cancellationToken, int intensity = 15)
        {
            var startTimeout = DateTime.Now;
            var startSecond = startTimeout;

            do
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if ((DateTime.Now - startSecond).Seconds >= intensity)
                {
                    var checkParameters = new Dictionary<string, object> { { "order_id", orderId } };

                    var purchResponse = await _client.CheckPurchaseState(checkParameters, cancellationToken);
                    if (purchResponse.State.Equals("failed"))
                    {
                        //Deployment.Current.Dispatcher.BeginInvoke(() => MessageBox.Show("При совершении платежа произошла непредвиденная ошибка."));
                        await new MessageDialog("При совершении платежа произошла непредвиденная ошибка.").ShowAsync();
                        return;
                    }
                    if (purchResponse.State.Equals("closed_ok"))
                    {
                        await BuyBookFromLitres(book, cancellationToken);
                        return;
                    }
                    startSecond = DateTime.Now;
                }
            } while ((DateTime.Now - startTimeout).Minutes < 5);

            await new MessageDialog("Совершение платежа завершено по таймауту.").ShowAsync();
            //Deployment.Current.Dispatcher.BeginInvoke(() => MessageBox.Show("Совершение платежа завершено по таймауту."));            
        }

        public async Task TakeBookFromLitres(Book book, CancellationToken cancellationToken)
        {
            try
            {
                _purchaseServiceDecorator.UpdateBook(book, true);
            }
            catch (Exception e)
            {
                await new MessageDialog("Невозможно получить книгу").ShowAsync();
                //MessageBox.Show("Невозможно получить книгу");
            }
        }

		private void UpdateBook( Book curBook )
		{
            Analytics.Instance.sendMessage(Analytics.ActionBuyFullCardOk);
            _purchaseServiceDecorator.UpdateBook(curBook);
		}

		private void UpdateBookFailed( Book art )
		{
			_purchaseServiceDecorator.UpdateBookFailed( art );
		}

        private void UpdateDeposit()
        {
            //Analytics.Instance.sendMessage(Analytics.ActionBuyFullCardOk);
            _purchaseServiceDecorator.UpdateAccountDeposit();
        }

        private void UpdateDepositFailed()
        {
            _purchaseServiceDecorator.UpdateAccountDepositFailed();
        }

	    public async Task Process3ds(CancellationToken cancellationToken)
	    {
	        await ProcessPurchase(_3dsBook, _3dsOrderId, cancellationToken, 3);
	    }
	}
}
