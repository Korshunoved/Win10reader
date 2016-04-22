using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Digillect.Collections;
using LitRes.LibraryTools;
using LitRes.Models;
using LitRes.Services.Connectivity;
using Digillect.Mvvm.Services;
using System.Text;
using Windows.UI.Popups;
using LitRes.Models.JsonModels;

namespace LitRes.Services
{
	internal class CatalogProvider : ICatalogProvider
	{
		const string ReadingBooksCacheItemName = "mybookslist";
		const string AllMyBooksCacheItemName = "mybookscache";
		const string AllMyBooksTileCacheItemName = "mybooktilescache";
		const string AllMyBooksIdCacheItemName = "mybooksidcache";
		const string ReadHistoryBooksIdCacheItemName = "readhistorybooksidcache";

        const string LastReadedBook = "lastreadedbook";

        const string MinDateTimeString = "2000-01-01";

		public const int CacheAgeInDays = 20;
		public const int SingleBooksCount = 20;

		public const int BooksInPage = 50;
		public const int BooksInPageShift = 10;

		private readonly ICatalitClient _client;
		private readonly IDataCacheService _dataCacheService;
		private readonly IFileDownloadService _fileDownloadService;
		private readonly IBookProvider _bookProvider;
        private readonly INetworkAvailabilityService _networkAvailabilityService;
        private readonly IProfileProvider _profileProvider;

		private List<int> _myBooksIds; 
		private List<int> _myHistoryBooksIds; 

		/// <summary>
		/// All my books
		/// </summary>
		private XCollection<Book> _myBooks;

		/// <summary>
		/// My books by time
		/// </summary>
		private XCollection<Book> _myBooksByTime;

	    private XCollection<Book> _myBasket;

		private XCollection<Book> _interestingBooks;
		private XCollection<Book> _noveltyBooks;
		private XCollection<Book> _popularBooks;

		private XCollection<Book> _noveltyBooksByGenre;
		private XCollection<Book> _noveltyBooksByGenres;
		private XCollection<Book> _popularBooksByGenre;
		private XCollection<Book> _booksByTag;
		private XCollection<Book> _popularBooksByGenres;
		private List<int> _noveltyBooksByGenresIds;
		private int _noveltyBooksByGenreId;
		private List<int> _popularBooksByGenresIds;
		private int _popularBooksByGenreId;
		private int _booksByTagId;

		private XCollection<Book> _booksBySequence;
		private int _booksBySequenceId;
		private XCollection<Book> _booksByAuthor;
		private string _booksByAuthorID;
		private XCollection<Book> _booksByBook;
		private int _booksByBookID;

		private XCollection<Book> _foundedBooks;
		private string _foundedBooksQuery;

		private XCollection<Book> _booksByCollection;
		private int _booksByCollectionIdCollection;

        private XCollection<Book> _popularBooksByCollection;
        private int _popularBooksByCollectionIdCollection;

        private XCollection<Book> _noveltyBooksByCollection;
        private int _noveltyBooksByCollectionIdCollection;

		private XCollection<Book> _singleBooks;  

		#region Constructors/Disposer
        public CatalogProvider(ICatalitClient client, IFileDownloadService fileDownloadService, IProfileProvider profileProvider, IBookProvider bookProvider, IDataCacheService dataCacheService, INetworkAvailabilityService networkAvailabilityService)
		{
			_client = client;
			_dataCacheService = dataCacheService;
			_fileDownloadService = fileDownloadService;
			_bookProvider = bookProvider;
            _networkAvailabilityService = networkAvailabilityService;
			_myBooksIds = new List<int>();
			_myHistoryBooksIds = new List<int>();
            _profileProvider = profileProvider;
            //_expirationGuardian = expirationGuardian;
		}
		#endregion

		public async Task<List<int>> GetMyBooksIds(CancellationToken cancellationToken)
		{
			_myBooksIds = _dataCacheService.GetItem<List<int>>( AllMyBooksIdCacheItemName );
			return _myBooksIds ?? new List<int>();
		}

		public async Task<XCollection<Book>> GetMyBooksFromCache( CancellationToken cancellationToken )
		{
			DateTime cacheModificationDate = await _dataCacheService.GetItemModificationDate(ReadingBooksCacheItemName);
			if (!cacheModificationDate.Equals(DateTime.MinValue))
			{
				try
				{
					_myBooksByTime = _dataCacheService.GetItem<XCollection<Book>>(ReadingBooksCacheItemName);
				}
				catch
				{
				}
			}
			else
			{
				if (_myBooksByTime != null)
				{
					_myBooksByTime.Clear();
				}
			}

			return _myBooksByTime;
		}

		public async Task<XCollection<Book>> GetAllMyBooksFromCache( CancellationToken cancellationToken )
		{
			DateTime cacheModificationDate = await _dataCacheService.GetItemModificationDate( AllMyBooksCacheItemName );
			if (!cacheModificationDate.Equals(DateTime.MinValue))
			{
				try
				{
					_myBooks = _dataCacheService.GetItem<XCollection<Book>>( AllMyBooksCacheItemName );
				}
				catch
				{
				}
			}
			else
			{
				if (_myBooks != null)
				{
					_myBooks.Clear();
				}
			}

			return _myBooks;
		}

        public async Task<XCollection<Book>> GetAndSyncAllMyBooksFromCache(CancellationToken cancellationToken)
        {
            var _mBooks = await GetAllMyBooksFromCache(cancellationToken);

            if (_mBooks != null && _mBooks.Count > 0 && _myBooksByTime != null && _myBooksByTime.Count > 0)
            {
                bool needSave = false;
                foreach (var mBook in _mBooks)
                {
                    var bookByTime = _myBooksByTime.FirstOrDefault(x => mBook.Id == x.Id);
                    if (bookByTime != null && !mBook.Equals(bookByTime))
                    {
                        mBook.Update(bookByTime);
                        needSave = true;
                    }   
                }
                if (needSave) _dataCacheService.PutItem(_mBooks, AllMyBooksCacheItemName, cancellationToken);
            }
            return _mBooks;
        }

        public void SaveMyBooksToCache(XCollection<Book> books, CancellationToken cancellationToken)
        {
            if (books != null) _dataCacheService.PutItem(books, ReadingBooksCacheItemName, cancellationToken);
        }

        public XCollection<Book> GetMyBooksByTimeLocal()
        {
            if (_myBooksByTime != null && _myBooksByTime.Count > 0)
            {
                var userBooks = new XCollection<Book>();
                foreach (var book in _myBooksByTime)
                {
                    if (book.IsMyBook) userBooks.Add(book);
                }
                return userBooks;
            }

            return _myBooksByTime;
        }

        public async Task<XCollection<Book>> GetAndRefreshMyBooks(CancellationToken cancellationToken)
		{
			string limit = string.Format("{0},{1}", 0, BooksInPage);

			var parameters = new Dictionary<string, object>
					{
						{"my", 1},			
#if PDF_ENABLED	
                        {"search_types", "0,4"},	
#else
                        {"search_types", "0"},	
#endif
						{"limit", limit},					
					};
			
			var books = await _client.GetMyBooks(parameters, cancellationToken);

            if (_myBooksByTime == null)
            {
                _myBooksByTime = new XCollection<Book>();
                _myBooksByTime.Update(books.Books);
            }

			if (books != null && books.Books != null && books.Books.Count > 0)
			{
				books.Books.ForEach( x => x.IsMyBook = true );

                if(_myBooksByTime != null && _myBooksByTime.Count > 0)
                {
                    var selfServiceMessage = new StringBuilder();
                    _myBooksByTime.ForEach(myBook =>
                    {
                        try
                        {
                            var book = books.Books.First(bk => bk.Id.Equals(myBook.Id));

                            if (!string.IsNullOrEmpty(myBook.SelfServiceMyRequest) && myBook.SelfServiceMyRequest.Equals("1"))
                            {
                                if (!string.IsNullOrEmpty(book.SelfServiceMyRequest) && book.SelfServiceMyRequest.Equals("0"))
                                {
                                    myBook.SelfService = book.SelfService;
                                    myBook.SelfServiceMyRequest = book.SelfServiceMyRequest;
                                    myBook.ExpiredDateStr = book.ExpiredDateStr;
                                    selfServiceMessage.AppendLine(book.Description.Hidden.TitleInfo.BookTitle);
                                }                                
                            }

                            books.Books.Remove(book);
                        }
                        catch (Exception ex)
                        {}                                                
                    });

                    if (books.Books != null && books.Books.Count > 0)
                    {
                        _myBooksByTime.AddRange(books.Books);
                    }

                    if (selfServiceMessage.Length > 0)
                    {
                        await new MessageDialog("Вам выданы книги:", selfServiceMessage.ToString()).ShowAsync();
                        //Deployment.Current.Dispatcher.BeginInvoke(() => MessageBox.Show(selfServiceMessage.ToString(), "Вам выданы книги:", MessageBoxButton.OK));
                    }
                }

                _myBooksByTime.BeginUpdate();
                _myBooksByTime.EndUpdate();

                _dataCacheService.PutItem(_myBooksByTime, ReadingBooksCacheItemName, cancellationToken);

                AddIdsToMyBooks(_myBooksByTime);
                CheckBooks();
			}
            			
			return _myBooksByTime;
		}

	    public async Task<XCollection<Book>> GetBooksInBasket(CancellationToken cancellationToken)
	    {
	        if (_myBasket == null)
	        {
	            _myBasket = new XCollection<Book>();
	        }

            string limit = string.Format("{0},{1}", 0, BooksInPage);
            var parameters = new Dictionary<string, object>
                    {
                        {"basket", 1},										
#if PDF_ENABLED	
                        {"search_types", "0,4"},	
#else
                        {"search_types", "0"},	
#endif
						{"limit", limit},
                    };

            var books = await _client.GetBooksInBasket(parameters, cancellationToken);

	        books?.Books?.ForEach(x => x.IsMyBook = false);

            _myBasket.Clear();
            _myBasket.Update(books.Books);

            //_dataCacheService.PutItem(_myBooksByTime, ReadingBooksCacheItemName, cancellationToken);

            //AddIdsToMyBooks(_myBooksByTime);

            return _myBasket;
        }

        public async Task<XCollection<Book>> GetMyBooks(CancellationToken cancellationToken)
        {
            if (_myBooksByTime == null)
            {
                _myBooksByTime = new XCollection<Book>();
            }

            string limit = string.Format("{0},{1}", 0, BooksInPage);
            var parameters = new Dictionary<string, object>
					{
						{"my", 1},										
#if PDF_ENABLED	
                        {"search_types", "0,4"},	
#else
                        {"search_types", "0"},	
#endif
						{"limit", limit},					
					};

            var books = await _client.GetMyBooks(parameters, cancellationToken);

            if (books != null && books.Books != null)
            {
                books.Books.ForEach(x => x.IsMyBook = true);
            }

            _myBooksByTime.Clear();
            _myBooksByTime.Update(books.Books);

            _dataCacheService.PutItem(_myBooksByTime, ReadingBooksCacheItemName, cancellationToken);

            AddIdsToMyBooks(_myBooksByTime);

            return _myBooksByTime;
        }
        public async Task<Book> GetMyBook(int bookId, CancellationToken cancellationToken, bool ignorCache = false)
		{
			Book book = null;

            if (!ignorCache)
            {
                if (_myBooksByTime != null)
                {
                    book = _myBooksByTime.FirstOrDefault(bk => bk.Id.Equals(bookId));
                }

                if (book == null && _myBooks != null)
                {
                    book = _myBooks.FirstOrDefault(bk => bk.Id.Equals(bookId));
                }
            }

            if (book == null)
			{
				var parameters = new Dictionary<string, object>
						{							
							{"my", 1},							
							{"art", bookId}								
						};
				var books = await _client.GetMyBooks(parameters, cancellationToken);
				if (books != null && books.Books != null && books.Books.Count > 0)
				{
					if (books.Books != null)
					{
						books.Books.ForEach(x => x.IsMyBook = true);
					}

					if(!ignorCache) AddIdsToMyBooks( new List<Book>( books.Books ) );
					return books.Books[0];
				}
			}

			return book;
		}
		public async Task<XCollection<Book>> GetAllMyBooks(CancellationToken cancellationToken)
		{
		    XCollection<Book> _Fragments = null; 
			if (_myBooks == null)
			{
				_myBooks = new XCollection<Book>();
			}
			else
			{
                _Fragments = _myBooks.Derive(bk => bk.isFragment);
				_myBooks.Clear();
			}
            if (_Fragments == null && _myBooksByTime != null) _Fragments = _myBooksByTime.Derive(bk => bk.isFragment);

			while (true)
			{
				string limit = string.Format("{0},{1}", _myBooks.Count, BooksInPage);
				var parameters = new Dictionary<string, object>
						{
							{ "my", 1 },
#if PDF_ENABLED	
                        {"search_types", "0,4"},	
#else
                        {"search_types", "0"},	
#endif
							{ "limit", limit }
						};

				var books = await _client.GetMyBooks(parameters, CancellationToken.None);

			    if (books != null && books.Books != null)
			    {
			        books.Books.ForEach(x => x.IsMyBook = true);


			        _myBooks.BeginUpdate();
			        _myBooks.AddRange(books.Books);
			        _myBooks.EndUpdate();

			        if (books.Books.Count >= BooksInPage || books.Books.Count == 0)
			        {
			            break;
			        }
			    }
			    else
			    {
			        break;
			    }
			}

            if (_Fragments != null)
		    {
                _Fragments.AddRange(_myBooks);
                _myBooks.BeginUpdate();
                _myBooks.Clear();
                _myBooks.AddRange(_Fragments);
                _myBooks.EndUpdate();
		        _Fragments = null;
		    }

			_dataCacheService.PutItem( _myBooks, AllMyBooksCacheItemName, CancellationToken.None );
			_dataCacheService.PutItem( GetTileBooksFromBookCollection( _myBooks ), AllMyBooksTileCacheItemName, CancellationToken.None );

			//cache images
			foreach( var myBook in _myBooks )
			{
				_fileDownloadService.DownloadFileAsynchronously( myBook.CoverPreviewSized );
			}
			
			//Update id
			if( _myBooksIds == null )
			{
				_myBooksIds = new List<int>();
			}
			else
			{
				_myBooksIds.Clear();
			}

			_myBooksIds.AddRange( _myBooks.Select( x => x.Id ) );

			_dataCacheService.PutItem( _myBooksIds, AllMyBooksIdCacheItemName, CancellationToken.None );

			//check exits books
			if( _myBooksByTime != null )
			{
				for( int i = _myBooksByTime.Count - 1; i >= 0; i-- )
				{
				    var mBook = _myBooks.FirstOrDefault(x => x.Id == _myBooksByTime[i].Id);
				    if (mBook == null)
				    {
				        _myBooksByTime.Remove(_myBooksByTime[i]);
				    }
				    else
				    {
				        _myBooksByTime[i].Update(mBook);
				    }
				}
			}

			_dataCacheService.PutItem( _myBooksByTime, ReadingBooksCacheItemName, CancellationToken.None );

			CheckBooks();

			return _myBooks;
		}
        public async Task<XCollection<Book>> GetPopularBooks(int fromPosition, CancellationToken cancellationToken, int customBooksOnPage = 0)
        {
            int booksOnPage = customBooksOnPage > 0 ? customBooksOnPage : BooksInPage;
			if (_popularBooks == null)
			{
                string limit = string.Format("{0},{1}", fromPosition, booksOnPage);

				var parameters = new Dictionary<string, object>
						{								
							{"sort", "pop_desc"},																
							{"limit", limit},
						};

				var books = await _client.SearchCatalog(parameters, cancellationToken);

				CheckMyBooks( books.Books );

				_popularBooks = books.Books;
			}
			else
			{
                int lastIndex = fromPosition + booksOnPage;

				var collection = new XCollection<Book>( _popularBooks.Take( lastIndex ) );

				if (collection.Count() + BooksInPageShift < lastIndex)
				{
					lastIndex = collection.Count();

                    string limit = string.Format("{0},{1}", lastIndex, booksOnPage);

					var parameters = new Dictionary<string, object>
						{								
							{"sort", "pop_desc"},										
							{"limit", limit},	
						};

					var books = await _client.SearchCatalog(parameters, cancellationToken);

					if (books != null && books.Books != null && books.Books.Any())
					{
						CheckMyBooks( books.Books );

						foreach (var book in books.Books)
						{
							if (!collection.ContainsKey( book.GetKey() ))
							{
								collection.Add( book );
							}
						}
					}

					_popularBooks = collection;
				}

				return collection;
			}

			return _popularBooks;
		}
        public async Task<XCollection<Book>> GetInterestingBooks(int fromPosition, CancellationToken cancellationToken, int customBooksOnPage = 0)
		{
            int booksOnPage = customBooksOnPage > 0 ? customBooksOnPage : BooksInPage;
			if (_noveltyBooks == null)
			{
                string limit = string.Format("{0},{1}", fromPosition, booksOnPage);

				var parameters = new Dictionary<string, object>
						{																
							{"rating", "hot"},										
							{"limit", limit},			
						};

				var books = await _client.SearchCatalog(parameters, cancellationToken);

				CheckMyBooks( books.Books );

				_noveltyBooks = books.Books;
			}
			else
			{
                int lastIndex = fromPosition + booksOnPage;

				var collection = new XCollection<Book>( _noveltyBooks.Take( lastIndex ) );

				if (collection.Count() + BooksInPageShift < lastIndex)
				{
					lastIndex = collection.Count();

                    string limit = string.Format("{0},{1}", lastIndex, booksOnPage);

					var parameters = new Dictionary<string, object>
						{																
							{"rating", "hot"},										
							{"limit", limit},
						};

					var books = await _client.SearchCatalog(parameters, cancellationToken);

					if (books != null && books.Books != null && books.Books.Any())
					{
						CheckMyBooks( books.Books );

						foreach (var book in books.Books)
						{
							if (!collection.ContainsKey( book.GetKey() ))
							{
								collection.Add( book );
							}
						}
					}

					_noveltyBooks = collection;
				}

				return collection;
			}
			return _noveltyBooks;
		}
        public async Task<XCollection<Book>> GetNoveltyBooks(int fromPosition, CancellationToken cancellationToken, int customBooksOnPage = 0)
		{
            int booksOnPage = customBooksOnPage > 0 ? customBooksOnPage : BooksInPage;
			if (_interestingBooks == null)
			{
                string limit = string.Format("{0},{1}", fromPosition, booksOnPage);

				var parameters = new Dictionary<string, object>
						{						
							{"sort", "time_desc"},									
							{"min_person_rating", "6"},								
							{"limit", limit},
						};

				var books = await _client.SearchCatalog(parameters, cancellationToken);

				CheckMyBooks( books.Books );

				_interestingBooks = books.Books;
			}
			else
			{
                int lastIndex = fromPosition + booksOnPage;

				var collection = new XCollection<Book>( _interestingBooks.Take( lastIndex ) );

				if (collection.Count() + BooksInPageShift < lastIndex)
				{
					lastIndex = collection.Count();

                    string limit = string.Format("{0},{1}", lastIndex, booksOnPage);

					var parameters = new Dictionary<string, object>
						{								
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif									
							{"rating", "authors"},								
							{"limit", limit},
						};

					var books = await _client.SearchCatalog(parameters, cancellationToken);

					if (books != null && books.Books != null && books.Books.Any())
					{
						CheckMyBooks( books.Books );

						foreach (var book in books.Books)
						{
							if (!collection.ContainsKey( book.GetKey() ))
							{
								collection.Add( book );
							}
						}
					}

					_interestingBooks = collection;
				}

				return collection;
			}
			return _interestingBooks;
		}
		public async Task<XCollection<Book>> GetNoveltyBooksByGenre(int fromPosition, int genreId, CancellationToken cancellationToken)
		{
			if (_noveltyBooksByGenre == null || _noveltyBooksByGenreId != genreId)
			{
				string limit = string.Format( "{0},{1}", fromPosition, BooksInPage );

				var parameters = new Dictionary<string, object>
						{								
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif								
							{"genre", genreId},														
							{"limit", limit},									
							{"sort", "time_desc"},
						};

				var books = await _client.SearchCatalog(parameters, cancellationToken);

				CheckMyBooks( books.Books );

				_noveltyBooksByGenreId = genreId;

				_noveltyBooksByGenre = books.Books;
			}
			else
			{
				int lastIndex = fromPosition + BooksInPage;

				XCollection<Book> collection = new XCollection<Book>( _noveltyBooksByGenre.Take( lastIndex ) );

				if (collection.Count() + BooksInPageShift < lastIndex)
				{
					lastIndex = collection.Count();

					string limit = string.Format( "{0},{1}", lastIndex, BooksInPage );

					var parameters = new Dictionary<string, object>
						{								
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif
                        {"genre", genreId},													
							{"limit", limit},										
							{"sort", "time_desc"},		
						};

					var books = await _client.SearchCatalog(parameters, cancellationToken);

					if (books != null && books.Books != null && books.Books.Any())
					{
						CheckMyBooks( books.Books );

						foreach (var book in books.Books)
						{
							if (!collection.ContainsKey( book.GetKey() ))
							{
								collection.Add( book );
							}
						}
					}

					_noveltyBooksByGenre = collection;
				}

				return collection;
			}
			return _noveltyBooksByGenre;
		}

		public async Task<XCollection<Book>> GetNoveltyBooksByGenres(int fromPosition, List<int> genreId, CancellationToken cancellationToken)
		{
			if (_noveltyBooksByGenres == null ||  _noveltyBooksByGenresIds == null || !_noveltyBooksByGenresIds.SequenceEqual( genreId ) )
			{
				string limit = string.Format( "{0},{1}", fromPosition, BooksInPage );

				var parameters = new Dictionary<string, object>
						{								
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif
                        {"genre", genreId},														
							{"limit", limit},								
							{"sort", "time_desc"},			
						};

				var books = await _client.SearchCatalog(parameters, cancellationToken);

				CheckMyBooks( books.Books );

				_noveltyBooksByGenresIds = genreId;

				_noveltyBooksByGenres = books.Books;
			}
			else
			{
				int lastIndex = fromPosition + BooksInPage;

				XCollection<Book> collection = new XCollection<Book>( _noveltyBooksByGenres.Take( lastIndex ) );

				if (collection.Count() + BooksInPageShift < lastIndex)
				{
					lastIndex = collection.Count();

					string limit = string.Format( "{0},{1}", lastIndex, BooksInPage );

					var parameters = new Dictionary<string, object>
						{								
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif
                        	{"genre", genreId},											
							{"limit", limit},							
							{"sort", "time_desc"},	
						};

					var books = await _client.SearchCatalog(parameters, cancellationToken);

					if (books != null && books.Books != null && books.Books.Any())
					{
						CheckMyBooks( books.Books );

						foreach (var book in books.Books)
						{
							if (!collection.ContainsKey( book.GetKey() ))
							{
								collection.Add( book );
							}
						}
					}

					_noveltyBooksByGenres = collection;
				}

				return collection;
			}
			return _noveltyBooksByGenres;
		}

	    public async Task<XCollection<Book>> GetBooksByTag(int fromPosition, int tagId, CancellationToken cancellationToken)
	    {
            if (_booksByTag == null || _booksByTagId != tagId)
            {
                string limit = string.Format("{0},{1}", fromPosition, BooksInPage);

                var parameters = new Dictionary<string, object>
                        {
                            {"tag", tagId},
                            {"limit", limit},
                        };

                var books = await _client.SearchCatalog(parameters, cancellationToken);

                CheckMyBooks(books.Books);

                _booksByTagId = tagId;

                _booksByTag = books.Books;
            }
            else
            {
                int lastIndex = fromPosition + BooksInPage;

                var collection = new XCollection<Book>(_booksByTag.Take(lastIndex));

                if (collection.Count() + BooksInPageShift < lastIndex)
                {
                    lastIndex = collection.Count();

                    string limit = string.Format("{0},{1}", lastIndex, BooksInPage);

                    var parameters = new Dictionary<string, object>
                        {
                            {"genre", tagId},
                            {"limit", limit},
                        };

                    var books = await _client.SearchCatalog(parameters, cancellationToken);

                    if (books != null && books.Books != null && books.Books.Any())
                    {
                        CheckMyBooks(books.Books);

                        foreach (var book in books.Books)
                        {
                            if (!collection.ContainsKey(book.GetKey()))
                            {
                                collection.Add(book);
                            }
                        }
                    }

                    _booksByTag = collection;
                }

                return collection;
            }
            return _booksByTag;
        }

        public async Task<XCollection<Book>> GetPopularBooksByGenre(int fromPosition, int genreId, CancellationToken cancellationToken)
		{
			if (_popularBooksByGenre == null || _popularBooksByGenreId != genreId)
			{
				string limit = string.Format( "{0},{1}", fromPosition, BooksInPage );

				var parameters = new Dictionary<string, object>
						{														
							{"genre", genreId},							
							{"limit", limit},									
							{"sort", "pop_desc"},								
						};

				var books = await _client.SearchCatalog(parameters, cancellationToken);

				CheckMyBooks( books.Books );

				_popularBooksByGenreId = genreId;

				_popularBooksByGenre = books.Books;
			}
			else
			{
				int lastIndex = fromPosition + BooksInPage;

				var collection = new XCollection<Book>( _popularBooksByGenre.Take( lastIndex ) );

				if (collection.Count() + BooksInPageShift < lastIndex)
				{
					lastIndex = collection.Count();

					string limit = string.Format( "{0},{1}", lastIndex, BooksInPage );

					var parameters = new Dictionary<string, object>
						{																
							{"genre", genreId},							
							{"limit", limit},									
							{"sort", "pop_desc"},		
						};

					var books = await _client.SearchCatalog(parameters, cancellationToken);

					if (books != null && books.Books != null && books.Books.Any())
					{
						CheckMyBooks( books.Books );

						foreach (var book in books.Books)
						{
							if (!collection.ContainsKey( book.GetKey() ))
							{
								collection.Add( book );
							}
						}
					}

					_popularBooksByGenre = collection;
				}

				return collection;
			}
			return _popularBooksByGenre;
		}

		public async Task<XCollection<Book>> GetPopularBooksByGenres(int fromPosition, List<int> genreId, CancellationToken cancellationToken)
		{
			if( _popularBooksByGenres == null || _popularBooksByGenresIds == null || !_popularBooksByGenresIds.SequenceEqual( genreId ) )
			{
				string limit = string.Format( "{0},{1}", fromPosition, BooksInPage );

				var parameters = new Dictionary<string, object>
						{																	
							{"genre", genreId},							
							{"limit", limit},																
							{"sort", "pop_desc"},								
						};

				var books = await _client.SearchCatalog(parameters, cancellationToken);

				CheckMyBooks( books.Books );

				_popularBooksByGenresIds = genreId;

				_popularBooksByGenres = books.Books;
			}
			else
			{
				int lastIndex = fromPosition + BooksInPage;

				var collection = new XCollection<Book>( _popularBooksByGenres.Take( lastIndex ) );

				if (collection.Count() + BooksInPageShift < lastIndex)
				{
					lastIndex = collection.Count();

					string limit = string.Format( "{0},{1}", lastIndex, BooksInPage );

					var parameters = new Dictionary<string, object>
						{															
							{"genre", genreId},							
							{"limit", limit},															
							{"sort", "pop_desc"},		
						};

					var books = await _client.SearchCatalog(parameters, cancellationToken);

					if (books != null && books.Books != null && books.Books.Any())
					{
						CheckMyBooks( books.Books );

						foreach (var book in books.Books)
						{
							if (!collection.ContainsKey( book.GetKey() ))
							{
								collection.Add( book );
							}
						}
					}

					_popularBooksByGenres = collection;
				}

				return collection;
			}
			return _popularBooksByGenres;
		}

		public async Task AddToHistory( Book book, CancellationToken cancellationToken )
		{
			if( book != null )
			{
				await GetBooksIdsFromHistory( cancellationToken );

				if( _myHistoryBooksIds.Contains( book.Id ) )
				{
					_myHistoryBooksIds.Remove( book.Id );
				}
				
				_myHistoryBooksIds.Insert( 0, book.Id );

				_dataCacheService.PutItem( _myHistoryBooksIds, ReadHistoryBooksIdCacheItemName, cancellationToken );
			}
		}

		public async Task<List<int>> GetBooksIdsFromHistory( CancellationToken cancellationToken )
		{
			if( _myHistoryBooksIds == null || _myHistoryBooksIds.Count == 0 )
			{
				DateTime cacheModificationDate = await _dataCacheService.GetItemModificationDate( ReadHistoryBooksIdCacheItemName );
				if( !cacheModificationDate.Equals( DateTime.MinValue ) )
				{
					try
					{
						_myHistoryBooksIds =  _dataCacheService.GetItem<List<int>>( ReadHistoryBooksIdCacheItemName );
					}
					catch
					{
					}
				}
				else
				{
					if( _myHistoryBooksIds != null )
					{
						_myHistoryBooksIds.Clear();
					}
				}

				_myHistoryBooksIds = _myHistoryBooksIds ?? new List<int>();
			}

			return _myHistoryBooksIds;
		}

        public async Task<BannersResponse> GetBanners(CancellationToken cancellationToken)
		{
            if (!_networkAvailabilityService.NetworkAvailable) return null;

            var parameters = new Dictionary<string, object>
			{								
				{"size", "phn"},																
			};
            return await _client.GetBanners(parameters, cancellationToken);
		}

		public async Task AddToMyBooks( Book book, CancellationToken cancellationToken )
		{
			if (book != null)
			{
				if (_myBooks == null)
				{
					_myBooks = await GetAllMyBooksFromCache( cancellationToken );
					_myBooks = _myBooks ?? new XCollection<Book>();
				}
				if (_myBooksByTime == null)
				{
					_myBooksByTime = await GetMyBooksFromCache( cancellationToken );
					_myBooksByTime = _myBooksByTime ?? new XCollection<Book>();
				}

				book.IsMyBook = true;

				var foundedBook = _myBooks.FirstOrDefault(bk => bk.Id.Equals(book.Id));

				if (foundedBook == null)
				{
					_myBooks.BeginUpdate();
					_myBooks.Insert(0, book);
					_myBooks.EndUpdate();

					_dataCacheService.PutItem( _myBooks, AllMyBooksCacheItemName, CancellationToken.None );
					_dataCacheService.PutItem( GetTileBooksFromBookCollection( _myBooks ), AllMyBooksTileCacheItemName, CancellationToken.None );
				}

				var foundedInMyBooks = _myBooksByTime.FirstOrDefault( bk => bk.Id.Equals( book.Id ) );

				_myBooksByTime.BeginUpdate();

				if( foundedInMyBooks != null )
				{
					_myBooksByTime.Remove( foundedInMyBooks );
				}

				_myBooksByTime.Insert( 0, book );

				if( _myBooksByTime.Count > BooksInPage )
				{
					_myBooksByTime = new XCollection<Book>( _myBooksByTime.Take( BooksInPage ).ToArray() );
				}

				_myBooksByTime.EndUpdate();

				_dataCacheService.PutItem( _myBooksByTime, ReadingBooksCacheItemName, CancellationToken.None );
			}

			AddIdsToMyBooks( new List<Book> { book } );
		}

        public async Task AddFragmentToMyBooks(Book book, CancellationToken cancellationToken)
        {
            if (book != null)
            {
                book.IsMyBook = false;
                book.isFragment = true;
                if (_myBooks == null)
                {
                    _myBooks = await GetAllMyBooksFromCache(cancellationToken);
                    _myBooks = _myBooks ?? new XCollection<Book>();
                }
                if (_myBooksByTime == null)
                {
                    _myBooksByTime = await GetMyBooksFromCache(cancellationToken);
                    _myBooksByTime = _myBooksByTime ?? new XCollection<Book>();
                }

                var foundedBook = _myBooks.FirstOrDefault(bk => bk.Id.Equals(book.Id));

                if (foundedBook == null)
                {
                    _myBooks.BeginUpdate();
                    _myBooks.Insert(0, book);
                    _myBooks.EndUpdate();

                    _dataCacheService.PutItem(_myBooks, AllMyBooksCacheItemName, CancellationToken.None);
                    _dataCacheService.PutItem(GetTileBooksFromBookCollection(_myBooks), AllMyBooksTileCacheItemName, CancellationToken.None);
                }

                var foundedInMyBooks = _myBooksByTime.FirstOrDefault(bk => bk.Id.Equals(book.Id));

                _myBooksByTime.BeginUpdate();

                if (foundedInMyBooks != null)
                {
                    _myBooksByTime.Remove(foundedInMyBooks);
                }

                _myBooksByTime.Insert(0, book);

                if (_myBooksByTime.Count > BooksInPage)
                {
                    _myBooksByTime = new XCollection<Book>(_myBooksByTime.Take(BooksInPage).ToArray());
                }

                _myBooksByTime.EndUpdate();

                _dataCacheService.PutItem(_myBooksByTime, ReadingBooksCacheItemName, CancellationToken.None);
            
                
                AddIdsToMyBooks(new List<Book> { book });
            }
        }

	    public async Task<Book> GetHiddenBook(int id, CancellationToken cancellationToken)
	    {
            var parameters = new Dictionary<string, object> { { "art", id } };
            var books = await _client.SearchCatalog(parameters, cancellationToken, "wp8-ebook-hidden.litres.ru");
            if (books != null && books.Books != null && books.Books.Count > 0)
            {
                CheckMyBooks(books.Books);

                _singleBooks.Insert(0, books.Books[0]);
                if (_singleBooks.Count > SingleBooksCount)
                {
                    _singleBooks.RemoveAt(SingleBooksCount);
                }

                books.Books[0].isHiddenBook = true;
                return books.Books[0];
            }
	        return null;
	    }

        public async Task<Book> GetBookOnline(int id, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<string, object>
					    {							
						    {"art", id}								
					    };
            var books = await _client.SearchCatalog(parameters, cancellationToken);
            if (books != null && books.Books != null && books.Books.Count > 0)
            {
                CheckMyBooks(books.Books);
                return books.Books[0];
            }
            return null;
        }
		public async Task<Book> GetBook(int id, CancellationToken cancellationToken)
		{
			Book book = null;
			if (_singleBooks == null)
			{
				_singleBooks = new XCollection<Book>();
			}
			book = _singleBooks.FirstOrDefault(bk => bk.Id.Equals(id));
			if (book != null)
			{
				//move to first
				_singleBooks.Remove( book );
				_singleBooks.Insert(0, book);
			}
            if (book == null)
            {
                var exists = await _bookProvider.GetExistBooks(cancellationToken);
                if (exists != null)
                {
                    book = exists.FirstOrDefault(x => x.Id == id);

                    if (book != null)
                    {
                        return book;
                    }
                }
            }
			else if (book == null && _myBooksByTime != null)
			{
				book = _myBooksByTime.FirstOrDefault(bk => bk.Id.Equals(id));
			}
			if (book == null && _myBooks == null)
			{
				_myBooks = await GetAllMyBooksFromCache( cancellationToken );
			}
			if (book == null && _myBooks != null)
			{
				book = _myBooks.FirstOrDefault(bk => bk.Id.Equals(id));
			}
			if (book == null && _interestingBooks != null)
			{
				book = _interestingBooks.FirstOrDefault(bk => bk.Id.Equals(id));
			}
			if (book == null && _noveltyBooks != null)
			{
				book = _noveltyBooks.FirstOrDefault(bk => bk.Id.Equals(id));
			}
			if (book == null && _popularBooks != null)
			{
				book = _popularBooks.FirstOrDefault(bk => bk.Id.Equals(id));
			}
		    if (book == null && _booksByCollection != null)
		    {
                book = _booksByCollection.FirstOrDefault(bk => bk.Id.Equals(id));    
		    }

			if (book == null)
			{			
				var parameters = new Dictionary<string, object>{{"art", id}};
				var books = await _client.SearchCatalog(parameters, cancellationToken);
				if (books != null && books.Books != null && books.Books.Count > 0)
				{
					CheckMyBooks( books.Books );

					_singleBooks.Insert(0, books.Books[0]);
					if (_singleBooks.Count > SingleBooksCount)
					{
						_singleBooks.RemoveAt(SingleBooksCount);
					}
					return books.Books[0];
				}
			}
			return book;
		}

		public async Task<Book> GetBookByDocumentId(string id, CancellationToken cancellationToken)
		{
			Book book = null;
			if (_singleBooks == null)
			{
				_singleBooks = new XCollection<Book>();
			}
			book = _singleBooks.FirstOrDefault(bk => bk.Description.Hidden.DocumentInfo.Id.Equals(id));
			if (book != null)
			{
				//move to first
				_singleBooks.Remove( book );
				_singleBooks.Insert(0, book);
			}

			if (book == null && _myBooksByTime != null)
			{
				book = _myBooksByTime.FirstOrDefault(bk => bk.Description.Hidden.DocumentInfo.Id.Equals(id));
			}
			if (book == null && _myBooks == null)
			{
				_myBooks = await GetAllMyBooksFromCache(cancellationToken);
			}
			if (book == null && _myBooks != null)
			{
				book = _myBooks.FirstOrDefault(bk => bk.Description.Hidden.DocumentInfo.Id.Equals(id));
			}
			if (book == null && _interestingBooks != null)
			{
				book = _interestingBooks.FirstOrDefault(bk => bk.Description.Hidden.DocumentInfo.Id.Equals(id));
			}
			if (book == null && _noveltyBooks != null)
			{
				book = _noveltyBooks.FirstOrDefault(bk => bk.Description.Hidden.DocumentInfo.Id.Equals(id));
			}
			if (book == null && _popularBooks != null)
			{
				book = _popularBooks.FirstOrDefault(bk => bk.Description.Hidden.DocumentInfo.Id.Equals(id));
			}
			if (book == null)
			{
				var parameters = new Dictionary<string, object> {{"uuid", id}};
				var books = await _client.SearchCatalog(parameters, cancellationToken);
				if (books != null && books.Books != null && books.Books.Count > 0)
				{
					CheckMyBooks( books.Books );

					_singleBooks.Insert(0, books.Books[0]);
					if (_singleBooks.Count > SingleBooksCount)
					{
						_singleBooks.RemoveAt(SingleBooksCount);
					}
					return books.Books[0];
				}
			}
			return book;
		}

        public async Task<XCollection<Book>> GetBooksBySequence(int sequenceId, CancellationToken cancellationToken)
		{
            if (_booksBySequence == null || _booksBySequenceId != sequenceId)
			{
				var parameters = new Dictionary<string, object>
						{								
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif
                        {"sequence", sequenceId}		
						};

				var books = await _client.SearchCatalog(parameters, cancellationToken);

				if(books != null && books.Books.Count > 0)
				{
					for (int i = books.Books.Count - 1; i >= 0; i--)
					{
						var book = books.Books[i];
                        if (book.Sequences == null || 
                            book.Sequences.Sequences == null || 
                            book.Sequences.Sequences.Count == 0 ||
                            book.Sequences.Sequences[0].Id != sequenceId)
						{
							books.Books.RemoveAt( i );
						}
					}				
				}

			    if (books != null)
			    {
			        CheckMyBooks(books.Books);

			        _booksBySequenceId = sequenceId;
			        _booksBySequence = books.Books;
			    }
			    return _booksBySequence;
			}

			return _booksBySequence;
		}

		public async Task<XCollection<Book>> GetBooksByAuthor(int fromPosition, string authorId, CancellationToken cancellationToken)
		{
			if (_booksByAuthor == null || _booksByAuthorID != authorId)
			{
				string limit = string.Format("{0},{1}", fromPosition, BooksInPage);

				var parameters = new Dictionary<string, object>
						{								
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif
                            {"person", authorId},									
							{"limit", limit},			
						};

				var books = await _client.SearchCatalog(parameters, cancellationToken);

				CheckMyBooks( books.Books );

				_booksByAuthorID = authorId;
				_booksByAuthor = books.Books;
			}
			else
			{
				int lastIndex = fromPosition + BooksInPage;

				var collection = new XCollection<Book>(_booksByAuthor.Take(lastIndex));

				if (collection.Count() + BooksInPageShift < lastIndex)
				{
					lastIndex = collection.Count();

					string limit = string.Format("{0},{1}", lastIndex, BooksInPage);

					var parameters = new Dictionary<string, object>
						{								
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif
							{"person", authorId},								
							{"limit", limit},
						};

					var books = await _client.SearchCatalog(parameters, cancellationToken);

					if (books != null && books.Books != null && books.Books.Any())
					{
						CheckMyBooks( books.Books );

						foreach (var book in books.Books)
						{
							if (!collection.ContainsKey(book.GetKey()))
							{
								collection.Add(book);
							}
						}
					}

					_booksByAuthor = collection;
				}

				return collection;
			}
			return _booksByAuthor;
		}

		public async Task<XCollection<Book>> GetBooksAreReadWithThisBook(int fromPosition, int bookId, CancellationToken cancellationToken)
		{
			if (_booksByBook == null || _booksByBookID != bookId)
			{
				//string limit = string.Format("{0},{1}", fromPosition, BooksInPage);
				//Get only 5 elements
				var limit = string.Format( "{0},{1}", 0, 5 );

				var parameters = new Dictionary<string, object>
						{								
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif
							{"rating", "with"},
							{"art", bookId},				
							{"limit", limit},			
						};

				var books = await _client.SearchCatalog(parameters, cancellationToken);

				CheckMyBooks( books.Books );

				_booksByBookID = bookId;
				_booksByBook = books.Books;
			}
			else
			{
				int lastIndex = fromPosition + BooksInPage;

				var collection = new XCollection<Book>(_booksByBook.Take(lastIndex));

				if (collection.Count() + BooksInPageShift < lastIndex)
				{
					lastIndex = collection.Count();

					string limit = string.Format("{0},{1}", lastIndex, BooksInPage);

					var parameters = new Dictionary<string, object>
						{								
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif
							{"rating", "with"},
							{"uuid", bookId},					
							{"limit", limit},
						};

					var books = await _client.SearchCatalog(parameters, cancellationToken);

					if (books != null && books.Books != null && books.Books.Any())
					{
						CheckMyBooks( books.Books );

						foreach (var book in books.Books)
						{
							if (!collection.ContainsKey(book.GetKey()))
							{
								collection.Add(book);
							}
						}
					}

					_booksByBook = collection;
				}

				return collection;
			}
			return _booksByBook;
		}

        public async Task<Rootobject> SearchAll(int limit, string searchString, CancellationToken cancellationToken)
	    {
            var parameters = new Dictionary<string, object>
                        {	{"json", "1"},	
							{"q", searchString},
                            {"limit", limit},
                            { "disable_cache", 1}
                        };

            return await _client.SearchAll(parameters, cancellationToken, "http://win10-ebook.litres.ru/pages/search_rmd2/");
        }

		public async Task<XCollection<Book>> SearchBooks(int fromPosition, string searchString, CancellationToken cancellationToken)
		{
			if (_foundedBooks == null || _foundedBooksQuery != searchString)
			{
				string limit = string.Format("{0},{1}", fromPosition, BooksInPage);

				var parameters = new Dictionary<string, object>
						{								
                        {"search_types", "0"},	

							{"search", searchString},					
							{"limit", limit},			
						};

				var books = await _client.SearchCatalog(parameters, cancellationToken);

				CheckMyBooks( books.Books );

				_foundedBooksQuery = searchString;
				_foundedBooks = books.Books;
			}
			else
			{
				int lastIndex = fromPosition + BooksInPage;

				var collection = new XCollection<Book>(_foundedBooks.Take(lastIndex));

				if (collection.Count() + BooksInPageShift < lastIndex)
				{
					lastIndex = collection.Count();

					string limit = string.Format("{0},{1}", lastIndex, BooksInPage);

					var parameters = new Dictionary<string, object>
						{								
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif
							{"search_title", searchString},						
							{"limit", limit},
						};

					var books = await _client.SearchCatalog(parameters, cancellationToken);

					if (books != null && books.Books != null && books.Books.Any())
					{
						CheckMyBooks( books.Books );

						foreach (var book in books.Books)
						{
							if (!collection.ContainsKey(book.GetKey()))
							{
								collection.Add(book);
							}
						}
					}

					_foundedBooks = collection;
				}

				return collection;
			}
			return _foundedBooks;
		}

        public async Task<XCollection<Book>> GetBooksByCollection(int fromPosition, int collectionId, bool fromWeb, CancellationToken cancellationToken)
        {
            if (fromWeb && _booksByCollection!=null)
            {
                _booksByCollection.Clear();
                _booksByCollection = null;
            }
           return await GetBooksByCollection(fromPosition, collectionId, cancellationToken);
        }

#if DEBUG
        public async Task<XCollection<Book>> GetBooksByCollection(int fromPosition, int collectionId, CancellationToken cancellationToken)
        {
            if (_booksByCollection == null || _booksByCollectionIdCollection != collectionId)
            {
                string limit = string.Format("{0},{1}", fromPosition, BooksInPage);

                var parameters = new Dictionary<string, object>
						{								
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif									
							{"collection", collectionId},							
							{"limit", limit},
						};

                var books = await _client.SearchCatalog(parameters, cancellationToken, "whub.litres.ru");

                CheckMyBooks(books.Books);

                _booksByCollection = books.Books;
                _booksByCollectionIdCollection = collectionId;
            }
            else
            {
                int lastIndex = fromPosition + BooksInPage;

                var collection = new XCollection<Book>(_booksByCollection.Take(lastIndex));

                if (collection.Count() + BooksInPageShift < lastIndex)
                {
                    lastIndex = collection.Count();

                    string limit = string.Format("{0},{1}", lastIndex, BooksInPage);

                    var parameters = new Dictionary<string, object>
						{								
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif									
							{"collection", collectionId},							
							{"limit", limit},
						};

                    var books = await _client.SearchCatalog(parameters, cancellationToken, "whub.litres.ru");

                    if (books != null && books.Books != null && books.Books.Any())
                    {
                        CheckMyBooks(books.Books);

                        foreach (var book in books.Books)
                        {
                            if (!collection.ContainsKey(book.GetKey()))
                            {
                                collection.Add(book);
                            }
                        }
                    }

                    _booksByCollection = collection;
                    _booksByCollectionIdCollection = collectionId;
                }

                return collection;
            }
            return _booksByCollection;
        }
#else 
        public async Task<XCollection<Book>> GetBooksByCollection(int fromPosition, int collectionId, CancellationToken cancellationToken)
		{
			if (_booksByCollection == null || _booksByCollectionIdCollection != collectionId)
			{
				string limit = string.Format("{0},{1}", fromPosition, BooksInPage);

				var parameters = new Dictionary<string, object>
						{								
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif									
							{"collection", collectionId},							
							{"limit", limit},
						};

                var books = await _client.SearchCatalog(parameters, cancellationToken);
        
				CheckMyBooks(books.Books);

				_booksByCollection = books.Books;
				_booksByCollectionIdCollection = collectionId;
			}
			else
			{
				int lastIndex = fromPosition + BooksInPage;

				var collection = new XCollection<Book>(_booksByCollection.Take(lastIndex));

				if (collection.Count() + BooksInPageShift < lastIndex)
				{
					lastIndex = collection.Count();

					string limit = string.Format("{0},{1}", lastIndex, BooksInPage);

					var parameters = new Dictionary<string, object>
						{								
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif									
							{"collection", collectionId},							
							{"limit", limit},
						};

					var books = await _client.SearchCatalog(parameters, cancellationToken);

					if (books != null && books.Books != null && books.Books.Any())
					{
						CheckMyBooks(books.Books);

						foreach (var book in books.Books)
						{
							if (!collection.ContainsKey(book.GetKey()))
							{
								collection.Add(book);
							}
						}
					}

					_booksByCollection = collection;
					_booksByCollectionIdCollection = collectionId;
				}

				return collection;
			}
			return _booksByCollection;
		}
#endif

        public async Task<XCollection<Book>> GetPopularBooksByCollection(int fromPosition, int collectionId, CancellationToken cancellationToken)
        {
            if (_popularBooksByCollection == null || _popularBooksByCollectionIdCollection != collectionId)
            {
                var limit = string.Format("{0},{1}", fromPosition, BooksInPage);

                var parameters = new Dictionary<string, object>
						{	
							{"sort", "pop_desc"},
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif									
							{"collection", collectionId},							
							{"limit", limit},
						};

                var books = await _client.SearchCatalog(parameters, cancellationToken);

                CheckMyBooks(books.Books);

                _popularBooksByCollection = books.Books;
                _popularBooksByCollectionIdCollection = collectionId;
            }
            else
            {
                int lastIndex = fromPosition + BooksInPage;

                var collection = new XCollection<Book>(_popularBooksByCollection.Take(lastIndex));

                if (collection.Count() + BooksInPageShift < lastIndex)
                {
                    lastIndex = collection.Count();

                    string limit = string.Format("{0},{1}", lastIndex, BooksInPage);

                    var parameters = new Dictionary<string, object>
						{	
                            {"sort", "pop_desc"},							
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif									
							{"collection", collectionId},							
							{"limit", limit},
						};

                    var books = await _client.SearchCatalog(parameters, cancellationToken);

                    if (books != null && books.Books != null && books.Books.Any())
                    {
                        CheckMyBooks(books.Books);

                        foreach (var book in books.Books)
                        {
                            if (!collection.ContainsKey(book.GetKey()))
                            {
                                collection.Add(book);
                            }
                        }
                    }

                    _popularBooksByCollection = collection;
                    _popularBooksByCollectionIdCollection = collectionId;
                }

                return collection;
            }
            return _popularBooksByCollection;
        }

        public async Task<XCollection<Book>> GetNoveltyBooksByCollection(int fromPosition, int collectionId, CancellationToken cancellationToken)
        {
            if (_noveltyBooksByCollection == null || _noveltyBooksByCollectionIdCollection != collectionId)
            {
                string limit = string.Format("{0},{1}", fromPosition, BooksInPage);

                var parameters = new Dictionary<string, object>
						{	
							{"sort", "time_desc"},
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif									
							{"collection", collectionId},							
							{"limit", limit},
						};

                var books = await _client.SearchCatalog(parameters, cancellationToken);

                CheckMyBooks(books.Books);

                _noveltyBooksByCollection = books.Books;
                _noveltyBooksByCollectionIdCollection = collectionId;
            }
            else
            {
                int lastIndex = fromPosition + BooksInPage;

                var collection = new XCollection<Book>(_noveltyBooksByCollection.Take(lastIndex));

                if (collection.Count() + BooksInPageShift < lastIndex)
                {
                    lastIndex = collection.Count();

                    string limit = string.Format("{0},{1}", lastIndex, BooksInPage);

                    var parameters = new Dictionary<string, object>
						{	
                            {"sort", "time_desc"},							
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif							
							{"collection", collectionId},							
							{"limit", limit},
						};

                    var books = await _client.SearchCatalog(parameters, cancellationToken);

                    if (books != null && books.Books != null && books.Books.Any())
                    {
                        CheckMyBooks(books.Books);

                        foreach (var book in books.Books)
                        {
                            if (!collection.ContainsKey(book.GetKey()))
                            {
                                collection.Add(book);
                            }
                        }
                    }

                    _noveltyBooksByCollection = collection;
                    _noveltyBooksByCollectionIdCollection = collectionId;
                }

                return collection;
            }
            return _noveltyBooksByCollection;
        }

		public async Task<Book> GetBookByCollection(int collectionId, int bookId, CancellationToken cancellationToken)
		{
			var cachedBook = GetBookByCollectionCache( collectionId, bookId );

			if( cachedBook != null )
			{
				return cachedBook;
			}

			var parameters = new Dictionary<string, object>
						{								
#if PDF_ENABLED	
                        {"search_types", "4"},	
#else
                        {"search_types", "0"},	
#endif									
							{"art", bookId},									
							{"collection", collectionId}
						};

			var books = await _client.SearchCatalog(parameters, cancellationToken);

			if (_booksByCollection == null || collectionId != _booksByCollectionIdCollection)
			{
				_booksByCollection = new XCollection<Book>();
				_booksByCollectionIdCollection = collectionId;
			}

			if (books != null && books.Books != null && books.Books.Count > 0)
			{
				CheckMyBooks( books.Books );
				_booksByCollection.Add( books.Books[0] );

				return books.Books.FirstOrDefault();
			}

			return null;
		}

        public async Task<Book> GetAudioBook(int bookId, CancellationToken cancellationToken)
	    {
            var parameters = new Dictionary<string, object>
						{								
							{"linked_to", 1},
		                    {"type", 8},
                            {"art", bookId},
						};

            var books = await _client.SearchAudioCatalog(parameters, cancellationToken, "wp8-audio.litres.ru", false);
            if (books != null && books.Books.Count > 0) return books.Books[0];
            return null;
	    }

		public Book GetBookByCollectionCache( int collectionId, int bookId )
		{
			if (_booksByCollection != null && _booksByCollectionIdCollection == collectionId)
			{
				var book = _booksByCollection.FirstOrDefault( x => x.Id == bookId );
				if (book != null)
				{
					return book;
				}
			}

			return null;
		}

	    public void ClearBooksCollectionCache(int collectionId)
	    {
	        if (_booksByCollection != null)
	        {
	            _booksByCollection.Clear();
	            _booksByCollection = null;
	        }
	        if (_booksByCollectionIdCollection == collectionId) _booksByCollectionIdCollection = -1;

	    }

		public async Task TakeBookFromCollectionBySubscription( int bookId, CancellationToken cancellationToken )
		{
			var parameters = new Dictionary<string, object> { {"art", bookId},};
            await _client.TakeBookFromCollectionBySubscription(parameters, cancellationToken);
		}

        public async Task<bool> CheckBoughtBook(int bookId, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<string, object>
						{							
							{"my", 1},							
							{"art", bookId}								
						};
            var books = await _client.GetMyBooks(parameters, cancellationToken);
            if (books != null && books.Books != null && books.Books.Count > 0)
            {
                return true;
            }
            return false;
        }

        public async Task DeleteBook(Book book, bool fromAnywhere = false)
        {
            bool isBought = await CheckBoughtBook(book.Id, CancellationToken.None);
            bool isTrial = false;
            if (_bookProvider.FullBookExistsInLocalStorage(book.Id))
            {
                await _bookProvider.RemoveFullBookInLocalStorage(book);
            }
            else if (_bookProvider.TrialBookExistsInLocalStorage(book.Id))
            {
                _bookProvider.RemoveTrialBookInLocalStorage(book);
                isTrial = true;
            }
            book.isFragment = false;
            if (!isBought || book.IsExpiredBook || fromAnywhere)
            {
                if(isTrial || book.isFragment) book.IsMyBook = false;
                book.isFragment = false;
                if (_myBooksIds != null && _myBooksIds.Contains(book.Id))
                {
                    _myBooksIds.Remove(book.Id);                    
                }
                _dataCacheService.PutItem(_myBooksIds, AllMyBooksIdCacheItemName, CancellationToken.None);

                if (_myBooks != null)
                {                                      
                    try
                    {
                        var bookToDel = _myBooks.First(bk => bk.Id == book.Id);
                        _myBooks.BeginUpdate();
                        _myBooks.Remove(bookToDel);
                        _myBooks.EndUpdate();
                    }
                    catch (Exception){}
                }
                _dataCacheService.PutItem(_myBooks, AllMyBooksCacheItemName, CancellationToken.None);
                _dataCacheService.PutItem(GetTileBooksFromBookCollection(_myBooks), AllMyBooksTileCacheItemName, CancellationToken.None);

                if (_myBooksByTime != null)
                {                    
                    try
                    {
                        var bookToDel = _myBooksByTime.First(bk => bk.Id == book.Id);
                        _myBooksByTime.BeginUpdate();
                        _myBooksByTime.Remove(bookToDel);
                        _myBooksByTime.EndUpdate();
                    }
                    catch (Exception){}
                }
                _dataCacheService.PutItem(_myBooksByTime, ReadingBooksCacheItemName, CancellationToken.None);

                var existsBooks = await _bookProvider.GetExistBooks(CancellationToken.None);
                if (existsBooks != null && existsBooks.Count > 0)
                {
                    try
                    {
                        var bookToDel = existsBooks.First(bk => bk.Id == book.Id);
                        existsBooks.BeginUpdate();
                        existsBooks.Remove(bookToDel);
                        existsBooks.EndUpdate();
                    }
                    catch (Exception){}
                }
                _dataCacheService.PutItem(existsBooks, "ExistBooks", CancellationToken.None);
            }
        }

		public async void Clear()
		{
			if( _myBooksIds != null )
			{
				_myBooksIds.Clear();
			}

            var existsBooks = await _bookProvider.GetExistBooks(CancellationToken.None);

            if (_myBooks != null)
            {
                if (existsBooks != null)
                {
                    _myBooks.ForEach(myBook =>
                    {
                        foreach (var existBook in existsBooks)
                        {
                            if (existBook.Id == myBook.Id)
                            {
                                existBook.ReadedPercent = myBook.ReadedPercent;
                                existBook.ExpiredDateStr = myBook.ExpiredDateStr;
                                break;
                            }
                        }
                    });
                }

				_myBooks.Clear();
			}

			if( _myBooksByTime != null )
			{
				_myBooksByTime.Clear();
			}

			//need check existing collections
			CheckBooks();
            
            _dataCacheService.PutItem(_myBooksIds, AllMyBooksIdCacheItemName, CancellationToken.None);
            _dataCacheService.PutItem(existsBooks, AllMyBooksCacheItemName, CancellationToken.None);
            _dataCacheService.PutItem(_myBooksByTime, ReadingBooksCacheItemName, CancellationToken.None);
		}

		private XCollection<TileBook> GetTileBooksFromBookCollection( IEnumerable<Book> books )
		{
			var tiles = new XCollection<TileBook>();

			if( books != null )
			{
				tiles = new XCollection<TileBook>(books.Select( x => new TileBook
				{
                    BookTitle = x != null ? x.BookTitle : string.Empty, 
                    Cover = x != null ? x.Cover : string.Empty
				}));
			}

			return tiles;
		}

		private void AddIdsToMyBooks(IEnumerable<Book> myBooks)
		{
			if (_myBooksIds == null)
			{
				_myBooksIds = new List<int>();
			}

			bool addNew = false;

			if (myBooks != null)
			{
				foreach (var myBook in myBooks)
				{
					if (myBook != null)
					{
						if (!_myBooksIds.Contains(myBook.Id))
						{
							_myBooksIds.Add( myBook.Id );
							addNew = true;
						}	
					}
				}
			}

			if (addNew)
			{
				_dataCacheService.PutItem( _myBooksIds, AllMyBooksIdCacheItemName, CancellationToken.None );

				//need check existing collections
				CheckBooks();
			}
		}

        public bool IsAllMyBooksFromCacheLoaded()
        {
            if (_myBooks != null && _myBooks.Count > 0) return true;
            return false;
        }

		public void CheckBooks()
		{
			CheckMyBooks( _interestingBooks );
			CheckMyBooks( _noveltyBooks );
			CheckMyBooks( _popularBooks );
			CheckMyBooks( _noveltyBooksByGenre );
			CheckMyBooks( _popularBooksByGenre );
			CheckMyBooks( _booksBySequence );
			CheckMyBooks( _booksByAuthor );
			CheckMyBooks( _booksByBook );
			CheckMyBooks( _foundedBooks );
			CheckMyBooks( _booksByCollection );
            CheckMyBooks(_popularBooksByCollection);
            CheckMyBooks(_noveltyBooksByCollection);
			CheckMyBooks( _singleBooks );
		}

		public void CheckMyBooks( XCollection<Book> books )
		{
			if (books != null && _myBooksIds != null)
			{
                CheckLibraryBooks(books);

				foreach (var book in books)
				{
					book.IsMyBook = _myBooksIds.Contains( book.Id );

                    if (book.IsMyBook)
                    {
                        Book myBook = null;
                        if (_myBooksByTime != null) myBook = _myBooksByTime.FirstOrDefault(x => x.Id == book.Id);
                        if (myBook == null && _myBooks != null) myBook = _myBooks.FirstOrDefault(x => x.Id == book.Id);

                        if (myBook != null)
                        {
                            books.BeginUpdate();
                            book.ReadedPercent = myBook.ReadedPercent;
                            book.ExpiredDateStr = myBook.ExpiredDateStr;
                            book.IsExpiredBook = myBook.IsExpiredBook;
                            book.SelfService = myBook.SelfService;
                            book.SelfServiceMyRequest = myBook.SelfServiceMyRequest;
                            book.isFragment = myBook.isFragment;
                            book.IsMyBook = !book.isFragment;
                            books.EndUpdate();
                        }  


                    }
				}
			}
		}

        private async void CheckLibraryBooks(IEnumerable<Book> books)
        {
            if (books != null)
            {
                try
                {
                    var user = await _profileProvider.GetUserInfo(CancellationToken.None);
                    bool isLibrary = user.AccountType == (int)AccountTypeEnum.AccountTypeLibrary;
                    foreach (var book in books)
                    {
                        book.isLibraryAccountBook = isLibrary;
                    }
                }
                catch{}
            }
        }

        public void ClearBooksCollections()
        {
            _interestingBooks = null;
            _noveltyBooks = null;
            _popularBooks = null;
            _noveltyBooksByGenre = null;
            _popularBooksByGenre = null;
            _booksBySequence = null;
            _booksByAuthor = null;
            _booksByBook = null;
            _foundedBooks = null;
            _booksByCollection = null;
            _popularBooksByCollection = null;
            _noveltyBooksByCollection = null;
            _singleBooks = null;
        }

        public bool IsBooksCollectionsCleared()
        {
            return (_interestingBooks == null && _noveltyBooks == null && _popularBooks == null);
        }


	    public bool IsBooksCollectionLoaded(int collectionId)
	    {
	        return _booksByCollection != null && _booksByCollection.Count > 0 && _booksByCollectionIdCollection == collectionId;
	    }

	    public async Task ExpireBooks(IEnumerable<Book> books)
	    {
	        if (_myBooks == null)
	        {
	            await GetAllMyBooksFromCache(CancellationToken.None);
	        }

	        foreach (var book in books)
	        {
                try
                {
                    if (_myBooks != null)
                    {
                        var tmpBook = _myBooks.First(bk => bk.Id == book.Id); //
                        if (tmpBook != null) tmpBook.IsExpiredBook = true;
                    }
                }
                catch (Exception)
                { }
	        }
	        
            if (_myBooks != null && _myBooks.Count > 0) _dataCacheService.PutItem(_myBooks, AllMyBooksCacheItemName, CancellationToken.None);
	    }

	    public async Task AddFragmentToMyBasket(Book book, CancellationToken token)
	    {
	        if (book != null)
	        {
	            book.IsMyBook = false;
	            book.isFragment = true;
                var parameters = new Dictionary<string, object>
                    {
                        {"hub_id", book.Id},										
#if PDF_ENABLED	
                        {"search_types", "0,4"},	
#else
                        {"type", "2"},	
#endif
                    };
                await _client.AddBookToBasket(parameters, token);
            }
	    }

	    public void UpdateBook(Book book)
	    {
            try
            {
                if(_myBooks != null) _myBooks.First(bk => bk.Id == book.Id).Update(book);                
            }
            catch (Exception){}
            try
            {
                if (_myBooksByTime != null) _myBooksByTime.First(bk => bk.Id == book.Id).Update(book);
            }
            catch (Exception) { }
	    }

	    public async Task UpdateExistBook(Book book)
	    {
	        await _bookProvider.UpdateExistBook(book, CancellationToken.None);
            UpdateBook(book);
	    }
	}
}
