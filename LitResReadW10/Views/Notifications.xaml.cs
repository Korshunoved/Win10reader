
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Digillect.Collections;
using Digillect.Mvvm.UI;
using LitRes.Models;
using LitRes.ViewModels;
using LitResReadW10.Controls;

namespace LitRes.Views
{
	[View( "Notifications" )]
	public partial class Notifications : NotificationsFitting
	{
		#region Constructors/Disposer
		public Notifications()
		{
			InitializeComponent();

			Loaded += Notifications_Loaded;
		}
		#endregion

		private void Notifications_Loaded( object sender, RoutedEventArgs e )
		{
			ViewModel.Load();
		}

	    protected override void OnNavigatedTo(NavigationEventArgs e)
	    {
	        ControlPanel.Instance.TopBarTitle = "Подписки";
	        base.OnNavigatedTo(e);
	    }

	    protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            UpdateSubscriptions();
            base.OnNavigatedFrom(e);
        }

        private void Edit_Click( object sender, System.EventArgs e )
		{
			ViewModel.ShowNotificationsEdit.Execute(null);
		}

		//private void Update_Click( object sender, System.EventArgs e )
		//{
		////	ApplicationBar = Resources["appbarRead"] as ApplicationBar;
		//}

		private void CancelUpdate_Click( object sender, System.EventArgs e )
		{

			//ApplicationBar = Resources["appbarRead"] as ApplicationBar;
		}

	    private void TsAutoOrientation_OnToggled(object sender, RoutedEventArgs e)
	    {
	        //var src = (ToggleSwitch) sender;
            
        }

        private void UpdateSubscriptions()
        {
            XCollection<Notification> toDel = new XCollection<Notification>();

            if (NotificationsGrid.ItemsPanelRoot != null && NotificationsGrid.ItemsPanelRoot.Children != null)
            {
                var itemsCount = NotificationsGrid.ItemsPanelRoot.Children.Count;
                for (int i = 0; i < itemsCount; i++)
                {
                   // if (NotificationsGrid.ItemContainerGenerator != null)
                    {
                 

                        GridViewItem item = (GridViewItem)NotificationsGrid.ItemsPanelRoot.Children[i];

                        var checkBox = FindFirstElementInVisualTree<ToggleSwitch>(item);

                        if (checkBox != null && !checkBox.IsOn)
                        {
                            if (item != null) toDel.Add((Notification)item.Content);
                        }
                    }
                }
            }

            ViewModel.DeleteNotificationsCalled(toDel);
        }

        public static T FindFirstElementInVisualTree<T>(DependencyObject parentElement) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(parentElement);

            if (count == 0)
                return null;

            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parentElement, i);
                if (child != null && child is T)
                { return (T)child; }
                else
                {
                    var result = FindFirstElementInVisualTree<T>(child);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }
    }

	public class NotificationsFitting : ViewModelPage<NotificationsViewModel>
	{
	}
}