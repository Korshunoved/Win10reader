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
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.Input;
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
using Digillect;
using Digillect.Mvvm.Services;
using LitResReadW10;
using LitResReadW10.Controllers;
using LitResReadW10.Controls;
using LitResReadW10.Controls.Manipulators;
using LitResReadW10.Helpers;
using LitResReadW10.Interaction;

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
        private bool _isBuyShowed;
        private bool _isLoaded;
        private IFontHelper activeFontHelper;
        private BookModel _book;
        private ReadController _readController;
        private int _tokenOffset;
        public int CurrentPage;

        private readonly INavigationService _navigationService = ((App)App.Current).Scope.Resolve<INavigationService>();

        public bool IsHardwareBack => ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons");

        public static Reader Instance;

        private ManipulationListener _pageManipulationListener;
        private ManipulationListener _textManipulationListener;

        private readonly SemaphoreSlim _event = new SemaphoreSlim(1, 1);

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
                _isLoaded = false;
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
            if (BrightnessBorder.Visibility == Visibility.Collapsed)
                BrightnessBorder.Visibility = Visibility.Visible;
            await ViewModel.LoadSettings();
            var margin = AppSettings.Default.MarginIndex;
            switch (margin)
            {
                case 1:
                    AppSettings.Default.Margin = new Thickness(18);
                    break;
                case 2:
                    AppSettings.Default.Margin = new Thickness(12);
                    break;
                case 3:
                    AppSettings.Default.Margin = new Thickness(8);
                    break;
            }
            var currentOrientation = DisplayInformation.GetForCurrentView().CurrentOrientation;
            DisplayInformation.AutoRotationPreferences = ViewModel.ReaderSettings.Autorotate ? DisplayOrientations.None : currentOrientation;
     
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

          //  AddBuySection(ViewModel.Entity.Price);
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
                if (ViewModel.Status == ReaderViewModel.LoadingStatus.FullBookLoaded) Analytics.Instance.sendMessage(Analytics.ActionReadFull);
                else if (ViewModel.Status == ReaderViewModel.LoadingStatus.TrialBookLoaded) Analytics.Instance.sendMessage(Analytics.ActionReadFragment);
                _isLoaded = true;

                _book = AppSettings.Default.CurrentBook;
                if (_book == null) return;
                activeFontHelper = BookFactory.GetActiveFontMetrics(AppSettings.Default.FontSettings.FontFamily.Source);
                AppSettings.Default.FontSettings.FontHelper = activeFontHelper;
                Redraw();
                _isSliderMoving = false; 
                CurrentPageSlider.ManipulationStarted += CurrentPageSliderOnManipulationStarted;    
                CurrentPageSlider.PointerReleased += CurrentPageSliderOnPointerReleased;
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

        private void CurrentPageSliderOnPointerReleased(object sender, PointerRoutedEventArgs pointerRoutedEventArgs)
        {
            _isSliderMoving = false;
            OnSliderClickOrMoved();
        }

        public async void UpdateBook()
        {
            PageHeader.ProgressIndicatorVisible = false;
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
                PageHeader.ProgressIndicatorVisible = false;
                await new MessageDialog("Ошибка получения книги. Попробуйте попозже.").ShowAsync();
                _navigationService.GoBack();
            }

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
                PageHeader.Margin = new Thickness(0, 0, 0, 40);
            }
            else
            {
                BookCover.Width = BookCoverBack.Width = 310;
                BookCover.Height = BookCoverBack.Height = 474;
                PageHeader.Margin = new Thickness(0, 0, 0, 75);
            }

            if (PageHeader.Visibility == Visibility.Collapsed)
            {
                
            }
        }
        
        private void AddBuySection(double price)
        {
            BuyBookText.Text = "Конец ознакомительного фрагмента. Для продолжения чтения купите книгу за " +
                        price.ToString(CultureInfo.InvariantCulture) + " рублей.";
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
        
        private async void HideMenu()
        {
            CurrentPageSlider.Opacity = 0;
            TopRelativePanel.Visibility = Visibility.Collapsed;
            if (!SystemInfoHelper.IsDesktop())
            {
                StatusBar statusBar = StatusBar.GetForCurrentView();
                await statusBar.HideAsync();
            }
        }

        private async void ShowMenu()
        {
            CurrentPageSlider.Opacity = 1;           
            TopRelativePanel.Visibility = Visibility.Visible;
            if (!SystemInfoHelper.IsDesktop())
            {
                StatusBar statusBar = StatusBar.GetForCurrentView();
                await statusBar.ShowAsync();
                
            }
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

        private bool _isReaderInitilized = false;

        private void BackButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            
        }

        private void Instance_PropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            var chapter = sender as BookChaptersViewModel.Chapter;
            if (chapter != null)
            {
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
                if (SystemInfoHelper.IsDesktop())
                {
                    BookmarksFrame.BackStack.Clear();
                    FlyoutBase.GetAttachedFlyout(BookmarsButton)?.Hide();
                }
                return;
            }

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

        public void ChangeTheme()
        {

        }

        public async void ChangeMargins()
        {

            await CreateController();
        }

        public async void ChangeCharacterSpacing()
        {
            await CreateController();
        }

        public async void ChangeFontSize()
        {
            await CreateController();
        }

        public async void ChangeFont()
        {
            activeFontHelper = BookFactory.GetActiveFontMetrics(AppSettings.Default.FontSettings.FontFamily.Source);
            AppSettings.Default.FontSettings.FontHelper = activeFontHelper;
            await CreateController();
        }

        public void ChangeJustification()
        {
            
        }

        public async void Redraw()
        {
            if (!_isLoaded)
                return;

            //using (await overlayAwait)
            {
                await _event.WaitAsync();

                Background = AppSettings.Default.ColorScheme.BackgroundBrush;

                PageCanvas.Clear();
                PageCanvas.SetSize(ReaderGrid.ActualWidth, ReaderGrid.ActualHeight, ReaderGrid.ActualWidth, ReaderGrid.ActualHeight);
                PageCanvas.Manipulator = new ManipulatorFactory(PageCanvas).CreateManipulator(AppSettings.Default.FlippingStyle, AppSettings.Default.FlippingMode);

                await CreateController();

                _event.Release();

                if (BookCoverBack.Visibility == Visibility.Visible)
                    BookCoverBack.Visibility = Visibility.Collapsed;

                Bottom.Visibility = Visibility.Visible;

                PageHeader.ProgressIndicatorVisible = false;
            }
        }

        private int? _preSelectionOffset;

        private async void OnSliderClickOrMoved()
        {
            if (_preSelectionOffset == null)
                _preSelectionOffset = _tokenOffset;
            int page = (int)CurrentPageSlider.Value;
            CurrentPage = page;
            int tokenOffset = (page - 1) * AppSettings.WORDS_PER_PAGE;
            _tokenOffset = tokenOffset;
            await CreateController();
        }

        public async void GoToChapter()
        {
            if (_preSelectionOffset == null)
                _preSelectionOffset = _tokenOffset;        
            int tokenOffset = (CurrentPage - 1) * AppSettings.WORDS_PER_PAGE;
            _tokenOffset = tokenOffset;
            await CreateController();
        }

        private async Task CreateController()
        {
            if (_book == null)
            {
                return;
            }
                      
            _readController = new ReadController(PageCanvas, _book, _book.BookID, _tokenOffset);
           
            await _readController.ShowNextPage();
            CurrentPageSlider.Value = _readController.CurrentPage;
            PageCanvas.Manipulator.UpdatePanelsVisibility();
            PageCanvas.Manipulator.IsFirstPage = _readController.IsFirst;
            PageCanvas.Manipulator.IsLastPage = _readController.IsLast;
            CurrentPageSlider.Maximum = _readController.TotalPages;
        }

        private int? _oldOffset;

        private async Task TurnPage(bool isRight)
        {
            _oldOffset = null;

            await _event.WaitAsync();

           // AppBar.CancelPageSelectionMode();

            if (isRight)
                await _readController.ShowNextPage();
            else
                await _readController.ShowPrevPage();

           _tokenOffset = _readController.Offset;

            _isSliderMoving = true;
            CurrentPageSlider.Value = _readController.CurrentPage;
            CurrentPage = (int) CurrentPageSlider.Value;
            PagesText.Text = _readController.CurrentPage + "/" + _readController.TotalPages;
            _isSliderMoving = false;
            PageCanvas.Manipulator.IsFirstPage = _readController.IsFirst;
            PageCanvas.Manipulator.IsLastPage = _readController.IsLast;
            PageCanvas.Manipulator.UpdatePanelsVisibility();

            _event.Release();
        }

        private async void PageCanvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
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
                if (TopRelativePanel.Visibility == Visibility.Collapsed)
                    ShowMenu();
                else
                    HideMenu();
        }

        private void LayoutRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PageCanvas.SetSize(ReaderGrid.ActualWidth, ReaderGrid.ActualHeight, ReaderGrid.ActualWidth, ReaderGrid.ActualHeight);
            PageCanvas.Clear();

         //   UpdateTrayVisibility();

            Redraw();
        }

        private bool _isSliderMoving;

        private void CurrentPageSliderOnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs manipulationStartedRoutedEventArgs)
        {
            _isSliderMoving = true;
        }

        private void ContentsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (SystemInfoHelper.IsDesktop())
            {
                FlyoutBase.ShowAttachedFlyout((Button)sender);
                TocFrame.Navigate(typeof(BookChapters));
            }
            else _navigationService.Navigate("BookChapters");
        }
    }


    public partial class ReaderFitting : ViewModelPage<ReaderViewModel>
    {
    }
}