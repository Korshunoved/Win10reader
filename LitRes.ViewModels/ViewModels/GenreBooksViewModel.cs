using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Digillect;
using Digillect.Collections;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Models;
using LitRes.Services;
using LitRes.Services.Connectivity;

namespace LitRes.ViewModels
{
	public class GenreBooksViewModel : EntityViewModel<Genre>
	{
		private const string LoadMoreNoveltyBooksPart = "LoadMoreNoveltyBooks";
		private const string LoadMorePopularBooksPart = "LoadMorePopularBooks";
        private const string BuyBookLitresPart = "BuyBookLitresPart";
        private const string BuyBookPart = "BuyBook";
        private const string CreditCardInfoPart = "CreditCardInfoPart";

        private readonly ICatalogProvider _catalogProvider;
		private readonly IGenresProvider _genresProvider;
		private readonly INavigationService _navigationService;
        private readonly ILitresPurchaseService _litresPurchaseService;
        private readonly IProfileProvider _profileProvider;
        private readonly ICredentialsProvider _credentialsProvider;

        private bool _loaded;

		private bool _isEndOfListNoveltyBooks;
		private bool _isEndOfListPopularBooks;

	    private bool _loadingPopularBooks;

        private UserInformation _userInformation;

        #region Public Properties

        public double AccoundDifferencePrice { get; private set; }
        public Book Book { get; private set; }

        public XCollection<Book> NoveltyBooks { get; private set; }
		public XCollection<Book> PopularBooks { get; private set; }
		public XCollection<Genre> Genres { get; private set; }

		public RelayCommand LoadMoreNoveltyBooks { get; private set; }
		public RelayCommand LoadMorePopularBooks { get; private set; }
		public RelayCommand<Book> BookSelected { get; private set; }
		public RelayCommand<Genre> GenreSelected { get; private set; }
        public RelayCommand RunCreditCardPaymentProcess { get; private set; }
        public RelayCommand<Book> ShowCreditCardView { get; private set; }
        public RelayCommand<Book> BuyBook { get; private set; }
        public RelayCommand BuyBookFromMicrosoft { get; private set; }
        #endregion

        #region Constructors/Disposer
        public GenreBooksViewModel(ICatalogProvider catalogProvider, INavigationService navigationService, IGenresProvider genresProvider, ILitresPurchaseService litresPurchaseService, IProfileProvider profileProvider, ICredentialsProvider credentialsProvider)
		{
			_catalogProvider = catalogProvider;
			_navigationService = navigationService;
			_genresProvider = genresProvider;
            _litresPurchaseService = litresPurchaseService;
            _profileProvider = profileProvider;
            _credentialsProvider = credentialsProvider;

            PopularBooks = new XCollection<Book>();
			NoveltyBooks = new XCollection<Book>();

		    RegisterAction(LoadMoreNoveltyBooksPart).AddPart((session) => LoadNoveltyBooks(session, Entity), (session) => !_isEndOfListNoveltyBooks);
		    RegisterAction(LoadMorePopularBooksPart).AddPart((session) => LoadPopularBooks(session, Entity), (session) => !_isEndOfListPopularBooks);
            RegisterAction(BuyBookLitresPart).AddPart((session) => BuyBookFromLitres(session, Book), (session) => true);
            RegisterAction(BuyBookPart).AddPart((session) => BuyBookAsync(session, Book), (session) => true);
            RegisterAction(CreditCardInfoPart).AddPart(session => CreditCardInfoAsync(session), (session) => true);

            BookSelected = new RelayCommand<Book>( book => _navigationService.Navigate( "Book", XParameters.Create("BookEntity", book ) ), book => book != null );
			GenreSelected = new RelayCommand<Genre>( genre => _navigationService.Navigate( "GenreBooks", XParameters.Create( "id", genre.Id ) ), genre => genre != null );
			LoadMoreNoveltyBooks = new RelayCommand( LoadMoreNoveltyBooksProceed, () => true );
			LoadMorePopularBooks = new RelayCommand( LoadMorePopularBooksProceed, () => true );
            BuyBook = new RelayCommand<Book>(book => BuyBookFromLitresAsync(book));
            BuyBookFromMicrosoft = new RelayCommand(BuyBookFromMicrosoftAsync);
            RunCreditCardPaymentProcess = new RelayCommand(CreditCardInfo);
            ShowCreditCardView = new RelayCommand<Book>(book => _navigationService.Navigate("CreditCardPurchase", XParameters.Create("BookEntity", book)), book => book != null);
        }
        #endregion

        private async void BuyBookFromLitresAsync(Book book)
        {
            OnPropertyChanged(new PropertyChangedEventArgs("BuyBookStart"));
            Book = book;
            await Load(new Session(BuyBookLitresPart));
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

        private async Task BuyBookAsync(Session session, Book book)
        {
            await _litresPurchaseService.BuyBook(book, CancellationToken.None);
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
