using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Digillect;
using Digillect.Collections;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Models;
using LitRes.Models.JsonModels;
using LitRes.Services;
using LitRes.Services.Connectivity;
using Genre = LitRes.Models.JsonModels.Genre;

namespace LitRes.ViewModels
{
	public class SearchViewModel : ViewModel
	{
		private const string LoadAllDataPart = "LoadAllData";
        private const string BuyBookLitresPart = "BuyBookLitresPart";
        private const string CreditCardInfoPart = "CreditCardInfoPart";



        private readonly ICatalogProvider _catalogProvider;
		private readonly INavigationService _navigationService;
        private readonly ILitresPurchaseService _litresPurchaseService;
        private readonly ICredentialsProvider _credentialsProvider;
        private readonly IProfileProvider _profileProvider;


        private string _searchQuery;
		private string _foundedCount;
	    private bool _isTagsAndGenresLoaded;
	    private List<object> _bestResult;
        private UserInformation _userInformation;


        #region Public Properties
        public XCollection<Book> FoundBooks { get; private set; }
        public XSubRangeCollection<Book> FirstBooks { get; private set; }
        public Book Book { get; private set; }

        public List<object> BestResult
	    {
	        get { return _bestResult; }
	        private set
	        {
                SetProperty(ref _bestResult, value, "BestResult");
            }
	    }

	    public List<object> TagsAndGenresCollection { get; private set; }

        public XCollection<Book.SequenceInfo> FoundSequeses { get; private set; }
        public XSubRangeCollection<Book.SequenceInfo> FirstSequeces { get; private set; }

        //public XCollection<Book.CollectionsInfo> FoundCollections { get; private set; }
        //public XSubRangeCollection<Book.CollectionsInfo> FirstCollections { get; private set; }

        public XCollection<Person> FoundPersons { get; private set; }
        public XSubRangeCollection<Person> FirstPersons { get; private set; }

        public string SearchQuery
		{
			get { return _searchQuery; }
			set { SetProperty( ref _searchQuery, value, "SearchQuery" ); }
		}

        public string FoundedCount
        {
            get { return _foundedCount; }
            set { SetProperty(ref _foundedCount, value, "FoundedCount"); }
        }

        public bool IsTagsAndGenresLoaded
        {
            get { return _isTagsAndGenresLoaded; }
            set { SetProperty(ref _isTagsAndGenresLoaded, value, "IsTagsAndGenresLoaded"); }
        }

        public bool IsBestResultExists
        {
            get
            {
                if (AllResultsObject == null) return false;
                if (AllResultsObject.bestResult is Art) return true;
                if (AllResultsObject.bestResult is Author) return true;
                if (AllResultsObject.bestResult is Series) return true;
                if (AllResultsObject.bestResult is Tag) return true;
                if (AllResultsObject.bestResult is Genre) return true;
                //if (AllResultsObject.bestResult is Collection) return true;

                return false;
            }
        }

        public int MoreSequencesCount
        {
            get
            {
                if (FoundSequeses.Count == FirstSequeces.Count) return 0;
                if (FoundSequeses.Count > 3) return FoundSequeses.Count - 3;
                return 0;
            }
        }

        //public int MoreCollectionsCount
        //{
        //    get
        //    {
        //        if (FoundCollections.Count == FirstCollections.Count) return 0;
        //        if (FoundCollections.Count > 3) return FoundCollections.Count - 3;
        //        return 0;
        //    }
        //}

        public int MoreOtherBooksCount
	    {
	        get
	        {
                if (FoundBooks.Count == FirstBooks.Count) return 0;
                if (FoundBooks.Count > 3) return FoundBooks.Count - 3;
	            return 0;
	        }
	    }

        public int MorePersonsCount
        {
            get
            {
                if (FirstPersons.Count == FoundPersons.Count) return 0;
                if (FoundPersons.Count > 3) return FoundPersons.Count - 3;
                return 0;
            }
        }

        public Rootobject AllResultsObject { get; set; }

        public RelayCommand<Book> BookSelected { get; private set; }
        public RelayCommand<Person> PersonSelected { get; private set; }
        public RelayCommand<Tag> TagSelected { get; private set; }
        public RelayCommand<Genre> GenreSelected { get; private set; }
        public RelayCommand<Book.SequenceInfo> SequenceSelected { get; private set; }
        public RelayCommand<Book> BuyBook { get; private set; }
        public RelayCommand RunCreditCardPaymentProcess { get; private set; }
        public RelayCommand<Book> ShowCreditCardView { get; private set; }
        public double AccoundDifferencePrice { get; private set; }


        //public RelayCommand<Book.SequenceInfo> CollectionSelected { get; private set; }

        public RelayCommand LoadMoreFoundBooks { get; private set; }
        #endregion

        #region Constructors/Disposer
        public SearchViewModel(ICatalogProvider catalogProvider, INavigationService navigationService, ILitresPurchaseService litresPurchaseService, ICredentialsProvider credentialsProvider, IProfileProvider profileProvider)
        {
            _catalogProvider = catalogProvider;
            _navigationService = navigationService;
            _litresPurchaseService = litresPurchaseService;
            _credentialsProvider = credentialsProvider;
            _profileProvider = profileProvider;

            FoundBooks = new XCollection<Book>();
            FoundPersons = new XCollection<Person>();
            FoundSequeses = new XCollection<Book.SequenceInfo>();
           
            FirstBooks = new XSubRangeCollection<Book>(FoundBooks, 0, 3);
            FirstPersons = new XSubRangeCollection<Person>(FoundPersons, 0, 3);
            FirstSequeces = new XSubRangeCollection<Book.SequenceInfo>(FoundSequeses, 0, 3);
            //FoundCollections = new XCollection<Book.CollectionsInfo>();
            //FirstCollections = new XSubRangeCollection<Book.CollectionsInfo>(FoundCollections, 0, 3);

            TagsAndGenresCollection = new List<object>(6);
            BestResult = new List<object>();

            RegisterAction(LoadAllDataPart).AddPart(SearchAll, session => true);
            RegisterAction(BuyBookLitresPart).AddPart((session) => BuyBookFromLitres(session, Book), (session) => true);
            RegisterAction(CreditCardInfoPart).AddPart(session => CreditCardInfoAsync(session), (session) => true);

            BuyBook = new RelayCommand<Book>(book => BuyBookFromLitresAsync(book));
            RunCreditCardPaymentProcess = new RelayCommand(CreditCardInfo);
            ShowCreditCardView = new RelayCommand<Book>(book => _navigationService.Navigate("CreditCardPurchase", XParameters.Create("BookEntity", book)), book => book != null);

            BookSelected = new RelayCommand<Book>(book => _navigationService.Navigate("Book", XParameters.Create("BookEntity", book)), book => book != null);
            PersonSelected = new RelayCommand<Person>(person => _navigationService.Navigate("Person", XParameters.Create("Id", person.Id)), person => person != null);
            GenreSelected = new RelayCommand<Genre>(genre => _navigationService.Navigate("BooksByCategory", XParameters.Empty.ToBuilder()
                .AddValue("category", 6)
                .AddValue("id", int.Parse(genre.id))
                .AddValue("title", genre.name)
                .ToImmutable()), genre => genre != null);
            TagSelected = new RelayCommand<Tag>(tag => _navigationService.Navigate("BooksByCategory", XParameters.Empty.ToBuilder()
                .AddValue("category", 5)
                .AddValue("id", int.Parse(tag.id))
                .AddValue("title", tag.name)
                .ToImmutable()), tag => tag != null);
            SequenceSelected = new RelayCommand<Book.SequenceInfo>(sequence => _navigationService.Navigate("BooksByCategory", XParameters.Empty.ToBuilder()
                .AddValue("category", 7)
                .AddValue("id", sequence.Id)
                .AddValue("title", sequence.Name)
                .ToImmutable()), sequence => sequence != null);

            //CollectionSelected = new RelayCommand<Book.SequenceInfo>(sequence => _navigationService.Navigate("BooksByCategory", XParameters.Empty.ToBuilder()
            //    .AddValue("category", 8)
            //    .AddValue("id", sequence.Id)
            //    .AddValue("title", sequence.Name)
            //    .ToImmutable()), sequence => sequence != null);

            LoadMoreFoundBooks = new RelayCommand(() => Load(new Session()));
        }
        #endregion

        #region SearchBooks

        public async Task SearchBooks()
        {
            Debug.WriteLine("SearchBooks");
            await Load(new Session(LoadAllDataPart));
        }
        
        private async Task SearchAll(Session session)
	    {
            try
            {
                IsTagsAndGenresLoaded = false;
                TagsAndGenresCollection.Clear();
                OnPropertyChanged(new PropertyChangedEventArgs("TagsAndGenresCollection"));

                BestResult.Clear();
                OnPropertyChanged(new PropertyChangedEventArgs("BestResult"));
                AllResultsObject = null;
                OnPropertyChanged(new PropertyChangedEventArgs("IsBestResultExists"));

                FoundBooks.Clear();
                OnPropertyChanged(new PropertyChangedEventArgs("FoundBooks"));
                FoundPersons.Clear();
                OnPropertyChanged(new PropertyChangedEventArgs("FoundPersons"));
                FoundSequeses.Clear();
                OnPropertyChanged(new PropertyChangedEventArgs("FoundSequeses"));
                //FoundCollections.Clear();
                //OnPropertyChanged(new PropertyChangedEventArgs("FoundCollections"));

                FoundedCount = string.Empty;
                OnPropertyChanged(new PropertyChangedEventArgs("FoundedCount"));
                OnPropertyChanged(new PropertyChangedEventArgs("MoreOtherBooksCount"));
                OnPropertyChanged(new PropertyChangedEventArgs("MorePersonsCount"));
                OnPropertyChanged(new PropertyChangedEventArgs("MoreSequencesCount"));
                //OnPropertyChanged(new PropertyChangedEventArgs("MoreCollectionsCount"));

                FirstPersons = new XSubRangeCollection<Person>(FoundPersons, 0, 3);
                FirstSequeces = new XSubRangeCollection<Book.SequenceInfo>(FoundSequeses, 0, 3);
                //FirstCollections = new XSubRangeCollection<Book.CollectionsInfo>(FoundCollections, 0, 3);
                FirstBooks = new XSubRangeCollection<Book>(FoundBooks, 0, 3);

                AllResultsObject = await _catalogProvider.SearchAll(30, SearchQuery, session.Token);

                if (AllResultsObject != null)
                {
                    SortBestResult();

                    FoundSequeses.BeginUpdate();
                    FoundSequeses.Update(AllResultsObject.GetSequences());
                    FoundSequeses.EndUpdate();
                    OnPropertyChanged(new PropertyChangedEventArgs("MoreSequencesCount"));

                    //FoundCollections.BeginUpdate();
                    //FoundCollections.Update(AllResultsObject.GetCollection());
                    //FoundCollections.EndUpdate();
                    //OnPropertyChanged(new PropertyChangedEventArgs("FoundCollections"));

                    InitGenresAndTagsList();
                    FoundedCount = AllResultsObject.FoundResults();

                    var books = AllResultsObject.GetBooks();
                    _catalogProvider.CheckMyBooks(books);

                    FoundBooks.BeginUpdate();
                    FoundBooks.Update(books);
                    FoundBooks.EndUpdate();
                    OnPropertyChanged(new PropertyChangedEventArgs("MoreOtherBooksCount"));

                    FoundPersons.BeginUpdate();
                    FoundPersons.Update(AllResultsObject.GetPersons());
                    FoundPersons.EndUpdate();
                    OnPropertyChanged(new PropertyChangedEventArgs("MorePersonsCount"));

                    InitBestResult();
                    if (Convert.ToInt32(FoundedCount) > 0)
                        OnPropertyChanged(new PropertyChangedEventArgs("Found"));
                    else
                        OnPropertyChanged(new PropertyChangedEventArgs("NotFound"));
                }
                else
                {
                    OnPropertyChanged(new PropertyChangedEventArgs("NotFound"));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                OnPropertyChanged(new PropertyChangedEventArgs("NotFound"));
            }
	    }

	    private void SortBestResult()
	    {
	        if (AllResultsObject.authors != null && AllResultsObject.authors.Length > 0)
	        {
	            AllResultsObject.bestResult = AllResultsObject.authors[0];
                return;
	        }
            if (AllResultsObject.series != null && AllResultsObject.series.Length > 0)
            {
                AllResultsObject.bestResult = AllResultsObject.series[0];
                return;
            }
            if (AllResultsObject.genres != null && AllResultsObject.genres.Length > 0)
            {
                AllResultsObject.bestResult = AllResultsObject.genres[0];
                return;
            }
            if (AllResultsObject.arts != null && AllResultsObject.arts.Length > 0)
            {
                AllResultsObject.bestResult = AllResultsObject.arts[0];
                return;
            }
        }

	    private void InitGenresAndTagsList()
	    {
            TagsAndGenresCollection.Clear();
	        
            try
	        {
	            if (AllResultsObject?.genres?.Length > 0)
	            {
	                var best = AllResultsObject.bestResult;
                    var genresCount = AllResultsObject.genres.Length > 3 ? 3 : AllResultsObject.genres.Length;
	                if (best is Genre)
	                {
                        for(int i = 0; i < genresCount; ++i) if(!AllResultsObject.genres[i].Equals(best)) TagsAndGenresCollection.Add(AllResultsObject.genres[i]);
	                }
	                else
	                {
                        TagsAndGenresCollection.AddRange(AllResultsObject.genres.Take(genresCount));
	                }
	            }
	        }
	        catch (Exception ex)
	        {
	            Debug.WriteLine(ex.Message);
	        }

	        try
	        {
	            if (AllResultsObject?.tags?.Length > 0)
	            {
                    var best = AllResultsObject.bestResult;
                    var tagsCount = AllResultsObject.tags.Length > 3 ? 3 : AllResultsObject.tags.Length;
	                if (best is Tag)
	                {
	                    for (int i = 0; i < tagsCount; ++i)
	                        if (!AllResultsObject.tags[i].Equals(best))
	                            TagsAndGenresCollection.Add(AllResultsObject.tags[i]);
	                }
	                else
	                {
	                    TagsAndGenresCollection.AddRange(AllResultsObject.tags.Take(tagsCount));
	                }
	            }
	        }
	        catch (Exception ex)
	        {
	            Debug.WriteLine(ex.Message);
	        }
	        if(TagsAndGenresCollection.Count > 0)
            { 
                TagsAndGenresCollection.Sort((a, b) =>
                {
                   int object1Length = (a is Genre)?((Genre)a).name.Length : ((Tag) a).name.Length;
                   int object2Length = (b is Genre) ? ((Genre)b).name.Length : ((Tag)b).name.Length;
                    return object1Length > object2Length ? -1 : 1;
                });

                IsTagsAndGenresLoaded = true;
                OnPropertyChanged(new PropertyChangedEventArgs("TagsAndGenresLoaded"));
            }
        }
        
		#endregion

	    private void InitBestResult()
	    { 
            Debug.WriteLine("InitBestResult");
	        try
	        {
	            if (IsBestResultExists)
	            {
	                BestResult.Clear();

	                OnPropertyChanged(new PropertyChangedEventArgs("IsBestResultExists"));

	                if (AllResultsObject.bestResult is Art)
	                {
	                    var tmpId = int.Parse(((Art) AllResultsObject.bestResult).id);
	                    var bestResultBook = FoundBooks.FirstOrDefault(book => tmpId == book.Id) ??
	                                         ((Art) AllResultsObject.bestResult).ToBook();

	                    BestResult = new List<object> {bestResultBook};

                        FoundBooks.BeginUpdate();
	                    FoundBooks.Remove(bestResultBook);
                        FoundBooks.EndUpdate();

                        OnPropertyChanged(new PropertyChangedEventArgs("BestResult"));
	                    //OnPropertyChanged(new PropertyChangedEventArgs("FoundBooks"));
	                }
	                else if (AllResultsObject.bestResult is Author)
	                {
	                    var bestResultPerson = ((Author) AllResultsObject.bestResult).ToPerson();
	                    bestResultPerson = FoundPersons.FirstOrDefault(person => person.Id.Equals(bestResultPerson.Id)) ??
	                                       bestResultPerson;
	                    BestResult = new List<object> {bestResultPerson};

                        FoundPersons.BeginUpdate();
                        FoundPersons.Remove(bestResultPerson);
                        FoundPersons.EndUpdate();

                        OnPropertyChanged(new PropertyChangedEventArgs("BestResult"));
	                    //OnPropertyChanged(new PropertyChangedEventArgs("FoundPersons"));
	                }
	                else if (AllResultsObject.bestResult is Series)
	                {
	                    var tmpId = int.Parse(((Series) AllResultsObject.bestResult).id);
	                    var bestResultSeries = FoundSequeses.FirstOrDefault(sequence => sequence.Id == tmpId);
	                    BestResult = new List<object> {bestResultSeries};

                        FoundSequeses.BeginUpdate();
	                    FoundSequeses.Remove(bestResultSeries);
                        FoundSequeses.EndUpdate();

                        OnPropertyChanged(new PropertyChangedEventArgs("BestResult"));
	                    OnPropertyChanged(new PropertyChangedEventArgs("FoundSequeses"));
	                }
	                //else if (AllResultsObject.bestResult is Collection)
	                //{
	                //    var tmpId = int.Parse(((Collection) AllResultsObject.bestResult).id);
	                //    var bestResultCollection = FoundCollections.FirstOrDefault(sequence => sequence.Id == tmpId);
	                //    BestResult = new List<object> {bestResultCollection};
	                //    FoundCollections.Remove(bestResultCollection);
	                //    OnPropertyChanged(new PropertyChangedEventArgs("BestResult"));
	                //    OnPropertyChanged(new PropertyChangedEventArgs("FoundSequeses"));
	                //}
	                else if (AllResultsObject.bestResult is Tag)
	                {
	                    var bestResultTag = (Tag) AllResultsObject.bestResult;
	                    BestResult = new List<object> {bestResultTag};
	                    OnPropertyChanged(new PropertyChangedEventArgs("BestResult"));
	                }
	                else if (AllResultsObject.bestResult is Genre)
	                {
	                    var bestResultGenre = (Genre) AllResultsObject.bestResult;
	                    BestResult = new List<object> {bestResultGenre};
	                    OnPropertyChanged(new PropertyChangedEventArgs("BestResult"));
	                }

	                OnPropertyChanged(new PropertyChangedEventArgs("MoreOtherBooksCount"));
	                OnPropertyChanged(new PropertyChangedEventArgs("MorePersonsCount"));
	                OnPropertyChanged(new PropertyChangedEventArgs("MoreSequencesCount"));
	                //OnPropertyChanged(new PropertyChangedEventArgs("MoreCollectionsCount"));
  
	            }
	        }
	        catch (Exception ex)
	        {
	            Debug.WriteLine(ex.Message);
	        }
	    }

	    public void ShowAllPersons()
	    {
            FirstPersons = new XSubRangeCollection<Person>(FoundPersons,0, FoundPersons.Count);
            OnPropertyChanged(new PropertyChangedEventArgs("MorePersonsCount"));
            OnPropertyChanged(new PropertyChangedEventArgs("FirstPersons"));
        }

        public void ShowAllSequences()
        {
            FirstSequeces = new XSubRangeCollection<Book.SequenceInfo>(FoundSequeses, 0, FoundSequeses.Count);
            OnPropertyChanged(new PropertyChangedEventArgs("MoreSequencesCount"));
            OnPropertyChanged(new PropertyChangedEventArgs("FirstSequeces"));
        }

        //public void ShowAllCollections()
        //{
        //    FirstCollections = new XSubRangeCollection<Book.CollectionsInfo>(FoundCollections, 0, FoundCollections.Count);
        //    OnPropertyChanged(new PropertyChangedEventArgs("MoreCollectionsCount"));
        //    OnPropertyChanged(new PropertyChangedEventArgs("FirstCollections"));
        //}

        public void ShowAllBooks()
        {
            FirstBooks = new XSubRangeCollection<Book>(FoundBooks, 0, FoundBooks.Count);
            OnPropertyChanged(new PropertyChangedEventArgs("MoreOtherBooksCount"));
            OnPropertyChanged(new PropertyChangedEventArgs("FirstBooks"));
        }


        public void RefreshBook(Book book)
        {
            if (FoundBooks.Count > 0)
            {
                for (int i = 0; i < FoundBooks.Count; ++i)
                {
                    if (FoundBooks[i].Id == book.Id)
                    {
                        FoundBooks.BeginUpdate();
                        FoundBooks[i] = book;
                        FoundBooks.EndUpdate();
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
