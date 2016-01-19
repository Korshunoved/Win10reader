
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Digillect.Mvvm.UI;

using LitRes.ViewModels;
using LitResReadW10.Helpers;

namespace LitRes.Views
{
	[View( "BookBookmarks" )]
	public partial class BookBookmarks : BookBookmarksFitting
	{
		#region Constructors/Disposer
		public BookBookmarks()
		{
			InitializeComponent();

			Loaded += BookBookmarks_Loaded;
		}
		#endregion

		private void BookBookmarks_Loaded( object sender, RoutedEventArgs e )
		{
            Analytics.Instance.sendMessage(Analytics.ViewBookmarks);
			ViewModel.LoadBookmarks();
		}

		private void Edit_Click( object sender, System.EventArgs e )
		{
			ViewModel.BookBookmarksEdit.Execute( null );
		}

	    private void BookmarksListView_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
            LocalBroadcastReciver.Instance.OnPropertyChanging(BookmarksListView.SelectedItem, new PropertyChangingEventArgs("BookmarkTapped"));
            if (!SystemInfoHelper.IsDesktop() && Frame.CanGoBack) Frame.GoBack();
        }
	}

	public class BookBookmarksFitting : EntityPage<Models.Book, BookBookmarksViewModel>
	{
	}
}