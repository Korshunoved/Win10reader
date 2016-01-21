using System;
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
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web;
using Autofac;
using Digillect;
using Digillect.Mvvm.Services;
using LitRes.Services;
using LitResReadW10;
using LitResReadW10.Controls;
using LitResReadW10.Crypto;
using LitResReadW10.Helpers;

namespace LitRes.Views
{
    [View("Reader")]
    [ViewParameter("bookmark", Required = false)]
    [ViewParameter("chapter", Required = false)]
    [ViewParameter("BookEntity", typeof(Models.Book), Required = true)]
    [ViewParameter("filetoken", Required = false)]

    public partial class Reader : ReaderFitting, IExpiredCallBack
    {
        private bool _menuVisible;
        private bool _fullVisible;
        private int _moveCount;

        private bool _syncInProgress;
        private readonly Object _thisLock = new object();
        private bool _isBuyShowed;
    
        private readonly INavigationService _navigationService = ((App)App.Current).Scope.Resolve<INavigationService>();

        public bool IsHardwareBack => ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons");

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
                pageHeader.ProgressIndicatorVisible = false;
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

            BookCoverBack.Visibility = Visibility.Collapsed;

            ShowMenu();
        }

        private void ApplyReaderSettings()
        {
            //PaintingContext.Settings.Face = (Faces)ViewModel.ReaderSettings.Font;
            //PaintingContext.Settings.Scale = (Scales)ViewModel.ReaderSettings.FontSize;
            //PaintingContext.Settings.Theme = (Themes)ViewModel.ReaderSettings.Theme;

            //PaintingContext.Settings.Indent = (Indents)ViewModel.ReaderSettings.Margin;
            //PaintingContext.Settings.LineSpacing = (LineSpacings)ViewModel.ReaderSettings.CharacterSpacing;
            //PaintingContext.Settings.Justify = ViewModel.ReaderSettings.FitWidth;
            //PaintingContext.Settings.ParagraphSpacing = ParagraphSpacing.Normal;

            //PaintingContext.Settings.Brightness = ViewModel.ReaderSettings.Brightness;
            //PaintingContext.Settings.UseKerning = true;
            //PaintingContext.Settings.Hyphenate = ViewModel.ReaderSettings.Hyphenate;
            //PaintingContext.Settings.Dencity = ResolutionHelper.CurrentResolution == Resolutions.WVGA ? Dencities.Low : Dencities.Medium;

      //      (ReadingPaneImage2.RenderTransform as TranslateTransform).Y = ViewModel.ReaderSettings.Margin * 10;
        //    (ReadingPaneImage.RenderTransform as TranslateTransform).Y = ViewModel.ReaderSettings.Margin * 10;

           // BrightnessSlider.Value = ViewModel.ReaderSettings.Brightness;

            //int fontSize = 18 + (int)PaintingContext.Settings.Scale * 4;
         //   ReadingPaneTextBottom.FontSize = fontSize;
         //   ReadingPaneTextBottom2.FontSize = fontSize;

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

        private async void LoadBookToWebReader()
        {
            ReaderWebView.Visibility = Visibility.Visible;
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