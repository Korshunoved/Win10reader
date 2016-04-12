using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Digillect.Collections;
using LitRes.Models;

namespace LitRes.Services
{
	public interface IBookmarksProvider
	{
		Task<XCollection<Bookmark>> GetBookmarks( CancellationToken cancellationToken );
		Task<XCollection<Bookmark>> GetBookmarksByDocumentId(string bookId, CancellationToken cancellationToken, bool isOnlyCurrentPosition = false);
		Task<XCollection<Bookmark>> GetLocalBookmarksByDocumentId( string bookId, CancellationToken cancellationToken );
		Task<Bookmark> GetCurrentBookmarkByDocumentId(string bookId, bool local, CancellationToken cancellationToken);
		void SetCurrentBookmarkByDocumentId( string bookId, Bookmark bookmark, CancellationToken cancellationToken );
		Task AddBookmark( Bookmark bookmark, CancellationToken cancellationToken );
        Task<XCollection<Bookmark>> RemoveBookmarks( XCollection<Bookmark> bookmarks, CancellationToken cancellationToken );
		void Clear();
	}
}
