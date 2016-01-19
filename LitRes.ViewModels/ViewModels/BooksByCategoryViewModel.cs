using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Digillect;
using Digillect.Collections;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Exceptions;
using LitRes.Models;
using LitRes.Services;

namespace LitRes.ViewModels
{
	public class BooksByCategoryViewModel : ViewModel
	{
		#region BooksViewModelTypeEnum
#if DEBUG
        public enum BooksViewModelTypeEnum:int
        {
            Popular = 0,
            Novelty = 1,
            Interesting = 2,
            NokiaCollection = 3734,
            FreeBooks = 4,
            Tags = 5,
            Genre = 6,
            Sequense = 7,
            Collection = 8
        }
#else
		public enum BooksViewModelTypeEnum
		{
			Popular,
			Novelty,
			Interesting,
			NokiaCollection = 2634,
            FreeBooks = 4,
            Tags = 5,
            Genre = 6,
            Sequense = 7,
            Collection = 8
		}
#endif
        #endregion

        public const string MainPart = "Main";
		public const string LoadMorePart = "LoadMorePart";

		public const string BooksTypeParameter = "BooksViewModelType";

		private readonly ICatalogProvider _catalogProvider;
		private readonly INavigationService _navigationService;

		private int _booksViewModelType;
	    private int _genreOrTagOrSeriaID;

		private bool _loaded;
		private bool _loading;
		private bool _isEndOfList;

		#region Public Properties
		public int BooksViewModelType
		{
			get { return _booksViewModelType; }
			set { SetProperty( ref _booksViewModelType, value, "BooksViewModelType" ); }
		}

        public int GenreOrTagOrSeriaID
        {
            get { return _genreOrTagOrSeriaID; }
            set { SetProperty(ref _genreOrTagOrSeriaID, value, "GenreOrTagOrSeriaID"); }
        }

        public XCollection<Book> Books { get; private set; }

		public RelayCommand<Book> BookSelected { get; private set; }
		public RelayCommand LoadMoreCalled { get; private set; }
		#endregion

		#region Constructors/Disposer
		public BooksByCategoryViewModel(ICatalogProvider catalogProvider, INavigationService navigationService)
		{
			_catalogProvider = catalogProvider;
			_navigationService = navigationService;
            

            RegisterAction(MainPart).AddPart(session => LoadBooks(session), session => !_loaded);
            RegisterAction(LoadMorePart).AddPart(session => LoadBooks(session), session => !_isEndOfList);
            //RegisterPart( MainPart, ( session, part ) => LoadBooks( session ), ( session, part ) => !_loaded, true );
			//RegisterPart( LoadMorePart, ( session, part ) => LoadBooks( session ), ( session, part ) => !_isEndOfList, false );

			Books = new XCollection<Book>();

			BookSelected = new RelayCommand<Book>(book => _navigationService.Navigate("Book", XParameters.Create("BookEntity", book)), book => book != null);
			LoadMoreCalled = new RelayCommand(() => LoadMore(), () => true);
            
        }
		#endregion

		#region LoadMore
		public async void LoadMore()
		{
		    if (!_loading)
		    {
		        _loading = true;
		        try
		        {
		            await Load(new Session(LoadMorePart));
		        }
		        catch (Exception ex)
		        {
		            Debug.WriteLine(ex.Message);
		        }
		        _loading = false;
		    }
		}
		#endregion

		#region LoadBooks
		private async Task LoadBooks(Session session)
		{
			XCollection<Book> books = null;
			try
			{
				switch ((BooksViewModelTypeEnum)BooksViewModelType)
				{
					case BooksViewModelTypeEnum.Interesting:
						books = await _catalogProvider.GetInterestingBooks( Books.Count, session.Token );
						break;
					case BooksViewModelTypeEnum.Novelty:
						books = await _catalogProvider.GetNoveltyBooks( Books.Count, session.Token );
						break;
					case BooksViewModelTypeEnum.Popular:
						books = await _catalogProvider.GetPopularBooks( Books.Count, session.Token );
						break;
					case BooksViewModelTypeEnum.NokiaCollection:
                        books = await _catalogProvider.GetBooksByCollection(Books.Count, (int)BooksViewModelType, session.Token);
						break;
                    case BooksViewModelTypeEnum.FreeBooks:
                        books = await _catalogProvider.GetBooksByCollection(Books.Count, (int)BooksViewModelType, session.Token);
						break;
                    case BooksViewModelTypeEnum.Tags:
                        books = await _catalogProvider.GetBooksByTag(Books.Count, GenreOrTagOrSeriaID, session.Token);
                        break;
                    case BooksViewModelTypeEnum.Genre:
                        books = await _catalogProvider.GetPopularBooksByGenre(Books.Count, GenreOrTagOrSeriaID, session.Token);
                        break;
                    case BooksViewModelTypeEnum.Sequense:
                        books = await _catalogProvider.GetBooksBySequence(GenreOrTagOrSeriaID, session.Token);
                        break;
                    default:
                        books = await _catalogProvider.GetBooksByCollection(Books.Count, (int)BooksViewModelType, session.Token);
                        break;
				}
			}
			catch (CatalitNoCredentialException)
			{
				//ToDo: Do something? Message?
			}

			_loaded = true;

			if (books != null)
			{
				if (books.Count <= Books.Count)
				{
					_isEndOfList = true;
				}

				Books.Update(books);
			}
		}
		#endregion

        public async Task ReloadBooks()
        {
            XCollection<Book> books = null;
            Books.Clear();
            try
            {
                switch ((BooksViewModelTypeEnum)BooksViewModelType)
                {
                    case BooksViewModelTypeEnum.Interesting:
                        books = await _catalogProvider.GetInterestingBooks(Books.Count, CancellationToken.None);
                        break;
                    case BooksViewModelTypeEnum.Novelty:
                        books = await _catalogProvider.GetNoveltyBooks(Books.Count, CancellationToken.None);
                        break;
                    case BooksViewModelTypeEnum.Popular:
                        books = await _catalogProvider.GetPopularBooks(Books.Count, CancellationToken.None);
                        break;
                    case BooksViewModelTypeEnum.NokiaCollection:
                        books = await _catalogProvider.GetBooksByCollection(Books.Count, (int)BooksViewModelType, CancellationToken.None);
                        break;
                    case BooksViewModelTypeEnum.FreeBooks:
                        books = await _catalogProvider.GetBooksByCollection(Books.Count, (int)BooksViewModelType, CancellationToken.None);
                        break;
                    case BooksViewModelTypeEnum.Tags:
                        books = await _catalogProvider.GetBooksByTag(Books.Count, GenreOrTagOrSeriaID, CancellationToken.None);
                        break;
                    case BooksViewModelTypeEnum.Genre:
                        books = await _catalogProvider.GetPopularBooksByGenre(Books.Count, GenreOrTagOrSeriaID, CancellationToken.None);
                        break;
                    case BooksViewModelTypeEnum.Sequense:
                        books = await _catalogProvider.GetBooksBySequence(GenreOrTagOrSeriaID, CancellationToken.None);
                        break;
                    default:
                        books = await _catalogProvider.GetBooksByCollection(Books.Count, (int)BooksViewModelType, CancellationToken.None);
                        break;
                }
            }
            catch (CatalitNoCredentialException)
            {
                //ToDo: Do something? Message?
            }

            if (books != null)
            {                
                if (books.Count <= Books.Count)
                {
                    _isEndOfList = true;
                }
                Books.BeginUpdate();
                Books.Update(books);
                Books.EndUpdate();                
            }
        }

        public void RefreshBook(Book book)
        {
            if (Books.Count > 0)
            {
                for(int i=0; i<Books.Count; ++i)
                {
                    if (Books[i].Id == book.Id)
                    {
                        Books.BeginUpdate();
                        Books[i] = book;
                        Books.EndUpdate();
                        break;
                    }
                }
            }
        }
	}
}
