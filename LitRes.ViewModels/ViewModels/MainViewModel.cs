﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Digillect.Collections;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Exceptions;
using LitRes.LibraryTools;
using LitRes.Models;
using LitRes.Services;
using LitRes.Services.Connectivity;
using System.ComponentModel;
using System.Diagnostics;
using Digillect;


namespace LitRes.ViewModels
{
    #region CategoriesTagTypeEnum
    public enum CategoriesTagTypeEnum
    {
        FreeBooksTag  = -2,
        NokiaBoardTag = -1
    }
    #endregion

	public class MainViewModel : ViewModel
	{
		#region MyBooksViewStateEnum
		public enum MyBooksViewStateEnum
		{
			Loading,
			WithoutAccount,
			BooksExits,
			WithoutBooks
		}
		#endregion

		public const string MyBooksPart = "MyBooks";
		public const string NewBooksPart = "NewBooks";

		private readonly IGenresProvider _genresProvider;
		private readonly ICatalogProvider _catalogProvider;
		private readonly ICredentialsProvider _credentialsProvider;
		private readonly IProfileProvider _profileProvider;
		private readonly IBookProvider _bookProvider;
		private readonly INavigationService _navigationService;
	    private readonly IExpirationGuardian _expirationGuardian;
	    private readonly INetworkAvailabilityService _networkAvailability;
	    private readonly IDeviceInfoService _deviceInfoService;

		private XCollection<Book> _myBooks;
		private XCollection<Book> _noveltyBooks;
		private XCollection<Book> _popularBooks;
		private XCollection<Book> _interestingBooks;

	    private int _booksPerPage = 30;

		private MyBooksViewStateEnum _myBooksViewState;

		//private bool _scrollToFirstPanoramaItemOnLoad;
	    private bool _isLoaded;

		#region Public Properties
		//public event EventHandler ScrollToFirstPanoramaItem;

		public XCollection<Genre> Genres { get; private set; }
		public XCollection<Banner> Banners { get; private set; }
		public XSubRangeCollection<Book> MyBooks { get; private set; }
		public XSubRangeCollection<Book> NoveltyBooks { get; private set; }
		public XSubRangeCollection<Book> PopularBooks { get; private set; }
		public XSubRangeCollection<Book> InterestingBooks { get; private set; }

		public MyBooksViewStateEnum MyBooksViewState
		{
			get { return _myBooksViewState; }
			private set { SetProperty( ref _myBooksViewState, value, "MyBooksViewState" ); }
		}

		public RelayCommand ShowMyBooks { get; private set; }
		public RelayCommand<Book> BookSelected { get; private set; }
		public RelayCommand ShowInterestingBooks { get; private set; }
		public RelayCommand ShowPopularBooks { get; private set; }
		public RelayCommand ShowNewBooks { get; private set; }
		public RelayCommand<int> GenreSelected { get; private set; }
		public RelayCommand ShowSearchHistory { get; private set; }
		public RelayCommand ShowAuthorization { get; private set; }
        public RelayCommand ShowRegistration { get; private set; }
		public RelayCommand ShowUserInfo { get; private set; }        
        public RelayCommand ShowAccountInfo { get; private set; }
		public RelayCommand ShowSettings { get; private set; }
		public RelayCommand ShowBookmarks { get; private set; }
		public RelayCommand ShowAbout { get; private set; }
		public RelayCommand ShowNotifications { get; private set; }

        public RelayCommand ShowAppSettings { get; private set; }

		#endregion

		#region Constructors/Disposer
        public MainViewModel(
         //   IExpirationGuardian expirationGuardian, 
            IProfileProvider profileProvider, 
            IGenresProvider genresProvider, 
            ICatalogProvider catalogProvider, 
            ICredentialsProvider credentialsProvider,
            IBookProvider bookProvider,
            INavigationService navigationService, 
            INetworkAvailabilityService networkAvailability,
            IDeviceInfoService deviceInfoService)
        {            
            _genresProvider = genresProvider;
			_catalogProvider = catalogProvider;
			_credentialsProvider = credentialsProvider;
			_profileProvider = profileProvider;
			_bookProvider = bookProvider;
			_navigationService = navigationService;
		//    _expirationGuardian = expirationGuardian;
		    _networkAvailability = networkAvailability;
            _deviceInfoService = deviceInfoService;

            var deviceFamily = _deviceInfoService.DeviceFamily;
            if (!string.IsNullOrEmpty(deviceFamily) && deviceFamily.Equals("Windows.Desktop"))
            {
                _booksPerPage = 30;
            }

            ////MyBooks reload allways, may change account information
            RegisterAction(MyBooksPart).AddPart( session =>  LoadMyBooks(session), session => true);
            //RegisterPart(MyBooksPart, (session, part) => LoadMyBooks(session), (session, part) => true, false);
			////RegisterPart(NewBooksPart, (session, part) => LoadNewBooks(session), (session, part) => true, false);

			MyBooksViewState = MyBooksViewStateEnum.Loading;

			Genres = new XCollection<Genre>();
			Banners = new XCollection<Banner>();

			_myBooks = new XCollection<Book>();
			_noveltyBooks = new XCollection<Book>();
			_popularBooks = new XCollection<Book>();
			_interestingBooks = new XCollection<Book>();

			MyBooks = new XSubRangeCollection<Book>(_myBooks, 0, 10);
            
            NoveltyBooks = new XSubRangeCollection<Book>(_noveltyBooks, 0, _booksPerPage);
			PopularBooks = new XSubRangeCollection<Book>(_popularBooks, 0, _booksPerPage);
			InterestingBooks = new XSubRangeCollection<Book>(_interestingBooks, 0, _booksPerPage);
			ShowMyBooks = new RelayCommand( ToMyBooks );
            BookSelected = new RelayCommand<Book>(book =>  _navigationService.Navigate("Book", XParameters.Create("BookEntity", book)), book => book != null);
			ShowInterestingBooks = new RelayCommand(() => _navigationService.Navigate("BooksByCategory", XParameters.Create("category", (int) BooksByCategoryViewModel.BooksViewModelTypeEnum.Interesting)));
			ShowPopularBooks = new RelayCommand(() => _navigationService.Navigate("BooksByCategory", XParameters.Create("category", (int) BooksByCategoryViewModel.BooksViewModelTypeEnum.Popular)));
			ShowNewBooks = new RelayCommand(() => _navigationService.Navigate("BooksByCategory", XParameters.Create("category", (int) BooksByCategoryViewModel.BooksViewModelTypeEnum.Novelty)));
			GenreSelected = new RelayCommand<int>(ChooseGenre);
			ShowSearchHistory = new RelayCommand(() => _navigationService.Navigate("Search"));

			ShowAuthorization = new RelayCommand(() => _navigationService.Navigate("Authorization"));
            ShowRegistration = new RelayCommand(() => _navigationService.Navigate("Registration"));
			ShowUserInfo = new RelayCommand( ToUserInfo );
           
            ShowAccountInfo = new RelayCommand(ToAccountInfo);
			ShowSettings = new RelayCommand(() => _navigationService.Navigate("Settings"));
			ShowBookmarks = new RelayCommand( () => _navigationService.Navigate( "Bookmarks" ) );
			ShowAbout = new RelayCommand( () => _navigationService.Navigate( "About" ) );
			ShowNotifications = new RelayCommand(() => _navigationService.Navigate("NotificationsEdit"));

            ShowAppSettings = new RelayCommand(ToAppSettings);

            //_expirationGuardian.StartGuardian();		    
        }

		#endregion

		#region LoadMyBooks
		public Task LoadMyBooks()
		{
		    if (_isLoaded) return null;
            return Load( new Session( MyBooksPart ) );
		}
		#endregion

		#region LoadGenres
		private async Task LoadGenres(Session session)
		{
			var genres = await _genresProvider.GetGenres(session.Token);

			//Add Nokia collection
		    var deviceManufacturer = _deviceInfoService?.DeviceManufacturer.ToLower();
            if (genres?.Children != null && genres.Children.Count > 0 && !string.IsNullOrEmpty(deviceManufacturer) && deviceManufacturer.Contains( "nokia" ))
			{
                if (genres.Children[0].Id != (int)CategoriesTagTypeEnum.NokiaBoardTag)
                {
                    XCollection<Book> books = await _catalogProvider.GetBooksByCollection(0, (int)BooksByCategoryViewModel.BooksViewModelTypeEnum.NokiaCollection, session.Token);
                   if (books.Count > 0)
                   {
                       Genre genre = new Genre { Id = (int)CategoriesTagTypeEnum.NokiaBoardTag, Title = "полка Lumia", Token = string.Empty };
                       genres.Children.Insert(0, genre);
                   }
                }
                if (genres.Children[genres.Children.Count - 1].Id != (int)CategoriesTagTypeEnum.FreeBooksTag)
                {
                    Genre genre = new Genre { Id = (int)CategoriesTagTypeEnum.FreeBooksTag, Title = "Бесплатные книги", Token = string.Empty };
                    genres.Children.Add(genre);
                }
			}

		    if (genres != null)
		    {
		        Genres.BeginUpdate();
		        Genres.Clear();
		        Genres.AddRange(genres.Children);
		        Genres.EndUpdate();
		    }
		}
		#endregion

		#region LoadNewBooks
		private async Task LoadNewBooks(Session session)
		{
		    try
		    {
		        var creds =  _credentialsProvider.ProvideCredentials(session.Token);

		        if (creds != null)
		        {
		            var ids = await _catalogProvider.GetMyBooksIds(session.Token);

		            //First time loading my books 
		            if (ids.Count == 0)
		            {
		                try
		                {
                            await _catalogProvider.GetAllMyBooks(session.Token);
                        }
		                catch (Exception ex)
		                {
		                    Debug.WriteLine(ex.Message);
		                }
		            }
		        }

                ClearBookCollectionsIfNeeds();

                await Task.WhenAll(
                                      LoadPopularBooks(session),
                                      LoadNoveltyBooks(session),
                                      LoadInterestingBooks(session),
                                      LoadGenres(session));
                //await RefreshLoadedBooks(session);
            }
		    catch (Exception ex)
		    {
		        Debug.WriteLine(ex.Message);
		    }
		}
		#endregion
		#region LoadMyBooks
		private async Task LoadMyBooks(Session session)
        {
            XCollection<Book> myBooks = null;

            var creds =  _credentialsProvider.ProvideCredentials(session.Token);

            //autoregister

            if (creds == null && _networkAvailability.NetworkAvailable)
            {
                try
                {
                    creds = await _profileProvider.RegisterDefault(session.Token);
                    _credentialsProvider.RegisterCredentials(creds, session.Token);
                    OnPropertyChanged(new PropertyChangedEventArgs("ShowAccountInfo"));
                }
                catch (CatalitRegistrationException e)
                {
                    //Refresh other books
                    RefreshOtherBooks(session);
                }
                catch (WebException)
                {
                    //Refresh other books
                    RefreshOtherBooks(session);
                }
                catch (CatalitParseException ex)
                {
                    //Refresh other books
                    RefreshOtherBooks(session);
                }
            }

            if (creds != null)
            {
                ////creds.Sid += "assadsdasdasdas";
                ////_credentialsProvider.RegisterCredentials(creds, session.Token);
                //if (_scrollToFirstPanoramaItemOnLoad && creds.IsRealAccount && ScrollToFirstPanoramaItem != null)
                //{
                //    ScrollToFirstPanoramaItem(this, EventArgs.Empty);
                //}

                var ids = await _catalogProvider.GetMyBooksIds(session.Token);
                var books = await _catalogProvider.GetMyBooksFromCache(session.Token);

                //First time loading my books 
                if (ids.Count == 0 && !isFragmentExist(books))
                {
                    try
                    {
                        if (_networkAvailability.NetworkAvailable) await _catalogProvider.GetAllMyBooks(session.Token);

                        //Refresh other books
                        RefreshOtherBooks(session);

                        books = await _catalogProvider.GetMyBooksFromCache(session.Token);
                    }
                    catch (CatalitRegistrationException) { }
                    catch (WebException) { }
                    catch (CatalitParseException) { }
                }

                //var books = await _catalogProvider.GetMyBooksFromCache(session.Token);
                if (books != null && books.Count > 0)
                {
                    myBooks = books;
                    try
                    {
                        if (_networkAvailability.NetworkAvailable)
                        {
                            myBooks = await _catalogProvider.GetAndRefreshMyBooks(session.Token);
                        }
                    }
                    catch (Exception ex) { }
                }
                else
                {
                    try
                    {
                        if (_networkAvailability.NetworkAvailable)
                        {
                            myBooks = await _catalogProvider.GetMyBooks(session.Token);
                        }
                    }
                    catch (CatalitRegistrationException) { }
                    catch (WebException) { }
                    catch (CatalitParseException) { }
                }
            }
            else
            {
                var books = await _catalogProvider.GetMyBooksFromCache(session.Token);

                if (books != null && books.Count > 0)
                {
                    myBooks = books;
                }
            }

            if (_networkAvailability.NetworkAvailable)
            {
                try
                {
                    var user = await _profileProvider.GetUserInfo(CancellationToken.None);
                    if (user.AccountType == (int) AccountTypeEnum.AccountTypeLibrary)
                        OnPropertyChanged(new PropertyChangedEventArgs("HideAccountInfo"));
                    else OnPropertyChanged(new PropertyChangedEventArgs("ShowAccountInfo"));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            //Load exist books

            var exist = await _bookProvider.GetExistBooks(session.Token);
            if (exist != null && exist.Count > 0)
            {
                myBooks = myBooks ?? new XCollection<Book>();
                int ext = myBooks.Count;

                foreach (var book in exist)
                {
                    if (ext > 9) { break; }
                    if (myBooks.All(x => x.Id != book.Id))
                    {
                        myBooks.Add(book);
                        ext++;
                    }
                }
            }

            //Load banners
            //if (Banners.Count == 0 && _networkAvailability.NetworkAvailable)
            //{
            //    try
            //    {
            //        var bannersResponse = await _catalogProvider.GetBanners(session.Token);

            //        if (bannersResponse != null)
            //        {
            //            Banners.Update(bannersResponse.Banners);
            //            OnPropertyChanged(new PropertyChangedEventArgs("Banners"));
            //        }
            //    }
            //    catch (Exception) { }
            //}

            //if (myBooks == null || myBooks.Count == 0)
            //{
            //    if (creds != null)
            //    {
            //        MyBooksViewState = MyBooksViewStateEnum.WithoutBooks;
            //        //if (!creds.IsRealAccount) OnPropertyChanged(new PropertyChangedEventArgs("ShowRegistrationBlock"));
            //    }
            //    else
            //    {
            //        MyBooksViewState = MyBooksViewStateEnum.WithoutAccount;
            //    }

            //    _myBooks.Clear();
            //}
            //else
            //{
            //    MyBooksViewState = MyBooksViewStateEnum.BooksExits;
            //    _myBooks.Clear();
            //    _myBooks.Update(myBooks);
            //}

            //if (!_catalogProvider.IsAllMyBooksFromCacheLoaded()) await _catalogProvider.GetAndSyncAllMyBooksFromCache(session.Token);

            if (_networkAvailability.NetworkAvailable)
            {
                try
                {
                    await LoadNewBooks(session);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
             _expirationGuardian.AddBook(null);
		    _isLoaded = true;
        }
		#endregion

	    private void ClearBookCollectionsIfNeeds()
	    {
            if (_catalogProvider.IsBooksCollectionsCleared())
            {
                if (_noveltyBooks != null && _noveltyBooks.Count > 0)
                {
                    _noveltyBooks.Clear();
                }
                if (_popularBooks != null && _popularBooks.Count > 0)
                {
                    _popularBooks.Clear();                 
                }
                if (_interestingBooks != null && _interestingBooks.Count > 0)
                {
                    _interestingBooks.Clear();                    
                }
            }
	    }

        #region RefreshOtherBooks
        public async Task RefreshLoadedBooks(Session session)
        {
            if (_catalogProvider.IsBooksCollectionsCleared())
            {
                if (_noveltyBooks != null && _noveltyBooks.Count > 0)
                {
                    _noveltyBooks.Clear();
                    await LoadNoveltyBooks(session);
                }
                if (_popularBooks != null && _popularBooks.Count > 0)
                {
                    _popularBooks.Clear();
                    await LoadPopularBooks(session);
                }
                if (_interestingBooks != null && _interestingBooks.Count > 0)
                {
                    _interestingBooks.Clear();
                    await LoadInterestingBooks(session);
                }
            }
        }
        public async void RefreshOtherBooks( Session session )
		{
			if( _noveltyBooks != null && _noveltyBooks.Count > 0 )
			{
				_noveltyBooks.Clear();
			    await LoadNoveltyBooks(session);
			}
			if( _popularBooks != null && _popularBooks.Count > 0 )
			{
				_popularBooks.Clear();
			    await LoadPopularBooks(session);
			}
			if( _interestingBooks != null && _interestingBooks.Count > 0 )
			{
				_interestingBooks.Clear();
			    await LoadInterestingBooks(session);
			}
		}
		#endregion
		#region LoadNoveltyBooks
		private async Task LoadNoveltyBooks( Session session )
		{
			var books = await _catalogProvider.GetNoveltyBooks( 0, session.Token, _booksPerPage);

            if (!_noveltyBooks.Equals(books))
		    {
		        _noveltyBooks.BeginUpdate();
		        _noveltyBooks.Update(books);
		        _noveltyBooks.EndUpdate();
		    }
		}
		#endregion
		#region LoadPopularBooks
		private async Task LoadPopularBooks(Session session)
		{
			var books = await _catalogProvider.GetPopularBooks( 0, session.Token, _booksPerPage);

            if (!_popularBooks.Equals(books))
		    {
		        _popularBooks.BeginUpdate();
		        _popularBooks.Update(books);
		        _popularBooks.EndUpdate();
		    }
		}
		#endregion
		#region LoadInterestingBooks
		private async Task LoadInterestingBooks( Session session )
		{
			var books = await _catalogProvider.GetInterestingBooks( 0, session.Token, _booksPerPage);

		    if (!_interestingBooks.Equals(books))
		    {
		        _interestingBooks.BeginUpdate();
		        _interestingBooks.Update(books);
		        _interestingBooks.EndUpdate();
		    }
		}
		#endregion
		
		#region ChooseGenre
		private void ChooseGenre(int index)
		{
			if (index >= 0)
			{
                if(Genres[index].Id >= 0) 
                    _navigationService.Navigate("GenreBooks", XParameters.Empty.ToBuilder().AddValue("id", index).AddValue("Index", true ).ToImmutable());
                else if(Genres[index].Id == (int)CategoriesTagTypeEnum.NokiaBoardTag) 
                    _navigationService.Navigate( "BooksByCategory", XParameters.Create( "category", ( int ) BooksByCategoryViewModel.BooksViewModelTypeEnum.NokiaCollection ) );
                else if(Genres[index].Id == (int)CategoriesTagTypeEnum.FreeBooksTag)
                    _navigationService.Navigate("FreeBooksByCategory", XParameters.Create("category", (int)BooksByCategoryViewModel.BooksViewModelTypeEnum.FreeBooks));
			}
		}
		#endregion	
		#region ToMyBooks
		private void ToMyBooks()
		{
			_navigationService.Navigate( "MyBooks" );
		}
		#endregion
        
        #region ToAppSettings
        private void ToAppSettings()
        {
            _navigationService.Navigate("AppSettings");
        }
        #endregion

		#region ToUserInfo
		private async void ToUserInfo()
		{
			var creds =  _credentialsProvider.ProvideCredentials( CancellationToken.None );

			if( creds != null && creds.IsRealAccount )
			{
				_navigationService.Navigate( "UserInfo" );
			}
			else
			{
				//_scrollToFirstPanoramaItemOnLoad = true;
				_navigationService.Navigate( "Authorization" );
			}
		}
		#endregion
        #region ToAccountInfo
        private async void ToAccountInfo()
        {
            UserInformation user = null;
            user = await _profileProvider.GetUserInfo(CancellationToken.None);

            if (user != null)
			{
				_navigationService.Navigate( "AccountInfo" );
			}
			else
			{
				//_scrollToFirstPanoramaItemOnLoad = true;
				_navigationService.Navigate( "Authorization" );
			}
		}
		#endregion

	    public async Task<bool> IsUserIdEquals(string userId)
	    {
            var user = await _profileProvider.GetUserInfo(CancellationToken.None);
            if(!string.IsNullOrEmpty(user.UserId) && user.UserId.Equals(userId)) return true;
	        return false;
	    }

        private bool isFragmentExist(IEnumerable<Book> books)
        {
            if (books != null)
            {
                foreach (var book in books)
                {
                    if (!book.IsMyBook || book.isFragment) return true;
                }
            }
            return false;
        }
	}
}
