using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Popups;
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
	public class BooksByCategoryViewModel : ViewModel
	{
		#region BooksViewModelTypeEnum
#if DEBUG
        public enum BooksViewModelTypeEnum:int
        {
            Popular = 0,
            Novelty = 1,
            Interesting = 2,
            NokiaCollection = 2634,
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
        private const string BuyBookLitresPart = "BuyBookLitresPart";
        private const string CreditCardInfoPart = "CreditCardInfoPart";

        public const string BooksTypeParameter = "BooksViewModelType";

		private readonly ICatalogProvider _catalogProvider;
		private readonly INavigationService _navigationService;
        private readonly ILitresPurchaseService _litresPurchaseService;
        private readonly IProfileProvider _profileProvider;
        private readonly ICredentialsProvider _credentialsProvider;

        private int _booksViewModelType;
	    private int _genreOrTagOrSeriaID;

		private bool _loaded;
		private bool _loading;
		private bool _isEndOfList;

        private UserInformation _userInformation;

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

        public Book Book { get; private set; }

        public XCollection<Book> Books { get; private set; }

        public double AccoundDifferencePrice { get; private set; }

        public RelayCommand<Book> BookSelected { get; private set; }
        public RelayCommand<Book> BuyBook { get; private set; }
        public RelayCommand LoadMoreCalled { get; private set; }
        public RelayCommand RunCreditCardPaymentProcess { get; private set; }
        public RelayCommand<Book> ShowCreditCardView { get; private set; }
        #endregion

        #region Constructors/Disposer
        public BooksByCategoryViewModel(ICatalogProvider catalogProvider, INavigationService navigationService, ILitresPurchaseService litresPurchaseService, IProfileProvider profileProvider, ICredentialsProvider credentialsProvider)
		{
			_catalogProvider = catalogProvider;
			_navigationService = navigationService;
            _litresPurchaseService = litresPurchaseService;
            _profileProvider = profileProvider;
            _credentialsProvider = credentialsProvider;


            RegisterAction(MainPart).AddPart(session => LoadBooks(session), session => !_loaded);
            RegisterAction(LoadMorePart).AddPart(session => LoadBooks(session), session => !_isEndOfList);
            RegisterAction(BuyBookLitresPart).AddPart((session) => BuyBookFromLitres(session, Book), (session) => true);
            RegisterAction(CreditCardInfoPart).AddPart(session => CreditCardInfoAsync(session), (session) => true);
            //RegisterPart( MainPart, ( session, part ) => LoadBooks( session ), ( session, part ) => !_loaded, true );
            //RegisterPart( LoadMorePart, ( session, part ) => LoadBooks( session ), ( session, part ) => !_isEndOfList, false );

            Books = new XCollection<Book>();

			BookSelected = new RelayCommand<Book>(book => _navigationService.Navigate("Book", XParameters.Create("BookEntity", book)), book => book != null);
			LoadMoreCalled = new RelayCommand(() => LoadMore(), () => true);
            BuyBook = new RelayCommand<Book>(book => BuyBookFromLitresAsync(book));
            RunCreditCardPaymentProcess = new RelayCommand(CreditCardInfo);
            ShowCreditCardView = new RelayCommand<Book>(book => _navigationService.Navigate("CreditCardPurchase", XParameters.Create("BookEntity", book)), book => book != null);

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
    }
}
