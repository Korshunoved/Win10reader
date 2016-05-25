using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Digillect.Mvvm;
using Digillect.Mvvm.UI;
using LitRes.LibraryTools;
using LitRes.Models;
using LitRes.ValueConverters;
using LitRes.ViewModels;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.System;
using Windows.System.Display;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Autofac;
using BookParser;
using BookParser.Fonts;
using BookParser.Models;
using BookParser.Parsers;
using BookRender.RenderData;
using BookRender.Tools;
using Digillect;
using Digillect.Mvvm.Services;
using LitResReadW10;
using LitResReadW10.Controllers;
using LitResReadW10.Controls;
using LitResReadW10.Controls.Manipulators;
using LitResReadW10.Helpers;
using LitResReadW10.Views;

namespace LitRes.Views
{
    [View("Reader")]
    [ViewParameter("bookmark", Required = false)]
    [ViewParameter("chapter", Required = false)]
    [ViewParameter("BookEntity", typeof(Models.Book), Required = true)]
    [ViewParameter("filetoken", Required = false)]

    public partial class Reader : IExpiredCallBack
    {
        private bool _readerGridLoaded;
        private bool _isLoaded;
        private IFontHelper _activeFontHelper;
        private BookModel _book;
        private ReadController _readController;
        private BookSearch _bookSearch;
        private int _tokenOffset;
        public int CurrentPage;
        private readonly INavigationService _navigationService = ((App)Application.Current).Scope.Resolve<INavigationService>();
        private double _pageSliderValue;
        private LinkRenderData _link;
        private ChapterModel _currentChapter;
        private DisplayRequest _displayRequest;
        private bool _isNeedToShowBuyFullBook;
        public bool IsHardwareBack => ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons");

        public static Reader Instance;

        private readonly BookTool _bookTool;

        private readonly SemaphoreSlim _event = new SemaphoreSlim(1, 1);

        #region Constructors/Disposer
        public Reader()
        {
            Debug.WriteLine("Reader()");    

            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
         
            Loaded += ReaderLoaded;
            Unloaded += (sender, args) =>
            {
                LocalBroadcastReciver.Instance.PropertyChanging -= Instance_PropertyChanging;
                _readerGridLoaded = false;
                _isLoaded = false;
            };
            LocalBroadcastReciver.Instance.PropertyChanging += Instance_PropertyChanging;
            DisplayInformation.GetForCurrentView().OrientationChanged += OnOrientationChanged;         
            if (ExpirationGuardian.Instance != null) ExpirationGuardian.Instance.AddCallBack(this);
            _bookTool = new BookTool();
            ReaderGrid.Opacity = 0;
            BookCoverBack.Visibility = Visibility.Visible;
            BookTitleTextBlock.Text = "";
        }

        #endregion

        #region ReaderLoaded
        async void ReaderLoaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadSettings();            
            Instance = this;
            _tokenOffset = AppSettings.Default.CurrentTokenOffset;            
            SetColorScheme();
            //PagesTextBlock.Visibility = Visibility.Collapsed;
            ReaderGrid.Loaded -= ReaderGridOnLoaded;
            ReaderGrid.Loaded += ReaderGridOnLoaded;
            AnchorStackPanel.MaxHeight = 350;
            var currentOrientation = DisplayInformation.GetForCurrentView().CurrentOrientation;
            DisplayInformation.AutoRotationPreferences = AppSettings.Default.Autorotate ? DisplayOrientations.None : currentOrientation;
            if (SystemInfoHelper.IsDesktop()) return;
            var hideBar = AppSettings.Default.HideStatusBar;
            var statusBar = StatusBar.GetForCurrentView();
            statusBar.BackgroundColor = AppSettings.Default.ColorScheme.PanelBackgroundBrush.Color;
            statusBar.ForegroundColor = AppSettings.Default.ColorScheme.TextForegroundBrush.Color;
            if (!hideBar) return;
            await statusBar.HideAsync();            
        }

        private void SetColorScheme()
        {
            LayoutRoot.Background = AppSettings.Default.ColorScheme.BackgroundBrush;
            MobileTop.Background = AppSettings.Default.ColorScheme.PanelBackgroundBrush;
            Bottom.Background = AppSettings.Default.ColorScheme.PanelBackgroundBrush;
            BookTitleStackPanel.Background = AppSettings.Default.ColorScheme.PanelBackgroundBrush;
            var deviceWidth = Window.Current.CoreWindow.Bounds.Width;
            var margin = (int) (deviceWidth*0.03f);
            BookTitleTextBlock.Width = deviceWidth - margin*2;
            BookTitleTextBlock.HorizontalAlignment = HorizontalAlignment.Center;
            BookTitleTextBlock.Margin = new Thickness(margin, 0, margin, 0);
            BookTitleTextBlock.Foreground = AppSettings.Default.ColorScheme.BookTitleBrush;            
            PagesTextBlock.Foreground = AppSettings.Default.ColorScheme.TextForegroundBrush;
            PagesTextBlock.FontFamily = AppSettings.Default.FontSettings.FontFamily;
            ChapterTextBlock.Foreground = AppSettings.Default.ColorScheme.ChapterTextBrush;
            SecondCurrentPageRun.Foreground = AppSettings.Default.ColorScheme.CurrentPageColorBrush;
            SecondTotalPagesRun.Foreground = AppSettings.Default.ColorScheme.ChapterTextBrush;
        }

        private async void ReaderGridOnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            if (_readerGridLoaded) return;
            _readerGridLoaded = true;
            if (AppSettings.Default.CurrentBook != null)
                await HandleLoadedBook();
        }

        #endregion

        #region CreateDataSession
        protected override Session CreateDataSession(DataLoadReason reason)
        {
            ViewModel.Id = ViewParameters.GetValue<int>("id");
            ViewModel.FileToken = ViewParameters.GetValue<string>("filetoken");

            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;           
            Debug.WriteLine("Reader CreateDataSession");

            return base.CreateDataSession(reason);
        }
        #endregion

        protected override async void OnDataLoadComplete(Session session)
        {
            Debug.WriteLine("OnDataLoadComplete Enter");
            if (!_readerGridLoaded) return;
            try
            {
                await HandleLoadedBook();
            }
            catch (Exception)
            {
                await ShowDownloadError();                    
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ShowPopup":
                    ChoosePaymentMethod();
                    break;
                case "HidePopup":

                    break;
                case "ShowSwitchPopup":
                    break;
                case "HideSwitchPopup":
                    break;
                case "UpdatePrice":
                    break;
                case "LoadBookProcessCompleted":
                    if (ViewModel.Status == ReaderViewModel.LoadingStatus.FullBookLoaded) Analytics.Instance.sendMessage(Analytics.ActionReadFull);
                    else if (ViewModel.Status == ReaderViewModel.LoadingStatus.TrialBookLoaded) Analytics.Instance.sendMessage(Analytics.ActionReadFragment);
                    _isLoaded = true;
                    _book = AppSettings.Default.CurrentBook;
                    BusyGrid.Visibility = Visibility.Collapsed;
                    if (_book == null) return;
                    _activeFontHelper = BookFactory.GetActiveFontMetrics(AppSettings.Default.FontSettings.FontFamily.Source);
                    AppSettings.Default.FontSettings.FontHelper = _activeFontHelper;                  
                    break;
                case "EntityLoaded":
                    BusyGrid.Visibility = Visibility.Visible;
                    BusyProgress.IsIndeterminate = true;
                    BookCover.Source = (BitmapImage)(new UrlToImageConverter().Convert(ViewModel.Entity.Cover, null, null));

                    break;
                case "IncProgress":
                    pageProgress.Value += 1;
                    break;
                case "ChoosePaymentMethod":
                    ChoosePaymentMethod();
                    break;
                    
            }
        }

        public async void UpdateBook()
        {
            BusyGrid.Visibility = Visibility.Collapsed;
            BusyProgress.IsIndeterminate = false;
            PageHeader.ProgressIndicatorVisible = false;
            await ViewModel.Reload();
            await HandleLoadedBook();
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SaveCurrentBookmark();

            if (e.NavigationMode == NavigationMode.Back || (e.Uri != null && string.Equals(e.Uri.OriginalString, "/Views/Main.xaml")) )
            {
                ControlPanel.Instance.NormalMode();
                NavigationCacheMode = NavigationCacheMode.Disabled;
                AppSettings.Default.SettingsChanged = true;
                new Task(async () =>
                {
                    await ViewModel.UpdateEntity();
                       }).RunSynchronously();

                if (ExpirationGuardian.Instance != null) ExpirationGuardian.Instance.RemoveCallBack(this);
            }

            if (!SystemInfoHelper.IsDesktop())
            {
                StatusBar statusBar = StatusBar.GetForCurrentView();
                await statusBar.ShowAsync();
            }

            ViewModel.SaveSettings();
            AppSettings.Default.ReaderOpen = false;
            _displayRequest.RequestRelease();
        }

        private void SaveCurrentBookmark()
        {
            var book = AppSettings.Default.CurrentBook;
            if (book == null) return;
            string pointer;
            var tokenId = _tokenOffset;
            var text = _bookTool.GetLastParagraphByToken(book, tokenId, out pointer);
            var chapter = _bookTool.GetChapterByToken(tokenId);
            var xpointer = ViewModel.GetXPointer(text);
            var newXpointer = ViewModel.GetNewXPointer(pointer);
            var percent = Convert.ToString((int) Math.Ceiling(CurrentPageSlider.Value/(CurrentPageSlider.Maximum/100)));
            if (xpointer != null)
                ViewModel.SetCurrentBookmark(text, xpointer, chapter, percent);
            else if (newXpointer != null)
                ViewModel.SetCurrentBookmark(text, newXpointer, chapter, percent);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ControlPanel.Instance.ReaderMode();
            base.OnNavigatedTo(e);
            Analytics.Instance.sendMessage(Analytics.ViewReader);
            _displayRequest = new DisplayRequest();
            _displayRequest.RequestActive(); //to request keep display on
            Bottom.Visibility = Visibility.Collapsed;
        }
        
        private async Task HandleLoadedBook()
        {
            if (App.OpenFromTile || AppSettings.Default.SettingsChanged)
                await Task.Delay(2000);
            if (ViewModel.Status == ReaderViewModel.LoadingStatus.FullBookLoaded || ViewModel.Status == ReaderViewModel.LoadingStatus.TrialBookLoaded)
            {
                CoverGrid.Visibility = Visibility.Collapsed;
                HideMenu();

                CurrentPageSlider.ManipulationStarted -= CurrentPageSliderOnManipulationStarted;
                CurrentPageSlider.ManipulationCompleted -= CurrentPageSliderOnManipulationCompleted;
               
                CurrentPageSlider.ManipulationStarted += CurrentPageSliderOnManipulationStarted;
                CurrentPageSlider.ManipulationCompleted += CurrentPageSliderOnManipulationCompleted;           
                
                CurrentPageSlider.Tapped -= CurrentPageSliderOnTapped;
                CurrentPageSlider.Tapped += CurrentPageSliderOnTapped;

                LayoutRoot.SizeChanged -= LayoutRoot_SizeChanged;
                LayoutRoot.SizeChanged += LayoutRoot_SizeChanged;

                BookTitleTextBlock.Text = ViewModel.Entity.BookTitle;

                _currentChapter = AppSettings.Default.Chapters.First();

                if (AppSettings.Default.Bookmark != null && !AppSettings.Default.ToChapter && AppSettings.Default.ToBookmark)
                {
                    await GetTokenPosition(AppSettings.Default.Bookmark);
                }

                if (AppSettings.Default.LastPositionBookmark != null && !AppSettings.Default.ToChapter && !AppSettings.Default.ToBookmark)
                {
                    await GetTokenPosition(AppSettings.Default.LastPositionBookmark);
                    AppSettings.Default.LastPositionBookmark = null;
                }

                if (AppSettings.Default.CurrentTokenOffset > 0)
                {
                    AppSettings.Default.Bookmark = null;                    
                    GoToChapter();
                }
                else
                    Redraw();
                SaveCurrentBookmark();
                AppSettings.Default.ReaderOpen = true;
                AppSettings.Default.LastBookId = ViewModel.Entity.Id;
            }
            else if (ViewModel.LoadingException != null)
            {
                await ShowDownloadError();
            }            
        }

        private async Task ShowDownloadError()
        {
            BusyGrid.Visibility = Visibility.Collapsed;
            BusyProgress.IsIndeterminate = false;
            PageHeader.ProgressIndicatorVisible = false;
            await new MessageDialog("Ошибка получения книги. Попробуйте попозже.").ShowAsync();
            _navigationService.GoBack();
        }

        private async Task GetTokenPosition(BookmarkModel bookmark)
        {
            try
            {
                var book = AppSettings.Default.CurrentBook;
                _bookSearch = new BookSearch(book);
                _bookSearch.Init();
                var query = new List<string>(bookmark.Text.Split(' ').ToList());
                string text1 = query.Aggregate("", (current, word) => current + (word + " ")).TrimEnd();
                text1 = text1.Remove(text1.Length - 1);
                query.RemoveAt(query.Count - 1);
                string text2 = query.Aggregate("", (current, word) => current + (word + " ")).TrimEnd();
                text1 = text1.Replace(Convert.ToChar(160).ToString(), " ");
                text2 = text2.Replace(Convert.ToChar(160).ToString(), " ");
                var result = await _bookSearch.Search(book, text1, query.Count);
                if (result.Count > 0)
                {
                    AppSettings.Default.CurrentTokenOffset = result[0].SearchResult[0].ID;
                }
                else
                {
                    result = await _bookSearch.Search(book, text2, query.Count);
                    if (result.Count > 0) AppSettings.Default.CurrentTokenOffset = result[0].SearchResult[0].ID;
                }
                _bookSearch.Dispose();
            }
            catch (Exception e)
            {                
                Debug.Write(e.Message);
            }

        }

        private void CurrentPageSliderOnTapped(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            if (Math.Abs(_pageSliderValue - CurrentPageSlider.Value) > 0.001)
                OnSliderClickOrMoved();
        }

        private void CurrentPageSliderOnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs manipulationCompletedRoutedEventArgs)
        {
            OnSliderClickOrMoved();
        }

        protected void OnOrientationChanged(DisplayInformation info, object sender)
        {
            var orientation = info.CurrentOrientation;
            
            if (orientation == DisplayOrientations.Landscape)
            {
                BookCover.Width = BookCoverBack.Width = 310.0 / 1.5;
                BookCover.Height = BookCoverBack.Height = 474.0 / 1.5;
                PageHeader.Margin = new Thickness(0, 0, 0, 40);
            }
            else
            {
                BookCover.Width = BookCoverBack.Width = 310;
                BookCover.Height = BookCoverBack.Height = 474;
                PageHeader.Margin = new Thickness(0, 0, 0, 75);
            }
        }

        private void GetCurrentChapter()
        {
            var chaptertext = "";
            foreach (var chapter in AppSettings.Default.Chapters.SkipWhile(ch => ch.TokenID > _currentChapter.TokenID))
            {
                if (chapter.MinTokenID <= _tokenOffset)
                {
                    chaptertext = chapter.Title;
                    _currentChapter = chapter;
                }
                else break;
            }
            ChapterTextBlock.Text = chaptertext.Trim();
        }

        public async void GoToBookmark()
        {
            var book = AppSettings.Default.CurrentBook;
            _bookSearch = new BookSearch(book);
            var query = new List<string>(AppSettings.Default.Bookmark.Text.Split(' ').ToList());
            string text1 = query.Aggregate("", (current, word) => current + (word + " ")).TrimEnd();
            text1=text1.Remove(text1.Length - 1);
            query.RemoveAt(query.Count - 1);
            string text2 = query.Aggregate("", (current, word) => current + (word + " ")).TrimEnd();
            text1 = text1.Replace(Convert.ToChar(160).ToString(), " ");
            text2 = text2.Replace(Convert.ToChar(160).ToString(), " ");
            _bookSearch.Init();
            var result = await _bookSearch.Search(book, text1, query.Count);
            if (result.Count > 0)
            {
                AppSettings.Default.CurrentTokenOffset = result[0].SearchResult[0].ID;
            }
            else
            {
                result = await _bookSearch.Search(book, text2, query.Count);
                if (result.Count > 0) AppSettings.Default.CurrentTokenOffset = result[0].SearchResult[0].ID;
            }
            _bookSearch.Dispose();
            AppSettings.Default.Bookmark = null;
            GoToChapter();
        }

        private void HideMenu()
        {           
            MobileTop.Visibility = Visibility.Collapsed;
            if (_isNeedToShowBuyFullBook) return;
            Bottom.Visibility = Visibility.Collapsed;
        }

        private void ShowMenu()
        {
            if (!ViewModel.Entity.IsMyBook && !ViewModel.Entity.IsFreeBook)
            {
                BuyFullBookButton.Visibility = Visibility.Visible;
            }
            if (!_isNeedToShowBuyFullBook)
            {
                BuyBookTextFirstLine.Visibility = Visibility.Collapsed;
                BuyBookTextSecondLine.Visibility = Visibility.Collapsed;
                ChapterTextBlock.Visibility = Visibility.Visible;
                PagesTextBlock.Visibility = Visibility.Visible;
                CurrentPageSlider.Visibility = Visibility.Visible;
            }
            Bottom.Visibility = Visibility.Visible;
            MobileTop.Visibility = Visibility.Visible;
        }

        public void ExpiredCallBack(Models.Book book)
        {
            if (!ViewModel.Entity.Id.Equals(book.Id)) return;
            var cancelArgs = new CancelEventArgs();
            do
            {
                cancelArgs.Cancel = false;                 
            } while (cancelArgs.Cancel);
        }

        private void Instance_PropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            var chapter = sender as BookChaptersViewModel.Chapter;
            if (chapter != null)
            {
                if (!SystemInfoHelper.IsDesktop()) return;
                TocFrame.BackStack.Clear();
                FlyoutBase.GetAttachedFlyout(ContentsButton)?.Hide();
                return;
            }

            var bookmark = sender as Bookmark;
            if (bookmark == null) return;
            if (!SystemInfoHelper.IsDesktop()) return;
            BookmarksFrame.BackStack.Clear();
            FlyoutBase.GetAttachedFlyout(BookmarksButton)?.Hide();
        }

        public static Brush ColorToBrush(string color) // color = "#E7E44D"
        {
            color = color.Replace("#", "");
            if (color.Length == 6)
            {
                return new SolidColorBrush(ColorHelper.FromArgb(255,
                    byte.Parse(color.Substring(0, 2), NumberStyles.HexNumber),
                    byte.Parse(color.Substring(2, 2), NumberStyles.HexNumber),
                    byte.Parse(color.Substring(4, 2), NumberStyles.HexNumber)));
            }
            return null;
        }
        
        public List<BookmarkModel> Bookmarks { get; set; } 
        private void BookmarsButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (SystemInfoHelper.IsDesktop())
            {
                var brush = ColorToBrush("#3b393f");
                BookmarksButton.Background = brush;
                Bookmarks = AppSettings.Default.CurrentBook.LoadBookmarks(AppSettings.Default.CurrentBook.GetBookmarksPath());
                FlyoutBase.ShowAttachedFlyout((Button) sender);
                BookmarksFrame.Navigate(typeof (BookBookmarks), XParameters.Create("BookEntity", ViewModel.Entity));
            }
            else
                _navigationService.Navigate("BookBookmarks", XParameters.Create("BookEntity", ViewModel.Entity));
        }

        public async void UpdateSettings()
        {
            SetColorScheme();
            await CreateController();
        }

        public async void ChangeFont()
        {
            _activeFontHelper = BookFactory.GetActiveFontMetrics(AppSettings.Default.FontSettings.FontFamily.Source);
            AppSettings.Default.FontSettings.FontHelper = _activeFontHelper;
            await CreateController();
        }

        public async void Redraw()
        {
            if (!_isLoaded)
                return;
            if (AppSettings.Default.SettingsChanged)
                await Task.Delay(2000);
            await _event.WaitAsync();

            Background = AppSettings.Default.ColorScheme.BackgroundBrush;

            await CreateController();

            _event.Release();

            if (BookCoverBack.Visibility == Visibility.Visible)
                BookCoverBack.Visibility = Visibility.Collapsed;

            //Bottom.Visibility = Visibility.Visible;

            BusyGrid.Visibility = Visibility.Collapsed;
            BusyProgress.IsIndeterminate = false;
            PageHeader.ProgressIndicatorVisible = false;
        }

        private async void OnSliderClickOrMoved()
        {
            int page = (int)CurrentPageSlider.Value;
            CurrentPage = page;            
            int tokenOffset = (page - 1) * AppSettings.WordsPerPage;
            _tokenOffset = tokenOffset;
            AppSettings.Default.CurrentTokenOffset = _tokenOffset;
            AppSettings.Default.ToBookmark = true;
            await CreateController();
            SaveCurrentBookmark();
        }

        public async void GoToChapter()
        {         
            int tokenOffset = AppSettings.Default.CurrentTokenOffset;
            _tokenOffset = tokenOffset;

            Background = AppSettings.Default.ColorScheme.BackgroundBrush;

            await CreateController();

            if (BookCoverBack.Visibility == Visibility.Visible)
                BookCoverBack.Visibility = Visibility.Collapsed;          

            BusyGrid.Visibility = Visibility.Collapsed;
            BusyProgress.IsIndeterminate = false;
            PageHeader.ProgressIndicatorVisible = false;            
        }

        private bool NeedUpdate()
        {
            return AppSettings.Default.SettingsChanged || AppSettings.Default.ToChapter ||
                   AppSettings.Default.ToBookmark;
        }

        private async Task CreateController()
        {
            if (_book == null || !NeedUpdate())
            {
                BusyGrid.Visibility = Visibility.Collapsed;
                BusyProgress.IsIndeterminate = false;
                PageHeader.ProgressIndicatorVisible = false;
                if (ReaderGrid.Opacity < 1) ReaderGrid.Opacity = 1;
                return;
            }                        

            PageCanvas.Clear();
            PageCanvas.SetSize(PageCanvas.ActualWidth, PageCanvas.ActualHeight, PageCanvas.ActualWidth, PageCanvas.ActualHeight);
            PageCanvas.Manipulator = new ManipulatorFactory(PageCanvas).CreateManipulator(AppSettings.Default.FlippingStyle, AppSettings.Default.FlippingMode);

            BusyGrid.Visibility = Visibility.Visible;
            BusyProgress.IsIndeterminate = true;          
            _readController = new ReadController(PageCanvas, _book, _book.BookID, _tokenOffset);
           
            await _readController.ShowNextPage();
            try
            {
                CurrentPageSlider.Value = _readController.CurrentPage;
            }
            catch (Exception)
            {
                CurrentPageSlider.Value = _readController.TotalPages;                
            }
            GetCurrentChapter();
            _pageSliderValue = CurrentPageSlider.Value;            
            PageCanvas.Manipulator.UpdatePanelsVisibility();
            PageCanvas.Manipulator.IsFirstPage = _readController.IsFirst;
            PageCanvas.Manipulator.IsLastPage = _readController.IsLast;
            CurrentPageSlider.Maximum = _readController.TotalPages;
            SecondCurrentPageRun.Text = _readController.CurrentPage.ToString();
            SecondTotalPagesRun.Text = "/ " + _readController.TotalPages;
            CurrentPageRun.Text = _readController.CurrentPage.ToString();
            TotalPagesRun.Text = "/ " + _readController.TotalPages;
            BusyGrid.Visibility = Visibility.Collapsed;
            BusyProgress.IsIndeterminate = false;
            PageHeader.ProgressIndicatorVisible = false;
            if (ReaderGrid.Opacity < 1) ReaderGrid.Opacity = 1;
            AppSettings.Default.SettingsChanged = false;
            AppSettings.Default.ToChapter = false;
            AppSettings.Default.ToBookmark = false;
        }        

        private async Task TurnPage(bool isRight)
        {                      
            await _event.WaitAsync();           

            if (isRight)
                await _readController.ShowNextPage();
            else
                await _readController.ShowPrevPage();

            _tokenOffset = _readController.Offset;
            GetCurrentChapter();
            CurrentPageSlider.Value = _readController.CurrentPage;
            CurrentPage = (int) CurrentPageSlider.Value;
            SecondCurrentPageRun.Text = _readController.CurrentPage.ToString();
            SecondTotalPagesRun.Text = "/ " + _readController.TotalPages;
            CurrentPageRun.Text = _readController.CurrentPage.ToString();
            TotalPagesRun.Text = "/ "+_readController.TotalPages;
            AppSettings.Default.CurrentTokenOffset = _tokenOffset;
            PageCanvas.Manipulator.IsFirstPage = _readController.IsFirst;
            PageCanvas.Manipulator.IsLastPage = _readController.IsLast;
            PageCanvas.Manipulator.UpdatePanelsVisibility();

            CheckIfLast();

            _event.Release();
            SaveCurrentBookmark();
        }

        private void CheckIfLast()
        {
            var concantinatedString = PageCanvas.CurrentTexts.Aggregate("", (current, textRenderData) => current + textRenderData.Text);
            concantinatedString = Regex.Replace(concantinatedString, "[^0-9a-zA-Zа-яА-Я]+", "");
            _isNeedToShowBuyFullBook = concantinatedString.Contains("Конецознакомительногофрагмента");

            if (_isNeedToShowBuyFullBook)
                ShowBuyFullBook();
            else
                HideBuyFullBook();
        }

        private void HideBuyFullBook()
        {
            if (MobileTop.Visibility == Visibility.Collapsed)
                Bottom.Visibility = Visibility.Collapsed;
            if (!ViewModel.Entity.IsMyBook && !ViewModel.Entity.IsFreeBook)
            {
                BuyFullBookButton.Visibility = Visibility.Visible;
            }
            BuyBookTextFirstLine.Visibility = Visibility.Collapsed;
            BuyBookTextSecondLine.Visibility = Visibility.Collapsed;
            ChapterTextBlock.Visibility = Visibility.Visible;
            PagesTextBlock.Visibility = Visibility.Visible;
            CurrentPageSlider.Visibility = Visibility.Visible;
        }

        private void ShowBuyFullBook()
        {
            Bottom.Visibility = Visibility.Visible;
            BuyFullBookButton.Visibility = Visibility.Visible;
            BuyBookTextFirstLine.Visibility = Visibility.Visible;
            BuyBookTextSecondLine.Visibility = Visibility.Visible;
            ChapterTextBlock.Visibility = Visibility.Collapsed;
            PagesTextBlock.Visibility = Visibility.Collapsed;
            CurrentPageSlider.Visibility = Visibility.Collapsed;
        }

        private bool _isAnchor;
        private bool FindLink(Thickness margin)
        {
            var offset = AppSettings.Default.Margin.Left;
            _link = PageCanvas.CurrentLinks.FirstOrDefault(l => Math.Abs(l.Rect.Y - margin.Top) < 1 && Math.Abs(margin.Left - l.Rect.X - offset) < 1);
            if (_link != null)
            {
                try
                {
                    int anchorsTokenId = AppSettings.Default.Anchors.FirstOrDefault(l => l.Name == _link.LinkID).TokenID;
                    var text = _bookTool.GetAnchorTextByToken(_book, anchorsTokenId);
                    AnchorTextBlock.Text = text;                    
                    AnchorStackPanel.Tapped -= AnchorStackPanelOnTapped;
                    AnchorStackPanel.Tapped += AnchorStackPanelOnTapped;                                          
                    AnchorStackPanel.Height = AnchorTextBlock.DesiredSize.Height;                                                           
                    AnchorStackPanel.Background = AppSettings.Default.ColorScheme.LinkPanelBackgroundBrush;
                    AnchorStackPanel.Visibility = Visibility.Visible;
                    _isAnchor = true;
                    return _isAnchor;
                }
                catch (Exception)
                {
                    var url = _link.LinkID;
                    Launch(url);                    
                }
                
            }
            return false;
        }

        async void Launch(string url)
        {           
            var uri = new Uri(url);

            var success = await Launcher.LaunchUriAsync(uri);

            if (success)
            {
                // URI launched
            }
            else
            {
                // URI launch failed
            }
        }

        private void AnchorStackPanelOnTapped(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            AnchorStackPanel.Visibility = Visibility.Collapsed;
            _isAnchor = false;           
        }

        private async void PageCanvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_isAnchor)
            {
                AnchorStackPanel.Visibility = Visibility.Collapsed;
                _isAnchor = false;
                return;
            }
            var textblock = ((FrameworkElement)e.OriginalSource) as TextBlock;
            if (textblock != null)
            {
                if (FindLink(textblock.Margin)) return;
            }

            Point pt = e.GetPosition(sender as Canvas);
            var clickOffset = 100;
            var width = ActualWidth;
            var height = ActualHeight;
            if (pt.X < (width/2 - width/4))
                await TurnPage(false);
            else if (pt.X > (width/2 + width/4))
                await TurnPage(true);
            else if (pt.X <= (width/2 + clickOffset) ||
                     pt.X >= (width/2 - clickOffset) &&
                     (pt.Y <= (height/2 + clickOffset) || (pt.Y >= (height/2 - clickOffset))))
                if (MobileTop.Visibility == Visibility.Collapsed)
                    ShowMenu();
                else
                    HideMenu();
        }

        private void LayoutRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!SystemInfoHelper.IsDesktop() &&
                Window.Current.CoreWindow.GetKeyState(VirtualKey.LeftButton) == CoreVirtualKeyStates.Down) return;
            Redraw();
        }

        private void CurrentPageSliderOnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs manipulationStartedRoutedEventArgs)
        {
        }

        private void SettingsButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (SystemInfoHelper.IsDesktop())
            {
                var brush = ColorToBrush("#3b393f");
                SettingsButton.Background = brush;
               // SettingsImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/W10Icons/Settings/setting_white.scale-100.png", UriKind.Absolute));
                //SettingsImage.Opacity = 1;
                FlyoutBase.ShowAttachedFlyout((Button)sender);
                SettingsFrame.Navigate(typeof(Settings));
            }
            else _navigationService.Navigate("Settings");
        }

        private void ContentsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (SystemInfoHelper.IsDesktop())
            {
                var brush = ColorToBrush("#3b393f");
                ContentsButton.Background = brush;
                ContentsImage.Source =
                    new BitmapImage(new Uri("ms-appx:///Assets/W10Icons/Toc/toc.white.png", UriKind.Absolute));
                FlyoutBase.ShowAttachedFlyout((Button) sender);
                TocFrame.Navigate(typeof (BookContent));
            }
            else Frame.Navigate(typeof (BookContent));
        }

        private void TocFrame_Unloaded(object sender, RoutedEventArgs e)
        {
            ContentsButton.Background = new SolidColorBrush(Colors.Transparent);
            ContentsImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/W10Icons/Toc/toc.png", UriKind.Absolute));
        }

        private void SettingsFrame_Unloaded(object sender, RoutedEventArgs e)
        {
            SettingsButton.Background = new SolidColorBrush(Colors.Transparent);
           // SettingsImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/W10Icons/Settings/setting_grey.scale-100.png", UriKind.Absolute));
            //SettingsImage.Opacity = 0.6;
        }

        private Point _initialPoint;

        private void PageCanvas_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var curPoint = e.GetCurrentPoint((UIElement)sender);
            _initialPoint = curPoint.Position;
        }

        private async void PageCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var curPoint = e.GetCurrentPoint((UIElement)sender);
            Point currentpoint = curPoint.Position;
            var abs = Math.Abs(currentpoint.X - _initialPoint.X);
            if (abs < 5) return;
            if (currentpoint.X - _initialPoint.X <= 50)
            {               
                await TurnPage(true);
            }
            else if (currentpoint.X - _initialPoint.X >= 50)
            {
                await TurnPage(false);
            }
        }        

        private async void AddBookmarkButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var book = AppSettings.Default.CurrentBook;
            string pointer;
            var tokenId = _tokenOffset;
            var text = _bookTool.GetLastParagraphByToken(book, tokenId, out pointer);
            var chapter = _bookTool.GetChapterByToken(tokenId);
            var xpointer = ViewModel.GetNewXPointer(pointer);
            var oldpointer = ViewModel.GetXPointer(text);
            if (xpointer == null && oldpointer == null)
            {
                await
                    new MessageDialog(
                        "Не удалось добавить закладку. Попробуйте еще раз, или перейдите на другую страницу.",
                        "Внимание").ShowAsync();
                return;
            }
            var percent = Convert.ToString((int)Math.Ceiling(CurrentPageSlider.Value / (CurrentPageSlider.Maximum / 100)));
            if (oldpointer == null) await ViewModel.AddBookmark(text, xpointer, chapter, false, percent);
            else await ViewModel.AddBookmark(text, oldpointer, chapter, false, percent);
            BookmarkGrid.Visibility = Visibility.Visible;
            BookmarkStoryboard.Begin();
            BookmarkStoryboard.Completed += BookmarkStoryboardOnCompleted;
        }

        private void BookmarkStoryboardOnCompleted(object sender, object o)
        {
            BookmarkGrid.Visibility = Visibility.Collapsed;
        }

        private void BookmarksFrame_Unloaded(object sender, RoutedEventArgs e)
        {
            BookmarksButton.Background = new SolidColorBrush(Colors.Transparent);          
        }

        private void BuyFullBookButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.BuyBook.Execute(null);
        }

        private async void ChoosePaymentMethod()
        {
            await ViewModel.UpdatePrice();
            var price = ViewModel.AccoundDifferencePrice;

            var dialog = new ContentDialog
            {
                Title = "Необходимо пополнить счет",
                IsPrimaryButtonEnabled = true,
                PrimaryButtonText = "Отмена"
            };

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 10, 0, 5),
                Text = "К сожалению на Вашем счете ЛитРес недостаточно средств.",
                TextWrapping = TextWrapping.Wrap
            });

            var creditButton = new Button
            {
                Margin = new Thickness(0, 10, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Content = $"пополнить счет на {price} руб.",
                BorderThickness = new Thickness(2),
                BorderBrush = new SolidColorBrush(Colors.Gray),
                Background = new SolidColorBrush(Colors.Transparent)
            };
            creditButton.Tapped += (sender, args) => { ViewModel.RunCreditCardPaymentProcess.Execute(null); dialog.Hide(); };
            panel.Children.Add(creditButton);

            var storeButton = new Button
            {
                Margin = new Thickness(0, 10, 0, 5),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Content = $"оплатить через Windows Store",
                BorderThickness = new Thickness(2),
                BorderBrush = new SolidColorBrush(Colors.Gray),
                Background = new SolidColorBrush(Colors.Transparent)
            };
            storeButton.Tapped += (sender, args) => { ViewModel.BuyBookFromMicrosoft.Execute(null); dialog.Hide(); };
            panel.Children.Add(storeButton);

            panel.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 10),
                Text = "Внимание! К цене будет добавлена комисия Windows Store.",

                TextWrapping = TextWrapping.Wrap
            });
            dialog.Content = panel;
            await dialog.ShowAsync();
        }
    }


    public class ReaderFitting : ViewModelPage<ReaderViewModel>
    {
    }
}