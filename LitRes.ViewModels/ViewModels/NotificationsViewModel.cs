using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Digillect;
using Digillect.Collections;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Models;
using LitRes.Services;

namespace LitRes.ViewModels
{
	public class NotificationsViewModel : ViewModel
	{
		public const string MainPart = "Main";
		public const string DeleteNotificationPart = "DeleteNotification";

		private readonly INotificationsProvider _notificationsProvider;
		private readonly INavigationService _navigationService;

		private bool _notificationsEmpty;

		#region Public Properties
		public XCollection<Notification> Notifications { get; private set; }

		public RelayCommand ShowNotificationsEdit { get; private set; }
		//public RelayCommand CancelEdit { get; private set; }
		public RelayCommand<Notification> NotificationSelected { get; private set; }
		public RelayCommand<XCollection<Notification>> DeleteNotifications { get; private set; }

		public bool NotificationsEmpty
		{
			get { return _notificationsEmpty; }
			private set { SetProperty( ref _notificationsEmpty, value, "NotificationsEmpty" ); }
		}
		#endregion

		public NotificationsViewModel( INotificationsProvider notificationsProvider, INavigationService navigationService )
		{
			_notificationsProvider = notificationsProvider;
			_navigationService = navigationService;

            RegisterAction(DeleteNotificationPart).AddPart(session => DeleteNotificationsProceed(session), session => true);
            RegisterAction(MainPart).AddPart(session => LoadNotifications(session), session => true);

			Notifications = new XCollection<Notification>();

			ShowNotificationsEdit = new RelayCommand( () => _navigationService.Navigate( "NotificationsEdit" ) );
			//CancelEdit = new RelayCommand( () => _navigationService.GoBack() );
			NotificationSelected = new RelayCommand<Notification>( NavigateToObject, notification => notification != null );
			DeleteNotifications = new RelayCommand<XCollection<Notification>>( DeleteNotificationsCalled, notifications => notifications != null );
		}

		public Task<Session> Load()
		{
			return Load( new Session(MainPart) );
		}

		public async Task LoadNotifications( Session session )
		{
			IXList<Notification> notifications = await _notificationsProvider.GetNotifications( session.Token );

		    if (notifications.Count > 200)
		    {
                notifications =  new XSubRangeCollection<Notification>(notifications, 0, 200);
		    }
            
			Notifications.Update(notifications);

			NotificationsEmpty = Notifications.Count == 0;
		}

		private void NavigateToObject( Notification notification )
		{
			if( notification != null )
			{
				if( !string.IsNullOrEmpty( notification.NotifiedPerson ) )
				{
					_navigationService.Navigate( "Person", XParameters.Empty.ToBuilder().AddValue( "id", "" )
                                                                                        .AddValue( "PersonName", notification.NotifiedPerson ).ToImmutable()
                                                                                        );
				}
			}
		}

		public async void DeleteNotificationsCalled( XCollection<Notification> notifications )
		{
			PreserveSessions( true );

			if( notifications != null )
			{
				var session = new Session( DeleteNotificationPart );
				session.AddParameter( "delete", notifications );
				await Load( session );
			}
		}

		private async Task DeleteNotificationsProceed( Session session )
		{
			var notifications = session.Parameters.GetValue<XCollection<Notification>>( "delete", null );

			if( notifications != null )
			{
				await _notificationsProvider.DeleteNotifications( notifications, session.Token );

				await Load();
			}

			PreserveSessions( false );
		}
	}
}
