using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Digillect.Collections;
using LitRes.Models;
using LitRes.Models.JsonModels;

namespace LitRes.Services
{
	public interface ICatalogProvider
	{
		Task AddToHistory( Book book, CancellationToken cancellationToken );
		Task<List<int>> GetBooksIdsFromHistory( CancellationToken cancellationToken );
		Task<List<int>> GetMyBooksIds( CancellationToken cancellationToken );
		Task AddToMyBooks( Book book, CancellationToken cancellationToken );
        Task AddFragmentToMyBooks(Book book, CancellationToken cancellationToken);
        XCollection<Book> GetMyBooksByTimeLocal();
		Task<XCollection<Book>> GetMyBooks( CancellationToken cancellationToken );
        Task<XCollection<Book>> GetAndRefreshMyBooks(CancellationToken cancellationToken);
		Task<Book> GetMyBook( int bookId, CancellationToken cancellationToken, bool ignorCache = false );
		Task<XCollection<Book>> GetAllMyBooks( CancellationToken cancellationToken );
		Task<XCollection<Book>> GetMyBooksFromCache( CancellationToken cancellationToken );
	    Task<XCollection<Book>> GetBooksInBasket(CancellationToken cancellationToken);
        Task<XCollection<Book>> GetAndSyncAllMyBooksFromCache(CancellationToken cancellationToken);
        void SaveMyBooksToCache(XCollection<Book> books, CancellationToken cancellationToken);
		Task<XCollection<Book>> GetAllMyBooksFromCache( CancellationToken cancellationToken );        
		Task<XCollection<Book>> GetPopularBooks( int fromPosition, CancellationToken cancellationToken, int customBooksOnPage = 0 );
	    Task<XCollection<Book>> GetPopularBooksFromCache(CancellationToken cancellationToken);
        Task<XCollection<Book>> GetInterestingBooks(int fromPosition, CancellationToken cancellationToken, int customBooksOnPage = 0);
	    Task<XCollection<Book>> GetInterestigBooksFromCache(CancellationToken cancellationToken);
        Task<XCollection<Book>> GetNoveltyBooks(int fromPosition, CancellationToken cancellationToken, int customBooksOnPage = 0);
	    Task<XCollection<Book>> GetNoveltyBooksFromCache(CancellationToken cancellationToken);
        Task<XCollection<Book>> GetBooksByTag(int fromPosition, int tagId, CancellationToken cancellationToken);
        Task<XCollection<Book>> GetPopularBooksByGenre( int fromPosition, int genreId, CancellationToken cancellationToken );
		Task<XCollection<Book>> GetPopularBooksByGenres( int fromPosition, List<int> genreId, CancellationToken cancellationToken );
		Task<XCollection<Book>> GetNoveltyBooksByGenre( int fromPosition, int genreId, CancellationToken cancellationToken );
		Task<XCollection<Book>> GetNoveltyBooksByGenres( int fromPosition, List<int> genreId, CancellationToken cancellationToken );
		Task<Book> GetBook( int id, CancellationToken cancellationToken );
        Task<Book> GetHiddenBook(int id, CancellationToken cancellationToken);
        Task<Book> GetBookOnline(int id, CancellationToken cancellationToken);
		Task<Book> GetBookByDocumentId( string id, CancellationToken cancellationToken );
		Task<XCollection<Book>> GetBooksBySequence( int sequenceId, CancellationToken cancellationToken );
		Task<XCollection<Book>> GetBooksByAuthor( int fromPosition, string authorId, CancellationToken cancellationToken );
		Task<XCollection<Book>> GetBooksAreReadWithThisBook(int fromPosition, int bookId, CancellationToken cancellationToken);
		Task<XCollection<Book>> SearchBooks( int fromPosition, string searchString, CancellationToken cancellationToken );
		Task<Rootobject> SearchAll( int fromPosition, string searchString, CancellationToken cancellationToken );
		Task<XCollection<Book>> GetBooksByCollection( int fromPosition, int collectionId, CancellationToken cancellationToken );
        Task<XCollection<Book>> GetBooksByCollection(int fromPosition, int collectionId, bool fromWeb, CancellationToken cancellationToken);
        Task<XCollection<Book>> GetPopularBooksByCollection(int collectionId, int bookId, CancellationToken cancellationToken);
        Task<XCollection<Book>> GetNoveltyBooksByCollection(int collectionId, int bookId, CancellationToken cancellationToken);
		Task<Book> GetBookByCollection( int collectionId, int bookId, CancellationToken cancellationToken );
        Task<Book> GetAudioBook(int bookId, CancellationToken cancellationToken);
        Task<BannersResponse> GetBanners(CancellationToken cancellationToken);
		Book GetBookByCollectionCache( int collectionId, int bookId );
        void ClearBooksCollectionCache(int collectionId);
		Task TakeBookFromCollectionBySubscription( int bookId, CancellationToken cancellationToken );
        Task<bool> CheckBoughtBook(int bookId, CancellationToken cancellationToken);
        void ClearBooksCollections();
        bool IsBooksCollectionsCleared();
	    bool IsBooksCollectionLoaded(int collectionId);
        Task DeleteBook(Book book, bool fromAnywhere = false);
		void Clear();
        void CheckBooks();
	    void CheckMyBooks(XCollection<Book> books);
        void UpdateBook(Book book);
        Task UpdateExistBook(Book book);
        bool IsAllMyBooksFromCacheLoaded();
	    Task ExpireBooks(IEnumerable<Book> books);
	    Task AddFragmentToMyBasket(Book book, CancellationToken token);	    
	}
}
