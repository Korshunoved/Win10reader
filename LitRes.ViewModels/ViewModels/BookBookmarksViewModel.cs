using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookParser.Models;
using Digillect;
using Digillect.Collections;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Exceptions;
using LitRes.Models;
using LitRes.Services;

namespace LitRes.ViewModels
{
    public class DisplayBookmark : XObject
    {
        public string Id { get; set; }

        public string BookID { get; set; }

        public string Title { get; set; }

        public string Percent { get; set; }

        public string Text { get; set; }

        public string LastUpdate { get; set; }

        public string CurrentPage { get; set; }

        public string TotalPages { get; set; }

        public string XPointer { get; set; }
    }

    public class BookBookmarksViewModel : EntityViewModel<Book>
	{
		public const string LoadBookmarksPart = "LoadBookmarks";
		public const string BookIdParameter = "BookId";
		public const string DeletePart = "Delete";

		private readonly IBookmarksProvider _bookmarksProvider;
		private readonly ICatalogProvider _catalogProvider;
		private readonly INavigationService _navigationService;

		private bool _loaded;

		#region Public Properties
		public XCollection<Bookmark> Bookmarks { get; private set; }

        public List<BookmarkModel> LocalBookmarks { get; set; }

        public XCollection<DisplayBookmark> DisplayBookmarks { get; private set; }

        public RelayCommand<Bookmark> ReadByBookmark { get; private set; }
		public RelayCommand BookBookmarksEdit { get; private set; }
		#endregion

		#region Constructors/Disposer
		public BookBookmarksViewModel( IBookmarksProvider bookmarksProvider, ICatalogProvider catalogProvider, INavigationService navigationService )
		{
			_bookmarksProvider = bookmarksProvider;
			_catalogProvider = catalogProvider;
			_navigationService = navigationService;

			Bookmarks = new XCollection<Bookmark>();

            DisplayBookmarks = new XCollection<DisplayBookmark>();

            RegisterAction(LoadBookmarksPart).AddPart(LoadBookmarks, session => true);
            RegisterAction(DeletePart).AddPart(DeleteBookmarks, session => true);

			ReadByBookmark = new RelayCommand<Bookmark>( bookmark =>
			{
                //if(bookmark != null) _navigationService.Navigate("Reader", XParameters.Empty.ToBuilder().AddValue("id", Entity.Id).AddValue("bookmark", bookmark.Selection).ToImmutable());
			} );
			BookBookmarksEdit = new RelayCommand( () => _navigationService.Navigate( "BookBookmarksEdit", XParameters.Create( "id", Entity.Id ) ) );
		}
		#endregion


		#region LoadEntity
		public Task LoadBookmarks()
		{
			return Load( new Session( LoadBookmarksPart ) );
		}

		#endregion

		#region DeleteBookmarks
		public Task DeleteBookmarks( XCollection<Bookmark> bookmarks )
		{
			PreserveSessions( true );

			var session = new Session( DeletePart );

			session.AddParameter( "delete", bookmarks );

		    var bookmark = bookmarks[0];

		    foreach (var displayBookmark in DisplayBookmarks.Where(displayBookmark => displayBookmark.Id == bookmark.Id))
		    {
		        DisplayBookmarks.Remove(displayBookmark);
		    }

			return Load(session);
		}

		private async Task DeleteBookmarks( Session session )
		{
			var bookmarks = session.Parameters.GetValue<XCollection<Bookmark>>( "delete", null );

			if( bookmarks != null )
			{
				await _bookmarksProvider.RemoveBookmarks( bookmarks, session.Token );
				
				await LoadBookmarks( session );
			}

			PreserveSessions( false );
		}
		#endregion
		
		#region LoadBookmarks
		private async Task LoadBookmarks( Session session )
		{
			XCollection<Bookmark> bookmarks = null;

		    try
		    {
		        bookmarks = await _bookmarksProvider.GetBookmarksByDocumentId(Entity.Description.Hidden.DocumentInfo.Id, session.Token);
		    }
		    catch (CatalitNoCredentialException)
		    {
		        //ToDo: Do something? Message?
		    }
		    catch (Exception ex)
		    {
		        
		    }

		    if (bookmarks == null || bookmarks.Count == 0)
		    {
                bookmarks = await _bookmarksProvider.GetLocalBookmarksByDocumentId(Entity.Description.Hidden.DocumentInfo.Id, session.Token);
		    }

			if( bookmarks != null )
			{
				bookmarks.RemoveAll( x => x.Group == "0" );

                var bookmarksSorted = bookmarks.ToList();
                bookmarksSorted.Sort(CompareXPointer);

                Bookmarks.Update(bookmarksSorted);

                /*   */
			    foreach (var bookmark in LocalBookmarks.Select(localBookmark => new DisplayBookmark
			    {
			        Id = localBookmark.BookmarkID,
			        CurrentPage = localBookmark.CurrentPage.ToString(),
			        TotalPages = localBookmark.Pages.ToString(),
			        Percent = localBookmark.Percent,
			    }))
			    {
			        DisplayBookmarks.Add(bookmark);
			    }


			    foreach (var bookmark in Bookmarks)
			    {
			        foreach (var displayBookmark in DisplayBookmarks.Where(displayBookmark => bookmark.Id == displayBookmark.Id))
			        {
			            displayBookmark.Title = bookmark.Title;
			            displayBookmark.LastUpdate = bookmark.LastUpdate;
			            displayBookmark.Text = bookmark.NoteText.Text;
			        }
			    }
            }
		}

	    private int CompareXPointer(Bookmark bookmark1, Bookmark bookmark2)
	    {
	        var xPointer1 = bookmark1.Selection;
            var xPointer2 = bookmark2.Selection;

            const int startPoint = 20;
	        const int minLength = startPoint + 2;
            if (!string.IsNullOrEmpty(xPointer1) && !string.IsNullOrEmpty(xPointer2))
            {
                if (xPointer1.Length > minLength)
                {
                    xPointer1 = xPointer1.Substring(startPoint, xPointer1.Length - minLength);
                }

                if (xPointer2.Length > minLength)
                {
                    xPointer2 = xPointer2.Substring(startPoint, xPointer2.Length - minLength);
                }

                var xArr1 = xPointer1.Split('/');
                var xArr2 = xPointer2.Split('/');

                for (int i = 0; i < xArr1.Length; i++)
                {
                    if (i < xArr2.Length)
                    {
                        float xF1 = 0;
                        float xF2 = 0;
                        float.TryParse(xArr1[i], out xF1);
                        float.TryParse(xArr2[i], out xF2);
                        if (xF1 > xF2) return 1;
                        if (xF1 < xF2) return -1;
                    }
                }               
            }
	        return 0;
	    }

        protected override async Task LoadEntity(Session session)
        {
            var entity = session.Parameters.GetValue<Book>("BookEntity");
            var book = await _catalogProvider.GetBook(entity.Id, session.Token);

            Entity = book;

            _loaded = true;
        }

        #endregion
    }
}
