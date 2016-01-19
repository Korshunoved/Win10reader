
using System.Windows.Controls;
using Digillect.Collections;
using Digillect.Mvvm.UI;
using LitRes.Models;
using LitRes.ViewModels;

namespace LitRes.Views
{
	[View( "BookBookmarksEdit" )]
	public partial class BookBookmarksEdit : BookBookmarksEditFitting
	{
		#region Constructors/Disposer
		public BookBookmarksEdit()
		{
			InitializeComponent();

			Loaded += BookBookmarks_Loaded;
		}
		#endregion 
		private void BookBookmarks_Loaded( object sender, System.Windows.RoutedEventArgs e )
		{
			ViewModel.LoadBookmarks();
		}

		private void CancelUpdate_Click( object sender, System.EventArgs e )
		{
			((App) App.Current).RootFrame.GoBack();
		}

		private async void Update_Click( object sender, System.EventArgs e )
		{
			var toDel = new XCollection<Bookmark>();

			var itemsCount = Bookmarks.Items.Count;
			for( int i = 0; i < itemsCount; i++ )
			{
				var item = (ListBoxItem) Bookmarks.ItemContainerGenerator.ContainerFromIndex( i );

				var checkBox = NotificationsEdit.FindFirstElementInVisualTree<CheckBox>( item );

				if( checkBox != null && checkBox.IsChecked != null && !checkBox.IsChecked.Value )
				{
					toDel.Add( (Bookmark) item.Content );
				}
			}

			await ViewModel.DeleteBookmarks( toDel );
		}
	}

	public class BookBookmarksEditFitting : EntityPage<int, Models.Book, BookBookmarksViewModel>
	{
	}
}