using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Popups;
using Digillect;
using Digillect.Collections;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Exceptions;
using LitRes.Models;
using LitRes.Services;

namespace LitRes.ViewModels
{
	public class MyBooksViewModel : ViewModel
	{
		public const string MainPart = "Main";
		public const string ReloadPart = "Reload";
		public const string RefreshPart = "Refresh";
        public const string UpdatePart = "Update";

		private readonly ICatalogProvider _catalogProvider;
		private readonly IBookProvider _bookProvider;
		private readonly INavigationService _navigationService;

		private bool _loaded;

		private IList<LongListGroup<Book>> _booksByAuthorsGrouped;

		#region Public Properties
		public XCollection<Book> BooksByTime { get; private set; }
		public XCollection<Book> BooksByAuthors { get; private set; }
		public IList<LongListGroup<Book>> BooksByAuthorsGrouped
		{
			get { return _booksByAuthorsGrouped; }
			private set { SetProperty( ref _booksByAuthorsGrouped, value, "BooksByAuthorsGrouped" ); }
		}
		public XCollection<Book> BooksByNames { get; private set; }

		public RelayCommand<Book> BookSelected { get; private set; }
        public RelayCommand<Book> Read { get; private set; }
        #endregion

        #region Constructors/Disposer
        public MyBooksViewModel(ICatalogProvider catalogProvider, IBookProvider bookProvider, INavigationService navigationService)
		{
			_catalogProvider = catalogProvider;
			_bookProvider = bookProvider;
			_navigationService = navigationService;
		    RegisterAction(MainPart).AddPart((session) => LoadMyBooks(session), (session) => !_loaded);
		    RegisterAction(ReloadPart).AddPart((session) => ReloadMyBooks(session), (session) => true);
		    RegisterAction(RefreshPart).AddPart((session) => RefreshMyBooks(session), (session) => true);
		    RegisterAction(UpdatePart).AddPart((session) => UpdateMyBooks(session), (session) => true);
           

			BooksByTime = new XCollection<Book>();
			BooksByAuthors = new XCollection<Book>();
			BooksByAuthorsGrouped = new List<LongListGroup<Book>>();
			BooksByNames = new XCollection<Book>();

			BookSelected = new RelayCommand<Book>( NavigateToBook, book => book != null );
            Read = new RelayCommand<Book>(book =>
			{
			    if (!book.IsExpiredBook) _navigationService.Navigate("Reader", XParameters.Create("BookEntity", book), false);
			    else new MessageDialog("Истёк срок выдачи.").ShowAsync();
			} );
		}

		#endregion

		#region NavigateToBook
		private void NavigateToBook( Book book )
		{
			if( book != null && !book.IsEmptyElement )
			{
				_navigationService.Navigate( "Book", XParameters.Create("BookEntity", book ) );
			}
		}
		#endregion

		#region Reload
		public Task Reload()
		{
			return Load( new Session( ReloadPart ) );
		}
		#endregion

		#region Refresh
		public Task Refresh()
		{
			return Load( new Session( RefreshPart ) );
		}
		#endregion

        #region Update
        public Task Update()
        {
            return Load(new Session(UpdatePart));
        }
        #endregion

        #region LoadMyBooks

        public async void LoadMyBooks()
        {
            await Load(new Session(MainPart));
        }

        private async Task LoadMyBooks(Session session)
		{
            try
            {
                XCollection<Book> myBooks = await _catalogProvider.GetAllMyBooksFromCache(session.Token);
                XCollection<Book> myBooksByTime = await _catalogProvider.GetMyBooksFromCache(session.Token);

                if (myBooks == null || myBooks.Count == 0)
                {
                    try
                    {
                        myBooks = await _catalogProvider.GetAllMyBooks(session.Token);
                    }
                    catch (CatalitNoCredentialException)
                    {
                        //ToDo: Do something? Message?
                    }
                }

                _loaded = true;

                XCollection<Book> clone = null;
                if (myBooks != null)
                {
                    clone = myBooks.Clone(false);
                }

                //Load exist books
                var exist = await _bookProvider.GetExistBooks(session.Token);
                if (exist != null && exist.Count > 0)
                {
                    clone = clone ?? new XCollection<Book>();

                    foreach (var book in exist)
                    {
                        if (clone.All(x => x.Id != book.Id))
                        {
                            clone.Add(book);
                        }
                    }
                }
                //((CatalogProvider)_catalogProvider).CheckMyBooks(clone);
                if (myBooksByTime != null) CheckMyBooks(myBooksByTime, clone);
                UpdateBooks(clone, myBooksByTime);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
		}
		#endregion

		#region RefreshMyBooks
		private async Task RefreshMyBooks( Session session )
		{
			XCollection<Book> myBooks = await _catalogProvider.GetAllMyBooksFromCache( session.Token );
		    XCollection<Book> myBooksByTime = await _catalogProvider.GetMyBooksFromCache( session.Token );

		    if( myBooks == null )
		    {
		        try
		        {
		            myBooks = await _catalogProvider.GetAllMyBooks( session.Token );
		        }
		        catch( CatalitNoCredentialException )
		        {
		            //ToDo: Do something? Message?
		        }
		    }
            else if (myBooks.Count == 0 && myBooksByTime.Count>0)
            {
                myBooks.Update(myBooksByTime);
            }

		    _loaded = true;

			XCollection<Book> clone = null;
			if( myBooks != null )
			{
				clone = myBooks.Clone( false );
			}

			//Load exist books
			var exist = await _bookProvider.GetExistBooks( session.Token );
			if( exist != null && exist.Count > 0 )
			{
				clone = clone ?? new XCollection<Book>();

				foreach( var book in exist )
				{
					if( clone.All( x => x.Id != book.Id ) )
					{
						clone.Add( book );
					}
				}
			}

            UpdateBooks(clone, myBooksByTime);
		}
		#endregion

		#region ReloadMyBooks
		private async Task ReloadMyBooks(Session session)
		{
		    XCollection<Book> myBooks = null;
			try
			{
				myBooks = await _catalogProvider.GetAllMyBooks( session.Token );
			}
			catch (CatalitNoCredentialException)
			{
				//ToDo: Do something? Message?
			}

            XCollection<Book> myBooksByTime = await _catalogProvider.GetMyBooksFromCache(session.Token);

			_loaded = true;

		    UpdateBooks( myBooks, myBooksByTime );
		}
		#endregion

        #region UpdateMyBooks
        private async Task UpdateMyBooks(Session session)
        {
            XCollection<Book> myBooks = await _catalogProvider.GetAllMyBooksFromCache(session.Token);
            XCollection<Book> myBooksByTime = await _catalogProvider.GetMyBooksFromCache(session.Token);

            if (myBooks == null)
            {
                try
                {
                    myBooks = await _catalogProvider.GetAllMyBooks(session.Token);
                }
                catch (CatalitNoCredentialException)
                {
                    //ToDo: Do something? Message?
                }
            }
            else if (myBooks.Count == 0 && myBooksByTime.Count>0)
            {
                myBooks.Update(myBooksByTime);
            }

            _loaded = true;

             UpdateBooks(myBooks, myBooksByTime);
        }
        #endregion

		#region ReloadMyBooks
        private async void UpdateBooks(XCollection<Book> books, XCollection<Book> booksByTime, bool onlyRefreshLastRead = false)
		{
			if (books != null)
			{
			    XCollection<Book> timed;
                if( booksByTime != null )
                {
                    timed = booksByTime.Clone( false );

                    foreach( var book in books )
                    {
                        if( !timed.ContainsKey( book.GetKey() ) )
                        {
                            timed.Add( book );
                        }
                    }
                }
                else
                {
                    timed = books;
                }
			    
				BooksByTime.Clear();
                BooksByTime.BeginUpdate();
				BooksByTime.Update(timed);
                BooksByTime.EndUpdate();
                OnPropertyChanged(new PropertyChangedEventArgs("BooksByTime"));

				if( !onlyRefreshLastRead )
				{
					var booksWithAuthors = (from book in books
											where book.Description.Hidden.TitleInfo.Author != null && !string.IsNullOrEmpty( book.Description.Hidden.TitleInfo.Author[0].LastName )
											orderby book.Description.Hidden.TitleInfo.Author[0].LastName
											select book).ToList();
					var booksWithoutAuthors = (from book in books
												where book.Description.Hidden.TitleInfo.Author == null
												select book).ToList();

					var merged = new XCollection<Book>( booksWithAuthors );
					merged.AddRange( booksWithoutAuthors );

					BooksByAuthors.Update( merged );
                    OnPropertyChanged(new PropertyChangedEventArgs("OnPropertyChanged"));
					var booksByNames = new XCollection<Book>( books.OrderBy( x => x.Description.Hidden.TitleInfo.BookTitle ) );

					BooksByNames.Update( booksByNames );

				    try
				    {
                       await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, (() =>
                        {
                            BooksByAuthorsGrouped = (from book in booksWithAuthors
                                                     group book by book.Description.Hidden.TitleInfo.Author[0].LastName.First().ToString()
                                                        into c
                                                     orderby c.Key
                                                     select new LongListGroup<Book>(c.Key, c)).ToList();
                        }));
                        
				    }
				    catch (Exception ex)
				    {
				        ex = ex;
				    }
				    if( booksWithoutAuthors.Count > 0 )
					{
						var group = new LongListGroup<Book>( " ", booksWithoutAuthors );
						BooksByAuthorsGrouped.Add( group );
					}
				}

				//BooksByTime.Add( new Book { IsEmptyElement = true } );			
			}
		}
		#endregion

        private void CheckMyBooks(XCollection<Book> from, XCollection<Book> to)
        {
            foreach (var book1 in from)
            {
                foreach (var book2 in to)
                {
                    if (book1.Id == book2.Id)
                    {
                        book2.IsMyBook = book1.IsMyBook;
                        book2.ReadedPercent = book1.ReadedPercent;
                        break;
                    }
                }
            }
        }
	}

	public class LongListGroup<T> : XCollection<Digillect.XObject>
	{
		public LongListGroup() 
		{ 
		}

		public LongListGroup(string title, IEnumerable<Digillect.XObject>
		items)
			: base(items)
		{
			this.Title = title;
		}
		public string Title
		{
			get;
			set;
		}
	}
}
