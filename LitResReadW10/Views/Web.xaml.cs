using System;

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Digillect.Mvvm.Services;
using Digillect.Mvvm.UI;

namespace LitRes.Views
{
	[View( "Web" )]
	public partial class Web : Page
	{
		#region Constructors/Disposer
		public Web()
		{
			//InitializeComponent();
		}
		#endregion

		#region View Events

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

          //  if (NavigationContext.QueryString.ContainsKey("NavigatedFrom"))
          //  {
          //      if (NavigationContext.QueryString["NavigatedFrom"].ToString().Equals("toast"))
          //      {
          //          if (NavigationContext.QueryString.ContainsKey(PushNotificationsViewModel.SPAMPACK_TAG))
          //              PushNotificationsViewModel.SendToastSpampack(NavigationContext.QueryString[PushNotificationsViewModel.SPAMPACK_TAG]);

          //          if (NavigationContext.QueryString.ContainsKey("id"))
          //          {
          //              WebBrowserTask web = new WebBrowserTask();
          //              web.Uri = new Uri(NavigationContext.QueryString["id"]);
          //              web.Show();
          //          }
          //      }
          //  }

          //if(NavigationService.CanGoBack) NavigationService.GoBack();
          //else NavigationService.Navigate(new Uri("/Views/Main.xaml", UriKind.RelativeOrAbsolute));
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        //    if (!NavigationService.CanGoBack) NavigationService.Navigate(new Uri("/Views/Main.xaml", UriKind.RelativeOrAbsolute));
        }
		
		#endregion
	}
}