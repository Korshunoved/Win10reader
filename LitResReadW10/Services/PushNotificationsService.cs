using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Autofac;
using LitRes.Services.Connectivity;

namespace LitRes.Services
{
	public class PushNotificationsService : IPushNotificationsService, IStartable
	{
	    public static string PushChannelName { get; } = "LitresPushChannel";

	    private readonly INotificationsProvider _notificationsProvider;
		private readonly ICredentialsProvider _credentialsProvider;

	    private PushNotificationChannel _channel;

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
                       _channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                       //var newParametres = new Dictionary<string,string>();
                       //newParametres.Add("type","test");
                       //newParametres.Add("text", "Бесплатная книга из раздела Популярное теперь в вашей библиотеке!");
                       //newParametres.Add("spam_pack_id", "test");
                       //await Task.Delay(10000);
                       //ViewModels.PushNotificationsViewModel.Instance.ShowToast(newParametres);
                       _channel.PushNotificationReceived += (sender, args) =>
                       {
                           OpenNotification(args);
                       };

                       Debug.WriteLine($"URI: {_channel.Uri}");
                      await _notificationsProvider.SubscribeDevice(_channel.Uri, CancellationToken.None);
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

	    private async void OpenNotification(PushNotificationReceivedEventArgs args)
	    {
            //TODO Возможно добавить другие типы пушей
	        if (args.ToastNotification != null)
	        {
	            args.Cancel = true;
	            var content = args.ToastNotification.Content;
	            var node = content.FirstChild;
	            if (node == null) return;
	            var parametrs = node.Attributes.ToDictionary(attribute => attribute.NodeName, attribute => attribute.NodeValue.ToString());
	            //await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
	            //{
	                //Debug.WriteLine(parametrs.ToString());
	                ViewModels.PushNotificationsViewModel.Instance.ShowToast(parametrs);
	            //});
	        }
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
