using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Email;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Autofac;
using Digillect.Mvvm.UI;
using LitRes.Services;
using LitRes.Services.Connectivity;
using LitResReadW10;
using LitResReadW10.Controls;
using LitResReadW10.Helpers;

namespace LitRes.Views
{
	[View( "About" )]
	public partial class About : Page
	{
        private readonly IDeviceInfoService _deviceInfoService = ((App)App.Current).Scope.Resolve<IDeviceInfoService>();

        public string AboutTitle => SystemInfoHelper.IsDesktop() ? "О программе" : string.Empty;

	    protected override void OnNavigatedTo(NavigationEventArgs e)
	    {
            if(!SystemInfoHelper.IsDesktop())  ControlPanel.Instance.TopBarTitle = "О программе";
            base.OnNavigatedTo(e);
	    }

	    #region Public Properties
		public string ApplicationVersion => _deviceInfoService.ApplicationVersion;

	    #endregion

		#region Constructors/Disposer
		public About()
		{
            InitializeComponent();
		    if (!SystemInfoHelper.IsDesktop())
		        Title.Visibility = Visibility.Collapsed;
		    Loaded += About_Loaded;
		}

        void About_Loaded(object sender, RoutedEventArgs e)
        {
            Analytics.Instance.sendMessage(Analytics.ViewAbout);
        }
        #endregion

        #region View Events
        private async void SendEmail_Click( object sender, RoutedEventArgs e )
		{
            EmailMessage emailMessage = new EmailMessage();

            var credentialsProvider = ((App) Application.Current).Scope.Resolve<ICredentialsProvider>();
            var creds =  credentialsProvider.ProvideCredentials( CancellationToken.None );

            var catalogProvider = ((App) Application.Current).Scope.Resolve<ICatalogProvider>();
            var books = await catalogProvider.GetMyBooksFromCache( CancellationToken.None );

            var bookProvider = ((App) Application.Current).Scope.Resolve<IBookProvider>();
            var exists = await bookProvider.GetExistBooks( CancellationToken.None );

            string message = "\r\n\r\n----- Техническая информация:\r\n";

            message += string.Format("Версия приложения: ЛитРес: Читай! версии {0}\r\n", ApplicationVersion);
            message += string.Format("Версия ОС: Windows 10 версии {0}\r\n", _deviceInfoService.OsVersion);

            if( creds != null )
            {
            	message += string.Format( "Логин: {0}\r\n", creds.Login );
            }

            if( books != null && books.Count > 0)
            {
            	message += string.Format( "Последняя книга: {0}, {1}\r\n", books[0].Id, books[0].Description.Hidden.TitleInfo.BookTitle );
            }

            if( exists != null )
            {
            	message += string.Format( "Всего книг на устройстве: {0}\r\n", exists.Count );
            }
          
            emailMessage.To.Add(new EmailRecipient("win10@litres.ru"));
		    emailMessage.Body = message;
            await EmailManager.ShowComposeNewEmailAsync(emailMessage);
        }
        #endregion

        private async void FacebookStackPanel_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var uriBing = new Uri(@"https://www.facebook.com/mylitres");
            await Windows.System.Launcher.LaunchUriAsync(uriBing);
        }

        private async void Star45_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var uriBing = new Uri(@"ms-windows-store://review/?ProductId=9nblggh4p89b");
            await Windows.System.Launcher.LaunchUriAsync(uriBing);
        }

	    private async void Star123_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
	    {
	        var dialog =
	            new MessageDialog(string.Format("Если вы знаете, как нам улучшить приложение, напишите, пожалуйста",
	                "Поддержка"))
	            {
	                DefaultCommandIndex = 0,
	                CancelCommandIndex = 1
	            };
	        dialog.Commands.Add(new UICommand("Написать в поддержку", command => EmailHelper.OpenEmailClientWithoutPayload()));
            dialog.Commands.Add(new UICommand("Не сейчас") { Id = 1 });
            await dialog.ShowAsync();
        }
    }
}