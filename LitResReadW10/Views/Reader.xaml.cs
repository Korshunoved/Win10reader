using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Digillect.Mvvm;
using Digillect.Mvvm.UI;

using LitRes.Helpers;
using LitRes.LibraryTools;
//using LitRes.LiveTileAgent;
using LitRes.Models;
using LitRes.ValueConverters;
using LitRes.ViewModels;
using System.ComponentModel;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web;
using Autofac;
using Digillect;
//using Digillect.ComponentModel;
using Digillect.Mvvm.Services;
using FictionBook;
using LitRes.Services;
using LitResReadW10;
using LitResReadW10.Controls;
using LitResReadW10.Crypto;
using LitResReadW10.Helpers;
using TextElement = FictionBook.TextElement;

namespace LitRes.Views
{
    [View("Reader")]
    [ViewParameter("bookmark", Required = false)]
    [ViewParameter("chapter", Required = false)]
    [ViewParameter("BookEntity", typeof(Models.Book), Required = true)]
    [ViewParameter("filetoken", Required = false)]

    public partial class Reader : IExpiredCallBack
    {
        private bool _menuVisible;
        private bool _fullVisible;
        private int _moveCount;
        private double _fractionRead;
        private bool _syncInProgress;
        private readonly Object _thisLock = new object();
        private bool _isBuyShowed;
    
        private readonly INavigationService _navigationService = ((App)App.Current).Scope.Resolve<INavigationService>();

        public bool IsHardwareBack => ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons");

        public static Reader Instance;

        public FlipView CurrentFlipView;

        private BackgroundWorker bw;

        #region Constructors/Disposer
        public Reader()
        {
            Debug.WriteLine("Reader()");
            ControlPanel.Instance.ReaderMode();

            InitializeComponent();

            Loaded += ReaderLoaded;
            Unloaded += (sender, args) =>
            {
                LocalBroadcastReciver.Instance.PropertyChanging -= Instance_PropertyChanging;
            };
            LocalBroadcastReciver.Instance.PropertyChanging += Instance_PropertyChanging;
            DisplayInformation.GetForCurrentView().OrientationChanged += OnOrientationChanged;
            Window.Current.SizeChanged += Current_SizeChanged;
            Instance = this;
            if (ExpirationGuardian.Instance != null) ExpirationGuardian.Instance.AddCallBack(this);
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
        }
        #endregion

        #region ReaderSettingsUpdated
        void ReaderSettingsUpdated(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("OnUpdated"))
            {
                Center.Visibility = Visibility.Visible;
                ApplyReaderSettings();
            }
        }
        #endregion

        #region ReaderLoaded
        async void ReaderLoaded(object sender, RoutedEventArgs e)
        {
            _moveCount = 0;
            await ViewModel.LoadSettings();
            var currentOrientation = DisplayInformation.GetForCurrentView().CurrentOrientation;
            DisplayInformation.AutoRotationPreferences = ViewModel.ReaderSettings.Autorotate ? DisplayOrientations.None : currentOrientation;
            CurrentFlipView = FlipView;
        }
        #endregion

        #region CreateDataSession
        protected override Session CreateDataSession(DataLoadReason reason)
        {
            ViewModel.Id = ViewParameters.GetValue<int>("id");
            ViewModel.FileToken = ViewParameters.GetValue<string>("filetoken");

            ViewModel.ReaderSettings.PropertyChanged -= ReaderSettingsUpdated;
            ViewModel.ReaderSettings.PropertyChanged += ReaderSettingsUpdated;

            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.DeffaultSettings = (ResolutionHelper.isFullHD) ? ReaderSettingsViewModel.DeffaultSettingsType.DeffaultSettingsTypeHD : ReaderSettingsViewModel.DeffaultSettingsType.DeffaultSettingsTypeNormal;
            Debug.WriteLine("Reader CreateDataSession");

            return base.CreateDataSession(reason);
        }
        #endregion

        protected override async void OnDataLoadComplete(Session session)
        {
            Debug.WriteLine("OnDataLoadComplete Enter");

            await HandleLoadedBook();
            if (ViewModel.Entity == null) return;

            AddBuySection(ViewModel.Entity.Price);
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ShowPopup")
            {

            }
            else if (e.PropertyName == "HidePopup")
            {
             
            }
            else if (e.PropertyName == "ShowSwitchPopup")
            {
            }
            else if (e.PropertyName == "HideSwitchPopup")
            {
            }
            else if (e.PropertyName == "UpdatePrice")
            {
                //LitresStore.Content = string.Format("пополнить счёт на {0} руб.", ViewModel.AccoundDifferencePrice);
            }
            else if (e.PropertyName == "LoadBookProcessCompleted")
            {
               
                InitWebReader();

                if (ViewModel.Status == ReaderViewModel.LoadingStatus.FullBookLoaded) Analytics.Instance.sendMessage(Analytics.ActionReadFull);
                else if (ViewModel.Status == ReaderViewModel.LoadingStatus.TrialBookLoaded) Analytics.Instance.sendMessage(Analytics.ActionReadFragment);
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
            await ViewModel.Reload();
            await HandleLoadedBook();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _moveCount = 0;

            base.OnNavigatedFrom(e);
            if (e.NavigationMode == NavigationMode.Back || (e.Uri != null && string.Equals(e.Uri.OriginalString, "/Views/Main.xaml")) )
            {
                ControlPanel.Instance.NormalMode();

                new Task(async () =>
                {
                    await ViewModel.UpdateEntity();
                       }).RunSynchronously();

                if (ExpirationGuardian.Instance != null) ExpirationGuardian.Instance.RemoveCallBack(this);
            }

            ViewModel.SaveSettings();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("Reader OnNavigatedTo");
            base.OnNavigatedTo(e);
            Analytics.Instance.sendMessage(Analytics.ViewReader);
        }
        
        private async Task HandleLoadedBook()
        {
            if (ViewModel.Status == ReaderViewModel.LoadingStatus.FullBookLoaded || ViewModel.Status == ReaderViewModel.LoadingStatus.TrialBookLoaded)
            {
               
            }
            else if (ViewModel.LoadingException != null)
            {
                pageHeader.ProgressIndicatorVisible = false;
                await new MessageDialog("Ошибка получения книги. Попробуйте попозже.").ShowAsync();
                _navigationService.GoBack();
            }

           // BookCoverBack.Visibility = Visibility.Collapsed;

            ShowMenu();
        }

        private void ApplyReaderSettings()
        {            
            if (ViewModel.ReaderSettings.Autorotate)
            {
                //SupportedOrientations = SupportedPageOrientation.PortraitOrLandscape;
                
            }
            else
            {
                //SupportedOrientations = SupportedPageOrientation.Portrait;
            }
            Center.Visibility = Visibility.Collapsed;
        }

        protected async void OnOrientationChanged(DisplayInformation info, object sender)
        {
            var orientation = info.CurrentOrientation;
            
            if (orientation == DisplayOrientations.Landscape)
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
                
            }
        }
        
        private void AddBuySection(double price)
        {
            BuyBookText.Text = "Конец ознакомительного фрагмента. Для продолжения чтения купите книгу за " +
                        price.ToString(CultureInfo.InvariantCulture) + " рублей.";
        }
    
        private void CheckBuyMenuVisible()
        {
            //if (ViewModel != null)
            //{
            //    if (ViewModel.Entity != null && !ViewModel.Entity.IsMyBook &&
            //        _cacheItem.Reader.Formatter.Pages.Count == _cacheItem.Reader.CurrentPage &&
            //        (int)BuyBookMenu.Height == 0)
            //    {
            //        if (ViewModel.UserInformation != null && ViewModel.UserInformation.AccountType != (int)AccountTypeEnum.AccountTypeLibrary)
            //            ShowBuyBookMenu.Begin();
            //        _isBuyShowed = true;
            //    }
            //    else if (ViewModel.Entity != null && !ViewModel.Entity.IsMyBook &&
            //        _cacheItem.Reader.Formatter.Pages.Count != _cacheItem.Reader.CurrentPage &&
            //        (int)BuyBookMenu.Height > 0)
            //    {
            //        HideBuyBookMenu.Begin();
            //        _isBuyShowed = false;
            //    }
            //}
        }

        private void CheckBuyMenuVisible(int currentPage, int totalPages)
        {
            if (ViewModel != null)
            {
                if (ViewModel.Entity != null && !ViewModel.Entity.IsMyBook &&
                    totalPages == currentPage && (int)BuyBookMenu.Height == 0)
                {
                    if (ViewModel.UserInformation != null && ViewModel.UserInformation.AccountType != (int)AccountTypeEnum.AccountTypeLibrary)

                    _isBuyShowed = true;
                }
                else if (ViewModel.Entity != null && !ViewModel.Entity.IsMyBook &&
                    totalPages != currentPage && (int)BuyBookMenu.Height > 0)
                {
       
                    _isBuyShowed = false;
                }
            }
        }

        private void ChangeSliderValue(double val)
        {
            _isSliderManipulationStarted = true;
            CurrentPageSlider.Value = val;
        }
        

        private bool _isSliderManipulationStarted;

        private void CurrentPageSlider_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _isSliderManipulationStarted = true;
            Debug.WriteLine("Manipulation Started");
        }

        private void CurrentPageSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Debug.WriteLine("Value changed");
            if (ViewModel?.Entity != null)
            {
                if (!_isSliderManipulationStarted) CurrentPageSlider_ManipulationCompleted(null, null);

                if (CurrentPageSlider != null)
                    ViewModel.Entity.ReadedPercent = (int)Math.Ceiling(CurrentPageSlider.Value / (CurrentPageSlider.Maximum / 100));
            }
        }

        private async void CurrentPageSlider_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            Debug.WriteLine("Manipulation Completed");
            if (_isSliderManipulationStarted)
            {
                int newPage = (int)CurrentPageSlider.Value;
                try
                {
                    await ReaderWebView.InvokeScriptAsync("GoToPage", new[] { Convert.ToString(newPage) });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    throw;
                }
            }
            _isSliderManipulationStarted = false;
            CheckBuyMenuVisible();
           
        }

        //protected override void OnBackKeyPress(CancelEventArgs e)
        //{
        //    if (_history.Count > 0)
        //    {
        //        var pointer = _history.Pop();

        //        if (ViewModel.ReaderSettings.AnimationMoveToPage) MoveToPageWithAnimation(_cacheItem.Reader, pointer, true);
        //        else MoveToPage(_cacheItem.Reader, pointer, true);

        //        e.Cancel = true;
        //    }
        //    else
        //    {                
        //        if (!NavigationService.CanGoBack)
        //        {
        //            NavigationService.Navigate(new Uri("/Views/Main.xaml", UriKind.RelativeOrAbsolute));
        //            NavigationService.RemoveBackEntry();
        //            e.Cancel = true;
        //        }
        //        else base.OnBackKeyPress(e);
        //    }
        //}
        
        //private async Task AddBookmark(bool isCurrent)
        //{
        //    try
        //    {
        //        var xpointer = await GetXPointer();
        //        Debug.WriteLine(xpointer);
        //        string bookmarkInfo = await ReaderWebView.InvokeScriptAsync("GetCurrentBookmarkInfo", null);
        //        if (string.IsNullOrEmpty(bookmarkInfo)) return;
        //        var bookmarkInfoArray = bookmarkInfo.Split('|');
        //        if (bookmarkInfoArray.Length < 2) return;

        //        string chapter = bookmarkInfoArray[0];
        //        var text = bookmarkInfoArray[1];
                
        //        Debug.WriteLine(xpointer);

        //        var percent = await ReaderWebView.InvokeScriptAsync("GetCurrentPercent", null);
        //        if (string.IsNullOrEmpty(percent)) percent = Convert.ToString((int) Math.Ceiling(CurrentPageSlider.Value/(CurrentPageSlider.Maximum/100)));

        //        if (isCurrent)
        //        {
        //            ViewModel.SetCurrentBookmark(text, xpointer, chapter, percent);
        //        }
        //        else
        //        {
        //            await ViewModel.AddBookmark(text, xpointer, chapter, false, percent);
        //        }

        //        if (!isCurrent)
        //        {
        //            await new MessageDialog("Закладка добавлена").ShowAsync();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex.Message);
        //    }
        //}
        
        private async void BuyBookMenuTap(object sender, TappedRoutedEventArgs e)
        {
            Analytics.Instance.sendMessage(Analytics.ActionBuyFromFragment);
            try
            {

                ViewModel.BuyBook.Execute(null);

                await ViewModel.BuyBookAsync();

                if( ViewModel.Entity.IsMyBook )
                {

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private void CountMoves()
        {
            _moveCount++;
            if (_moveCount > 20)
            {
                _moveCount = 0;
            }
        }
        
        private void HideMenu()
        {
            Bottom.Visibility = Visibility.Collapsed;
            TopRelativePanel.Visibility = Visibility.Collapsed;
        }

        private void ShowMenu()
        {
            Bottom.Visibility = Visibility.Visible;
            TopRelativePanel.Visibility = Visibility.Visible;
        }

        public void ExpiredCallBack(Models.Book book)
        {
            if (ViewModel.Entity.Id.Equals(book.Id))
            {
                var cancelArgs = new CancelEventArgs();
                do
                {
                    cancelArgs.Cancel = false;
                 //   OnBackKeyPress(cancelArgs);
                } while (cancelArgs.Cancel == true);
            }
        }

        private void ReaderWebView_OnDOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
            LoadBookToWebReader();
        }

        private bool _isReaderInitilized = false;

        private void InitWebReader()
        {
            ReaderWebView.Settings.IsJavaScriptEnabled = true;
            ReaderWebView.NavigateToLocalStreamUri(ReaderWebView.BuildLocalStreamUri("MyTag", $"/FB3Reader/default.htm?sid={ViewModel.Sid}"), new StreamUriWinRTResolver());
            ReaderWebView.ScriptNotify += ReaderWebView_ScriptNotify;
        }

        private async void ReaderWebView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            if (e.Value.Equals("book_cache_loaded"))
            {
                if (_isReaderInitilized == false) FirstReaderInitialization();
                
                var pages =  await ReaderWebView.InvokeScriptAsync("PagesCount", null);
                CurrentPageSlider.Maximum = double.Parse(pages);
                UpdatePageSliderValue();

                _isReaderInitilized = true;
            }
            else if (e.Value.Equals("UpdatePagePosition"))
            {
                UpdatePageSliderValue();
            }
        }

        private void FirstReaderInitialization()
        {
        
        }

        private async void UpdatePageSliderValue()
        {
            var currentPage = await ReaderWebView.InvokeScriptAsync("CurrentPage", null);
            if(!string.IsNullOrEmpty(currentPage) && !currentPage.Equals("undefined"))
                ChangeSliderValue(double.Parse(currentPage));
        }

        private ObservableCollection<BookElement> _bookList;
        private ObservableCollection<ObservableCollection<BookElement>> _pagesList;
        private ObservableCollection<string> _pagesCollection; 

        private void LoadBookToWebReader()
        {
            var book = ViewModel.Document;
            _pagesList = new ObservableCollection<ObservableCollection<BookElement>>();
            GeneratePages(book);
            ReformatPages();
            FlipView.Visibility = Visibility.Visible;
            /*ReaderWebView.Visibility = Visibility.Visible;
            try
            {

                var _deviceInfoService = ((App)App.Current).Scope.Resolve<IDeviceInfoService>();

                var deviceId = _deviceInfoService.DeviceId;
                var folderName = ViewModel.BookFolderName;

                await ReaderWebView.InvokeScriptAsync("RunReader", new[] {folderName, "2"});
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _navigationService.GoBack();
            }*/
        }

        private void ReformatPages()
        {
            _pagesCollection = new ObservableCollection<string>();

            foreach (var obj in _pagesList)
            {
                var pageText = "";
                var index = 0;
                foreach (var bookElement in obj)
                {
                    switch (bookElement.Type)
                    {
                        case "FictionBook.ParagraphElement":
                            pageText += bookElement.Text;
                            if (index > 0 && index < obj.Count - 1 && !obj[index - 1].Type.Contains("EmphasisElement") && !obj[index + 1].Type.Contains("EmphasisElement"))
                                pageText += "\r\n";
                            break;
                        case "FictionBook.EmphasisElement":
                            pageText += bookElement.Text;
                            break;
                        case "FictionBook.LinkElement":
                            pageText += " " + bookElement.Text;
                            break;
                    }
                    index++;
                }
                _pagesCollection.Add(pageText);
            }
        }

        private void GeneratePages(Document bookDocument)
        {
            _bookList = new ObservableCollection<BookElement>();
            foreach (var body in bookDocument.Bodies)
            {
                foreach (var child in body.Children)
                {
                    GetString(child);
                }
            }
            var tmpList = new ObservableCollection<BookElement>();
            var i = 0;
            foreach (var elem in _bookList)
            {
                if (i >= 50)
                {
                    _pagesList.Add(tmpList);
                    i = 0;
                    tmpList = new ObservableCollection<BookElement>();
                }
                tmpList.Add(elem);
                i++;
            }
        }

        private void GetString(Element element)
        {
            if (element.Children.Count > 0)
            {
                foreach (var child in element.Children)
                {
                    GetString(child);
                }
            }
            else
            {
                var elem = element as TextElement;
                if (elem == null) return;
                var bookElement = new BookElement {Text = elem.Text, Type = elem.Type};
                _bookList.Add(bookElement);
            }
        }

        private void BackButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            
        }

        private void Instance_PropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            var chapter = sender as BookChaptersViewModel.Chapter;
            if (chapter != null)
            {
                GoToc(chapter.s);
                if (SystemInfoHelper.IsDesktop())
                {
                    TocFrame.BackStack.Clear();
                    FlyoutBase.GetAttachedFlyout(ContentsButton)?.Hide();
                }
                return;
            }

            var bookmark = sender as Bookmark;
            if (bookmark != null)
            {
                GoToXPath(bookmark.Selection);
                if (SystemInfoHelper.IsDesktop())
                {
                    BookmarksFrame.BackStack.Clear();
                    FlyoutBase.GetAttachedFlyout(BookmarsButton)?.Hide();
                }
                return;
            }

        }

        private async void GoToc(string toc)
        {
            await ReaderWebView.InvokeScriptAsync("GoToc", new[] { toc });
        }

        private async void GoToXPath(string xpath)
        {
           // await ReaderWebView.InvokeScriptAsync("GoXPath", new[] {xpath });
        }

        private void SettingsButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (SystemInfoHelper.IsDesktop())
            {
                FlyoutBase.ShowAttachedFlyout((Button)sender);
                SettingsFrame.Navigate(typeof(Settings));
            }
            else _navigationService.Navigate("Settings");
        }

        private void BookmarsButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (SystemInfoHelper.IsDesktop())
            {
                FlyoutBase.ShowAttachedFlyout((Button)sender);
                BookmarksFrame.Navigate(typeof(BookBookmarks), XParameters.Create("BookEntity", ViewModel.Entity));
            }
            else _navigationService.Navigate("BookBookmarks", XParameters.Create("BookEntity", ViewModel.Entity));
        }

        private async void ContentsButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var json = await ReaderWebView.InvokeScriptAsync("GetTOC", null);
            Debug.WriteLine(json);
            if (SystemInfoHelper.IsDesktop())
            {
                FlyoutBase.ShowAttachedFlyout((Button)sender);
                TocFrame.Navigate(typeof(BookChapters), XParameters.Create("TocJson", json));
            }
            else _navigationService.Navigate("BookChapters", XParameters.Create("TocJson", json));

            Debug.WriteLine(json);
        }

        private async void AddBookmarkButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
           // await AddBookmark(false);
        }

        private void FlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int pageNum = FlipView.SelectedIndex + 1;
            pageNumber.Text = pageNum.ToString();
            _fractionRead = (pageNum - 1.0) / FlipView.Items.Count;
            int cnt = FlipView.Items.Count;
            pageCount.Text = cnt.ToString();
            CurrentPageSlider.Minimum = 1;
            CurrentPageSlider.Maximum = FlipView.Items.Count;
            CurrentPageSlider.Value = pageNum;
            var richText = FlipView.Items[0] as RichTextBlock;
            if (richText != null)
            {
                ChangeFontSize();
                ChangeFont();
                ChangeJustification();
                ChangeTheme();
                ChangeMargins();
                ChangeCharacterSpacing();
            }
            if (FlipView.SelectedIndex == FlipView.Items.Count - 1 && richTextBlockOverflow != null)
            {
                RichTextBlockOverflow newRichTextBlockOverflow = new RichTextBlockOverflow();
                richTextBlockOverflow.OverflowContentTarget = newRichTextBlockOverflow;
                richTextBlockOverflow = newRichTextBlockOverflow;
                FlipView.Items.Add(richTextBlockOverflow);
                richTextBlockOverflow.Measure(containerSize);
            }
            BookCoverBack.Visibility = Visibility.Collapsed;
            pageHeader.ProgressIndicatorVisible = false;
        }

        private RichTextBlockOverflow richTextBlockOverflow;

        private Size containerSize;

        public void ChangeTheme()
        {
            var theme = ViewModel.ReaderSettings.Theme;
            var richText = FlipView.Items?[0] as RichTextBlock;
            if (richText == null) return;            
            switch (theme)
            {
                case 1:
                    richText.Foreground = new SolidColorBrush(Colors.Black);
                    LayoutRoot.Background = new SolidColorBrush(Colors.White);
                    FlipView.Background = new SolidColorBrush(Colors.White);
                    break;
                case 2:
                    richText.Foreground = new SolidColorBrush(Colors.Black);
                    LayoutRoot.Background = new SolidColorBrush(Colors.Wheat);
                     FlipView.Background = new SolidColorBrush(Colors.Wheat);
                    break;
                case 3:
                    richText.Foreground = new SolidColorBrush(Colors.White);
                    LayoutRoot.Background = new SolidColorBrush(Colors.Black);
                    FlipView.Background = new SolidColorBrush(Colors.Black);
                    break;
            }
        }

        public void ChangeMargins()
        {
            var margin = ViewModel.ReaderSettings.Margin;
            switch (margin)
            {
                case 1:
                    FlipView.Margin = new Thickness(0, 0, 0, 80);
                    break;
                case 2:
                    FlipView.Margin = new Thickness(20, 0, 20, 80);
                    break;
                case 3:
                    FlipView.Margin = new Thickness(40, 0, 40, 80);
                    break;
            }
        }

        public void ChangeCharacterSpacing()
        {
            var characterSpacing = ViewModel.ReaderSettings.CharacterSpacing;
            var richText = FlipView.Items[0] as RichTextBlock;
            switch (characterSpacing)
            {
                case 1:
                    richText.LineHeight = 25;
                    break;
                case 2:
                    richText.LineHeight = 35;
                    break;
            }
        }

        public void ChangeFontSize()
        {
            var fontSize = ViewModel.ReaderSettings.FontSize + 20;            
            if (FlipView.Items == null) return;
            try
            {
                var richText = FlipView.Items[0] as RichTextBlock;
                richText.FontSize = fontSize;
            }
            catch (Exception)
            {
                                
            }
            
        }

        public void ChangeFont()
        {
            var intFont = ViewModel.ReaderSettings.Font;
            switch (intFont)
            {
                case 1:
                    var richText = FlipView.Items[0] as RichTextBlock;
                    if (richText != null) richText.FontFamily = new FontFamily("/Fonts/PT Sans.ttf#PT Sans");
                    break;
                case 2:
                    richText = FlipView.Items[0] as RichTextBlock;
                    if (richText != null) richText.FontFamily = new FontFamily("PT Serif");
                    break;
                case 3:
                    richText = FlipView.Items[0] as RichTextBlock;
                    if (richText != null) richText.FontFamily = new FontFamily("/Fonts/PT Mono.ttf#PT Mono");
                    break;
            }
        }

        public void ChangeJustification()
        {
            var richText = FlipView.Items[0] as RichTextBlock;
            if (richText == null) return;
            switch (ViewModel.ReaderSettings.FitWidth)
            {
                case false:
                    richText.TextAlignment = TextAlignment.Left;
                    break;
                case true:
                    richText.TextAlignment = TextAlignment.Justify;
                    break;
            }
             
        }

        private FontFamily GetCurrentFont()
        {
            var intFont = ViewModel.ReaderSettings.Font;
            FontFamily currentFont = null;
            switch (intFont)
            {
                case 0:
                case 1:
                    currentFont = new FontFamily("/Fonts/PT Sans.ttf#PT Sans");
                    break;
                case 2:
                    currentFont = new FontFamily("PT Serif");
                    break;
                case 3:
                    currentFont = new FontFamily("/Fonts/PT Mono.ttf#PT Mono");
                    break;
            }
            return currentFont;
        }

        private async void FlipView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CurrentPageSlider.Visibility = Visibility.Visible;            
            // Actual value gets modified during processing here, so save it
            double saveFractionRead = _fractionRead;            
            // First time through after program is launched
            if (FlipView.Items != null && _bookList != null)
            {
                try
                {
                    FlipView.Items.Clear();
                }
                catch (Exception)
                {
                  //
                }
               
                // Load book resource
                var bookLines = _bookList;
                // Create RichTextBlock
                var fontSize = ViewModel.ReaderSettings.FontSize;
                RichTextBlock richTextBlock = new RichTextBlock
                {
                    FontSize = fontSize,
                    FontFamily = GetCurrentFont(),
                    Foreground = new SolidColorBrush(Colors.Black)
                };

                // Create paragraphs
                Paragraph paragraph = new Paragraph();
                paragraph.Margin = new Thickness(12);
                richTextBlock.Blocks.Add(paragraph);
                containerSize = richTextBlock.RenderSize;
                foreach (var line in bookLines)
                {
                    // End of paragraph, make new Paragraph
                    if (line.Text.Length == 0)
                    {
                        paragraph = new Paragraph();
                        paragraph.Margin = new Thickness(12);
                        richTextBlock.Blocks.Add(paragraph);
                    }
                    // Continue the paragraph
                    else
                    {
                        string textLine = line.Text;
                        char lastChar = line.Text[line.Text.Length - 1];

                        if (lastChar != ' ')
                            textLine += "\r\n";

                        if (line.Text[0] == ' ')
                            paragraph.Inlines.Add(new LineBreak());
                        paragraph.Inlines.Add(line.Type.Contains("Emphasis")
                            ? new Run {Text = textLine, FontStyle = FontStyle.Italic}
                            : new Run {Text = textLine});
                    }
                }

                var deviceInfo = new DeviceInfoService();


                /* var listGrid = new Grid
                {
                    Width = FlipView.Width,
                    Height = FlipView.Height
                };

                listGrid.ColumnDefinitions.Add(new ColumnDefinition());
                listGrid.ColumnDefinitions.Add(new ColumnDefinition());

                Grid.SetColumn(richTextBlock, 0);

                containerSize = listGrid.RenderSize;*/
                // Make RichTextBlock the same size as the FlipView
                try
                {
                    FlipView.Items.Add(richTextBlock);
                }
                catch (Exception)
                {
                    //
                }
              
                richTextBlock.Measure(containerSize);

                // Generate RichTextBlockOverflow elements
                if (richTextBlock.HasOverflowContent)
                {
                    // Add the first one
                    richTextBlockOverflow = new RichTextBlockOverflow();
                    richTextBlock.OverflowContentTarget = richTextBlockOverflow;
                    FlipView.Items.Add(richTextBlockOverflow);
                    richTextBlockOverflow.Measure(containerSize);
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                 {
                     while (richTextBlockOverflow.HasOverflowContent && FlipView.Items.Count < 10)
                     {
                         RichTextBlockOverflow newRichTextBlockOverflow = new RichTextBlockOverflow();
                         richTextBlockOverflow.OverflowContentTarget = newRichTextBlockOverflow;
                         richTextBlockOverflow = newRichTextBlockOverflow;
                         FlipView.Items.Add(richTextBlockOverflow);
                         richTextBlockOverflow.Measure(containerSize);
                         int cnt = FlipView.Items.Count;
                         pageNumber.Visibility = Visibility.Visible;
                         pageCount.Text = cnt.ToString();
                         CurrentPageSlider.Minimum = 1;
                         CurrentPageSlider.Maximum = FlipView.Items.Count;
                     }
                 });

            }
            // Subsequent SizeChanged events
            /*else
            {
                // Resize all the items in the FlipView
                if (FlipView.Items != null)
                {
                    foreach (object obj in FlipView.Items)
                    {
                        var frameworkElement = obj as FrameworkElement;
                        frameworkElement?.Measure(containerSize);
                    }

                    // Generate new RichTextBlockOverflow elements if needed
                    var textBlockOverflow = FlipView.Items[FlipView.Items.Count - 1]
                        as RichTextBlockOverflow;
                    while (textBlockOverflow != null && textBlockOverflow.HasOverflowContent)
                    {
                        RichTextBlockOverflow richTextBlockOverflow =
                            FlipView.Items[FlipView.Items.Count - 1] as RichTextBlockOverflow;
                        RichTextBlockOverflow newRichTextBlockOverflow = new RichTextBlockOverflow();
                        richTextBlockOverflow.OverflowContentTarget = newRichTextBlockOverflow;
                        richTextBlockOverflow = newRichTextBlockOverflow;
                        FlipView.Items.Add(richTextBlockOverflow);
                        richTextBlockOverflow.Measure(e.NewSize);
                    }
                    // Remove superfluous RichTextBlockOverflow elements
                    var blockOverflow = FlipView.Items[FlipView.Items.Count - 2]
                        as RichTextBlockOverflow;
                    while (blockOverflow != null && !blockOverflow.HasOverflowContent)
                    {
                        FlipView.Items.RemoveAt(FlipView.Items.Count - 1);
                    }
                }
            }*/

            // Initialize the header and Slider
           /* int count = FlipView.Items.Count;
            pageNumber.Visibility = Visibility.Visible;
            pageNumber.Text = "1";              // probably modified soon
            pageCount.Text = count.ToString();
            CurrentPageSlider.Minimum = 1;
            CurrentPageSlider.Maximum = FlipView.Items.Count;
            CurrentPageSlider.Value = 1;               // probably modified soon
            */
            // Go to approximate page
            _fractionRead = saveFractionRead;            
        }

        private async Task LoadRestBook()
        {
            
        }

        private void CurrentPageSlider_ValueChanged_1(object sender, RangeBaseValueChangedEventArgs e)
        {
            int cnt = FlipView.Items.Count;
            pageCount.Text = cnt.ToString();
            CurrentPageSlider.Minimum = 1;
            CurrentPageSlider.Maximum = FlipView.Items.Count;
            FlipView.SelectedIndex = Math.Min(FlipView.Items.Count, (int)e.NewValue) - 1;
        }

        private void FlipView_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            
        }
    }

    public class BookElement
    {
        public string Text { get; set; }
        public string Type { get; set; }
    }

    public partial class ReaderFitting : ViewModelPage<ReaderViewModel>
    {
    }


    public sealed class StreamUriWinRTResolver : IUriToStreamResolver
    {
        public IAsyncOperation<IInputStream> UriToStreamAsync(Uri uri)
        {
            if (uri == null)
            {
                throw new Exception();
            }
            string path = uri.AbsolutePath;

            // Because of the signature of the this method, it can't use await, so we 
            // call into a seperate helper method that can use the C# await pattern.
            return GetContent(path).AsAsyncOperation();
        }

        private async Task<IInputStream> GetContent(string path)
        {
            // We use a package folder as the source, but the same principle should apply
            // when supplying content from other locations
            try
            {
               
                if (path.Contains("/MyBooks/"))
                {
                    var localUri1 = new Uri("ms-appdata:///local" + path);
                    var f1 = await StorageFile.GetFileFromApplicationUriAsync(localUri1);
                    var stream1 = await f1.OpenAsync(FileAccessMode.Read);

                    var dataReader = new DataReader(stream1);
                    await dataReader.LoadAsync((uint)stream1.Size);
                    var encryptedBuffer = dataReader.ReadBuffer((uint)stream1.Size);
                    var decryptedBuffer = EncryptionProvider.Decrypt(encryptedBuffer);
                    //dataReader.DetachStream();

                    var memoryStream = new InMemoryRandomAccessStream();
                    await memoryStream.WriteAsync(decryptedBuffer);
                    memoryStream.Seek(0);
                    //await memoryStream.FlushAsync();
                    return memoryStream; 
                }

                Uri localUri = new Uri("ms-appx:///Assets" + path);
                StorageFile f = await StorageFile.GetFileFromApplicationUriAsync(localUri);
                IRandomAccessStream stream = await f.OpenAsync(FileAccessMode.Read);
                return stream;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw new Exception("Invalid path");
            }
        }
    }
}