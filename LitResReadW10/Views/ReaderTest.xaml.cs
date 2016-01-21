using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Windows.System;
using LitRes.BookReader;
using Athenaeum.Formatter;
using Digillect.Mvvm;
using Digillect.Mvvm.UI;
using FictionBook;

using LitRes.Helpers;
using LitRes.LibraryTools;

using LitRes.Models;
using LitRes.Services;
using LitRes.ValueConverters;
using LitRes.ViewModels;

//using Telerik.Windows.Controls.SlideView;
//using GestureEventArgs = System.Windows.Input.GestureEventArgs;

using System.ComponentModel;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using LitResReadW10;

#if PDF_ENABLED
using pdftron;
using pdftron.PDF;
using pdftron.PDF.Controls;
#endif
namespace LitRes.Views
{
    [View("Reader")]
    [ViewParameter("bookmark")]
    [ViewParameter("chapter")]
    [ViewParameter("id", typeof(int))]
    [ViewParameter("filetoken")]

    public class ReaderTest : ReaderFitting, IExpiredCallBack
    {
        private BookReadingContext _cacheItem;
        private bool _menuVisible;
        private bool _fullVisible;
        private DispatcherTimer _timer;
        private readonly Stack<Pointer> _history = new Stack<Pointer>();
        private int _moveCount;
        private bool _inSlide;
        private WriteableBitmap _tileBitmap;
        private Themes _lastTheme;
        private bool _isActive;
        private bool _syncInProgress;
        private object _thisLock = new object();
        private bool _isBuyShowed;
        private int _lastThemeIndex;
        private WriteableBitmap _backgroundTile;
        private IBookReader _bookReader = null;

#if PDF_ENABLED
        private PDFViewCtrl pdfViewCtrl = null;
        private double pdfViewCtrlOriginalZoom;
#endif

        #region Constructors/Disposer
        public ReaderTest()
        {
            Debug.WriteLine("Reader()");
            _lastThemeIndex = -1;
            
            PhoneApplicationService.Current.Activated += CurrentOnActivated;
            PhoneApplicationService.Current.Deactivated += Current_Deactivated;
            InitializeComponent();

            Loaded += ReaderLoaded;
            if (ExpirationGuardian.Instance != null) ExpirationGuardian.Instance.AddCallBack(this);
            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            PhoneApplicationService.Current.ApplicationIdleDetectionMode = IdleDetectionMode.Disabled;
        }
        #endregion

        #region ReaderSettingsUpdated
        void ReaderSettingsUpdated(object sender, EventArgs e)
        {
            Center.Visibility = Visibility.Visible;
            ChangeGridBackground();
            ApplyReaderSettings();
        }
        #endregion

        #region ReaderLoaded
        async void ReaderLoaded(object sender, RoutedEventArgs e)
        {
            _moveCount = 0;
            await ViewModel.LoadSettings();
        }
        #endregion

        #region CreateDataSession
        protected override Session CreateDataSession(DataLoadReason reason)
        {
            ViewModel.Id = ViewParameters.Get<int>("id");
            ViewModel.FileToken = ViewParameters.Get<string>("filetoken");

            ViewModel.ReaderSettings.Updated -= ReaderSettingsUpdated;
            ViewModel.ReaderSettings.Updated += ReaderSettingsUpdated;

            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.DeffaultSettings = (ResolutionHelper.isFullHD) ? ReaderSettingsViewModel.DeffaultSettingsType.DeffaultSettingsTypeHD :
                                                                       ReaderSettingsViewModel.DeffaultSettingsType.DeffaultSettingsTypeNormal;
            Debug.WriteLine("Reader CreateDataSession");

            return base.CreateDataSession(reason);
        }
        #endregion

        protected override async void OnDataLoadComplete(Session session)
        {
            Debug.WriteLine("OnDataLoadComplete Enter");

            var bookmark = ViewParameters.Get<string>("bookmark");
            var chapter = ViewParameters.Get<string>("chapter");

            await HandleLoadedBook();
            if (ViewModel.Entity == null || _cacheItem == null) return;

            if (ViewModel.Entity.TypeBook == Models.Book.BookType.Fb2)
            {
                try
                {
                    var currentBookmark1 = await ViewModel.GetCurrentBookmark(true, session.Token);
                    if (currentBookmark1 != null) bookmark = currentBookmark1.Selection;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            // Show menu when application first run
            ShowFirstRunAnimation();

            NavigateBook(bookmark, chapter);

            SyncReaderStartPositionWithServer(bookmark, chapter, session);

            AddBuySection(ViewModel.Entity.Price);

            InitTimer();
        }

        private void InitTimer()
        {
            _timer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += RefreshTimer;
            _timer.Start();
        }

        private void ShowFirstRunAnimation()
        {
            if (!IsolatedStorageSettings.ApplicationSettings.Contains("isFirstRun"))
            {
                menuVisible(false, true);
                IsolatedStorageSettings.ApplicationSettings.Add("isFirstRun", true);
                IsolatedStorageSettings.ApplicationSettings.Save();
            }
        }

        private async void LoadPdfPosition()
        {
#if PDF_ENABLED
            int pos = await ViewModel.LoadPdfBookPosition();
            if (pos > 0)
            {

                pdfViewCtrl.SetCurrentPage(pos);
                CurrentPageSlider.Value = pdfViewCtrl.GetCurrentPage();

            }
#endif
        }

        private void SavePdfPosition()
        {
#if PDF_ENABLED
            ViewModel.SavePdfBookPosition(pdfViewCtrl.GetCurrentPage());
#endif
        }

        private async void SyncReaderStartPositionWithServer(string bookmark, string chapter, Session session)
        {
            if (ViewModel.Entity.TypeBook == Models.Book.BookType.Fb2)
            {
                Debug.WriteLine("OnDataLoadComplete SyncReadPosition");
                if (_syncInProgress)
                {
                    Debug.WriteLine("OnDataLoadComplete SyncInProgress Now! Return!");
                    return;
                }
                lock (_thisLock) _syncInProgress = true;
                Debug.WriteLine("OnDataLoadComplete SyncInProgress = true");

                string bookmarkGlobal = null;
                if (string.IsNullOrEmpty(chapter))
                {
                    try
                    {
                        var currentbookmark2 = await ViewModel.GetCurrentBookmark(false, session.Token);
                        if (currentbookmark2 != null) bookmarkGlobal = currentbookmark2.Selection;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                    string nextPoint = null;
                    if (bookmarkGlobal != null)
                    {
                        if (bookmark != null && !bookmark.Equals(bookmarkGlobal))
                        {
                            nextPoint = bookmarkGlobal;
                        }
                        else if (ViewModel.BlockIndex != null && !ViewModel.BlockIndex.Equals(bookmarkGlobal))
                        {
                            nextPoint = bookmarkGlobal;
                        }
                    }

                    Dispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            if (NavigationService.CurrentSource.OriginalString.Contains("Reader"))
                            {
                                if (nextPoint != null)
                                {
                                    Pointer localPointer = null;
                                    if (!string.IsNullOrEmpty(ViewModel.BlockIndex))
                                        localPointer = _cacheItem?.Reader.ResolveXPointer(ViewModel.BlockIndex);
                                    var nextPointPointer = _cacheItem?.Reader.ResolveXPointer(nextPoint);
                                    if (nextPointPointer != null && (localPointer == null ||
                                                                     (localPointer.BlockId != nextPointPointer.BlockId &&
                                                                      localPointer.DocumentId ==
                                                                      nextPointPointer.DocumentId)))
                                    {
                                        var result = MessageBox.Show(
                                            "Обнаружена более новая позиция чтения. Хотите переместиться на нее?",
                                            string.Empty,
                                            MessageBoxButton.OKCancel);
                                        if (result == MessageBoxResult.OK) NavigateBook(nextPoint, null);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                        lock (_thisLock) { _syncInProgress = false; }
                        Debug.WriteLine("OnDataLoadComplete SyncInProgress = false");
                    });
                }
            }
            else if (ViewModel.Entity.TypeBook == Models.Book.BookType.Pdf)
            {
                LoadPdfPosition();
            }
        }

        private async void SyncReadPosition()
        {
            Debug.WriteLine("SyncReadPosition");
            if (ViewModel.Entity.TypeBook != Models.Book.BookType.Fb2) return;
            if (_syncInProgress)
            {
                Debug.WriteLine("SyncInProgress Now! Return!");
                return;
            }
            lock (_thisLock) { _syncInProgress = true; }
            Debug.WriteLine("SyncInProgress = true");

            string bookmarkGlobal = null;

            try
            {
                var currentbookmark2 = await ViewModel.GetCurrentBookmark(false, CancellationToken.None);
                if (currentbookmark2 != null) bookmarkGlobal = currentbookmark2.Selection;
            }
            catch (WebException e)
            {
                Debug.WriteLine($"WebException on Sync: {e.Message}");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"undefined Exception on Sync: {e.Message}");
            }

            string nextPoint = null;
            if (bookmarkGlobal != null)
            {
                if (ViewModel.BlockIndex != null && !ViewModel.BlockIndex.Equals(bookmarkGlobal))
                    nextPoint = bookmarkGlobal;
            }

            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    if (NavigationService.CurrentSource.OriginalString.Contains("Reader"))
                    {
                        if (nextPoint != null)
                        {
                            var result =
                                MessageBox.Show(
                                    "Обнаружена более новая позиция чтения. Хотите переместиться на нее?",
                                    string.Empty,
                                    MessageBoxButton.OKCancel);
                            if (result == MessageBoxResult.OK) NavigateBook(nextPoint, null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                lock (_thisLock) { _syncInProgress = false; }
                Debug.WriteLine("SyncInProgress = false");
            });
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ShowPopup")
            {
                showMainPopup();
            }
            else if (e.PropertyName == "HidePopup")
            {
                hideMainPopup();
            }
            else if (e.PropertyName == "ShowSwitchPopup")
            {
                showSwitchPopup();
            }
            else if (e.PropertyName == "HideSwitchPopup")
            {
                hideSwitchPopup();
            }
            else if (e.PropertyName == "UpdatePrice")
            {
                LitresStore.Content = $"пополнить счёт на {ViewModel.AccoundDifferencePrice} руб.";
            }
            else if (e.PropertyName == "LoadBookProcessCompleted")
            {
                switch (ViewModel.Status)
                {
                    case ReaderViewModel.LoadingStatus.FullBookLoaded:
                        Analytics.Instance.sendMessage(Analytics.ActionReadFull);
                        break;
                    case ReaderViewModel.LoadingStatus.TrialBookLoaded:
                        Analytics.Instance.sendMessage(Analytics.ActionReadFragment);
                        break;
                    case ReaderViewModel.LoadingStatus.NoBookLoaded:
                        MessageBox.Show("Не удалось загрузить книгу");
                        if (NavigationService.CanGoBack) NavigationService.GoBack();
                        break;
                    case ReaderViewModel.LoadingStatus.BeforeLoaded:
                        break;
                    default:
                        break;
                }
            }
            else if (e.PropertyName == "EntityLoaded")
            {
                BookCover.Source = (BitmapImage)(new UrlToImageConverter().Convert(ViewModel.Entity.Cover, null, null));// new BitmapImage(new Uri(ViewModel.Entity.Cover, UriKind.RelativeOrAbsolute));
            }
            else if (e.PropertyName == "IncProgress")
            {
                pageProgress.Value += 1;
            }
        }

        public async void UpdateBook()
        {
            pageHeader.ProgressIndicatorVisible = false;

            SetBlockIndex();

            await ViewModel.Reload();

            _cacheItem = null;

            await HandleLoadedBook();

            NavigateBook(string.Empty, string.Empty);
        }


      /*  void ScreenLocked(object sender, ObscuredEventArgs e)
        {
            if (Debugger.IsAttached) Debug.WriteLine("Screen locked");
        }*/

        private void ScreenUnlocked(object sender, EventArgs e)
        {
        }

        private void CurrentOnActivated(object sender)
        {
            Debug.WriteLine("CurrentOnActivated");
            lock (_thisLock)
            {
                Debug.WriteLine(string.Format("_isActive = {0}", _isActive));
                if (!_isActive)
                {
                    _isActive = true;
                    Debug.WriteLine(string.Format("_isActive = {0}", _isActive));
                    SyncReadPosition();
                }
            }
        }

        void Current_Deactivated(object sender)
        {
            _isActive = false;
            Debug.WriteLine("Current_Deactivated");
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _moveCount = 0;

            SetBlockIndex();
            base.OnNavigatedFrom(e);
            if (e.NavigationMode == NavigationMode.Back || (e.Uri != null && string.Equals(e.Uri.OriginalString, "/Views/Main.xaml")))
            {
                PhoneApplicationService.Current.Activated -= CurrentOnActivated;
                PhoneApplicationService.Current.Deactivated -= Current_Deactivated;

                new Task(async () =>
                {
                    await ViewModel.UpdateEntity();
                    SaveBookState();
                }).RunSynchronously();

                if (ExpirationGuardian.Instance != null) ExpirationGuardian.Instance.RemoveCallBack(this);

#if PDF_ENABLED
                DestroyAllEntities();
#endif
            }

            ViewModel.SaveSettings();

            if (_timer != null) _timer.Stop();
        }

        private void DestroyAllEntities()
        {
#if PDF_ENABLED
            if (ViewModel.Entity.TypeBook == Models.Book.BookType.Pdf)
            {

                if (this.pdfViewCtrl != null)
                {
                    var doc = this.pdfViewCtrl.GetDoc();
                    this.pdfViewCtrl.CloseDoc();
                    if (doc != null)
                    {
                        doc.Destroy();
                    }
                    PdfContainer.Children.Clear();
                    pdfViewCtrl.Dispose();
                    pdfViewCtrl = null;
                }
                return;
            }
#endif
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("Reader OnNavigatedTo");
            base.OnNavigatedTo(e);

            var tile = ShellTile.ActiveTiles.FirstOrDefault(
                x => x.NavigationUri.OriginalString == "/Views/Main.xaml?book=" + ViewParameters.Get<int>("id"));

            ViewModel.PinExist = tile == null;

            Analytics.Instance.sendMessage(Analytics.ViewReader);
        }

        public void RefreshTimer(object sender, EventArgs e)
        {
            var timeStr = DateTime.Now.ToString("HH:mm");
            tbTimer.Text = timeStr;
            tbTimer2.Text = timeStr;
            tbTimer3.Text = timeStr;
        }

        public void SaveBookState()
        {
            if (ViewModel.Entity.TypeBook == Models.Book.BookType.Fb2) SaveCurrentBookmarkAsync();
            else if (ViewModel.Entity.TypeBook == Models.Book.BookType.Pdf) SavePdfPosition();
        }

        public void SaveCurrentBookmarkAsync()
        {
            SetBlockIndex();

            var task = new Task(async () =>
            {
                try
                {
                    await ViewModel.SaveBooksIndexes();
                    await AddBookmark(true);
                    var tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.OriginalString == string.Format("/Views/Reader.xaml?id={0}", ViewModel.Entity.Id));
                    UpdateTileData(tile);
                }
                catch { }
            });

            task.RunSynchronously();
        }

        private async Task HandleLoadedBook()
        {
            if (ViewModel.Status == ReaderViewModel.LoadingStatus.FullBookLoaded || ViewModel.Status == ReaderViewModel.LoadingStatus.TrialBookLoaded)
            {
                await ReformatBook();
            }
            else if (ViewModel.LoadingException != null)
            {
                pageHeader.ProgressIndicatorVisible = false;
                MessageBox.Show("Ошибка получения книги. Попробуйте попозже.");
                NavigationService.GoBack();
            }

            BookCoverBack.Visibility = Visibility.Collapsed;
        }

        private void NavigateBook(string bookmark, string chapter)
        {
            if (!string.IsNullOrEmpty(chapter))
            {
                if (_cacheItem != null)
                {
                    _cacheItem.Reader.MoveTo(Convert.ToInt32(chapter));

                    CurrentPageSlider.Value = _cacheItem.Reader.CurrentPage;

                    PaintPane1Page();
                }
            }
            else if (!string.IsNullOrEmpty(bookmark))
            {
                if (_cacheItem != null)
                {
                    var pointer = _cacheItem.Reader.ResolveXPointer(bookmark);

                    if (pointer == null)
                    {
                        //MessageBox.Show( "Невозможно открыть закладку!", "Ошибка", MessageBoxButton.OK );
                    }
                    else
                    {
                        Debug.WriteLine("Navigate by bookmark");
                        _cacheItem.Reader.MoveTo(pointer);

                        CurrentPageSlider.Value = _cacheItem.Reader.CurrentPage;

                        PaintPane1Page();
                    }
                }
            }
            else if (!string.IsNullOrEmpty(ViewModel.BlockIndex))
            {
                if (_cacheItem != null)
                {
                    var pointer = _cacheItem.Reader.ResolveXPointer(ViewModel.BlockIndex);

                    if (pointer != null)
                    {
                        _cacheItem.Reader.MoveTo(pointer);

                        CurrentPageSlider.Value = _cacheItem.Reader.CurrentPage;

                        PaintPane1Page();
                    }
                    else
                    {
                        MessageBox.Show("Невозможно открыть закладку!", "Ошибка", MessageBoxButton.OK);
                    }
                }
            }
        }

        private void ChangeGridBackground()
        {
            Debug.WriteLine("ChangeGridBackground start");
            if (_lastThemeIndex == -1)
            {
                Debug.WriteLine("ChangeGridBackground changing");
                try
                {
                    switch (ViewModel.ReaderSettings.Theme)
                    {
                        case 0:
                            BitmapImage day = new BitmapImage(new Uri("/Assets//BackgroundDay.png", UriKind.Relative));
                            day.CreateOptions = BitmapCreateOptions.None;
                            ReadingCanva.ImageSource = day;
                            break;
                        case 1:
                            BitmapImage night = new BitmapImage(new Uri("/Assets//BackgroundNight.png", UriKind.Relative));
                            night.CreateOptions = BitmapCreateOptions.None;
                            ReadingCanva.ImageSource = night;
                            break;
                        case 2:
                            BitmapImage sepia = new BitmapImage(new Uri("/Assets//BackgroundSepia.png", UriKind.Relative));
                            sepia.CreateOptions = BitmapCreateOptions.None;
                            ReadingCanva.ImageSource = sepia;
                            break;
                        default:
                            BitmapImage deffaultDay = new BitmapImage(new Uri("/Assets//BackgroundDay.png", UriKind.Relative));
                            deffaultDay.CreateOptions = BitmapCreateOptions.None;
                            ReadingCanva.ImageSource = deffaultDay;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    BitmapImage day = new BitmapImage(new Uri("/Assets//BackgroundDay.png", UriKind.Relative));
                    day.CreateOptions = BitmapCreateOptions.None;
                    ReadingCanva.ImageSource = day;
                }
                _lastThemeIndex = ViewModel.ReaderSettings.Theme;
                _backgroundTile = null;
            }

            Debug.WriteLine("ChangeGridBackground end");
        }

        private async void ApplyReaderSettings()
        {
            PaintingContext.Settings.Face = (Faces)ViewModel.ReaderSettings.Font;
            PaintingContext.Settings.Scale = (Scales)ViewModel.ReaderSettings.FontSize;
            PaintingContext.Settings.Theme = (Themes)ViewModel.ReaderSettings.Theme;

            PaintingContext.Settings.Indent = (Indents)ViewModel.ReaderSettings.Margin;
            PaintingContext.Settings.LineSpacing = (LineSpacings)ViewModel.ReaderSettings.CharacterSpacing;
            PaintingContext.Settings.Justify = ViewModel.ReaderSettings.FitWidth;
            PaintingContext.Settings.ParagraphSpacing = ParagraphSpacing.Normal;

            PaintingContext.Settings.Brightness = ViewModel.ReaderSettings.Brightness;
            PaintingContext.Settings.UseKerning = true;
            PaintingContext.Settings.Hyphenate = ViewModel.ReaderSettings.Hyphenate;
            PaintingContext.Settings.Dencity = ResolutionHelper.CurrentResolution == Resolutions.WVGA ? Dencities.Low : Dencities.Medium;

            (ReadingPaneImage2.RenderTransform as TranslateTransform).Y = ViewModel.ReaderSettings.Margin * 10;
            (ReadingPaneImage.RenderTransform as TranslateTransform).Y = ViewModel.ReaderSettings.Margin * 10;

            BrightnessSlider.Value = ViewModel.ReaderSettings.Brightness;

            int fontSize = 18 + (int)PaintingContext.Settings.Scale * 4;
            ReadingPaneTextBottom.FontSize = fontSize;
            ReadingPaneTextBottom2.FontSize = fontSize;

            if (ViewModel.ReaderSettings.Autorotate)
            {
                SupportedOrientations = SupportedPageOrientation.PortraitOrLandscape;
            }
            else
            {
                SupportedOrientations = SupportedPageOrientation.Portrait;
            }

            if (ViewModel.Entity != null && ViewModel.Document != null)
            {
                _backgroundTile = null;
                await ReformatBook();
            }

            Center.Visibility = Visibility.Collapsed;
        }

        protected override async void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            HideAnnotation();

            if (_cacheItem != null)
            {
                _cacheItem.PlainToC = null;
            }
            if (e.Orientation == PageOrientation.Landscape || e.Orientation == PageOrientation.LandscapeLeft || e.Orientation == PageOrientation.LandscapeRight)
            {
                BookCover.Width = BookCoverBack.Width = 310.0 / 1.5;
                BookCover.Height = BookCoverBack.Height = 474.0 / 1.5;
                pageHeader.Margin = new Thickness(0, 0, 0, 40);
            }
            else
            {
                BookCover.Width = BookCoverBack.Width = 310;
                BookCover.Height = BookCoverBack.Height = 474;
                pageHeader.Margin = new Thickness(0, 0, 0, 75);
            }

            if (pageHeader.Visibility == Visibility.Collapsed)
            {
                await ReformatBook(e.Orientation);
            }
            base.OnOrientationChanged(e);

        }

        private async Task ReformatBook(PageOrientation pageOrient = 0)
        {
            if (ViewModel.Entity.TypeBook == Models.Book.BookType.Pdf)
            {
                ShowChaptersButton.IsEnabled = ToBookmarksButton.IsEnabled = AllBookmarksButton.IsEnabled = false;
                ReformatPdfBook(pageOrient);
            }
            else
            {
                await ReformatFb2Book(pageOrient);
            }
        }

        private async Task ReformatFb2Book(PageOrientation pageOrient = 0)
        {
            var actualHeight = ResolutionHelper.GetActualHeight();
            var actualWidth = ResolutionHelper.GetActualWidth();
            Debug.WriteLine("Reformat Book");
            var bounds = Orientation == PageOrientation.PortraitUp ? new XSize(actualWidth, actualHeight) : new XSize(actualHeight, actualWidth);
            PageOrientation orient = pageOrient != 0 ? pageOrient : Orientation;
            var curRes = ResolutionHelper.CurrentResolution;
            await Task.Run(() =>
            {
                double pageNumperPlaceHeight = 35 + 3 * (int)PaintingContext.Settings.Scale;

                switch (curRes)
                {
                    case Resolutions.WXGA:
                        pageNumperPlaceHeight *= 1.6;
                        break;
                    case Resolutions.HD720p:
                        pageNumperPlaceHeight *= 1.5;
                        break;
                }

                bounds = new XSize(bounds.Width, (int)(bounds.Height - pageNumperPlaceHeight - ((int)PaintingContext.Settings.Indent * 15)));

                try
                {
                    if (_cacheItem == null)
                    {
                        var cache = Scope.Resolve<IBookReadingContextService>();
                        if (cache.HasContext(ViewModel.Entity.Id) &&
                            cache.GetContext(ViewModel.Entity.Id).Orientation == orient)
                        {
                            _cacheItem = cache.GetContext(ViewModel.Entity.Id);
                            _cacheItem.Reader.OnMoveTo = Reader_onMoveTo;
                        }
                        else
                        {
                            cache.Clear();
                            Dispatcher.BeginInvoke(() =>
                            {
                                if (ViewModel.Document != null)
                                {
                                    AddBuySection(ViewModel.Entity.Price);
                                    var reader = new Athenaeum.BookReader(new DrawingContext(), ViewModel.Document,
                                    bounds);
                                    _cacheItem = new BookReadingContext { Reader = reader };
                                    cache.SetContext(ViewModel.Entity.Id, _cacheItem);
                                    _cacheItem.Reader.OnMoveTo = Reader_onMoveTo;
                                }
                            });
                        }
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            AddBuySection(ViewModel.Entity.Price);
                            _cacheItem.Reader.Reformat(bounds);
                        });
                        _cacheItem.PlainToC = _cacheItem.Reader.GetTableOfContent(TableOfContentBuildMode.Plain);
                    }

                    Dispatcher.BeginInvoke(() =>
                    {
                        if (_cacheItem != null)
                        {
                            if (_cacheItem.PlainToC == null)
                            {
                                _cacheItem.PlainToC = _cacheItem.Reader.GetTableOfContent(TableOfContentBuildMode.Plain);
                                SetChapter();
                            }
                            _cacheItem.Orientation = orient;

                            CurrentPageSlider.Maximum = _cacheItem.Reader.Formatter.Pages.Count;
                            CurrentPageSlider.Value = _cacheItem.Reader.CurrentPage;
                        }
                    });
                    if (ViewModel.Entity.Pages == 0)
                    {
                        if (_cacheItem != null) ViewModel.Entity.Pages = _cacheItem.Reader.Formatter.Pages.Count;
                        ViewModel.UpdateExistBook(ViewModel.Entity);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Unable to format book: {0}", ex);
                }

                Dispatcher.BeginInvoke(() =>
                {
                    if (_cacheItem != null)
                    {
                        PaintPane1Page();
                        pageHeader.ProgressIndicatorVisible = false;
                        ReadingCanva.ImageSource = null;
                    }
                });
            });
        }

        private async void ReformatPdfBook(PageOrientation pageOrient = 0)
        {
#if PDF_ENABLED
            if (BookReader == null)
            {
                BookReader = BookReaderBuilder.BuildBookReader(ViewModel.PdfDocument);
            }

            LayoutRoot.Background = new SolidColorBrush(Color.FromArgb(255,0,0,0));

            if (this.pdfViewCtrl == null)
            {
                this.pdfViewCtrl = new PDFViewCtrl();
                this.PdfContainer.Children.Insert(0, pdfViewCtrl);

                long maxMemory = DeviceStatus.ApplicationMemoryUsageLimit/(1024*1024);
                var alowedMemory = ((int) maxMemory - 100)/6;
                pdfViewCtrl.SetRenderedContentCacheSize(alowedMemory);
                pdfViewCtrl.SetProgressiveRendering(true);
                pdfViewCtrl.SetPagePresentationMode(pdftron.PDF.Controls.PagePresentationMode.e_single_continuous);
           //     pdfViewCtrl.SetPageViewMode(pdftron.PDF.Controls.PageViewMode.e_fit_page);
                
                string temp_path = (await ApplicationData.Current.LocalFolder.CreateFolderAsync("thumbCache", CreationCollisionOption.OpenIfExists)).Path;
                this.pdfViewCtrl.SetupThumbnails(true, true, /*true,*/ 600, 1, 200*200*4*900); // TOFIX

                pdfViewCtrl.SetBackgroundColor(Color.FromArgb(255, 1, 1, 1));
                pdfViewCtrl.SetDoc(ViewModel.PdfDocument);

                pdfViewCtrl.OnScale += OnScaleHandler;
                pdfViewCtrl.OnPageNumberChanged += PageNumberChangedHandler;
                pdfViewCtrl.Tap += TappedHandler;
                pdfViewCtrl.DoubleTap += DoubleTappedHandler;
                //pdfViewCtrlOriginalZoom = pdfViewCtrl.GetZoom();
            }
            CurrentPageSlider.Maximum = ViewModel.PdfDocument.GetPageCount();
            CurrentPageSlider.Value = pdfViewCtrl.GetCurrentPage();
            //PaintPdf1Page();
           pageHeader.ProgressIndicatorVisible = false;
           ReadingCanva.ImageSource = null;
           Debug.WriteLine("MEMORY USAGE: {0} mb", DeviceStatus.ApplicationCurrentMemoryUsage / (1024 * 1024));
#endif
        }

        private void PageNumberChangedHandler(int current_page, int num_pages)
        {
            CurrentPageSlider.Value = current_page;
            CheckBuyMenuVisible(current_page, num_pages);
        }

        private void TappedHandler(Object sender, GestureEventArgs e)
        {
            HandleReadingPanePdfTaps(e, 1);
        }

        private void DoubleTappedHandler(object sender, GestureEventArgs e)
        {
#if PDF_ENABLED
            if (pdfViewCtrlOriginalZoom == 0) pdfViewCtrlOriginalZoom = pdfViewCtrl.GetZoom();
            if (pdfViewCtrl.GetZoom() != pdfViewCtrlOriginalZoom)
                pdfViewCtrl.SetZoom(pdfViewCtrlOriginalZoom);
            else 
                pdfViewCtrl.SetZoom(2.5);

            pdfViewCtrl.SetBackgroundColor(Color.FromArgb(255, 1, 1, 1));
#endif
        }

        private void OnScaleHandler()
        { }

        async void Reader_onMoveTo(object sender)
        {
            if (sender is BookChaptersViewModel)
            {
                await ReformatBook();
            }
        }

        private void SetBlockIndex()
        {
            if (_cacheItem != null)
            {
                var xpointer = _cacheItem.Reader.GetXPointer();

                if (!string.IsNullOrEmpty(xpointer))
                {
                    ViewModel.BlockIndex = xpointer;
                }
            }
        }

        private void AddBuySection(double price)
        {
            string text = "Конец ознакомительного фрагмента. Для продолжения чтения купите книгу за " +
                        price.ToString(CultureInfo.InvariantCulture) + " рублей.";

            BuyBookText.Text = text;
        }

        private void ReadingPaneTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            HandleReadingPaneTaps(e, 1);
        }

        private void PaintPane1Page(bool recreateBackground = false)
        {
            var textImage = (WriteableBitmap)_cacheItem.Reader.Paint();

            double pageNumperPlaceHeight = 35 + 3 * (int)PaintingContext.Settings.Scale;

            switch (ResolutionHelper.CurrentResolution)
            {
                case Resolutions.WXGA:
                    pageNumperPlaceHeight *= 1.6;
                    break;
                case Resolutions.HD720p:
                    pageNumperPlaceHeight *= 1.5;
                    break;
            }
            if (recreateBackground) _backgroundTile = null;
            Color textImageColor;
            if (_backgroundTile == null)
            {
                _backgroundTile = CreateTileBackground(textImage.PixelWidth, (int)pageNumperPlaceHeight * 2);
                textImageColor = textImage.GetPixel(0, 0);
                for (int x = 0; x < _backgroundTile.PixelWidth; ++x)
                    for (int y = 0; y < _backgroundTile.PixelHeight; ++y)
                        _backgroundTile.SetPixel(x, y, textImageColor);
            }

            var brush = new ImageBrush
            {
                ImageSource = _backgroundTile
            };

            LayoutRoot.Background = brush;
            ReadingPaneImage.Source = textImage;

            ReadingPaneImageBottom.Source = _backgroundTile;
            ReadingPaneTextBottom.Text = _cacheItem.Reader.CurrentPage + " / " + _cacheItem.Reader.Formatter.Pages.Count;
        }

        private void PaintPane2Page()
        {
            var textImage = (WriteableBitmap)_cacheItem.Reader.Paint();

            double pageNumperPlaceHeight = 35 + 3 * (int)PaintingContext.Settings.Scale;

            switch (ResolutionHelper.CurrentResolution)
            {
                case Resolutions.WXGA:
                    pageNumperPlaceHeight *= 1.6;
                    break;
                case Resolutions.HD720p:
                    pageNumperPlaceHeight *= 1.5;
                    break;
            }

            if (_backgroundTile == null)
            {
                _backgroundTile = CreateTileBackground(textImage.PixelWidth, (int)pageNumperPlaceHeight * 2);
                Color textImageColor = textImage.GetPixel(0, 0);
                for (int x = 0; x < _backgroundTile.PixelWidth; ++x)
                    for (int y = 0; y < _backgroundTile.PixelHeight; ++y)
                        _backgroundTile.SetPixel(x, y, textImageColor);
            }


            var brush = new ImageBrush
            {
                ImageSource = _backgroundTile
            };

            LayoutRoot.Background = brush;
            ReadingPaneImage2.Source = textImage;

            ReadingPaneImageBottom2.Source = _backgroundTile;
            ReadingPaneTextBottom2.Text = _cacheItem.Reader.CurrentPage + " / " +
                                          _cacheItem.Reader.Formatter.Pages.Count;

        }

        //private void PaintPdf1Page()
        //{
        //    PaintPdfPage(pdfImage1);
        //}

        //private void PaintPdf2Page()
        //{
        //    PaintPdfPage(pdfImage2);
        //}

        //private async void PaintPdfPage(PanAndZoomImage canvas)
        //{
        //    //pdfImage2.Zoom = pdfImage1.Zoom = 0;
        //    Debug.WriteLine("MEMORY USAGE: {0} mb", DeviceStatus.ApplicationCurrentMemoryUsage / (1024 * 1024));
        //    canvas.Source = GetEmptyPageImage((int)canvas.ActualWidth, (int)canvas.ActualHeight);
        //    var renderedPage = await BookReader.RenderCurrentPage();      

        //    Dispatcher.BeginInvoke(() => { canvas.Source = renderedPage; });
        //    Debug.WriteLine("MEMORY USAGE: {0} mb", DeviceStatus.ApplicationCurrentMemoryUsage / (1024 * 1024));
        //}

        //private WriteableBitmap GetEmptyPageImage(int width, int height)
        //{
        //    if (BlackBackground == null)
        //    {
        //        BlackBackground = new WriteableBitmap(width, height);
        //        var textImageColor = Color.FromArgb(255, 0, 0, 0);
        //        for (int x = 0; x < width; ++x)
        //            for (int y = 0; y < height; ++y)
        //                BlackBackground.SetPixel(x, y, textImageColor);
        //    }
        //    return BlackBackground;
        //}

        private WriteableBitmap CreateTileBackground(int width, int height)
        {
            if (_tileBitmap == null || _tileBitmap.PixelWidth != width || _tileBitmap.PixelHeight != height || _lastTheme != PaintingContext.Settings.Theme)
            {
                string uriString = string.Empty;
                switch (PaintingContext.Settings.Theme)
                {
                    case Themes.Day:
                        uriString = "Assets\\BackgroundDay.png";
                        break;
                    case Themes.Night:
                        uriString = "Assets\\BackgroundNight.png";
                        break;
                    case Themes.Sepia:
                        uriString = "Assets\\BackgroundSepia.png";
                        break;
                }
                _lastTheme = PaintingContext.Settings.Theme;

                StreamResourceInfo imageResource = Application.GetResourceStream(new Uri(uriString, UriKind.Relative));
                WriteableBitmap tile = new WriteableBitmap(0, 0);
                tile.SetSource(imageResource.Stream);
                _tileBitmap = TileBitmap(tile, width, height);
            }

            return _tileBitmap;
        }

        private WriteableBitmap TileBitmap(WriteableBitmap tile, int width, int height)
        {
            const int sizeOfArgb = 4;

            int tileWidth = tile.PixelWidth;
            int tileHeight = tile.PixelHeight;

            // Copy the pixels line by line using fast BlockCopy
            var result = new WriteableBitmap(width, height);

            int x = 0;
            int y = 0;
            for (var line = 0; line < height; line++)
            {
                x = 0;
                for (var col = 0; col < width; col++)
                {
                    result.SetPixel(col, line, tile.GetPixel(x, y));

                    x++;
                    if (x == tileWidth)
                    {
                        x = 0;
                    }
                }

                y++;
                if (y == tileHeight)
                {
                    y = 0;
                }
            }

            return result;
        }

        //private void MoveToPage(int pageNumber)
        //{
        //    if (pageNumber < 1)
        //    {
        //        pageNumber = 1;
        //    }
        //    if (pageNumber > BookReader.PagesCount)
        //    {
        //        pageNumber = BookReader.PagesCount;
        //    }

        //    if (pageNumber != BookReader.CurrentPageIndex)
        //    {
        //        BookReader.MoveToPage(pageNumber);
        //        PaintPdf1Page();
        //    }
        //}

        private void MoveToPage(Athenaeum.BookReader reader, int pageNumber)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }
            if (pageNumber > reader.Formatter.Pages.Count())
            {
                pageNumber = reader.Formatter.Pages.Count();
            }

            if (pageNumber > reader.CurrentPage)
            {
                reader.MoveTo(pageNumber);
                PaintPane1Page();
            }
            else if (pageNumber < reader.CurrentPage)
            {
                reader.MoveTo(pageNumber);
                PaintPane1Page();
            }
        }

        private void MoveToPage(Athenaeum.BookReader reader, Pointer pointer, bool isBack)
        {
            if (!isBack)
            {
                reader.MoveTo(pointer);
                PaintPane1Page();
            }
            else
            {
                reader.MoveTo(pointer);
                PaintPane1Page();
            }
        }

        private void MoveToPageWithAnimation(Athenaeum.BookReader reader, Pointer pointer, bool isBack)
        {
            if (_inSlide)
            {
                return;
            }

            SlideLeftFadeIn.Stop();
            SlideLeftFadeOut.Stop();
            SlideLeftFadeInLandscape.Stop();
            SlideLeftFadeOutLandscape.Stop();

            if (!isBack)
            {
                ReadingPaneImage2.Source = ReadingPaneImage.Source;
                ReadingPaneImageBottom2.Source = ReadingPaneImageBottom.Source;
                ReadingPaneTextBottom2.Text = ReadingPaneTextBottom.Text;

                var bounds = Orientation == PageOrientation.PortraitUp ?
                        new XSize(ResolutionHelper.GetActualWidth(), ResolutionHelper.GetActualHeight()) :
                        new XSize(ResolutionHelper.GetActualHeight(), ResolutionHelper.GetActualWidth());

                ((TranslateTransform)(ReadingPane.RenderTransform)).X = bounds.Width;

                reader.MoveTo(pointer);
                PaintPane1Page();

                _inSlide = true;

                if (Orientation == PageOrientation.PortraitUp)
                {
                    Storyboard.SetTarget(SlideLeftFadeIn, ReadingPane);
                    SlideLeftFadeIn.Begin();
                }
                else
                {
                    Storyboard.SetTarget(SlideLeftFadeInLandscape, ReadingPane);
                    SlideLeftFadeInLandscape.Begin();
                }
            }
            else
            {
                ReadingPaneImage2.Source = ReadingPaneImage.Source;
                ReadingPaneImageBottom2.Source = ReadingPaneImageBottom.Source;
                ReadingPaneTextBottom2.Text = ReadingPaneTextBottom.Text;

                ((TranslateTransform)(ReadingPane.RenderTransform)).X = 0;

                reader.MoveTo(pointer);
                PaintPane2Page();

                _inSlide = true;

                if (Orientation == PageOrientation.PortraitUp)
                {
                    Storyboard.SetTarget(SlideLeftFadeOut, ReadingPane);
                    SlideLeftFadeOut.Begin();
                }
                else
                {
                    Storyboard.SetTarget(SlideLeftFadeOutLandscape, ReadingPane);
                    SlideLeftFadeOutLandscape.Begin();
                }
            }
        }

        //private void MoveToPdfPageWithAnimation(int pageNumber)
        //{
        //    if (_inSlide)
        //    {
        //        return;
        //    }

        //    if (pageNumber < 1)
        //    {
        //        pageNumber = 1;
        //    }
        //    if (pageNumber > BookReader.PagesCount)
        //    {
        //        pageNumber = BookReader.PagesCount;
        //    }

        //    SlideLeftFadeIn.Stop();
        //    SlideLeftFadeOut.Stop();
        //    SlideLeftFadeInLandscape.Stop();
        //    SlideLeftFadeOutLandscape.Stop();

        //    if (pageNumber > BookReader.CurrentPageIndex)
        //    {

        //        pdfImage2.Source = pdfImage1.Source;
        //        ReadingPaneImageBottom2.Source = ReadingPaneImageBottom.Source;
        //        ReadingPaneTextBottom2.Text = ReadingPaneTextBottom.Text;

        //        var bounds = Orientation == PageOrientation.PortraitUp ?
        //                new XSize(ResolutionHelper.GetActualWidth(), ResolutionHelper.GetActualHeight()) :
        //                new XSize(ResolutionHelper.GetActualHeight(), ResolutionHelper.GetActualWidth());

        //        ((TranslateTransform)(ReadingPane.RenderTransform)).X = bounds.Width;

        //        BookReader.MoveToPage(pageNumber);
        //        PaintPdf1Page();

        //        _inSlide = true;

        //        if (Orientation == PageOrientation.PortraitUp)
        //        {
        //            Storyboard.SetTarget(SlideLeftFadeIn, ReadingPane);
        //            SlideLeftFadeIn.Begin();
        //        }
        //        else
        //        {
        //            Storyboard.SetTarget(SlideLeftFadeInLandscape, ReadingPane);
        //            SlideLeftFadeInLandscape.Begin();
        //        }
        //    }
        //    else if (pageNumber < BookReader.CurrentPageIndex)
        //    {
        //        pdfImage2.Source = pdfImage1.Source;
        //        ReadingPaneImageBottom2.Source = ReadingPaneImageBottom.Source;
        //        ReadingPaneTextBottom2.Text = ReadingPaneTextBottom.Text;

        //        ((TranslateTransform)(ReadingPane.RenderTransform)).X = 0;

        //        BookReader.MoveToPage(pageNumber);
        //        PaintPdf2Page();

        //        _inSlide = true;

        //        if (Orientation == PageOrientation.PortraitUp)
        //        {
        //            Storyboard.SetTarget(SlideLeftFadeOut, ReadingPane);
        //            SlideLeftFadeOut.Begin();
        //        }
        //        else
        //        {
        //            Storyboard.SetTarget(SlideLeftFadeOutLandscape, ReadingPane);
        //            SlideLeftFadeOutLandscape.Begin();
        //        }
        //    }
        //    else
        //    {
        //        if (Orientation == PageOrientation.PortraitUp)
        //        {
        //            ((TranslateTransform)(ReadingPane.RenderTransform)).X = 0;
        //        }
        //        else
        //        {
        //            ((TranslateTransform)(ReadingPane.RenderTransform)).X = 0;
        //        }
        //    }
        //}

        private void MoveToPageWithAnimation(Athenaeum.BookReader reader, int pageNumber)
        {
            if (_inSlide)
            {
                return;
            }

            if (pageNumber < 1)
            {
                pageNumber = 1;
            }
            if (pageNumber > reader.Formatter.Pages.Count())
            {
                pageNumber = reader.Formatter.Pages.Count();
            }

            SlideLeftFadeIn.Stop();
            SlideLeftFadeOut.Stop();
            SlideLeftFadeInLandscape.Stop();
            SlideLeftFadeOutLandscape.Stop();

            if (pageNumber > reader.CurrentPage)
            {
                ReadingPaneImage2.Source = ReadingPaneImage.Source;
                ReadingPaneImageBottom2.Source = ReadingPaneImageBottom.Source;
                ReadingPaneTextBottom2.Text = ReadingPaneTextBottom.Text;

                var bounds = Orientation == PageOrientation.PortraitUp ?
                        new XSize(ResolutionHelper.GetActualWidth(), ResolutionHelper.GetActualHeight()) :
                        new XSize(ResolutionHelper.GetActualHeight(), ResolutionHelper.GetActualWidth());

                ((TranslateTransform)(ReadingPane.RenderTransform)).X = bounds.Width;

                reader.MoveTo(pageNumber);
                PaintPane1Page();

                _inSlide = true;

                if (Orientation == PageOrientation.PortraitUp)
                {
                    Storyboard.SetTarget(SlideLeftFadeIn, ReadingPane);
                    SlideLeftFadeIn.Begin();
                }
                else
                {
                    Storyboard.SetTarget(SlideLeftFadeInLandscape, ReadingPane);
                    SlideLeftFadeInLandscape.Begin();
                }
            }
            else if (pageNumber < reader.CurrentPage)
            {
                ReadingPaneImage2.Source = ReadingPaneImage.Source;
                ReadingPaneImageBottom2.Source = ReadingPaneImageBottom.Source;
                ReadingPaneTextBottom2.Text = ReadingPaneTextBottom.Text;

                ((TranslateTransform)(ReadingPane.RenderTransform)).X = 0;

                reader.MoveTo(pageNumber);
                PaintPane2Page();

                _inSlide = true;

                if (Orientation == PageOrientation.PortraitUp)
                {
                    Storyboard.SetTarget(SlideLeftFadeOut, ReadingPane);
                    SlideLeftFadeOut.Begin();
                }
                else
                {
                    Storyboard.SetTarget(SlideLeftFadeOutLandscape, ReadingPane);
                    SlideLeftFadeOutLandscape.Begin();
                }
            }
            else
            {
                if (Orientation == PageOrientation.PortraitUp)
                {
                    ((TranslateTransform)(ReadingPane.RenderTransform)).X = 0;
                }
                else
                {
                    ((TranslateTransform)(ReadingPane.RenderTransform)).X = 0;
                }
            }
        }

        private void HandleReadingPaneTaps(GestureEventArgs e, int numberOfTaps)
        {
            if (ViewModel.Entity.TypeBook == Models.Book.BookType.Fb2)
            {
                HandleReadingPaneFb2Taps(e, numberOfTaps);
            }
            else
            {
                HandleReadingPanePdfTaps(e, numberOfTaps);
            }
        }

        private void HandleReadingPanePdfTaps(GestureEventArgs e, int numberOfTaps)
        {
            if (numberOfTaps > 1) return;
            bool moved = false;
            var point = e.GetPosition(ReadingPane);
            if (point.Y > ReadingPane.ActualHeight * 0.75 || point.X > ReadingPane.ActualWidth * 0.75)
            {
                //if (ViewModel.ReaderSettings.AnimationMoveToPage)
                //    MoveToPdfPageWithAnimation(BookReader.CurrentPageIndex + 1);
                //else
                //{
                //    MoveToPage(BookReader.CurrentPageIndex + 1);
                //}

                moved = true;
            }
            else if (point.Y < ReadingPane.ActualHeight * 0.25 || point.X < ReadingPane.ActualWidth * 0.25)
            {
                //if (ViewModel.ReaderSettings.AnimationMoveToPage)
                //    MoveToPdfPageWithAnimation(BookReader.CurrentPageIndex - 1);
                //else
                //{
                //    MoveToPage(BookReader.CurrentPageIndex - 1);
                //}

                moved = true;
            }
            menuVisible(moved);
        }

        private void HandleReadingPaneFb2Taps(GestureEventArgs e, int numberOfTaps)
        {
            bool moved = false;

            if (_cacheItem != null && !_menuVisible)
            {
                System.Windows.Point point = e.GetPosition(ReadingPane);

                float factor = (float)App.Current.Host.Content.ScaleFactor / 100;
                XPoint scaleXpoint = new XPoint((int)(point.X * factor), (int)(point.Y * factor));
                bool isHD = ((ResolutionHelper.CurrentResolution == Resolutions.HD720p) ? true : false);

                if (ResolutionHelper.CurrentResolution == Resolutions.WVGA) scaleXpoint.Y -= 90;
                else scaleXpoint.Y -= 80;
                var linkPointer = _cacheItem.Reader.GetLinkPointer(scaleXpoint, isHD);

                if (linkPointer != null)
                {
                    Debug.WriteLine("linkPointer clicked");
                    // move to link
                    if (linkPointer is Pointer)
                    {
                        _history.Push(_cacheItem.Reader.Formatter.Pages[_cacheItem.Reader.CurrentPage - 1]);

                        if (ViewModel.ReaderSettings.AnimationMoveToPage)
                            MoveToPageWithAnimation(_cacheItem.Reader, linkPointer as Pointer, false);
                        else MoveToPage(_cacheItem.Reader, linkPointer as Pointer, false);

                        CountMoves();
                        moved = true;
                    }
                    else if (linkPointer is string)
                    {
                        try
                        {
                            Launcher.LaunchUriAsync(new Uri(linkPointer as string, UriKind.RelativeOrAbsolute));
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }
                    else if (linkPointer is SectionElement)
                    {
                        ShowAnnotation(linkPointer as SectionElement);
                        moved = true;
                    }
                }
                else
                {
                    // listing
                    if (point.Y > ReadingPane.ActualHeight * 0.75 || point.X > ReadingPane.ActualWidth * 0.75)
                    {
                        if (ViewModel.ReaderSettings.AnimationMoveToPage) MoveToPageWithAnimation(_cacheItem.Reader, _cacheItem.Reader.CurrentPage + numberOfTaps);
                        else MoveToPage(_cacheItem.Reader, _cacheItem.Reader.CurrentPage + numberOfTaps);

                        CheckBuyMenuVisible();
                        CountMoves();
                        moved = true;
                    }
                    else if (point.Y < ReadingPane.ActualHeight * 0.25 || point.X < ReadingPane.ActualWidth * 0.25)
                    {
                        if (ViewModel.ReaderSettings.AnimationMoveToPage) MoveToPageWithAnimation(_cacheItem.Reader, _cacheItem.Reader.CurrentPage - numberOfTaps);
                        else MoveToPage(_cacheItem.Reader, _cacheItem.Reader.CurrentPage - numberOfTaps);

                        CheckBuyMenuVisible();
                        CountMoves();
                        moved = true;
                    }
                }
            }

            menuVisible(moved);
        }

        private void ShowAnnotation(SectionElement linkPointer)
        {
            var buildedMessage = new StringBuilder();
            foreach (var lChild in linkPointer.Children)
            {
                buildedMessage.AppendLine(lChild.ToString());
            }

            AnnotationPopup.IsOpen = true;
            AnnotaTextBlock.Text = buildedMessage.ToString();
            var blockHeight = (ActualHeight / 3);
            var blockWidth = ActualWidth;
            AnnotationScrollViewer.Height = blockHeight;
            (AnnotationScrollViewer.RenderTransform as TranslateTransform).Y = ActualHeight;
            AnnotationScrollViewer.Width = blockWidth;
            AnnotationScrollViewer.ScrollToVerticalOffset(0);
            (AnimationShowAnnotation.Children[1] as DoubleAnimation).To = ActualHeight - blockHeight;
            AnimationShowAnnotation.Begin();
        }

        private void HideAnnotation()
        {
            if (AnnotationPopup.IsOpen)
            {
                (AnimationHideAnnotation.Children[1] as DoubleAnimation).To = ActualHeight;
                AnimationHideAnnotation.Begin();
            }
        }

        private void menuVisible(bool moved = false, bool isFullVisible = false)
        {

            if (_menuVisible || !moved)// && numberOfTaps > 1) )
            {
                if (_menuVisible)
                {
                    hide();
                    _fullVisible = false;
                }
                else
                {
                    if (ViewModel.Entity.TypeBook == Models.Book.BookType.Fb2)
                    {
                        if (_cacheItem != null) CurrentPageSlider.Value = _cacheItem.Reader.CurrentPage;
                    }
                    else if (ViewModel.Entity.TypeBook == Models.Book.BookType.Pdf)
                    {
#if PDF_ENABLED
                        CurrentPageSlider.Value = pdfViewCtrl.GetCurrentPage();
#endif
                    }

                    if (isFullVisible) showFull();
                    else show();
                }

                _menuVisible = !_menuVisible;
                if (isFullVisible) _fullVisible = !_fullVisible;
            }
        }

        private void CheckBuyMenuVisible()
        {
            if (ViewModel != null)
            {
                if (ViewModel.Entity != null && !ViewModel.Entity.IsMyBook &&
                    _cacheItem.Reader.Formatter.Pages.Count == _cacheItem.Reader.CurrentPage &&
                    (int)BuyBookMenu.Height == 0)
                {
                    if (ViewModel.UserInformation != null && ViewModel.UserInformation.AccountType != (int)AccountTypeEnum.AccountTypeLibrary)
                        ShowBuyBookMenu.Begin();
                    _isBuyShowed = true;
                }
                else if (ViewModel.Entity != null && !ViewModel.Entity.IsMyBook &&
                    _cacheItem.Reader.Formatter.Pages.Count != _cacheItem.Reader.CurrentPage &&
                    (int)BuyBookMenu.Height > 0)
                {
                    HideBuyBookMenu.Begin();
                    _isBuyShowed = false;
                }
            }
        }

        private void CheckBuyMenuVisible(int currentPage, int totalPages)
        {
            if (ViewModel != null)
            {
                if (ViewModel.Entity != null && !ViewModel.Entity.IsMyBook &&
                    totalPages == currentPage && (int)BuyBookMenu.Height == 0)
                {
                    if (ViewModel.UserInformation != null && ViewModel.UserInformation.AccountType != (int)AccountTypeEnum.AccountTypeLibrary)
                        ShowBuyBookMenu.Begin();
                    _isBuyShowed = true;
                }
                else if (ViewModel.Entity != null && !ViewModel.Entity.IsMyBook &&
                    totalPages != currentPage && (int)BuyBookMenu.Height > 0)
                {
                    HideBuyBookMenu.Begin();
                    _isBuyShowed = false;
                }
            }
        }

        private void BrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            PaintingContext.Settings.Brightness = (float)e.NewValue;

            if (BrightnessControl != null) BrightnessControl.Opacity = 1.0 - e.NewValue;

            if (ViewModel != null && ViewModel.Entity != null)
            {
                if (ViewModel.Entity.TypeBook == Models.Book.BookType.Fb2)
                {
                    if (_cacheItem != null) PaintPane1Page(true);
                }
                else if (ViewModel.Entity.TypeBook == Models.Book.BookType.Pdf)
                {
                }
            }
        }

        private void CurrentPageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ViewModel != null && ViewModel.Entity != null)
            {
                if (ViewModel.Entity.TypeBook == Models.Book.BookType.Fb2)
                {
                    if (_cacheItem != null && _cacheItem.PlainToC != null)
                    {
                        var node =
                            _cacheItem.PlainToC.Nodes.OrderBy(n => n.PageNumber)
                                .LastOrDefault(n => n.PageNumber <= (int)e.NewValue);
                        if (node != null) SectionTitle.Text = node.Text.TrimEnd().Replace("\r\n", " - ");
                    }
                }
                else if (ViewModel.Entity.TypeBook == Models.Book.BookType.Pdf)
                {
                    //SectionTitle.Text = (BookReader as PdfBookReader).GetPageTitle((int) e.NewValue);
                }

                if (CurrentPageSlider != null)
                    ViewModel.Entity.ReadedPercent =
                        (int)Math.Ceiling(CurrentPageSlider.Value / (CurrentPageSlider.Maximum / 100));
            }
        }

        private void CurrentPageSlider_ManipulationStarted(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
        }

        private void CurrentPageSlider_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            int newPage = (int)CurrentPageSlider.Value;
            if (ViewModel != null && ViewModel.Entity != null)
            {
                if (ViewModel.Entity.TypeBook == Models.Book.BookType.Fb2)
                {
                    if (_cacheItem != null && newPage != _cacheItem.Reader.CurrentPage)
                    {
                        if (ViewModel.ReaderSettings.AnimationMoveToPage)
                            MoveToPageWithAnimation(_cacheItem.Reader, newPage);
                        else MoveToPage(_cacheItem.Reader, newPage);
                        CurrentPageSlider.Value = _cacheItem.Reader.CurrentPage;
                        SetChapter();
                        CheckBuyMenuVisible();
                    }
                }
                else if (ViewModel.Entity.TypeBook == Models.Book.BookType.Pdf)
                {
                    //if (ViewModel.ReaderSettings.AnimationMoveToPage) MoveToPdfPageWithAnimation(newPage);
                    //else MoveToPage(newPage);
                    //CurrentPageSlider.Value = BookReader.CurrentPageIndex;
#if PDF_ENABLED
                    pdfViewCtrl.SetCurrentPage(newPage);
                    CurrentPageSlider.Value = pdfViewCtrl.GetCurrentPage();
#endif
                }
            }
        }

        private void SetChapter()
        {
            var node = _cacheItem.PlainToC.Nodes.OrderBy(n => n.PageNumber).LastOrDefault(n => n.PageNumber <= _cacheItem.Reader.CurrentPage);
            if (node != null)
            {
                SectionTitle.Text = node.Text.TrimEnd().Replace("\r\n", " - ");
            }
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (_history.Count > 0)
            {
                var pointer = _history.Pop();

                if (ViewModel.ReaderSettings.AnimationMoveToPage) MoveToPageWithAnimation(_cacheItem.Reader, pointer, true);
                else MoveToPage(_cacheItem.Reader, pointer, true);

                e.Cancel = true;
            }
            else
            {
                if (!NavigationService.CanGoBack)
                {
                    NavigationService.Navigate(new Uri("/Views/Main.xaml", UriKind.RelativeOrAbsolute));
                    NavigationService.RemoveBackEntry();
                    e.Cancel = true;
                }
                else base.OnBackKeyPress(e);
            }
        }

        private void FullSize_Tap(object sender, GestureEventArgs e)
        {
            if (!_fullVisible)
            {
                showFull();
            }
            else
            {
                hideFull();
            }

            _fullVisible = !_fullVisible;
        }

        private async void ToBookmarks_OnTap(object sender, GestureEventArgs e)
        {
            try
            {
                await AddBookmark(false);
            }
            catch (Exception) { }
        }

        private async Task AddBookmark(bool isCurrent)
        {
            if (_cacheItem != null && _cacheItem.Reader != null)
            {
                string chapter = string.Empty;

                if (_cacheItem.PlainToC != null)
                {
                    var nodes = _cacheItem.PlainToC.Nodes.Where(n => n.Pointer.BlockId <= _cacheItem.Reader.Pointer.BlockId).ToList();

                    for (int i = nodes.Count - 1; i >= 0; i--)
                    {
                        chapter = nodes[i].Text.TrimEnd().Replace("\r\n", " - ");
                        if (!string.IsNullOrEmpty(chapter))
                        {
                            break;
                        }
                    }
                }

                string xpointer = _cacheItem.Reader.GetXPointer();

                var blockId = Math.Abs(_cacheItem.Reader.Pointer.BlockId);
                var block = _cacheItem.Reader.Formatter.FindBlockById(blockId);

                if (block != null && !string.IsNullOrEmpty(xpointer))
                {
                    var text = block.Element.ToString();
                    string percent = Convert.ToString((int)Math.Ceiling(CurrentPageSlider.Value / (CurrentPageSlider.Maximum / 100)));

                    if (isCurrent)
                    {
                        try
                        {
                            ViewModel.SetCurrentBookmark(text, xpointer, chapter, percent);
                        }
                        catch (Exception e)
                        {
                        }
                    }
                    else
                    {
                        await ViewModel.AddBookmark(text, xpointer, chapter, false, percent);
                    }

                    if (!isCurrent)
                    {
                        MessageBox.Show("Закладка добавлена");
                    }
                }
            }
        }

        private void AddToHomeScreen(object sender, GestureEventArgs e)
        {
            var task = new Task(async () =>
            {
                try
                {
                    var tileData = await CreateFlipTileData();
                    if (tileData != null)
                    {
                        var tileUri = string.Format("/Views/Reader.xaml?id={0}", ViewModel.Entity.Id);
                        ShellTile.Create(new Uri(tileUri, UriKind.RelativeOrAbsolute), tileData, true);
                    }
                }
                catch (Exception ex) { }
            });
            task.RunSynchronously();
        }

        private void UpdateTileData(ShellTile tile)
        {
            if (tile != null)
            {
                var task = new Task(async () =>
                {
                    try
                    {
                        var tileData = await CreateFlipTileData();
                        if (tileData != null) tile.Update(tileData);
                    }
                    catch (Exception ex) { }
                });
                task.RunSynchronously();
            }
        }

        private async Task<FlipTileData> CreateFlipTileData()
        {
            var book = ViewModel.Entity;
            if (book != null && !string.IsNullOrEmpty(book.Cover))
            {
                var activeTile = new TileBook { BookTitle = book.Description.Hidden.TitleInfo.BookTitle, Cover = book.Cover };
                string filePath = await TileImageEditor.CreateTileImage(activeTile);
                string wideFilePath = await TileImageEditor.CreateWideTileImage(activeTile);
                if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(wideFilePath))
                {
                    Uri iUri = new Uri("isostore:/" + filePath, UriKind.Absolute);
                    Uri iWideUri = new Uri("isostore:/" + wideFilePath, UriKind.Absolute);

                    var appTileData = new FlipTileData
                    {
                        Title = GetAppTitle(),
                        BackgroundImage = iUri,
                        WideBackgroundImage = iWideUri,
                        SmallBackgroundImage = iUri
                    };
                    if (ViewModel.Entity.ReadedPercent > 0)
                    {
                        var backTitle = string.Format("Прочитано: {0}%", ViewModel.Entity.ReadedPercent);
                        appTileData.BackTitle = backTitle;
                        appTileData.BackBackgroundImage = iUri;
                        appTileData.WideBackBackgroundImage = iWideUri;
                        Debug.WriteLine(backTitle);
                    }
                    return appTileData;
                }
            }
            return null;
        }

        private static string GetAppTitle()
        {
            var settings = new XmlReaderSettings();
            settings.XmlResolver = new XmlXapResolver();

            using (XmlReader rdr = XmlReader.Create("WMAppManifest.xml", settings))
            {
                rdr.ReadToDescendant("App");
                if (!rdr.IsStartElement())
                {
                    throw new FormatException("WMAppManifest.xml is missing App");
                }

                return rdr.GetAttribute("Title");
            }
        }

        private void Body_Tap(object sender, RoutedEventArgs e)
        {
            if (mainPopup.IsOpen)
            {
                hideMainPopup(true);
            }
            if (switchPurchase.IsOpen)
            {
                hideSwitchPopup(true);
            }
            if (AnnotationPopup.IsOpen)
            {
                HideAnnotation();
            }
        }

        private void showPopup(Popup pop, Canvas popRoot)
        {
            pop.IsOpen = true;
            var storyboard = new Storyboard();
            var translateTransformTop = popRoot.RenderTransform as TranslateTransform;
            var topAnimation = new DoubleAnimation();
            topAnimation.To = 0;
            var duration = new TimeSpan(0, 0, 0, 0, 400);
            topAnimation.Duration = duration;
            Storyboard.SetTarget(topAnimation, translateTransformTop);
            Storyboard.SetTargetProperty(topAnimation, new
            PropertyPath(TranslateTransform.YProperty));
            storyboard.Children.Add(topAnimation);
            storyboard.Begin();
        }

        private void hidePopup(Canvas pop, double pos, EventHandler func)
        {
            var storyboard = new Storyboard();
            var translateTransformTop = pop.RenderTransform as TranslateTransform;
            var topAnimation = new DoubleAnimation();
            topAnimation.To = pos;
            var duration = new TimeSpan(0, 0, 0, 0, 400);
            topAnimation.Duration = duration;
            Storyboard.SetTarget(topAnimation, translateTransformTop);
            Storyboard.SetTargetProperty(topAnimation, new
            PropertyPath(TranslateTransform.YProperty));
            topAnimation.Completed += func;
            storyboard.Children.Add(topAnimation);
            storyboard.Begin();
        }

        private async void showMainPopup()
        {
            Fade.Visibility = Visibility.Visible;
            Animatio1n_FadeIn.Begin();
            showPopup(mainPopup, mainPopupRoot);
            await ViewModel.UpdatePrice();
        }

        private void hideMainPopup(bool widthFade = false)
        {
            if (widthFade) Animation1_FadeOut.Begin();
            else Fade.Visibility = Visibility.Collapsed;
            hidePopup(mainPopupRoot, -370, hideAnimationEnd);
        }

        private void hideSwitchPopup(bool widthFade = false)
        {
            if (widthFade) Animation2_FadeOut.Begin();
            else Fade2.Visibility = Visibility.Collapsed;
            hidePopup(switchPurchaseRoot, -555, hideSwitchPopupAnimationEnd);
        }

        private void showSwitchPopup()
        {
            Fade2.Visibility = Visibility.Visible;
            Fade2.Opacity = 0.6;
            if (ViewModel.SimCardDetected) switchPurchaseBack.Height = 555;
            else switchPurchaseBack.Height = 445;
            showPopup(switchPurchase, switchPurchaseRoot);
        }

        private void hideAnimationEnd(object sender, EventArgs e)
        {
            mainPopup.IsOpen = false;
        }

        private void hideSwitchPopupAnimationEnd(object sender, EventArgs e)
        {
            switchPurchase.IsOpen = false;
        }

        private async void BuyBookMenuTap(object sender, GestureEventArgs e)
        {
            Analytics.Instance.sendMessage(Analytics.ActionBuyFromFragment);
            try
            {
                //At comments sections
                if (_cacheItem.Reader.Formatter.Document.Bodies.Count > 1)
                {
                    var body = _cacheItem.Reader.Formatter.Document.Bodies[1];
                    var firstItem = body.Children[0];

                    var block = _cacheItem.Reader.Formatter.FindBlockForElement(firstItem);
                    var p = new Pointer(block.Id - 3);

                    _cacheItem.Reader.MoveTo(p);

                    CurrentPageSlider.Value = _cacheItem.Reader.CurrentPage;

                    PaintPane1Page();
                }

                HideBuyBookMenu.Begin();

                ViewModel.BuyBook.Execute(null);

                //await ViewModel.BuyBookAsync();

                //if( ViewModel.Entity.IsMyBook )
                //{
                //    HideBuyBookMenu.Begin();
                //}
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private void Image_OnFlick(object sender, FlickGestureEventArgs e)
        {
            var horizontalVelocity = e.Direction == System.Windows.Controls.Orientation.Horizontal;
            var absVelocity = horizontalVelocity ? Math.Abs(e.HorizontalVelocity) : Math.Abs(e.VerticalVelocity);

            if (absVelocity > 400)
            {
                if (_cacheItem != null && !_menuVisible)
                {
                    // listing
                    if ((horizontalVelocity ? e.HorizontalVelocity < 0 : e.VerticalVelocity < 0))
                    {
                        if (ViewModel.ReaderSettings.AnimationMoveToPage) MoveToPageWithAnimation(_cacheItem.Reader, _cacheItem.Reader.CurrentPage + 1);
                        else MoveToPage(_cacheItem.Reader, _cacheItem.Reader.CurrentPage + 1);
                    }
                    else
                    {
                        if (ViewModel.ReaderSettings.AnimationMoveToPage) MoveToPageWithAnimation(_cacheItem.Reader, _cacheItem.Reader.CurrentPage - 1);
                        else MoveToPage(_cacheItem.Reader, _cacheItem.Reader.CurrentPage - 1);
                    }

                    CheckBuyMenuVisible();

                    CountMoves();
                }
            }
        }

        private void CountMoves()
        {
            _moveCount++;
            if (_moveCount > 20)
            {
                _moveCount = 0;
                SaveCurrentBookmarkAsync();
            }
        }

        private void SlideOnCompletedOut(object sender, EventArgs e)
        {
            _inSlide = false;

            //pdfImage1.Source = pdfImage2.Source;
            ReadingPaneImage.Source = ReadingPaneImage2.Source;
            ReadingPaneTextBottom.Text = ReadingPaneTextBottom2.Text;
            ((TranslateTransform)(ReadingPane.RenderTransform)).X = 0;
        }

        private void SlideOnCompletedIn(object sender, EventArgs e)
        {
            _inSlide = false;

            ReadingPaneImage2.Source = null;
            //  pdfImage2.Source = null;
        }

        private void showFull()
        {
            var storyboard = new Storyboard();
            var translateTransformBottom = Bottom.RenderTransform as TranslateTransform;
            var translateTransformTop = Top.RenderTransform as TranslateTransform;
            var bottomAnimation = new DoubleAnimation();
            var topAnimation = new DoubleAnimation();
            topAnimation.To = 0;

            if (!_isBuyShowed) bottomAnimation.To = -Bottom.ActualHeight;
            else bottomAnimation.To = -Bottom.ActualHeight + BuyBookMenu.ActualHeight;

            var duration = new TimeSpan(0, 0, 1);
            bottomAnimation.Duration = duration;
            topAnimation.Duration = duration;
            var cubicEase = new CubicEase();
            cubicEase.EasingMode = EasingMode.EaseOut;
            bottomAnimation.EasingFunction = cubicEase;
            topAnimation.EasingFunction = cubicEase;
            Storyboard.SetTarget(bottomAnimation, translateTransformBottom);
            Storyboard.SetTargetProperty(bottomAnimation, new
            PropertyPath(TranslateTransform.YProperty));
            Storyboard.SetTarget(topAnimation, translateTransformTop);
            Storyboard.SetTargetProperty(topAnimation, new
            PropertyPath(TranslateTransform.YProperty));

            storyboard.Children.Add(bottomAnimation);
            storyboard.Children.Add(topAnimation);
            storyboard.Begin();
        }

        private void hideFull()
        {
            var storyboard = new Storyboard();
            var translateTransform = Bottom.RenderTransform as TranslateTransform;
            var bottomAnimation = new DoubleAnimation();

            if (!_isBuyShowed) bottomAnimation.To = -(Bottom.ActualHeight - 236);
            else bottomAnimation.To = -(Bottom.ActualHeight - (236 + BuyBookMenu.ActualHeight));

            bottomAnimation.Duration = new TimeSpan(0, 0, 0, 1, 0);
            var cubicEase = new CubicEase();
            cubicEase.EasingMode = EasingMode.EaseOut;
            bottomAnimation.EasingFunction = cubicEase;
            Storyboard.SetTarget(bottomAnimation, translateTransform);
            Storyboard.SetTargetProperty(bottomAnimation, new
            PropertyPath(TranslateTransform.YProperty));
            storyboard.Children.Add(bottomAnimation);
            storyboard.Begin();
        }

        private void hide()
        {
            var storyboard = new Storyboard();
            var translateTransformBottom = Bottom.RenderTransform as TranslateTransform;
            var translateTransformTop = Top.RenderTransform as TranslateTransform;
            var bottomAnimation = new DoubleAnimation();
            var topAnimation = new DoubleAnimation();
            topAnimation.To = -170;
            bottomAnimation.To = 0;
            var duration = new TimeSpan(0, 0, 1);
            bottomAnimation.Duration = duration;
            topAnimation.Duration = duration;
            var cubicEase = new CubicEase();
            cubicEase.EasingMode = EasingMode.EaseOut;
            bottomAnimation.EasingFunction = cubicEase;
            topAnimation.EasingFunction = cubicEase;
            Storyboard.SetTarget(bottomAnimation, translateTransformBottom);
            Storyboard.SetTargetProperty(bottomAnimation, new
            PropertyPath(TranslateTransform.YProperty));
            Storyboard.SetTarget(topAnimation, translateTransformTop);
            Storyboard.SetTargetProperty(topAnimation, new
            PropertyPath(TranslateTransform.YProperty));

            storyboard.Children.Add(bottomAnimation);
            storyboard.Children.Add(topAnimation);
            storyboard.Begin();
        }

        private void show()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
            _timer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += RefreshTimer;
            _timer.Start();

            var storyboard = new Storyboard();
            var translateTransformBottom = Bottom.RenderTransform as TranslateTransform;
            var translateTransformTop = Top.RenderTransform as TranslateTransform;
            var bottomAnimation = new DoubleAnimation();
            var topAnimation = new DoubleAnimation();
            topAnimation.To = 0;
            if (!_isBuyShowed) bottomAnimation.To = -(Bottom.ActualHeight - 236);
            else bottomAnimation.To = -(Bottom.ActualHeight - (236 + BuyBookMenu.ActualHeight));
            var duration = new TimeSpan(0, 0, 1);
            bottomAnimation.Duration = duration;
            topAnimation.Duration = duration;
            var cubicEase = new CubicEase();
            cubicEase.EasingMode = EasingMode.EaseOut;
            bottomAnimation.EasingFunction = cubicEase;
            topAnimation.EasingFunction = cubicEase;
            Storyboard.SetTarget(bottomAnimation, translateTransformBottom);
            Storyboard.SetTargetProperty(bottomAnimation, new
            PropertyPath(TranslateTransform.YProperty));
            Storyboard.SetTarget(topAnimation, translateTransformTop);
            Storyboard.SetTargetProperty(topAnimation, new
            PropertyPath(TranslateTransform.YProperty));

            storyboard.Children.Add(bottomAnimation);
            storyboard.Children.Add(topAnimation);
            storyboard.Begin();
        }

        public void ExpiredCallBack(Models.Book book)
        {
            if (ViewModel.Entity.Id.Equals(book.Id))
            {
                var cancelArgs = new CancelEventArgs();
                do
                {
                    cancelArgs.Cancel = false;
                    OnBackKeyPress(cancelArgs);
                } while (cancelArgs.Cancel == true);
            }
        }

        private void ShowMyBooks(object sender, GestureEventArgs e)
        {
            var tsk = new Task(async () =>
            {
                await ViewModel.UpdateEntity();
                SaveCurrentBookmarkAsync();
                ViewModel.ShowMyBooks.Execute(null);
            });
            tsk.RunSynchronously();
        }

        private void AnotationHided(object sender, EventArgs e)
        {
            AnnotationPopup.IsOpen = false;
        }
    }

    public partial class ReaderFitting : ViewModelPage<ReaderViewModel>
    {
    }
}