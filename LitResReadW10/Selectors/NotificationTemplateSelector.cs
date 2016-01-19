using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LitRes.Models;

namespace LitRes.Selectors
{
	public class NotificationTemplateSelector : DataTemplateSelector
	{
		public DataTemplate Genre { get; set; }
		public DataTemplate Person { get; set; }

		public override DataTemplate SelectTemplate( object item, DependencyObject container )
		{
			var notification = item as Notification;

			if( notification != null )
			{
				if( notification.NotifiedPerson != null )
				{
					return Person;
				}
				else
				{
					return Genre;
				}
			}

			return base.SelectTemplate( item, container );
		}
	}
}
