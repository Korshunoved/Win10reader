using System;
using System.Threading;
using System.Threading.Tasks;
using Digillect;
using Digillect.Collections;
using LitRes.Models;

namespace LitRes.Services
{
	public interface INotificationsProvider
	{
		Task<XCollection<Notification>> GetNotifications( CancellationToken cancellationToken );
		Task<XCollection<Notification>> RefreshNotifications( CancellationToken cancellationToken );
		Task<Notification> GetNotificationByAuthor( Person person, CancellationToken cancellationToken );
		Task AddNotification( string personId, CancellationToken cancellationToken );
		Task DeleteNotification(string personId, CancellationToken cancellationToken);
		Task DeleteNotifications( XCollection<Notification> notifications, CancellationToken cancellationToken );
		Task SubscribeDevice( string token, CancellationToken cancellationToken );
        Task SendSpampack(string id,int eventId, CancellationToken cancellationToken);
		void Clear();
	}
}
