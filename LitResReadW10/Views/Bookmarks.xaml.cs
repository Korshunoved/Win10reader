using System;
using System.Collections.Generic;
using System.Linq;

using Digillect.Mvvm.UI;

using LitRes;
using LitRes.Models;
using LitRes.ViewModels;

namespace LitRes.Views
{
	[View( "Bookmarks" )]
	public partial class Bookmarks : BookmarksFitting
	{
		#region Constructors/Disposer
		public Bookmarks()
		{
			InitializeComponent();

			Loaded += Bookmarks_Loaded;
		}
		#endregion

		private void Bookmarks_Loaded( object sender, System.Windows.RoutedEventArgs e )
		{
            Analytics.Instance.sendMessage(Analytics.ViewBookmarks);
			ViewModel.Load();
		}
	}

	public class BookmarksFitting : ViewModelPage<BookmarksViewModel>
	{
	}

}