﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Digillect.Collections;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.LibraryTools;
using LitRes.Models;
using LitRes.Services;
using LitRes.Services.Connectivity;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Linq;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using BookParser;
using BookParser.Models;
using BookParser.Parsers;
using Digillect;

namespace LitRes.ViewModels
{
    public class ReaderViewModel : EntityViewModel<Book>
    {
        #region
        public enum LoadingStatus
        {
            BeforeLoaded,
            FullBookLoaded,
            TrialBookLoaded,
            NoBookLoaded,
        }
        #endregion

        private const string SettingsPart = "Settings";
        private const string AddBookmarkPart = "AddBookmark";
        private const string BuyBookPart = "BuyBook";
        private const string BuyBookLitresPart = "BuyBookLitresPart";
        private const string CreditCardInfoPart = "CreditCardInfoPart";
        private const string ReloadBookPart = "ReloadBook";
        public const string LoadBookPart = "LoadBook";

        private readonly ICatalogProvider _catalogProvider;
        private readonly IBookProvider _bookProvider;
        private readonly ICredentialsProvider _credentialsProvider;
        private readonly ISettingsService _settingsService;
        private readonly IDataCacheService _dataCacheService;
        private readonly IBookmarksProvider _bookmarksProvider;
        private readonly ILitresPurchaseService _litresPurchaseService;
        private readonly INavigationService _navigationService;
        private readonly IProfileProvider _profileProvider;
        private readonly IDeviceInfoService _deviceInfoService;
        private readonly IExpirationGuardian _expirationGuardian;
        private readonly INetworkAvailabilityService _networkAvailability;
        private readonly IFileCacheService _fileCacheService;

        private string _bookFolderName;
#if PDF_ENABLED
        private pdftron.PDF.PDFDoc _pdfDocument;
#endif
        private ReaderSettings _settings;
        private LoadingStatus _status;
        private Exception _exception;
        private bool _accountExist;
        private string _blockIndex;
        private bool _pinExist;
        private UserInformation _userInformation;
        private bool _loaded;
        public RelayCommand ShowMyBooks { get; private set; }
        public RelayCommand ShowSettings { get; private set; }
        public RelayCommand ShowBookBookmarks { get; private set; }
        public RelayCommand ShowBookChapters { get; private set; }
        public RelayCommand BuyBook { get; private set; }
        public RelayCommand BuyBookFromMicrosoft { get; private set; }
        public RelayCommand Recharge { get; private set; }
        public RelayCommand RunCreditCardPaymentProcess { get; private set; }
        public RelayCommand ProcessMobilePayment { get; private set; }
        public RelayCommand<Book> ShowMobilePayment { get; }
        public RelayCommand<Book> SmsMobilePayment { get; private set; }
        public RelayCommand<Book> ShowCreditCardView { get; }

        public string BookTitle
        {
            get
            {
                try
                {
                    return String.Format("\u00AB{0}\u00BB", Entity.Description.Hidden.TitleInfo.BookTitle);
                }
                catch (Exception)
                {
                    return String.Empty;
                }
            }
        }

        public string Sid { get; private set; }

        public ReaderSettingsViewModel.DeffaultSettingsType DeffaultSettings
        {
            set
            {
                _settingsService.DeffaultSettings = value;
            }
        }

        #region Constructors/Disposer
        public ReaderViewModel(IExpirationGuardian expirationGuardian, IDataCacheService dataCacheService, IDeviceInfoService deviceInfoService, ICatalogProvider catalogProvider, ICredentialsProvider credentialsProvider, IBookProvider bookProvider, IBookmarksProvider bookmarksProvider, ISettingsService settingsService, ILitresPurchaseService litresPurchaseService, INavigationService navigationService, IProfileProvider profileProvider, INetworkAvailabilityService networkAvailability, IFileCacheService fileCacheService)
        {
            _catalogProvider = catalogProvider;
            _bookProvider = bookProvider;
            _credentialsProvider = credentialsProvider;
            _settingsService = settingsService;
            _bookmarksProvider = bookmarksProvider;
            _litresPurchaseService = litresPurchaseService;
            _dataCacheService = dataCacheService;
            _navigationService = navigationService;
            _profileProvider = profileProvider;
            _deviceInfoService = deviceInfoService;
            _networkAvailability = networkAvailability;
            _expirationGuardian = expirationGuardian;
            _fileCacheService = fileCacheService;

            _settings = new ReaderSettings();

            Id = -1;

            RegisterAction(LoadBookPart).AddPart(LoadBook, session => !_loaded);
            RegisterAction(SettingsPart).AddPart(LoadSettings, session => true);
            RegisterAction(AddBookmarkPart).AddPart(AddBookmark, session => true);
            RegisterAction(ReloadBookPart).AddPart(Reload, session => true);

            RegisterAction(BuyBookPart).AddPart(session => BuyBookAsync(session, Entity), session => true);
            RegisterAction(BuyBookLitresPart).AddPart(session => BuyBookFromLitres(session, Entity), session => true);
            RegisterAction(CreditCardInfoPart).AddPart(CreditCardInfoAsync, session => true);
           
            ShowMyBooks = new RelayCommand(() => _navigationService.Navigate("MyBooks"));
            ShowSettings = new RelayCommand(() => { SaveSettings(); _navigationService.Navigate("Settings", XParameters.Create("PDFBook", Entity.TypeBook == Book.BookType.Pdf)); });
            ShowBookBookmarks = new RelayCommand(() => _navigationService.Navigate("BookBookmarks", XParameters.Create("id", Entity.Id)));
            ShowBookChapters = new RelayCommand(() => _navigationService.Navigate("BookChapters", XParameters.Create("id", Entity.Id)));

            BuyBook = new RelayCommand(BuyBookFromLitresAsync);
            BuyBookFromMicrosoft = new RelayCommand(BuyBookFromMicrosoftAsync);
            Recharge = new RelayCommand(RechargeThisShit);
            RunCreditCardPaymentProcess = new RelayCommand(CreditCardInfo);
            ProcessMobilePayment = new RelayCommand(ProcessMobileCommerce);
            ShowMobilePayment = new RelayCommand<Book>(book => _navigationService.Navigate("MobilePurchase", XParameters.Create("id", book.Id)), book => book != null);
            SmsMobilePayment = new RelayCommand<Book>(book => _navigationService.Navigate("SmsPurchase", XParameters.Create("id", book.Id)), book => book != null);
            ShowCreditCardView = new RelayCommand<Book>(book => _navigationService.Navigate("CreditCardPurchase", XParameters.Create("id", book.Id)), book => book != null);
        }
        #endregion

        #region Public Properties

        public int Id { get; set; }

        public string FileToken { get; set; }       

        public double AccoundDifferencePrice { get; private set; }

        public UserInformation UserInformation
        {
            get { return _userInformation; }
            private set { SetProperty(ref _userInformation, value, "UserInformation"); }
        }

        public bool SimCardDetected => _deviceInfoService.IsSimCardDetected;

        public bool AccountExist
        {
            get { return _accountExist; }
            private set { SetProperty(ref _accountExist, value, "AccountExist"); }
        }

        public string BlockIndex
        {
            get { return _blockIndex; }
            set
            {
                SetProperty(ref _blockIndex, value, "BlockIndex");
            }
        }

        public bool PinExist
        {
            get { return _pinExist; }
            set { SetProperty(ref _pinExist, value, "PinExist"); }
        }

        public string BookFolderName
        {
            get { return _bookFolderName; }
            set { SetProperty(ref _bookFolderName, value, "BookFolderName"); }
        }

        public LoadingStatus Status
        {
            get { return _status; }
            set { SetProperty(ref _status, value, "Status"); }
        }

        public Exception LoadingException
        {
            get { return _exception; }
            set { SetProperty(ref _exception, value, "LoadingException"); }
        }

        public ReaderSettings ReaderSettings
        {
            get { return _settings; }
            set { SetProperty(ref _settings, value, "ReaderSettings"); }
        }

        public IBookSummaryParser BookSummary { get; set; }

        public Bookmark CurrentBookmark { get; set; }
        
        #endregion

        protected override Task LoadEntity(Session session)
        {
            Id = session.Parameters.GetValue<Book>("BookEntity").Id;
            return Load(new Session(LoadBookPart));
        }

        public bool ReaderLoaded { get; set; }

        #region LoadSettings
        public async Task LoadSettings()
        {
            var session = new Session(SettingsPart);
            await Load(session);
        }

        private async Task LoadSettings(Session session)
        {
            AccountExist = ( _credentialsProvider.ProvideCredentials(CancellationToken.None)) != null;
        }
        #endregion

        #region SaveSettings
        public void SaveSettings()
        {
           _settingsService.SetSettings(ReaderSettings);
        }
        #endregion

        #region AddBookmark
        public async Task AddBookmark(string text, string xpointer, string chapter, bool isCurrent, string percent)
        {
            var session = new Session(AddBookmarkPart);
            session.AddParameter("Text", text);
            session.AddParameter("Xpointer", xpointer);
            session.AddParameter("Chapter", chapter);
            session.AddParameter("IsCurrent", isCurrent);
            session.AddParameter("Percent", percent);
            await Load(session);
        }

        private async Task AddBookmark(Session session)
        {
            string text = session.Parameters.GetValue<string>("Text");
            string xpointer = session.Parameters.GetValue<string>("Xpointer");
            string chapter = session.Parameters.GetValue<string>("Chapter");
            bool isCurrent = session.Parameters.GetValue<bool>("IsCurrent");
            string percent = session.Parameters.GetValue<string>("Percent");
            Bookmark bookmark = CreateBookmark(text, xpointer, chapter, isCurrent, percent);
            CurrentBookmark = bookmark;
            if (bookmark != null)
            {
                await _bookmarksProvider.AddBookmark(bookmark, session.Token);
            }
        }
        #endregion

        #region LoadBook
        private async Task LoadBook(Session session)
        {
            try
            {
                var userInfo = await _profileProvider.GetUserInfo(session.Token);
                Sid = userInfo.SessionId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            try
            {
                await LoadSettings(session);
                OnPropertyChanged(new PropertyChangedEventArgs("IncProgress"));
                var book = await _catalogProvider.GetBook(Id, session.Token);
                OnPropertyChanged(new PropertyChangedEventArgs("IncProgress"));
                Entity = book;

                await LoadBook(book, session);
                _loaded = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async Task LoadBook(Book book, Session session)
        {
            Entity = book;            
            _dataCacheService.PutItem(Entity,"lastreadedbook",session.Token);

            OnPropertyChanged(new PropertyChangedEventArgs("EntityLoaded"));

            LoadBooksIndexes(session);
            OnPropertyChanged(new PropertyChangedEventArgs("IncProgress"));
            if (book != null)
            {
                await LoadBookFile(session, book);
                OnPropertyChanged(new PropertyChangedEventArgs("IncProgress"));
            }

            if (Status == LoadingStatus.FullBookLoaded)
            {
                await _catalogProvider.AddToMyBooks(book, session.Token);
                _expirationGuardian.AddBook(book);
                OnPropertyChanged(new PropertyChangedEventArgs("IncProgress"));
            }
            if (Status == LoadingStatus.TrialBookLoaded)
            {
                await _catalogProvider.AddFragmentToMyBooks(book, session.Token);
                await _catalogProvider.AddFragmentToMyBasket(book, session.Token);
                OnPropertyChanged(new PropertyChangedEventArgs("IncProgress"));
            }

            if (Status == LoadingStatus.FullBookLoaded || Status == LoadingStatus.TrialBookLoaded)
            {
                await _catalogProvider.AddToHistory(book, session.Token);
                OnPropertyChanged(new PropertyChangedEventArgs("IncProgress"));
            }

            if (_networkAvailability.NetworkAvailable)
            {
                try
                {
                    _userInformation = await _profileProvider.GetUserInfo(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            OnPropertyChanged(new PropertyChangedEventArgs("IncProgress"));
        }

        public async void ChangeEntity(Session session)
        {
            if (Entity != null) return;
            XCollection<Book> myBooks = await _catalogProvider.GetAllMyBooks(session.Token);            
            foreach (var book in myBooks.Where(book => book.Id == Id))
            {
                Entity = book;
                return;
            }
        }
        #endregion

        #region CreateBookmark
        private Bookmark CreateBookmark(string text, string xpointer, string chapter, bool isCurrent, string percent)
        {
            if (text.Length > 150)
            {
                text = text.Substring(0, 150);
            }

            if (Entity != null && Entity.Description != null && Entity.Description.Hidden != null &&
                Entity.Description.Hidden.DocumentInfo != null && !String.IsNullOrEmpty(Entity.Description.Hidden.DocumentInfo.Id))
            {
                var bookmark = new Bookmark {ArtId = Entity.Description.Hidden.DocumentInfo.Id};

                //ToDo: fix this hack!
                var expointer = xpointer.Remove(12, 6).Remove(xpointer.Length - 7);
                if (expointer.Contains("."))
                {
                    expointer = expointer.Split('.')[0] + ")";
                }

                if (chapter.Length > 99)
                {
                    chapter = chapter.Substring(0, 99);
                }

                //0 - текущая позиция в тексте
                //1 - закладка
                bookmark.Group = isCurrent ? "0" : "1";
                bookmark.Title = chapter != string.Empty ? chapter : null;
                bookmark.Id = Guid.NewGuid().ToString();
                bookmark.Percent = percent;              
               
                //fb2#xpointer(point(/1/2/ - это указатель на /FictionBook/body в целевом fb2-файле. 
                //Далее следует путь к целевому узлу и после точки - позиция
                bookmark.Selection = xpointer;
                //string timeZoneString = String.Format("{0}{1}:{2:00}", (TimeZoneInfo.Local.BaseUtcOffset >= TimeSpan.Zero) ? "+" : "-", Math.Abs(TimeZoneInfo.Local.BaseUtcOffset.Hours), Math.Abs(TimeZoneInfo.Local.BaseUtcOffset.Minutes));
                //bookmark.LastUpdate = string.Format("{0}{1}", DateTime.Now.ToString( "s", CultureInfo.InvariantCulture ),timeZoneString);               

                bookmark.LastUpdate = DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm:sszzz");
                if (!isCurrent)
                {
                    bookmark.NoteText = new Note {Text = text};
                    bookmark.Class = "default";
                }
                else
                {
                    bookmark.NoteText = new Note { Text = text };
                    bookmark.Percent = percent;                
                }


                //bookmark.ExtractInfo = new Bookmark.Extract();
                //bookmark.ExtractInfo.OriginalLocation = expointer;
                //bookmark.ExtractInfo.SelectionText = text;

                return bookmark;
            }

            return null;
        }
        #endregion

        #region LoadBookFile
        private async Task LoadBookFile(Session session, Book book)
        {
            await LoadFb2BookFile(session, book);
        }

        private async Task LoadFb2BookFile(Session session, Book book)
        {
            string bookFolderName = null;

            Exception exception = null;
            LoadingStatus status = LoadingStatus.BeforeLoaded;
            var credentials = _credentialsProvider.ProvideCredentials(session.Token);
            var exist = _bookProvider.FullBookExistsInLocalStorage(book.Id);
            var existTrial = _bookProvider.TrialBookExistsInLocalStorage(book.Id);
            if (AppSettings.Default.CurrentBook != null && book.Id.ToString() != AppSettings.Default.CurrentBook.BookID)
            {
                AppSettings.Default.CurrentTokenOffset = 0;                
            }
            if (credentials != null || exist)
            {
                try
                {
                    if (exist)
                    {
                        BookSummary = _bookProvider.GetBookFromStorage(book, false);
                    }
                    else if (book.IsMyBook || book.IsFreeBook)
                    {
                        await _bookProvider.GetFullBook(book, session.Token);
                        BookSummary = _bookProvider.GetSummaryParser(book, false);
                    }
                    else if (existTrial)
                    {
                        BookSummary = _bookProvider.GetBookFromStorage(book, true);
                        book.IsMyBook = false;
                    }
                    else
                    {
                        await _bookProvider.GetTrialBook(book, session.Token);
                        BookSummary = _bookProvider.GetSummaryParser(book, true);
                    }
                    status = await LoadLastPosition(book);
                }
                catch (Exception e)
                {
                    exception = e;
                    status = LoadingStatus.NoBookLoaded;
                }
            }

            if (status == LoadingStatus.NoBookLoaded)
            {
                try
                {
                    if (exist)
                    {
                        _bookProvider.GetBookFromStorage(book, true);
                    }
                    else
                    {
                        await _bookProvider.GetTrialBook(book, session.Token);
                    }
                    status = LoadingStatus.TrialBookLoaded;
                }
                catch (Exception e)
                {
                    exception = e;
                    status = LoadingStatus.NoBookLoaded;
                }
            }

            LoadingException = exception;
            Status = status;
            OnPropertyChanged(new PropertyChangedEventArgs("LoadBookProcessCompleted"));
        }

        private async Task<LoadingStatus> LoadLastPosition(Book book)
        {
            var bookmark = await GetCurrentBookmark(false, CancellationToken.None) ??
               await GetCurrentBookmark(true, CancellationToken.None);
            if (bookmark?.NoteText?.Text != null)
            {
                var myBookmark = new BookmarkModel
                {
                    BookID = bookmark.Id,
                    Text = bookmark.NoteText.Text
                };
                AppSettings.Default.LastPositionBookmark = myBookmark;
            }
            else if (bookmark?.Selection != null)
            {
                var text = GetParagraphByXPointer(bookmark?.Selection);
                var myBookmark = new BookmarkModel
                {
                    BookID = bookmark.Id,
                    Text = text
                };
                AppSettings.Default.LastPositionBookmark = myBookmark;
            }
            return book.IsMyBook || book.IsFreeBook ? LoadingStatus.FullBookLoaded : LoadingStatus.TrialBookLoaded;
        }

        #endregion
        #region BuyBookAsync
        public async Task BuyBookAsync()
        {
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
        #endregion
        #region LoadBooksIndexes
        private void LoadBooksIndexes(Session session)
        {
            var indexes =  _dataCacheService.GetItem<XCollection<BookIndex>>("booksindexes");

            BlockIndex = string.Empty;

            var bookIndex = indexes?.FirstOrDefault(x => x.BookId == Entity.Id);

            if (bookIndex != null)
            {
                BlockIndex = bookIndex.BlockIndex;
            }
        }
        #endregion
        #region SetCurrentBookmark
        public void SetCurrentBookmark(string text, string xpointer, string chapter, string percent)
        {
            if (Entity != null && AccountExist)
            {
                var bookmark = CreateBookmark(text, xpointer, chapter, true, percent);

                _bookmarksProvider.SetCurrentBookmarkByDocumentId(Entity.Description.Hidden.DocumentInfo.Id, bookmark, CancellationToken.None);
            }
        }
        #endregion
        #region GetCurrentBookmark
        public async Task<Bookmark> GetCurrentBookmark(bool local, CancellationToken token)
        {
            if (Entity?.Description.Hidden?.DocumentInfo != null && AccountExist)
            {
                try
                {
                    var bookmark = await _bookmarksProvider.GetCurrentBookmarkByDocumentId(Entity.Description.Hidden.DocumentInfo.Id, local, token);
                    if (bookmark != null)
                    {
                        var indexes = _dataCacheService.GetItem<XCollection<BookIndex>>("booksindexes") ?? new XCollection<BookIndex>();

                        var bookIndex = indexes.FirstOrDefault(x => x.BookId == Entity.Id);

                        if (bookIndex != null)
                        {
                            DateTime lastupdate = Convert.ToDateTime(bookmark.LastUpdate);

                            if (bookIndex.SaveDateTime < lastupdate)
                            {
                                return bookmark;
                            }
                        }
                        else
                        {
                            return bookmark;
                        }
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return null;
        }
        #endregion
        #region SaveBooksIndexes
        public void SaveBooksIndexes()
        {
            if (Entity != null)
            {
                var indexes =  _dataCacheService.GetItem<XCollection<BookIndex>>("booksindexes") ?? new XCollection<BookIndex>();

                var bookIndex = indexes.FirstOrDefault(x => x.BookId == Entity.Id);

                if (bookIndex != null)
                {
                    bookIndex.BlockIndex = BlockIndex;
                    bookIndex.SaveDateTime = DateTime.Now;
                }
                else
                {
                    indexes.Add(new BookIndex { BlockIndex = BlockIndex, BookId = Entity.Id, SaveDateTime = DateTime.Now });
                }

                _dataCacheService.PutItem(indexes, "booksindexes", CancellationToken.None);
            }
        }
        #endregion
        #region Reload
        public async Task Reload()
        {
            SaveBooksIndexes();
            Session session = new Session(ReloadBookPart);
            await Load(session);
        }

        private async Task Reload(Session session)
        {
            await LoadBook(Entity, session);
        }
        #endregion

        #region UpdateEntity
        public async Task UpdateEntity()
        {
            if (Entity != null)
            {
                var books = _dataCacheService.GetItem<XCollection<Book>>("mybooks");
                if (books != null)
                {
                    for (int i = 0; i < books.Count; ++i)
                    {
                        if (books[i].Id == Entity.Id) { books[i] = Entity; break; }
                    }

                    _dataCacheService.PutItem(books, "mybooks", CancellationToken.None);
                }
            }
        }
        #endregion

        private async void BuyBookFromLitresAsync()
        {
            await Load(new Session(BuyBookLitresPart));
        }

#warning READER_VIEW_MODEL_BuyBookFromLitres_CHECK_IF_IT_WORKS
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
                //CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, (() =>
                //{
                //    new MessageDialog("Авторизируйтесь, пожалуйста.").ShowAsync();
                //}));


                //Deployment.Current.Dispatcher.BeginInvoke(() =>
                //{
                //    MessageBox.Show("Авторизируйтесь, пожалуйста.");
                //});
            }
            if (userInfo == null) return;
            if (userInfo.Account - book.Price >= 0)
            {
                var dialog = new ContentDialog
                {
                    Title = "Подтвердите покупку",
                    Content = String.Format("Подтвердите покупку книги за {0} руб.", Entity.Price),
                    PrimaryButtonText = "купить",
                    SecondaryButtonText = "отмена",
                    IsSecondaryButtonEnabled = true,
                    IsPrimaryButtonEnabled = true
                };
                var dialogResult = await dialog.ShowAsync();
                if (dialogResult == ContentDialogResult.Primary)
                {
                    await _litresPurchaseService.BuyBookFromLitres(book, session.Token);
                }

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
                OnPropertyChanged(new PropertyChangedEventArgs("ShowPopup"));
            }
        }

        private async void BuyBookFromMicrosoftAsync()
        {
            OnPropertyChanged(new PropertyChangedEventArgs("HidePopup"));
            try
            {
                await Load(new Session(BuyBookPart));
            }
            catch (Exception)
            {
                // ignored
            }
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

            if (!userInfo.CanRebill.Equals("0") && !string.IsNullOrEmpty(cred?.UserId) && userInfo.UserId == cred.UserId && !String.IsNullOrEmpty(cred.CanRebill) && !cred.CanRebill.Equals("0"))
            {
                var box = new Dictionary<string, object> { { "isSave", true }, { "isAuth", false } };
                var param = XParameters.Empty.ToBuilder()
                    .AddValue("Id", Entity.Id)
                    .AddValue("Operation", (int) AccountDepositViewModel.AccountDepositOperationType.AccountDepositOperationTypeCreditCard)
                    .AddValue("ParametersDictionary", ModelsUtils.DictionaryToString(box))
                    .ToImmutable();
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
            var userInfo = await _profileProvider.GetUserInfo(CancellationToken.None, true);
            if (userInfo == null) return;
            if (_userInformation == null) _userInformation = userInfo;
            AccoundDifferencePrice = Entity.Price - userInfo.Account;
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

            if (String.IsNullOrEmpty(userInfo.Phone))
            {
                ShowMobilePayment.Execute(Entity);
            }
            else
            {
                var param = XParameters.Empty.ToBuilder()
                    .AddValue("Id", Entity.Id)
                    .AddValue("Operation", (int)AccountDepositViewModel.AccountDepositOperationType.AccountDepositOperationTypeMobile)
                    .ToImmutable();

                _navigationService.Navigate("AccountDeposit", param);
            }
        }

        public async Task<int> LoadPdfBookPosition()
        {
            var filename = string.Format("save_{0}", Entity.Id);

            if (Entity != null && !Entity.IsMyBook)
            {
                filename += "_trial"; 
            }

            var path = Path.Combine("CachedFiles", filename);

            if (_fileCacheService.FileExists(path))
            {
                 var  stream = await _fileCacheService.OpenFile(path, CancellationToken.None);
                 var savedStr = new StreamReader(stream).ReadToEnd();
                try
                {
                    int result;
                    Int32.TryParse(savedStr, out result);
                    return result;
                }
                catch (Exception ex)
                {      
                    Debug.WriteLine(ex.Message);                                 
                }
            }
            return -1;
        }

        public void SavePdfBookPosition(int pageNumber)
        {
            var filename = string.Format("save_{0}", Entity.Id);

            if (Entity != null && !Entity.IsMyBook)
            {
                filename += "_trial";
            }

            var path = Path.Combine("CachedFiles", filename);
            if (_fileCacheService.FileExists(path)) _fileCacheService.DeleteFile(path);

            var str = pageNumber.ToString();
            var bts = Encoding.UTF8.GetBytes(str);
            var ms = new MemoryStream(bts);
            ms.Seek(0, SeekOrigin.Begin);
            _fileCacheService.SaveFile(ms, path, CancellationToken.None);
        }

        public async void UpdateExistBook(Book book)
        {
            await _catalogProvider.UpdateExistBook(book);
        }

        const string XpointerStartMagicWord = "fb2#xpointer(point(";
        const string XpointerEndMagicWord = "))";
        public static string XpointerFormatString { get; } = XpointerStartMagicWord + "{0}" + XpointerEndMagicWord;

        public string GetNewXPointer(string pointer)
        {
            string NewXpointerStartMagicWord = "fb2#xpointer(point(/1/";
            string NewXpointerEndMagicWord = ".1))";
            return string.Format(NewXpointerStartMagicWord + "{0}" + NewXpointerEndMagicWord, pointer);
        }

        public string GetXPointer(string pattern)
        {
            var root = BookSummary.Root;
            XAttribute attribute = root.Attribute("xmlns");
            XNamespace ns = attribute.Value;
            var body = root.Element(ns + "body");
            var block = FindFirst(body, pattern);
            StringBuilder buffer = new StringBuilder();
            bool hasPoint = false;
            if (block != null)
            {
                try
                {
                    while (block != null)
                    {
                        if (!block.HasElements)
                        {
                            var foundString = block.Value.Replace(Convert.ToChar(160).ToString(), " ");
                            int position = foundString.IndexOf(pattern, StringComparison.Ordinal);
                            if (position > 0)
                            {
                                buffer.Insert(0, (position + 1).ToString());
                                buffer.Insert(0, ".");
                                hasPoint = true;
                            }
                        }
                        buffer.Insert(0, (block.ElementsBeforeSelf().Count() + 1).ToString());
                        buffer.Insert(0, "/");
                        block = block.Parent;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
            if (!hasPoint)
                buffer.Append(".1");
            return string.Format(XpointerFormatString, buffer);
        }

        private XElement FindFirst(XElement parent, string pattern)
        {
            try
            {
                foreach (var element in parent.Elements())
                {
                    if (!element.HasElements)
                    {
                        if (element.Value.Contains("горести"))
                            Debug.WriteLine("asdasd");
                        var tmp = pattern.Replace(" ", string.Empty).Replace(Convert.ToChar(160).ToString(), "");
                        var elemText = element.Value.Replace(" ", "").Replace(Convert.ToChar(160).ToString(), "");
                        if (elemText != string.Empty && elemText.Contains(tmp))
                            return element;
                    }
                    else
                    {
                        var block = FindFirst(element, pattern);
                        if (block != null) return block;
                    }
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string GetParagraphByXPointer(string pointer)
        {
            pointer = pointer.Replace(XpointerStartMagicWord, string.Empty).Replace(XpointerEndMagicWord,string.Empty).Remove(0,4);
            var index = pointer.IndexOf('.');
            pointer = pointer.Remove(index);
            var root = BookSummary.Root;
            XAttribute attribute = root.Attribute("xmlns");
            XNamespace ns = attribute.Value;
            var body = root.Element(ns + "body");
            var block = FindBlock(pointer, body);
            return block?.Value;
        }

        private XElement FindBlock(string pointer, XElement parent)
        {
            var index = pointer.IndexOf('/');
            pointer = pointer.Remove(0, index + 1);            
            var array = pointer.Split('/');
            string level = array[0];
      //      level = level.Replace("/", string.Empty);
            var intLevel = int.Parse(level);
            intLevel--;
            var idx = 0;
            XElement block = null;
            foreach (var child in parent.Elements())
            {
               // if ()
                if (idx == intLevel)
                {
                    if (!pointer.Contains("/"))
                        return child;
                    if (child.HasElements) block = FindBlock(pointer, child);
                    else return child;
                }
                idx++;
            }
            return block;
        }
    }
}
