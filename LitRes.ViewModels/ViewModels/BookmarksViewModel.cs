using System.Linq;
using System.Threading.Tasks;
using Digillect.Collections;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Exceptions;
using LitRes.Models;
using LitRes.Services;

namespace LitRes.ViewModels
{
	public class BookmarksViewModel : ViewModel
	{
		public const string MainPart = "Main";

		private readonly IBookmarksProvider _bookmarksProvider;
		private readonly ICatalogProvider _catalogProvider;
		private readonly INavigationService _navigationService;

		private bool _bookmarkedBooksEmpty;

		#region Public Properties
		public XCollection<Bookmark> Bookmarks { get; private set; }
		public XCollection<Book> BookmarkedBooks { get; private set; }

		public RelayCommand<Book> BookSelected { get; private set; }

		public bool BookmarkedBooksEmpty
		{
			get { return _bookmarkedBooksEmpty; }
			private set { SetProperty( ref _bookmarkedBooksEmpty, value, "BookmarkedBooksEmpty" ); }
		}
		#endregion

		#region Constructors/Disposer
		public BookmarksViewModel(IBookmarksProvider bookmarksProvider, ICatalogProvider catalogProvider, INavigationService navigationService)
		{
			_bookmarksProvider = bookmarksProvider;
			_catalogProvider = catalogProvider;
			_navigationService = navigationService;

			RegisterPart( MainPart, ( session, part ) => LoadBookmarks( session ), ( session, part ) => true );
			//RegisterPart( DeletePart, ( session, part ) => DeleteBookmarks( session ), ( session, part ) => true, false );


			Bookmarks = new XCollection<Bookmark>();
			BookmarkedBooks = new XCollection<Book>();

			BookSelected = new RelayCommand<Book>( book => _navigationService.Navigate( "BookBookmarks", Parameters.From( "id", book.Id )), book => book != null );
		}
		#endregion

		#region Load
		public Task<Session> Load()
		{
			return Load( new Session() );
		}
		#endregion

		#region LoadBookmarks
		private async Task LoadBookmarks( Session session )
		{
			XCollection<Bookmark> bookmarks = null;
			try
			{
				bookmarks = await _bookmarksProvider.GetBookmarks( session.Token );
			}
			catch (CatalitNoCredentialException)
			{
				//ToDo: Do something? Message?
			}

			XCollection<Book> books = new XCollection<Book>();

			foreach (var bookmark in bookmarks)
			{
				if( bookmark.Group != "0" )
				{
					var exits = books.FirstOrDefault( x => x.Description.Hidden.DocumentInfo.Id == bookmark.ArtId );

					if( exits != null )
					{
						exits.BookmarksCount++;
					}
					else
					{
						var book = await _catalogProvider.GetBookByDocumentId( bookmark.ArtId, session.Token );
						if( book != null )
						{
							book.BookmarksCount = 1;
							books.Add( book );
						}
					}
				}
			}

			//sort books by last opened
			int top = 0;
			var myBooksHistory = await _catalogProvider.GetBooksIdsFromHistory( session.Token );

			foreach( var mybook in myBooksHistory )
			{
				var inbookmarked = books.FirstOrDefault(x => x.Id == mybook );

				if( inbookmarked != null )
				{
					books.Remove( inbookmarked );
					books.Insert( top, inbookmarked );
					top++;
				}
			}

			BookmarkedBooks.Update( books );

			BookmarkedBooksEmpty = BookmarkedBooks.Count == 0;

			if (bookmarks != null)
			{
				Bookmarks.Update(bookmarks);
			}
		}
		#endregion
	}
}
