using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Digillect.Collections;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.LibraryTools;
using LitRes.Models;
using LitRes.Services;
using LitRes.Services.Connectivity;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Digillect;

namespace LitRes.ViewModels
{
	public class BookViewModel : EntityViewModel<Book>
	{
		private const string LoadMoreSequenceBooksPart = "LoadMoreSequenceBooks";
		private const string LoadMoreReadWithBooksPart = "LoadMoreReadWithBooks";
		private const string LoadPersonPart = "LoadPerson";
		private const string BuyBookPart = "BuyBook";
        private const string BuyBookLitresPart = "BuyBookLitresPart";
	    private const string BuyBookFromSectionLitresPart = "BuyBookLitresPart";
        private const string CreditCardInfoPart = "CreditCardInfoPart";
        private const string SelfServiceRequestPart = "SelfServiceRequestPart";
        private const string RecensesRequestPart = "RecensesRequestPart";
        private const string AddBookRecensePart = "AddBookRecense";

        private readonly IGenresProvider _genresProvider;
		private readonly ICatalogProvider _catalogProvider;
		private readonly IRecensesProvider _recensesProvider;
		private readonly INavigationService _navigationService;
		private readonly ICredentialsProvider _credentialsProvider;
		private readonly IDeviceInfoService _deviceInfoService;
		private readonly IPersonsProvider _personsProvider;
		private readonly ILitresPurchaseService _litresPurchaseService;
        private readonly IProfileProvider _profileProvider;
        private readonly ICatalitClient _client;
	    private readonly IExpirationGuardian _expirationGuardian;
	    private readonly INetworkAvailabilityService _networkAvailability;
	    private readonly IPurchaseServiceDecorator _purchaseServiceDecorator;

		private string _addRecenseText;
		private bool _inNokiaCollection;

		private bool _loaded;
	    private bool _recensesLoaded;
		private bool _accountExist;
		private bool _recenseExist;

		private bool _isEndOfListSequenceBooks;
		private bool _isEndOfListReadWithBooks;
		private bool _canBuyBook;
		private bool _canGetBook;
        private UserInformation _userInformation;

        #region Public Properties
        public Book Book { get; private set; }
        public XCollection<Genre> BookGenres { get; private set; }
		public XCollection<Book> SequenceBooks { get; private set; }
		public XCollection<Book> ReadWithBooks { get; private set; }
		public XCollection<Recense> BookRecenses { get; private set; }
	    public string SequencesToString => Entity?.Sequences?.SequencesToString;
	    public double AccoundDifferencePrice { get; private set; }
        public UserInformation UserInformation
        {
            get { return _userInformation; }
            private set { SetProperty(ref _userInformation, value, "UserInformation"); }
        }
		public bool AccountExist
		{
			get { return _accountExist; }
			private set { SetProperty( ref _accountExist, value, "AccountExist" ); }
		}
        public bool RecenseExist
		{
			get { return _recenseExist; }
			private set { SetProperty( ref _recenseExist, value, "RecenseExist"); }
		}
		public string AddRecenseText
		{
			get { return _addRecenseText; }
			private set { SetProperty( ref _addRecenseText, value, "AddRecenseText" ); }
		}
		public bool InNokiaCollectionForNokia
		{
			get { return _inNokiaCollection; }
			private set { SetProperty( ref _inNokiaCollection, value, "InNokiaCollectionForNokia" ); }
		}
		public bool InNokiaCollectionNotNokia
		{
			get { return _inNokiaCollection; }
			private set { SetProperty( ref _inNokiaCollection, value, "InNokiaCollectionNotNokia" ); }
		}
		public bool CanBuyBook
		{
			get { return _canBuyBook; }
			private set { SetProperty( ref _canBuyBook, value, "CanBuyBook" ); }
		}
		public bool CanGetBook
		{
			get { return _canGetBook; }
			private set { SetProperty( ref _canGetBook, value, "CanGetBook" ); }
		}
        public bool SimCardDetected => _deviceInfoService.IsSimCardDetected;

	    public string ReadButtonText => Entity!=null && (Entity.IsMyBook || Entity.isFreeBook) && !Entity.isFragment ? "ЧИТАТЬ" : "ЧИТАТЬ ФРАГМЕНТ";

	    public bool IsHiddenBook { get; set; }
        public Book RelatedAudioBook { get; set; }
        public RelayCommand<Book> BookSelected { get; private set; }
        public RelayCommand<Genre> TagSelected { get; private set; }
		public RelayCommand<Genre> GenreSelected { get; private set; }
		public RelayCommand<Book.TitleInfo.AuthorInfo> AuthorSelected { get; private set; }
		public RelayCommand LoadMoreSequenceBooks { get; private set; }
		public RelayCommand LoadMoreReadWithBooks { get; private set; }
		public RelayCommand Read { get; private set; }
		public RelayCommand WriteRecenseSelected { get; private set; }
        public RelayCommand BuyBook { get; private set; }
        public RelayCommand<Book> BuyBookFromSection { get; private set; }
        public RelayCommand BuyBookFromMicrosoft { get; private set; }
        public RelayCommand Recharge { get; private set; }
        public RelayCommand RunCreditCardPaymentProcess { get; private set; }
        public RelayCommand ProcessMobilePayment { get; private set; }
        public RelayCommand<Book> ShowMobilePayment { get; private set; }
        public RelayCommand<Book> SmsMobilePayment { get; private set; }
        public RelayCommand<Book> ShowCreditCardView { get; private set; }
        public RelayCommand SelfServiceRequest { get; private set; }

		#endregion

		#region Constructors/Disposer
        public BookViewModel(
            IPurchaseServiceDecorator _ipurchaseServiceDecorator,
            IExpirationGuardian expirationGuardian, 
            IPersonsProvider personsProvider, 
            IDeviceInfoService deviceInfoService, 
            IGenresProvider genresProvider, 
            ICatalogProvider catalogProvider, 
            INavigationService navigationService, 
            IRecensesProvider recensesProvider, 
            ILitresPurchaseService litresPurchaseService,
            ICredentialsProvider credentialsProvider, 
            IProfileProvider profileProvider, 
            ICatalitClient client, 
            INetworkAvailabilityService networkAvailability
            )
		{
			_genresProvider = genresProvider;
			_catalogProvider = catalogProvider;
			_navigationService = navigationService;
			_recensesProvider = recensesProvider;
			_credentialsProvider = credentialsProvider;
			_deviceInfoService = deviceInfoService;
			_personsProvider = personsProvider;
			_litresPurchaseService = litresPurchaseService;
            _profileProvider = profileProvider;
            _client = client;
            _networkAvailability = networkAvailability;
		    _expirationGuardian = expirationGuardian;
            _purchaseServiceDecorator = _ipurchaseServiceDecorator;
            _recensesLoaded = false;
            RecenseExist = false;

            BookGenres = new XCollection<Genre>();
			SequenceBooks = new XCollection<Book>();
			ReadWithBooks = new XCollection<Book>();
			BookRecenses = new XCollection<Recense>();
            Book = null;

            RegisterAction(LoadMoreReadWithBooksPart).AddPart(session => LoadReadWithBooks(session, Entity), session => !_isEndOfListReadWithBooks);
            RegisterAction(LoadMoreSequenceBooksPart).AddPart(session => LoadSequenceBooks(session, Entity), session => !_isEndOfListSequenceBooks);
            RegisterAction(LoadPersonPart).AddPart((session) => LoadPerson(session), (session) => true);

            RegisterAction(BuyBookPart).AddPart((session) => BuyBookAsync(session, Entity), (session) => true);
            RegisterAction(BuyBookLitresPart).AddPart((session) => BuyBookFromLitres(session, Book), (session) => true);
            RegisterAction(CreditCardInfoPart).AddPart(session => CreditCardInfoAsync(session), (session) => true) ;
            RegisterAction(SelfServiceRequestPart).AddPart((session) => SelfServiceRequestProcessing(session, Entity), (session) => true);
            RegisterAction(RecensesRequestPart).AddPart((session) => LoadRecenses(session, Entity), (session) => !_recensesLoaded);
            RegisterAction(AddBookRecensePart).AddPart(session => AddBookRecense(session), (session) => true);

            LoadMoreSequenceBooks = new RelayCommand( LoadMoreSequenceBooksProceed, () => !_isEndOfListSequenceBooks );
			LoadMoreReadWithBooks = new RelayCommand( LoadMoreReadWithBooksProceed, () => !_isEndOfListReadWithBooks );
			BookSelected = new RelayCommand<Book>( book => _navigationService.Navigate( "Book", XParameters.Create("BookEntity", book ) ), book => book != null );
			TagSelected = new RelayCommand<Genre>( genre => _navigationService.Navigate("BooksByCategory", XParameters.Empty.ToBuilder()
                .AddValue("category", 6 )
                .AddValue("id", genre.Id)
                .AddValue("title", genre.Title)
                .ToImmutable() ), genre => genre != null );
			GenreSelected = new RelayCommand<Genre>( ToSelectedGenre, genre => genre != null );
			AuthorSelected = new RelayCommand<Book.TitleInfo.AuthorInfo>( AuthorSelectedProceed );
			Read = new RelayCommand(() =>
			{
			    if (!Entity.IsExpiredBook)                    
                    _navigationService.Navigate("Reader", XParameters.Create("BookEntity", Entity));
			    else new MessageDialog("Истёк срок выдачи.").ShowAsync();
			} );
			WriteRecenseSelected = new RelayCommand( () => _navigationService.Navigate( "AddRecense", XParameters.Create( "bookId", Convert.ToString(Entity.Id) ) ), () => Entity != null );
			
            BuyBook = new RelayCommand( BuyBookFromLitresAsync );
            BuyBookFromMicrosoft = new RelayCommand( BuyBookFromMicrosoftAsync );
            Recharge = new RelayCommand(RechargeThisShit);
            RunCreditCardPaymentProcess = new RelayCommand(CreditCardInfo);
            ProcessMobilePayment = new RelayCommand(ProcessMobileCommerce);
            ShowMobilePayment =  new RelayCommand<Book>(book => _navigationService.Navigate("MobilePurchase", XParameters.Create("id", book.Id)), book => book != null);
            SmsMobilePayment = new RelayCommand<Book>(book => _navigationService.Navigate("SmsPurchase", XParameters.Create("id", book.Id)), book => book != null);
            ShowCreditCardView = new RelayCommand<Book>(book => _navigationService.Navigate("CreditCardPurchase", XParameters.Create("BookEntity", book)), book => book != null);
            BuyBookFromSection = new RelayCommand<Book>(book => BuyBookFromLitresAsync(book));
            SelfServiceRequest = new RelayCommand(SelfServiceRequestAsync);
		}

        private async void BuyBookFromLitresAsync(Book book)
        {
            OnPropertyChanged(new PropertyChangedEventArgs("BuyBookStart"));
            Book = book;
            await Load(new Session(BuyBookFromSectionLitresPart));
        }
        #endregion

        #region LoadEntity

        protected override Task LoadEntity(Session session)
        {
            Entity = session.Parameters.GetValue<Models.Book>("BookEntity");
            session.AddParameter("LoadBookSessison", Entity);
            return LoadBook(session);
        }

        #endregion

        #region UpdateButtons
        public void UpdateButtons()
        {
            if (Entity!=null && Entity.isFreeBook)
            {
                if (!Entity.IsMyBook) OnPropertyChanged(new PropertyChangedEventArgs("FreeButton"));
                else OnPropertyChanged(new PropertyChangedEventArgs("ReadButton"));
            }
        }
        #endregion

		#region ShouldLoadSession
		protected override bool ShouldLoadEntity( Session session )
		{
			if(session.Parameters.Contains("LoadBookSession"))
			{
				return !_loaded;
			}
			return true;
		}
		#endregion

		#region LoadMoreSequenceBooksProceed
		private async void LoadMoreSequenceBooksProceed()
		{
			Session session = new Session( LoadMoreSequenceBooksPart );
			await Load( session );
		}
		#endregion

		#region LoadMoreReadWithBooksProceed
		private async void LoadMoreReadWithBooksProceed()
		{
			Session session = new Session( LoadMoreReadWithBooksPart );
			await Load( session );
		}
		#endregion

		#region UpdateBookAfterPurchasing
		public async Task UpdateBookAfterPurchasing()
		{
			await CheckBuyButton( CancellationToken.None );
            OnPropertyChanged(new PropertyChangedEventArgs("ReadButtonText"));
		    
		}
		#endregion

		#region LoadGenres
		private async Task LoadGenres( Session session, Book book )
		{
			if( book.Description != null && book.Description.Hidden != null &&
				book.Description.Hidden.TitleInfo != null && book.Description.Hidden.TitleInfo.Genres != null )
			{
				var genres = await _genresProvider.GetGenresByTokens( book.Description.Hidden.TitleInfo.Genres, session.Token );
                var sortedGenres = genres.ToList();
                sortedGenres.Sort((a, b) => a.Title.Length > b.Title.Length ? -1 : 1);
			    genres.Update(sortedGenres);
			    
                BookGenres.Update( genres );
                OnPropertyChanged(new PropertyChangedEventArgs("GenresLoaded"));
			}
		}
		#endregion
		#region LoadBook
		private async Task LoadBook( Session session )
		{
		    try
		    {
		        AccountExist = ( _credentialsProvider.ProvideCredentials(session.Token)) != null;
		    }
		    catch (Exception ex) {}
		        		    
		    Book book = null;
		    if (IsHiddenBook)
		    {
                book = await _catalogProvider.GetHiddenBook(Entity.Id, session.Token);
		    }
		    else
		    {
		        book = await _catalogProvider.GetBook( Entity.Id, session.Token );
		    }

            if (book == null)
            {
                await new MessageDialog(string.Concat("Невозможно получить информацию о книге Id=", Convert.ToString(Entity.Id))).ShowAsync();
                _navigationService.GoBack();
                return;
            }

		    if (!book.IsLocal)
		    {
		        new Task(async () =>
		        {
                    try
                    {
                        RelatedAudioBook = await _catalogProvider.GetAudioBook(book.Id, session.Token);
                        if (RelatedAudioBook != null)
                        {

                           await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,(() =>{ OnPropertyChanged(new PropertyChangedEventArgs("HasRelationBook")); }));
                        }              
                    }
                    catch (Exception ex) { Debug.WriteLine(ex.ToString()); }
		        }).Start();		        
		    }

		    if (book.isFreeBook)
		    {
                var propertyChangedEventArgs = "ReadButton";
                if (!book.IsMyBook) propertyChangedEventArgs = "FreeButton";
                OnPropertyChanged(new PropertyChangedEventArgs(propertyChangedEventArgs));
            }

			Entity = book;
            if (!book.IsLocal) AddRecenseText = "Загрузка...";
            
            OnPropertyChanged(new PropertyChangedEventArgs("BookLoaded"));           

		    if (_networkAvailability.NetworkAvailable && !Entity.IsLocal)
		    {
		        try
		        {
		            await CheckBuyButton(session.Token);
		        }
                catch (Exception ex) { Debug.WriteLine(ex.ToString()); }
                OnPropertyChanged(new PropertyChangedEventArgs("UpdateSelfServiceUI"));
		    }
		    
			_loaded = true;

            if (_networkAvailability.NetworkAvailable && !Entity.IsLocal)
		    {
		        try
		        {
		            await LoadBookInfo(session, book);
		        }
                catch (Exception ex) { Debug.WriteLine(ex.ToString()); }
		    }
		}
		#endregion

		#region LoadBookInfo
		private async Task LoadBookInfo( Session session, Book book )
		{
			Task loadGenres = LoadGenres( session, book );
			//Task loadRecenses = LoadRecenses( session, book );
			Task loadSequenceBooks = LoadSequenceBooks( session, book );
			Task loadReadWithBooks = LoadReadWithBooks( session, book );

			await Task.WhenAll( loadGenres,  loadSequenceBooks, loadReadWithBooks );
		}
		#endregion
		#region LoadRecenses
        public async void LoadRecenses()
        {
            if (!_recensesLoaded) await Load(new Session(RecensesRequestPart));
        }

		private async Task LoadRecenses( Session session, Book book )
		{
            if (!_recensesLoaded)
            {
                RecenseExist = true;
                try
                {
                    var recenses = await _recensesProvider.GetRecenses(book.Id, session.Token);
                    BookRecenses.BeginUpdate();
                    BookRecenses.Update(recenses);
                    BookRecenses.EndUpdate();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }

                if (BookRecenses.Count == 0)
                {
                    RecenseExist = false;
                    var bookTitle = string.Empty;
                    try
                    {
                        if (Entity.IsLocal) book.Description.Hidden.TitleInfo.Sequence = null;
                        bookTitle = book.Description.Hidden.TitleInfo.BookTitle;
                    }
                    catch (Exception ex) { Debug.WriteLine(ex.ToString()); }
                    if (!book.IsLocal) AddRecenseText = "Будьте первым, кто напишет рецензию на книгу \"" + bookTitle + "\"";
                }
                else
                {
                    RecenseExist = true;
                }
                _recensesLoaded = true;
		    }

            OnPropertyChanged(new PropertyChangedEventArgs("RecensesLoaded"));
		}
		#endregion
		#region LoadSequenceBooks
		private async Task LoadSequenceBooks( Session session, Book book )
		{
            
			if( book.Sequences != null && book.Sequences.Sequences != null && book.Sequences.Sequences.Count > 0 )
			{
                var sequenceBooks = await _catalogProvider.GetBooksBySequence(book.Sequences.Sequences[0].Id, session.Token);

				if( sequenceBooks != null )
				{
					if( sequenceBooks.Count <= SequenceBooks.Count )
					{
						_isEndOfListSequenceBooks = true;
					}

					SequenceBooks.Update( sequenceBooks );

					if( sequenceBooks.Count == 0 )
					{
						Entity.Description.Hidden.TitleInfo.Sequence = null;
					}
				}
			}

            OnPropertyChanged(new PropertyChangedEventArgs("SequenceBooksLoaded"));
		}
		#endregion
		#region LoadReadWithBooks
		private async Task LoadReadWithBooks( Session session, Book book )
		{
			var readWith = await _catalogProvider.GetBooksAreReadWithThisBook( ReadWithBooks.Count, book.Id, session.Token );

			if( readWith != null )
			{
				if( readWith.Count <= ReadWithBooks.Count )
				{
					_isEndOfListReadWithBooks = true;
				}

				ReadWithBooks.Update( readWith );
			}
            OnPropertyChanged(new PropertyChangedEventArgs("ReadWithBooksLoaded"));
		}
		#endregion
		#region LoadPerson
		private async Task<bool> LoadPerson( string person )
		{
			Session session = new Session( LoadPersonPart );
			session.AddParameter( "person", person );
			await Load( session );

			bool isExist = session.Parameters.GetValue<bool>( "isExist" );

			return isExist;
		}

		private async Task LoadPerson( Session session )
		{
			Person person = null;

			string id = session.Parameters.GetValue<string>( "person" );
			if( !string.IsNullOrEmpty( id ) )
			{
				person = await _personsProvider.GetPersonById( id, session.Token );
			}

			session.AddParameter( "isExist", person != null );
		}
		#endregion

		private async Task CheckBuyButton( CancellationToken token )
		{
		    try
		    {                                
                UserInformation = await _profileProvider.GetUserInfo(token);
                if (Entity.isFreeBook || UserInformation.AccountType == (int)AccountTypeEnum.AccountTypeLibrary)
                {
                    CanBuyBook = false;
                }
                else
                {
                    const int collectionId = (int)BooksByCategoryViewModel.BooksViewModelTypeEnum.NokiaCollection;
                    Book bookFromNokiaCollection;                    
                    if (_catalogProvider.IsBooksCollectionLoaded(collectionId))
                    {
                        bookFromNokiaCollection = _catalogProvider.GetBookByCollectionCache(collectionId, Entity.Id);
                    }
                    else
                    {
                        bookFromNokiaCollection = await _catalogProvider.GetBookByCollection(collectionId, Entity.Id, token);                        
                    }
                    
                    if (_deviceInfoService.IsNokiaDevice)
                    {
                        InNokiaCollectionForNokia = bookFromNokiaCollection != null && !string.IsNullOrEmpty(Entity.InGifts) && Entity.InGifts == "1";
                        if (InNokiaCollectionForNokia) CanGetBook = !Entity.IsMyBook;                
                        else CanBuyBook = !Entity.IsMyBook;                        
                    }
                    else
                    {
                        InNokiaCollectionNotNokia = bookFromNokiaCollection != null;
                        CanBuyBook = !Entity.IsMyBook;
                    }
                }
		    }
		    catch (Exception ex){Debug.WriteLine(ex.ToString());}			
			OnPropertyChanged( new PropertyChangedEventArgs( "Entity" ) );
		}

        private async void AuthorSelectedProceed( Book.TitleInfo.AuthorInfo author )
		{
			if( !string.IsNullOrEmpty( author?.Id ) )
			{
				var isExist = await LoadPerson( author.Id );
				if( isExist )
				{
                    Analytics.Instance.sendMessage(Analytics.ActionGotoAuthor);
					_navigationService.Navigate( "Person", XParameters.Create( "Id", author.Id ) );
				}
			}
		}

        private async void BuyBookFromLitresAsync()
        {                       
            OnPropertyChanged(new PropertyChangedEventArgs("BuyBookStart"));
            Book = Entity;
            await Load(new Session(BuyBookLitresPart));
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
            if (!string.IsNullOrEmpty(Entity.InGifts) && Entity.InGifts.Equals("1"))
            {
                await _litresPurchaseService.BuyBookFromLitres(book, session.Token);       
            }
            else if (userInfo.Account - book.Price >= 0)
            {
                var dialog = new MessageDialog(string.Format("Подтвердите покупку книги за {0} руб.", book.Price), "Подтвердите покупку");
                dialog.DefaultCommandIndex = 0;
                dialog.CancelCommandIndex = 1;
                dialog.Commands.Add(new UICommand("купить", command => Task.Run(async ()=> await _litresPurchaseService.BuyBookFromLitres(book, session.Token))));
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

		private async Task BuyBookAsync( Session session, Book book )
		{
			await _litresPurchaseService.BuyBook( book, CancellationToken.None );
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
            var cred =  _credentialsProvider.ProvideCredentials(session.Token);

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
                    .AddValue("Id", Entity.Id)
                    .AddValue("Operation",(int) AccountDepositViewModel.AccountDepositOperationType.AccountDepositOperationTypeCreditCard)
                    .AddValue("ParametersDictionary", ModelsUtils.DictionaryToString(box)).ToImmutable();
            

                _navigationService.Navigate("AccountDeposit", param);
            }
            else
            {
                if (cred != null) _credentialsProvider.ForgetCredentialsRebill(cred, session.Token);
                ShowCreditCardView.Execute(Entity);
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

        private void RechargeThisShit()
        {
            OnPropertyChanged(new PropertyChangedEventArgs("HidePopup"));
            OnPropertyChanged(new PropertyChangedEventArgs("ShowSwitchPopup"));
        }

        public async void ProcessMobileCommerce()
        {
            OnPropertyChanged(new PropertyChangedEventArgs("HideSwitchPopup"));

            var userInfo = await _profileProvider.GetUserInfo(CancellationToken.None, true);
            if (userInfo == null) return;

            double sum = Entity.Price - userInfo.Account;
            if (sum < 10) sum = 10;
            if (string.IsNullOrEmpty(userInfo.Phone))
            {
                ShowMobilePayment.Execute(Entity);
            }
            else
            {
                var param = XParameters.Empty.ToBuilder()
                    .AddValue("Id", Entity.Id)
                    .AddValue("Operation",(int) AccountDepositViewModel.AccountDepositOperationType.AccountDepositOperationTypeMobile)
                    .ToImmutable();
                _navigationService.Navigate("AccountDeposit", param);
            }
        }

        private void ToSelectedGenre(Genre genre)
        {
            Analytics.Instance.sendMessage(Analytics.ActionGotoGenre);
            _navigationService.Navigate("GenreBooks", XParameters.Create("id", genre.Id));
        }

        private async void SelfServiceRequestAsync()
        {
           await Load(new Session(SelfServiceRequestPart));
        }

        private async Task SelfServiceRequestProcessing(Session session, Book book)
        {
            try
            {
                var parameters = new Dictionary<string, object> {{"art", book.Id}};
                var rawData =
                    await _client.SelfServiceRequest(parameters, book.SelfServiceMyRequest.Equals("1"), session.Token);
                var resp = new SelfServiceResponse(rawData);
                switch (resp.State)
                {
                    case SelfServiceResponseEnum.Ok:
                    {
                        await _litresPurchaseService.TakeBookFromLitres(book, session.Token);
                        book.SelfServiceMyRequest = "0";
                        book.IsMyBook = true;
                        _catalogProvider.UpdateBook(book);
                        break;
                    }
                    case SelfServiceResponseEnum.QueueOk:
                    {
                        book.SelfServiceMyRequest = "1";
                        await _catalogProvider.AddToMyBooks(book, session.Token);
                        _expirationGuardian.AddBook(book);
                        _catalogProvider.UpdateBook(book);
                        await _purchaseServiceDecorator.RefreshPages(book);

                        await
                            new MessageDialog(
                                string.Format("Запрос в библиотеку отправлен.\nВаше место в очереди - {0}.",
                                    resp.QueueCount)).ShowAsync();
                        //MessageBox.Show(string.Format("Запрос в библиотеку отправлен.\nВаше место в очереди - {0}.", resp.QueueCount));

                        break;
                    }
                    case SelfServiceResponseEnum.DropOk:
                    {
                        book.SelfServiceMyRequest = "0";
                        book.IsMyBook = false;
                        _catalogProvider.UpdateBook(book);
                        _catalogProvider.CheckBooks();
                        await _catalogProvider.DeleteBook(book);
                        _catalogProvider.CheckBooks();
                        break;
                    }
                    case SelfServiceResponseEnum.Error:
                    case SelfServiceResponseEnum.QueueError:
                    case SelfServiceResponseEnum.DropError:
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            OnPropertyChanged(new PropertyChangedEventArgs("UpdateSelfServiceUI"));
        }

        public async Task AddBookRecense(string message, int bookId)
        {
            Session session = new Session(AddBookRecensePart);
            session.AddParameter("message", message);
            session.AddParameter("bookId", bookId);
            await Load(session);
        }

        private async Task AddBookRecense(Session session)
        {
            string message = session.Parameters.GetValue<string>("message");
            int bookId = session.Parameters.GetValue<int>("bookId");
            await _recensesProvider.AddRecenseForBook(message, bookId, session.Token);
        }

    }
}
