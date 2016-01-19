using System.Threading.Tasks;
using Digillect.Collections;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Exceptions;
using LitRes.Models;
using LitRes.Services;

namespace LitRes.ViewModels
{

	public class FreeBooksByCategoryViewModel : ViewModel
	{
		public const string MainPart = "Main";
		//public const string LoadMorePart = "LoadMorePart";
        private const string LoadMoreNoveltyBooksPart = "LoadMoreNoveltyBooks";
        private const string LoadMorePopularBooksPart = "LoadMorePopularBooks";

		public const string BooksTypeParameter = "BooksViewModelType";

		private readonly ICatalogProvider _catalogProvider;
		private readonly INavigationService _navigationService;
        private int _booksViewModelType;

		private bool _loadedPopular;
        private bool _loadedNovelty;
        private bool _isEndOfListNoveltyBooks;
        private bool _isEndOfListPopularBooks;

        public int BooksViewModelType
        {
            get { return _booksViewModelType; }
            set { SetProperty(ref _booksViewModelType, value, "BooksViewModelType"); }
        }
  
		//public XCollection<Book> Books { get; private set; }

        public XCollection<Book> NoveltyBooks { get; private set; }
        public XCollection<Book> PopularBooks { get; private set; }
        public RelayCommand LoadMoreNoveltyBooks { get; private set; }
        public RelayCommand LoadMorePopularBooks { get; private set; }
		public RelayCommand<Book> BookSelected { get; private set; }
        
        //public RelayCommand LoadMoreCalled { get; private set; }

		#region Constructors/Disposer
        public FreeBooksByCategoryViewModel(ICatalogProvider catalogProvider, INavigationService navigationService)
		{
			_catalogProvider = catalogProvider;
			_navigationService = navigationService;

            PopularBooks = new XCollection<Book>();
            NoveltyBooks = new XCollection<Book>();

            RegisterPart(MainPart, (session, part) => LoadBooks(session), (session, part) => !_loadedPopular && !_loadedNovelty, true);
            //RegisterPart( LoadMorePart, ( session, part ) => LoadBooks( session ), ( session, part ) => !_isEndOfList, false );

            RegisterPart(LoadMoreNoveltyBooksPart, (session, part) => LoadNoveltyBooks(session), (session, part) => !_isEndOfListNoveltyBooks, false);
            RegisterPart(LoadMorePopularBooksPart, (session, part) => LoadPopularBooks(session), (session, part) => !_isEndOfListPopularBooks, false);

			//Books = new XCollection<Book>();

			BookSelected = new RelayCommand<Book>(book => _navigationService.Navigate("Book", Parameters.From("id", book.Id)), book => book != null);

            LoadMoreNoveltyBooks = new RelayCommand(LoadMoreNoveltyBooksProceed, () => true);
            LoadMorePopularBooks = new RelayCommand(LoadMorePopularBooksProceed, () => true);

			//LoadMoreCalled = new RelayCommand(() => LoadMore(), () => true);
		}
		#endregion

        //#region LoadMore
        //public Task LoadMore()
        //{
        //    Session session = new Session( LoadMorePart );

        //    return Load(session);
        //}
        //#endregion


        #region LoadMoreNoveltyBooks
        private async void LoadMoreNoveltyBooksProceed()
        {
            Session session = new Session(LoadMoreNoveltyBooksPart);
            await Load(session);
        }
        #endregion
        
        #region LoadMorePopularBooks
        private async void LoadMorePopularBooksProceed()
        {
            Session session = new Session(LoadMorePopularBooksPart);
            await Load(session);
        }
        #endregion

        #region LoadPopularBooks
        private async Task LoadPopularBooks(Session session)
        {
            XCollection<Book> books = null;
            try
            {
                books = await _catalogProvider.GetPopularBooksByCollection(PopularBooks.Count, 4, session.Token);
            }
            catch (CatalitNoCredentialException)
            {
                //ToDo: Do something? Message?
            }

            _loadedPopular = true;

            if (books != null)
            {
                if (books.Count <= PopularBooks.Count)
                {
                    _isEndOfListPopularBooks = true;
                }

                PopularBooks.Update(books);
            }
        }
        #endregion

        #region LoadNoveltyBooks
        private async Task LoadNoveltyBooks(Session session)
        {
            XCollection<Book> books = null;
            try
            {
                books = await _catalogProvider.GetNoveltyBooksByCollection(NoveltyBooks.Count, 4, session.Token);
            }
            catch (CatalitNoCredentialException)
            {
                //ToDo: Do something? Message?
            }

            _loadedNovelty = true;

            if (books != null)
            {
                if (books.Count <= NoveltyBooks.Count)
                {
                    _isEndOfListNoveltyBooks = true;
                }

                NoveltyBooks.Update(books);
            }
        }
        #endregion

        #region LoadBooks
        private async Task LoadBooks(Session session)
        {
            await Task.WhenAll(new Task[] { LoadPopularBooks(session), LoadNoveltyBooks(session) });
            //XCollection<Book> books = null;
            //try
            //{
            //    books = await _catalogProvider.GetPopularBooksByCollection(Books.Count, 4, session.Token);
            //}
            //catch (CatalitNoCredentialException)
            //{
            //    //ToDo: Do something? Message?
            //}

            //_loaded = true;

            //if (books != null)
            //{
            //    if (books.Count <= Books.Count)
            //    {
            //        _isEndOfList = true;
            //    }

            //    Books.Update(books);
            //}
        }
        #endregion
	}
}
