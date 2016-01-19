
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Digillect.Collections;
using Digillect.Mvvm.UI;
using LitRes.Models;
using LitRes.ViewModels;
using Microsoft.Phone.Shell;

namespace LitRes.Views
{
	[View( "NotificationsEdit" )]
	public partial class NotificationsEdit : NotificationsEditFitting
	{
		#region Constructors/Disposer
		public NotificationsEdit()
		{
			InitializeComponent();

            Loaded += NotificationsEdit_Loaded;
		}

        void NotificationsEdit_Loaded(object sender, RoutedEventArgs e)
        {
            Analytics.Instance.sendMessage(Analytics.ViewSubscriptions);
            loadingText.Visibility = Visibility.Collapsed;
            listBox.Visibility = Visibility.Visible;
        }
		#endregion

		private void Update_Click( object sender, System.EventArgs e )
		{
			XCollection<Notification> toDel = new XCollection<Notification>();

			var itemsCount = listBox.Items.Count;
			for(int i = 0; i < itemsCount; i++)
			{
				ListBoxItem item = (ListBoxItem) listBox.ItemContainerGenerator.ContainerFromIndex( i );

				var checkBox = FindFirstElementInVisualTree<CheckBox>( item );

				if( checkBox != null && !checkBox.IsChecked.Value )
				{
					toDel.Add( (Notification) item.Content );
				}
			}

			ViewModel.DeleteNotificationsCalled( toDel );
		}

		public static T FindFirstElementInVisualTree<T>( DependencyObject parentElement ) where T : DependencyObject
		{
			var count = VisualTreeHelper.GetChildrenCount( parentElement );
			
			if( count == 0 )
				return null;

			for( int i = 0; i < count; i++ )
			{
				var child = VisualTreeHelper.GetChild( parentElement, i );
				if( child != null && child is T )
				{ return (T) child; }
				else
				{
					var result = FindFirstElementInVisualTree<T>( child );
					if( result != null )
						return result;
				}
			}
			return null;
		}

		private void CancelUpdate_Click( object sender, System.EventArgs e )
		{
			((App) App.Current).RootFrame.GoBack();
		}
	}

	public class NotificationsEditFitting : ViewModelPage<NotificationsViewModel>
	{
	}


}