using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Networking.PushNotifications;
using Autofac;
using LitRes.Services.Connectivity;

namespace LitRes.Services
{
	public class PushNotificationsService : IPushNotificationsService, IStartable
	{
		private const string PushChannelName = "LitresPushChannel";

		private readonly INotificationsProvider _notificationsProvider;
		private readonly ICredentialsProvider _credentialsProvider;

        PushNotificationChannel channel = null;

        public PushNotificationsService(INotificationsProvider notificationsProvider, ICredentialsProvider credentialsProvider)
		{
			_notificationsProvider = notificationsProvider;
			_credentialsProvider = credentialsProvider;

            //ViewModels.PushNotificationsViewModel.Instance.NotificationsProvider = _notificationsProvider;
		}

		public async void Start()
		{
			await AcquirePushChannel();
		}

		public async Task AcquirePushChannel()
		{
            await Task.Run(async () =>
               {
                   var credentials = _credentialsProvider.ProvideCredentials(CancellationToken.None);

                   if (credentials == null)
                   {
                       return;
                   }

                   try
                   {
                       channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                       channel.PushNotificationReceived += (sender, args) =>
                       {
                           OpenNotification(args);
                       };

                       Debug.WriteLine($"URI: {channel.Uri}");
                       await _notificationsProvider.SubscribeDevice(channel.Uri, CancellationToken.None);
                   }

                   catch (Exception ex)
                   {
                       Debug.WriteLine(ex.Message); 
                   }
               });
        }

	    private string GetToken(string channelUri)
	    {
            var uri = new Uri(channelUri, UriKind.RelativeOrAbsolute);

            var query = uri.Query;
            var tokenParam = "?token=";
            if (query.Contains(tokenParam))
            {
                var parameters = query.Split('?', '=', '&');

                for (var i = 0; i < parameters.Length; ++i)
                {
                    if (parameters[i].Equals("token") && (i + 1) < parameters.Length)
                        return parameters[i + 1];
                }
            }

            return string.Empty;
	    }

	    private void OpenNotification(PushNotificationReceivedEventArgs args)
	    {
            //          var sb = new System.Text.StringBuilder();
            //          foreach (string key in e.Collection.Keys){
            //              sb.AppendFormat("{0}:{1}\n", key, e.Collection[key]);
            //          }
            //          System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            //          {
            //              Debug.WriteLine(sb.ToString());
            //              ViewModels.PushNotificationsViewModel.Instance.ShowToast(e.Collection);
            //          });
        }
    }
}
