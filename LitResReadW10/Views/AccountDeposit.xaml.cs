using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Digillect.Mvvm.UI;
using LitRes.ViewModels;
using System.ComponentModel;
using System.Net;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using Autofac;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitResReadW10;
using LitResReadW10.Controls;
using LitResReadW10.Helpers;

namespace LitRes.Views
{
    [View("AccountDeposit")]
    [ViewParameter("Id", typeof(int), Required = false)]
    [ViewParameter("Operation", typeof(int), Required = false)]
    [ViewParameter("ParametersDictionary", typeof(string), Required = false)]
    
	public partial class AccountDeposit : AccountDepositFitting
    {
        public string Title => SystemInfoHelper.IsDesktop() ? "Пополнение счета" : string.Empty;
        private bool isPopupOpen = false;
        private readonly INavigationService _navigationService = ((App)App.Current).Scope.Resolve<INavigationService>();

        #region Constructors/Disposer
        public AccountDeposit()
		{
			InitializeComponent();
		}
		#endregion			

        #region CreateDataSession
        protected override Session CreateDataSession(DataLoadReason reason)
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            if (ViewParameters.GetValue<int>("Operation") != 0)
            {
                ViewModel.AccountDepositOperation = (AccountDepositViewModel.AccountDepositOperationType)ViewParameters.GetValue<int>("Operation");

                if (ViewModel.AccountDepositOperation == AccountDepositViewModel.AccountDepositOperationType.AccountDepositOperationTypeMobile)
                    bodyTextBlock.Text = "Следуйте инструкциям, отправленным в sms-сообщении.";
            }
            if (ViewParameters.GetValue<string>("ParametersDictionary") != null)
            {
                var encodeParamDict = ViewParameters.GetValue<string>("ParametersDictionary");
                if(!string.IsNullOrEmpty(encodeParamDict))
                {
                    var dictionary = new Dictionary<string, object>();
                    var parameters = new Dictionary<string, object>();
                    var keyValueArray = encodeParamDict.Split('|');
                    foreach(var item in keyValueArray)
                    {
                        var keyValue = item.Split(':');
                        if (keyValue[0].Equals("isSave") || keyValue[0].Equals("isAuth"))
                        {
                            dictionary.Add(keyValue[0], keyValue[1]);
                        }
                        else
                        {
                            parameters.Add(keyValue[0], keyValue[1]);
                        }
                    }
                    dictionary.Add("params", parameters);
                    ViewModel.ParametersDictionary = dictionary;
                }
            }

            return base.CreateDataSession(reason);
        }
        #endregion

        private async new void GoBack()
        {
            await
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High,
                        () =>
                        {
                            if (_navigationService.CanGoBack()) _navigationService.GoBack();
                            else
                            {
                                _navigationService.Navigate("MyBooks");
                                _navigationService.RemoveBackEntry();
                            }
                        });
        }

        private async void GoBackWithError()
        {
            await new MessageDialog("Произошла ошибка во время оплаты.").ShowAsync();
            try
            {
                await
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High,
                        () =>
                        {
                            if (_navigationService.CanGoBack()) _navigationService.GoBack();
                            else
                            {
                                _navigationService.Navigate("MyBooks");
                                _navigationService.RemoveBackEntry();
                            }
                        });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("LetsGetOutOfHere"))
            {
                GoBack();   
            }
            else if (e.PropertyName.Equals("LetsGetOutOfHereWithError"))
            {
                GoBackWithError();
            }
            else if (e.PropertyName.Equals("Start3dsAuthorization"))
            {
                Run3DsPayment();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (!SystemInfoHelper.IsDesktop()) ControlPanel.Instance.TopBarTitle = "Пополнение счета";
            if (isPopupOpen) webView.Visibility = Visibility.Visible;
            
            else if (_navigationService.CanGoBack()) _navigationService.RemoveBackEntry();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            ViewModel.Cancel();
            if (_navigationService.CanGoBack()) _navigationService.GoBack();
            else
            {
                _navigationService.Navigate("Main.xaml");
                _navigationService.RemoveBackEntry();
            }
        }

        private async void Run3DsPayment()
        {
            await
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                if (ViewModel.ProcessingData != null && ViewModel.TermUrl != null)
                {
                    string url = ViewModel.ProcessingData.Secure.Acsurl;
                    string postData = String.Format("PaReq={0}&MD={1}&TermUrl={2}",
                        WebUtility.UrlEncode(ViewModel.ProcessingData.Secure.Pareq),
                        WebUtility.UrlEncode(String.Format("{0};{1}", ViewModel.ProcessingData.Id, ViewModel.ProcessingData.Secure.Pd)),
                        WebUtility.UrlEncode(ViewModel.TermUrl));

                    Debug.WriteLine(url);
                    Debug.WriteLine(postData);
                    Debug.WriteLine("");

                    webView.Visibility = Visibility.Visible;
                    isPopupOpen = true;
                    webView.Settings.IsJavaScriptEnabled = true;
                    var httpReqMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri(url),
                        Method = HttpMethod.Post,
                        Content = new HttpStringContent(postData),
                       
                    };
                    httpReqMessage.Content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/x-www-form-urlencoded");
                   // httpReqMessage.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    webView.NavigateWithHttpRequestMessage(httpReqMessage);
                //    webView.Navigate(new Uri(url), Encoding.UTF8.GetBytes(postData), "Content-Type: application/x-www-form-urlencoded" + Environment.NewLine);
                }
                else
                {
                    new MessageDialog("Произошла ошибка во время оплаты.").ShowAsync();

                    if (_navigationService.CanGoBack()) _navigationService.GoBack();
                    else
                    {
                        _navigationService.Navigate("MyBooks");
                        _navigationService.RemoveBackEntry();
                    }
                }
            }); 
        }

        private void WebBrowser_OnLoadCompleted(object sender, NavigationEventArgs e)
        {
            Debug.WriteLine("");
            Debug.WriteLine("On load completed");
            Debug.WriteLine(e.Content);
            if (e.Uri != null)
            {
                try
                {
                   // Debug.WriteLine(webView.SaveToString());
                    if (e.Uri.AbsoluteUri.Contains("https://secure.payonlinesystem.com/payment/transaction/auth/3ds/"))
                    {
                        ViewModel.AccountDepositOperation = AccountDepositViewModel.AccountDepositOperationType.AccountDepositOperationType3ds;                        
                        ViewModel.RunPaymentProcessing();
                        webView.Visibility = Visibility.Collapsed;
                        isPopupOpen = false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

      

        private void WebView_OnNavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            Debug.WriteLine("Navigated");
            Debug.WriteLine(args.Uri);
        }
    }

    public class AccountDepositFitting : EntityPage<Models.Book, AccountDepositViewModel>
	{
	}
}