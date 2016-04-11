using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Digillect;
using Digillect.Collections;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Exceptions;
using LitRes.Models;
using LitRes.Services;
using LitRes.Services.Connectivity;

namespace LitRes.ViewModels
{
	public class MyBooksViewModel : ViewModel
	{
		public const string MainPart = "Main";
	    public const string BasketPart = "Basket";
		public const string ReloadPart = "Reload";
		public const string RefreshPart = "Refresh";
        public const string UpdatePart = "Update";
        private const string BuyBookLitresPart = "BuyBookLitresPart";
        private const string CreditCardInfoPart = "CreditCardInfoPart";
        private const string BuyBookPart = "BuyBook";

        private readonly ICatalogProvider _catalogProvider;
		private readonly IBookProvider _bookProvider;
		private readonly INavigationService _navigationService;
        private readonly ILitresPurchaseService _litresPurchaseService;
        private readonly IProfileProvider _profileProvider;
        private readonly ICredentialsProvider _credentialsProvider;

        private bool _loaded;

		private IList<LongListGroup<Book>> _booksByAuthorsGrouped;

        private UserInformation _userInformation;

        #region Public Properties
        public XCollection<Book> BooksByTime { get; private set; }
		public XCollection<Book> BooksByAuthors { get; private set; }
        public XCollection<Book> Basket { get; private set; } 
        public IList<LongListGroup<Book>> BooksByAuthorsGrouped
		{
			get { return _booksByAuthorsGrouped; }
			private set { SetProperty( ref _booksByAuthorsGrouped, value, "BooksByAuthorsGrouped" ); }
		}
		public XCollection<Book> BooksByNames { get; private set; }

		public RelayCommand<Book> BookSelected { get; private set; }
        public RelayCommand<Book> Read { get; private set; }
        public RelayCommand RunCreditCardPaymentProcess { get; private set; }
        public RelayCommand<Book> ShowCreditCardView { get; private set; }
        public RelayCommand<Book> BuyBook { get; private set; }
        public RelayCommand BuyBookFromMicrosoft { get; private set; }
        public double AccoundDifferencePrice { get; private set; }
        public Book Book { get; private set; }
        #endregion

        #region Constructors/Disposer
        public MyBooksViewModel(ICatalogProvider catalogProvider, IBookProvider bookProvider, INavigationService navigationService, ILitresPurchaseService litresPurchaseService, IProfileProvider profileProvider, ICredentialsProvider credentialsProvider)
		{
			_catalogProvider = catalogProvider;
			_bookProvider = bookProvider;
			_navigationService = navigationService;
            _litresPurchaseService = litresPurchaseService;
            _profileProvider = profileProvider;
            _credentialsProvider = credentialsProvider;
            RegisterAction(MainPart).AddPart((session) => LoadMyBooks(session), (session) => !_loaded);
            RegisterAction(BasketPart).AddPart((session) => LoadMyBasket(session), (session) => !_loaded);
            RegisterAction(ReloadPart).AddPart((session) => ReloadMyBooks(session), (session) => true);
		    RegisterAction(RefreshPart).AddPart((session) => RefreshMyBooks(session), (session) => true);
		    RegisterAction(UpdatePart).AddPart((session) => UpdateMyBooks(session), (session) => true);
            RegisterAction(BuyBookLitresPart).AddPart((session) => BuyBookFromLitres(session, Book), (session) => true);
            RegisterAction(CreditCardInfoPart).AddPart(session => CreditCardInfoAsync(session), (session) => true);
            RegisterAction(BuyBookPart).AddPart((session) => BuyBookAsync(session, Book), (session) => true);

            BooksByTime = new XCollection<Book>();
			BooksByAuthors = new XCollection<Book>();
			BooksByAuthorsGrouped = new List<LongListGroup<Book>>();
			BooksByNames = new XCollection<Book>();
            Basket = new XCollection<Book>();         
            BookSelected = new RelayCommand<Book>( NavigateToBook, book => book != null );
            Read = new RelayCommand<Book>(book =>
			{
			    if (!book.IsExpiredBook) _navigationService.Navigate("Reader", XParameters.Create("BookEntity", book), false);
			    else new MessageDialog("Истёк срок выдачи.").ShowAsync();
			} );
            BuyBook = new RelayCommand<Book>(book => BuyBookFromLitresAsync(book));
            RunCreditCardPaymentProcess = new RelayCommand(CreditCardInfo);
            ShowCreditCardView = new RelayCommand<Book>(book => _navigationService.Navigate("CreditCardPurchase", XParameters.Create("BookEntity", book)), book => book != null);
            BuyBookFromMicrosoft = new RelayCommand(BuyBookFromMicrosoftAsync);
        }

        public async void LoadMyBasket()
        {
            await Load(new Session(BasketPart));
        }

        private async Task LoadMyBasket(Session session)
	    {
            try
            {
                XCollection<Book> myBasket = await _catalogProvider.GetBooksInBasket(session.Token);

                if (myBasket != null)
                {
                    Basket = myBasket.Clone(false);

                    OnPropertyChanged(new PropertyChangedEventArgs("Basket"));
                }

                _loaded = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

	    #endregion

        private async void BuyBookFromMicrosoftAsync()
        {
            OnPropertyChanged(new PropertyChangedEventArgs("HidePopup"));
            try
            {
                await Load(new Session(BuyBookPart));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async Task BuyBookAsync(Session session, Book book)
        {
            await _litresPurchaseService.BuyBook(book, CancellationToken.None);
        }

        private async void BuyBookFromLitresAsync(Book book)
        {
            OnPropertyChanged(new PropertyChangedEventArgs("BuyBookStart"));
            Book = book;
            await Load(new Session(BuyBookLitresPart));
        }

        private async void CreditCardInfo()
        {
            Analytics.Instance.sendMessage(Analytics.ActionGotoLitres);
            OnPropertyChanged(new PropertyChangedEventArgs("HideSwitchPopup"));
            await Load(new Session(CreditCardInfoPart));
        }

        private async Task CreditCardInfoAsync(Session session)
        {
            var userInfo = await _profileProvider.GetUserInfo(session.Token, true);
            if (userInfo == null) return;
            if (_userInformation == null) _userInformation = userInfo;
            var cred = _credentialsProvider.ProvideCredentials(session.Token);

            if (!userInfo.CanRebill.Equals("0") &&
                cred != null &&
                !string.IsNullOrEmpty(cred.UserId) &&
                userInfo.UserId == cred.UserId &&
                !string.IsNullOrEmpty(cred.CanRebill) &&
                !cred.CanRebill.Equals("0"))
            {
                var box = new Dictionary<string, object>
                {
                    { "isSave", true },
                    { "isAuth", false }
                };
                var param = XParameters.Empty.ToBuilder()
                    .AddValue("Id", Book.Id)
                    .AddValue("Operation", (int)AccountDepositViewModel.AccountDepositOperationType.AccountDepositOperationTypeCreditCard)
                    .AddValue("ParametersDictionary", ModelsUtils.DictionaryToString(box)).ToImmutable();


                _navigationService.Navigate("AccountDeposit", param);
            }
            else
            {
                if (cred != null) _credentialsProvider.ForgetCredentialsRebill(cred, session.Token);
                ShowCreditCardView.Execute(Book);
            }
        }

        private async Task BuyBookFromLitres(Session session, Book book)
        {
            UserInformation userInfo = null;
            try
            {
                userInfo = await _profileProvider.GetUserInfo(session.Token, true);
            }
            catch (Exception ex)
            {
                await new MessageDialog("Авторизируйтесь, пожалуйста.").ShowAsync();
            }
            if (userInfo == null) return;
            if (!string.IsNullOrEmpty(book.InGifts) && book.InGifts.Equals("1"))
            {
                await _litresPurchaseService.BuyBookFromLitres(book, session.Token);
            }
            else if (userInfo.Account - book.Price >= 0)
            {
                var dialog = new MessageDialog(string.Format("Подтвердите покупку книги за {0} руб.", book.Price), "Подтвердите покупку");
                dialog.DefaultCommandIndex = 0;
                dialog.CancelCommandIndex = 1;
                dialog.Commands.Add(new UICommand("купить", command => Task.Run(async () => await _litresPurchaseService.BuyBookFromLitres(book, session.Token))));
                dialog.Commands.Add(new UICommand("отмена") { Id = 1 });
                await dialog.ShowAsync();

                //var result = Microsoft.Xna.Framework.GamerServices.Guide.BeginShowMessageBox(
                //"Подтвердите покупку",
                //string.Format("Подтвердите покупку книги за {0} руб.", Entity.Price),
                //new string[] { "купить", "отмена" },
                //0,
                //Microsoft.Xna.Framework.GamerServices.MessageBoxIcon.None,
                //null,
                //null);

                //result.AsyncWaitHandle.WaitOne();
                //int? choice = Microsoft.Xna.Framework.GamerServices.Guide.EndShowMessageBox(result);
                //if (choice.HasValue && choice.Value == 0)
                //{
                //    await _litresPurchaseService.BuyBookFromLitres(book, session.Token);              
                //}                
            }
            else
            {
                OnPropertyChanged(new PropertyChangedEventArgs("ChoosePaymentMethod"));
            }
        }

        public async Task UpdatePrice()
        {
            var userInfo = await _profileProvider.GetUserInfo(CancellationToken.None, false);
            if (userInfo == null) return;
            if (_userInformation == null) _userInformation = userInfo;
            AccoundDifferencePrice = Book.Price - userInfo.Account;
            if (AccoundDifferencePrice < 10) AccoundDifferencePrice = 10;
            OnPropertyChanged(new PropertyChangedEventArgs("UpdatePrice"));
        }

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
                       // myBooks.Clear();
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
               // Basket = await _bookProvider.GetExistBooks(session.Token);
                //if (exist != null && exist.Count > 0)
                //{
                //    clone = clone ?? new XCollection<Book>();

                //    foreach (var book in exist)
                //    {
                //        if (clone.All(x => x.Id != book.Id))
                //        {
                //            clone.Add(book);
                //        }
                //    }
                //}
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
				    catch (Exception)
				    {
                        //
				    }
				    if( booksWithoutAuthors.Count > 0 )
					{
						var group = new LongListGroup<Book>( " ", booksWithoutAuthors );
						BooksByAuthorsGrouped.Add( group );
					}
				}
			    Basket = new XCollection<Book>(BooksByTime.Where(x => string.Equals(x.Basket, "1")).ToList());
                BooksByTime = new XCollection<Book>(BooksByTime.Where(x => x.IsMyBook).ToList());
                BooksByAuthors = new XCollection<Book>(BooksByAuthors.Where(x => x.IsMyBook).ToList());
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
