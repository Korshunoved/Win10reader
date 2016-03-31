
using System.ComponentModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using BookParser;
using BookParser.Models;
using Digillect.Collections;
using Digillect.Mvvm.UI;
using LitRes.Models;
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
	        var list = sender as ListView;
	        var item = list.SelectedItem;
            var bookmark = item as Bookmark;
	        if (bookmark != null)
	        {                
	            var myBookmark = new BookmarkModel
	            {
	                BookID = bookmark.Id,
	                Text = bookmark.NoteText.Text
	            };
	            AppSettings.Default.Bookmark = myBookmark;
	        }
            LocalBroadcastReciver.Instance.OnPropertyChanging(BookmarksListView.SelectedItem, new PropertyChangingEventArgs("BookmarkTapped"));
            if (!SystemInfoHelper.IsDesktop() && Frame.CanGoBack) Frame.GoBack();
            else
            {
                var reader = Reader.Instance;
                reader?.GoToBookmark();
            }
        }

        private void BookmarksListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {         
            var bookmark = ((FrameworkElement)e.OriginalSource).DataContext as Bookmark;
            if (bookmark == null) return;
            XCollection<Bookmark> bookmarks = new XCollection<Bookmark>();
            bookmarks.Add(bookmark);
            ViewModel.DeleteBookmarks(bookmarks);
            //foreach (var bookmark1 in ViewModel.Bookmarks.Where(bookmark1 => bookmark1.Id == bookmark.Id))
            //{
            //    bookmark1.ToDelete = true;
            //}
        }

        private void BookmarksListView_Holding(object sender, HoldingRoutedEventArgs e)
        {
            var bookmark = ((FrameworkElement)e.OriginalSource).DataContext as Bookmark;
            if (bookmark == null) return;
            XCollection<Bookmark> bookmarks = new XCollection<Bookmark>();
            bookmarks.Add(bookmark);
            ViewModel.DeleteBookmarks(bookmarks);
            //foreach (var bookmark1 in ViewModel.Bookmarks.Where(bookmark1 => bookmark1.Id == bookmark.Id))
            //{
            //    bookmark1.ToDelete = true;
            //}
        }
    }

    public class BookBookmarksFitting : EntityPage<Models.Book, BookBookmarksViewModel>
	{
	}
}