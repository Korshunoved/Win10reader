using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Digillect;
using Digillect.Collections;
using LitRes.Models;
using LitRes.Services.Connectivity;

namespace LitRes.Services
{
	internal class NotificationsProvider : INotificationsProvider, IStartable
	{
		const string CacheItemName = "notifications";

		private readonly ICatalitClient _client;
		private readonly IDataCacheService _dataCacheService;
		private readonly IDeviceInfoService _deviceInfoService;

		private XCollection<Notification> _notifications; 

		#region Constructors/Disposer
		public NotificationsProvider(ICatalitClient client, 
            IDeviceInfoService deviceInfoService, 
            IDataCacheService dataCacheService)
		{
			_client = client;
			_dataCacheService = dataCacheService;
			_deviceInfoService = deviceInfoService;
		}
		#endregion

		public async Task<XCollection<Notification>> RefreshNotifications( CancellationToken cancellationToken )
		{
		    try
		    {
                var subscriptions = await _client.Subscriptions(new Dictionary<string, object>(), cancellationToken);
                if (subscriptions != null)
                {
                    UpdateNotificationList(subscriptions.Notifications, cancellationToken);
                }
		    }
		    catch (Exception ex)
		    {
		        ex = ex;
		    }
			
			return _notifications;
		}

		public async Task<XCollection<Notification>> GetNotifications( CancellationToken cancellationToken )
		{
			if (_notifications == null)
			{
				_notifications = _dataCacheService.GetItem<XCollection<Notification>>(CacheItemName);
			}

			return _notifications ?? new XCollection<Notification>();
		}

		public async Task<Notification> GetNotificationByAuthor( Person person, CancellationToken cancellationToken )
		{
			if(person == null)
			{
				return null;
			}

			await Load( cancellationToken );

			if (_notifications != null)
			{
				string name = string.Empty;
				if(!string.IsNullOrEmpty( person.LastName ))
				{
					name += person.LastName;
				}
				if(!string.IsNullOrEmpty( person.FirstName ))
				{
					name += " " + person.FirstName;
					name = name.Trim();
				}
				if(!string.IsNullOrEmpty( person.MiddleName ))
				{
					name += " " + person.MiddleName;
					name = name.Trim();
				}

				var exits = _notifications.FirstOrDefault( x => x.NotifiedPerson.Trim() == name );
				return exits;
			}

			return null;
		}

		public async Task AddNotification( string personId, CancellationToken cancellationToken )
		{
			var parameters = new Dictionary<string, object>
				{							
					{ "add_to_subscr", personId }								
				};

			var result = await _client.Subscriptions( parameters, cancellationToken );

			if (result != null)
			{
				UpdateNotificationList( result.Notifications, cancellationToken );
			}
		}

		public async Task DeleteNotifications( XCollection<Notification> notifications, CancellationToken cancellationToken )
		{
			var exits = await RefreshNotifications( cancellationToken );

			var newcollection = new XCollection<Notification>();
			foreach (var notification in exits)
			{
				if (notifications.All( x => x.NotifiedPerson != notification.NotifiedPerson ))
				{
					newcollection.Add( notification );
				}
			}

			var authors = new List<string>();
			foreach (var notification in newcollection)
			{
				if (!string.IsNullOrEmpty( notification.NotifiedPerson ))
				{
					authors.Add( notification.NotifiedPerson );
				}				
			}

			string authorsString = string.Join( "||", authors );

			XCollection<Notification> result;

			Dictionary<string, object> parameters;
			if( string.IsNullOrEmpty( authorsString ) )
			{
				if( exits.Count > 0 )
				{
                    for (int i = notifications.Count - 1; i >= 0; i--)
                    {
                        var notif = exits.FirstOrDefault(x => x.NotifiedPerson == notifications[i].NotifiedPerson);
                        if (notif != null) exits.Remove(notif);
                    }

                    string newAuthorsString = string.Join("||", exits);

                    parameters = new Dictionary<string, object>
					{							
						{ "update_subscr", newAuthorsString }								
					};

					result = (await _client.Subscriptions( parameters, cancellationToken )).Notifications;

                    //parameters = new Dictionary<string, object>
                    //{							
                    //    { "del_subscr", exits[0].NotifiedPerson }								
                    //};

                    //result = ( await _client.Subscriptions( parameters, cancellationToken ) ).Notifications;
				}
				else
				{
					result = new XCollection<Notification>();
				}
			}
			else
			{
				parameters = new Dictionary<string, object>
					{							
						{ "update_subscr", authorsString }								
					};

				result = (await _client.Subscriptions( parameters, cancellationToken )).Notifications;
			}

			UpdateNotificationList( result, cancellationToken );
		}

		public async Task DeleteNotification( string personId, CancellationToken cancellationToken )
		{
			var parameters = new Dictionary<string, object>
				{							
					{ "del_subscr", personId }								
				};

			var result = await _client.Subscriptions(parameters, cancellationToken);

			if (result != null)
			{
				UpdateNotificationList(result.Notifications, cancellationToken);
			}
		}

		public async Task SubscribeDevice(string token, CancellationToken cancellationToken)
		{
			var parameters = new Dictionary<string, object>
				{
					{"action", "store_device_key"},									
					{"token", token}
                    //{"app", 93}			
					//{"mac", _deviceInfoService.DeviceId}
                   
				};

			var result = await _client.SubscribeDevice(parameters, cancellationToken);
		}

        public async Task SendSpampack(string id,int eventId, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<string, object>
				{
					{"action", "user_spam"},									
					{"id", id},			
					{"event", eventId}			
				};

            await _client.SendSpampack(parameters, cancellationToken);
        }

		public void Clear()
		{
			if( _notifications != null )
			{
				_notifications.Clear();
			}
		}

		private async Task Load( CancellationToken cancellationToken )
		{
			if (_notifications == null)
			{
				await GetNotifications( cancellationToken );
			}
			if (_notifications == null)
			{
				_notifications = new XCollection<Notification>();
			}
		}

		public void Start()
		{
            new Task(async () => await RefreshNotifications(CancellationToken.None)).Start();
		}

		private void UpdateNotificationList(XCollection<Notification> notifications, CancellationToken cancellationToken)
		{
			if (notifications != null)
			{
				_notifications = new XCollection<Notification>( notifications.OrderBy( x => x.NotifiedPerson ) );

				_dataCacheService.PutItem( _notifications, CacheItemName, cancellationToken );

				Save( cancellationToken );
			}
		}

		private void Save( CancellationToken cancellationToken )
		{
			_dataCacheService.PutItem( _notifications, CacheItemName, cancellationToken);
		}
	}
}
