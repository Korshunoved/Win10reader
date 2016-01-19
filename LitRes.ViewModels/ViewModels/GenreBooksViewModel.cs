using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Digillect;
using Digillect.Collections;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Models;
using LitRes.Services;

namespace LitRes.ViewModels
{
	public class GenreBooksViewModel : EntityViewModel<Genre>
	{
		private const string LoadMoreNoveltyBooksPart = "LoadMoreNoveltyBooks";
		private const string LoadMorePopularBooksPart = "LoadMorePopularBooks";

		private readonly ICatalogProvider _catalogProvider;
		private readonly IGenresProvider _genresProvider;
		private readonly INavigationService _navigationService;

		private bool _loaded;

		private bool _isEndOfListNoveltyBooks;
		private bool _isEndOfListPopularBooks;

	    private bool _loadingPopularBooks;

        #region Public Properties

        public XCollection<Book> NoveltyBooks { get; private set; }
		public XCollection<Book> PopularBooks { get; private set; }
		public XCollection<Genre> Genres { get; private set; }

		public RelayCommand LoadMoreNoveltyBooks { get; private set; }
		public RelayCommand LoadMorePopularBooks { get; private set; }
		public RelayCommand<Book> BookSelected { get; private set; }
		public RelayCommand<Genre> GenreSelected { get; private set; }
		#endregion

		#region Constructors/Disposer
		public GenreBooksViewModel(ICatalogProvider catalogProvider, INavigationService navigationService, IGenresProvider genresProvider)
		{
			_catalogProvider = catalogProvider;
			_navigationService = navigationService;
			_genresProvider = genresProvider;

			PopularBooks = new XCollection<Book>();
			NoveltyBooks = new XCollection<Book>();

		    RegisterAction(LoadMoreNoveltyBooksPart).AddPart((session) => LoadNoveltyBooks(session, Entity), (session) => !_isEndOfListNoveltyBooks);
		    RegisterAction(LoadMorePopularBooksPart).AddPart((session) => LoadPopularBooks(session, Entity), (session) => !_isEndOfListPopularBooks);
           
			BookSelected = new RelayCommand<Book>( book => _navigationService.Navigate( "Book", XParameters.Create("BookEntity", book ) ), book => book != null );
			GenreSelected = new RelayCommand<Genre>( genre => _navigationService.Navigate( "GenreBooks", XParameters.Create( "id", genre.Id ) ), genre => genre != null );
			LoadMoreNoveltyBooks = new RelayCommand( LoadMoreNoveltyBooksProceed, () => true );
			LoadMorePopularBooks = new RelayCommand( LoadMorePopularBooksProceed, () => true );
		}
		#endregion

		#region LoadMoreNoveltyBooks
		private async void LoadMoreNoveltyBooksProceed()
		{
			Session session = new Session(LoadMoreNoveltyBooksPart);
			session.AddParameter("id", Entity.Id);
			session.AddParameter("genre", Entity);
			await Load(session);
		}
		#endregion

		#region LoadMorePopularBooks
		private async void LoadMorePopularBooksProceed()
		{
            Session session = new Session(LoadMorePopularBooksPart);
            session.AddParameter("id", Entity.Id);
            session.AddParameter("genre", Entity);
            await Load(session);
        }
		#endregion
 
		#region LoadEntity
		protected override async Task LoadEntity(Session session)
		{
            await LoadGenre(session);
		}
		#endregion
		
		#region LoadGenreInfo
		private async Task LoadGenreInfo( Session session, Genre genre )
		{
			Task loadNoveltyBooks = LoadNoveltyBooks( session, genre );
			Task loadPopularBooks = LoadPopularBooks( session, genre );
			await Task.WhenAll( new Task[] { loadNoveltyBooks, loadPopularBooks } );
		    //await LoadPopularBooks(session, genre);
        }
        #endregion

        #region ShouldLoadSession
        protected override bool ShouldLoadEntity(Session session)
        {
			return true;
		}
		#endregion

		#region LoadPopularBooks
		private async Task LoadPopularBooks(Session session, Genre genre)
		{
		    if (!_loadingPopularBooks)
		    {
		        _loadingPopularBooks = true;
                XCollection<Book> popularBooks = null;

		        if (genre.Id > 0)
		        {
		            popularBooks = await _catalogProvider.GetPopularBooksByGenre(PopularBooks.Count, genre.Id, session.Token);
		        }
		        else
		        {
		            if (genre.Children != null)
		            {
		                List<int> ids = genre.Children.Select(x => x.Id).ToList();
		                popularBooks = await _catalogProvider.GetPopularBooksByGenres(PopularBooks.Count, ids, session.Token);
		            }
		        }

		        if (popularBooks != null)
		        {
		            if (popularBooks.Count <= PopularBooks.Count)
		            {
		                _isEndOfListPopularBooks = true;
		            }

		            PopularBooks.Update(popularBooks);
		        }
		        _loadingPopularBooks = false;
		    }
		}
		#endregion

		#region LoadNoveltyBooks
		private async Task LoadNoveltyBooks(Session session, Genre genre)
		{
			XCollection<Book> noveltyBooks = null;

			if( genre.Id > 0 )
			{
				noveltyBooks = await _catalogProvider.GetNoveltyBooksByGenre( NoveltyBooks.Count, genre.Id, session.Token );
			}
			else
			{
				if( genre.Children != null )
				{
					List<int> ids = genre.Children.Select( x => x.Id ).ToList();
					noveltyBooks = await _catalogProvider.GetNoveltyBooksByGenres( NoveltyBooks.Count, ids, session.Token );	
				}
			}
			
			if (noveltyBooks != null)
			{
				if (noveltyBooks.Count <= NoveltyBooks.Count)
				{
					_isEndOfListNoveltyBooks = true;
				}

				NoveltyBooks.Update(noveltyBooks);
			}
		}
		#endregion

		#region LoadGenre
		private async Task LoadGenre(Session session)
		{
			Genre genre;
		    var id = session.Parameters.GetValue<int>("id");
            if ( session.Parameters.GetValue<bool>( "index" ) )
			{
				genre = await _genresProvider.GetGenreByIndex(Convert.ToString(id), session.Token );
			}
			else
			{
				genre = await _genresProvider.GetGenreById(id, session.Token );
			}

			Entity = genre;

			_loaded = true;

			if (genre != null)
			{
				await LoadGenreInfo(session, genre);
			}
		}
		#endregion

        public void RefreshBook(Book book)
        {
            if (NoveltyBooks.Count > 0)
            {
                for (int i = 0; i < NoveltyBooks.Count; ++i)
                {
                    if (NoveltyBooks[i].Id == book.Id)
                    {
                        NoveltyBooks.BeginUpdate();
                        NoveltyBooks[i] = book;
                        NoveltyBooks.EndUpdate();
                        break;
                    }
                }
            }

            if (PopularBooks.Count > 0)
            {
                for (int i = 0; i < PopularBooks.Count; ++i)
                {
                    if (PopularBooks[i].Id == book.Id)
                    {
                        PopularBooks.BeginUpdate();
                        PopularBooks[i] = book;
                        PopularBooks.EndUpdate();
                        break;
                    }
                }
            }
        }
	}
}
