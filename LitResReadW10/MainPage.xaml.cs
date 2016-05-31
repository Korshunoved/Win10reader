using System;
using System.Diagnostics;
using System.Threading;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Autofac;
using BookParser;
using Digillect;
using Digillect.Mvvm.Services;
using Digillect.Mvvm.UI;
using LitRes.Models;
using LitRes.Services;
using LitRes.Services.Connectivity;
using LitResReadW10.Controls;
using LitResReadW10.Helpers;

namespace LitResReadW10
{
    public sealed partial class MainPage : WindowsRTPage
    {
        public static MainPage Instance;

        public Frame AppFrame => this.frame;

        public Frame CurrentOpenedFrame;

        public ControlPanel MainControlPanel { get; set; }

        public bool IsNeedToShowReviewDialog { get; set; }

        public StackPanel DescriptionPanel;

        private readonly INavigationService _navigationService = ((App) App.Current).Scope.Resolve<INavigationService>();
        private readonly IDataCacheService _dataCacheService = ((App) App.Current).Scope.Resolve<IDataCacheService>();
        private readonly ICredentialsProvider _credentialsProvider = ((App) App.Current).Scope.Resolve<ICredentialsProvider>();
        private readonly IDeviceInfoService _deviceInfoService = ((App) App.Current).Scope.Resolve<IDeviceInfoService>();

        public MainPage()
        {
            //_credentialsProvider.MigrateFromWp8ToWp10();
            //migration.RunSynchronously();

            if (MainControlPanel == null)
                MainControlPanel = ControlPanel.Instance;            
            this.InitializeComponent();         
            ControlPanel.Instance.PhoneSearchBox = PhoneSearchBox;            
            ((WindowsRTApplication)Application.Current).RootFrame = AppFrame;
            ((WindowsRTApplication) Application.Current).RootSubFrame = ContentDialogFrame;

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += SystemNavigationManager_BackRequested;
            
            CheckNavButton(SystemInfoHelper.HasInternet() ? EditorsChoiceButton : MyBooksButton, false);
            DescriptionPanel = PhonePresentDescriptionStackPanel;
            CheckIfNeedToOpenReviewDialog();
            Instance = this;
        }

        public async void ShowMessageBox(string message)
        {
            await new MessageDialog(message).ShowAsync();
        }

        private void CheckIfNeedToOpenReviewDialog()
        {
            var dontAskMoreButtonPressed = _dataCacheService.GetItem<bool>("DontAskMoreButtonPressed");
            var askLaterButtonPressed = _dataCacheService.GetItem<bool>("AskLaterButtonPressed");
            var fiveStarRatingPressed = _dataCacheService.GetItem<bool>("FiveStarRatingPressed");
            var anyStarRatingPressed = _dataCacheService.GetItem<bool>("AnyStarRatingPressed");
            var firstLaunchDateTime = _dataCacheService.GetItem<DateTime>("FirstLaunchDateTime");
            var lastDateRattingPressed = _dataCacheService.GetItem<DateTime>("LastDateRattingPressed");
            var launchCount = _dataCacheService.GetItem<int>("LaunchCount");
            var dontAskMoreDate = _dataCacheService.GetItem<DateTime>("DontAskMoreDate");
            var askLaterDate = _dataCacheService.GetItem<DateTime>("AskLaterDate");
            var lastVersion = _dataCacheService.GetItem<string>("LastVersion");
            launchCount++;
            
            if (firstLaunchDateTime == default(DateTime))
            {
                firstLaunchDateTime = DateTime.Now;
                _dataCacheService.PutItem(firstLaunchDateTime, "FirstLaunchDateTime", CancellationToken.None);
            }
            if (lastVersion != default(string) && lastVersion != _deviceInfoService.ApplicationVersion && launchCount > 5)
            {
                launchCount = 1;
                firstLaunchDateTime = DateTime.Now;
                _dataCacheService.PutItem(firstLaunchDateTime, "FirstLaunchDateTime", CancellationToken.None);
            }
            
            if (fiveStarRatingPressed)
                return;
            if (dontAskMoreButtonPressed && dontAskMoreDate.AddMonths(3) < DateTime.Now)
            {
                IsNeedToShowReviewDialog = true;
            }
            else if (askLaterButtonPressed && askLaterDate.AddDays(7) < DateTime.Now)
            { 
                IsNeedToShowReviewDialog = true;
              
            }
            else if (!anyStarRatingPressed && firstLaunchDateTime.AddHours(48) < DateTime.Now && launchCount >= 5)
            {
                IsNeedToShowReviewDialog = true;
            }
            else if (lastDateRattingPressed.AddMonths(1) < DateTime.Now && lastDateRattingPressed.AddMonths(1) != default(DateTime).AddMonths(1) && lastVersion != _deviceInfoService.ApplicationVersion && firstLaunchDateTime.AddHours(48) < DateTime.Now && launchCount >= 5)
            {
                IsNeedToShowReviewDialog = true;
            }
            else
            {
                IsNeedToShowReviewDialog = false;
            }

            if (AppSettings.Default.ReaderOpen)
            {
                IsNeedToShowReviewDialog = false;
            }

            _dataCacheService.PutItem(launchCount, "LaunchCount", CancellationToken.None);
        }

        private void OnNavigatingToPage(object sender, NavigatingCancelEventArgs e)
        {
            ControlPanel.Instance.StatusBarEnable = true;

            if (e.NavigationMode == NavigationMode.Back)
            {
            }
        }

        private void OnNavigatedToPage(object sender, NavigationEventArgs e)
        {
            // After a successful navigation set keyboard focus to the loaded page
            if (e.Content is Page && e.Content != null)
            {
                var control = (Page)e.Content;
                control.Loaded += Page_Loaded;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ((Page)sender).Focus(FocusState.Programmatic);
            ((Page)sender).Loaded -= Page_Loaded;
            UpdateRadioButtons(sender);            
            if (!IsNeedToShowReviewDialog) return;
            CurrentOpenedFrame = _navigationService.NavigateToFrame("AskReview");
            CurrentOpenedFrame.BorderThickness = new Thickness(1);
            CurrentOpenedFrame.Width = 300;
            IsNeedToShowReviewDialog = false;
        }

        private void UpdateRadioButtons(object page)
        {
            //if (page is Main)
            //{
            //    CheckNavButton(ShopButton);
            //}
            //else if (page is About)
            //{
            //    if (!SystemInfoHelper.IsDesktop()) CheckNavButton(AboutButton);
            //}
            //else if (page is GeneralSettings)
            //{
            //    CheckNavButton(SettingsButton);
            //}
            //else if (page is MyBooks)
            //{
            //    CheckNavButton(MyBooksButton);
            //}
            //else if (page is Notifications)
            //{
            //    CheckNavButton(SupportButton);
            //}
        }

        private void SystemNavigationManager_BackRequested(object sender, BackRequestedEventArgs e)
        {
            bool handled = e.Handled;
            this.BackRequested(ref handled);
            e.Handled = handled;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            bool ignored = false;
            this.BackRequested(ref ignored);
        }

        public void CloseSubFrame()
        {
            SubFrameDialog.Children.Remove(ContentDialogFrame);
            ContentDialogFrame = null;
            ((WindowsRTApplication) Application.Current).RootSubFrame = null;
            ContentDialogFrame = new Frame()
            {
                Width = 500,
                MinHeight = 200,
                MaxHeight = 700,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = new Thickness(0, 0, 0, 0),
                Margin = new Thickness(0, 0, 0, 0),
                BorderThickness = new Thickness(1, 1, 1, 1),
                BorderBrush = new SolidColorBrush(Colors.Black)
            };
            SubFrameDialog.Children.Add(ContentDialogFrame);
            ((WindowsRTApplication) Application.Current).RootSubFrame = ContentDialogFrame;
            SubFrameDialog.Visibility = Visibility.Collapsed;

        }

        private void BackRequested(ref bool handled)
        {
            if (CurrentOpenedFrame != null)
            {
                SubFrameDialog.Children.Remove(ContentDialogFrame);
                ContentDialogFrame = null;
                ((WindowsRTApplication)Application.Current).RootSubFrame = null;
                ContentDialogFrame = new Frame()
                {
                    Width = 500,
                    MinHeight = 200,
                    MaxHeight = 700,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Padding = new Thickness(0, 0, 0, 0),
                    Margin = new Thickness(0, 0, 0, 0),
                    BorderThickness = new Thickness(1, 1, 1, 1),
                    BorderBrush = new SolidColorBrush(Colors.Black)
                };
                SubFrameDialog.Children.Add(ContentDialogFrame);
                ((WindowsRTApplication)Application.Current).RootSubFrame = ContentDialogFrame;
                SubFrameDialog.Visibility = Visibility.Collapsed;
            }
            //// Check to see if this is the top-most page on the app back stack.
            if (this.AppFrame.CanGoBack && !handled)
            {
                // If not, set the event to handled and go back to the previous page in the app.
                handled = true;
                _navigationService.GoBack();
            }
        }

        private void HamburgerTapped(object sender, TappedRoutedEventArgs e)
        {
            MainSplitView.IsPaneOpen = !MainSplitView.IsPaneOpen;
        }

        private void PaneNavButtons_OnChecked(object sender, RoutedEventArgs e)
        {
            var navButton = sender as RadioButton;
            if (navButton == null) return;
 
            MainSplitView.IsPaneOpen = false;

            if (_navButtonManualChecked)
            {
                _navButtonManualChecked = false;
                return;
            }

            _navigationService.ClearBackstack();

            if (navButton.Tag.Equals("About"))
            {
                
                
                if (SystemInfoHelper.IsDesktop())
                {
                    CurrentOpenedFrame = _navigationService.NavigateToFrame("About");
                    CheckNavButton(_previousNavButton);
                    return;
                }
                else
                {
                    _navigationService.Navigate("About", SystemInfoHelper.IsDesktop());
                }
            }
            else if (navButton.Tag.Equals("Shop"))
            {
                _navigationService.Navigate("Main");
            }
            else if (navButton.Tag.Equals("Settings"))
            {
                _navigationService.Navigate("GeneralSettings");
            }
            else if (navButton.Tag.Equals("MyBooks"))
            {
                _navigationService.Navigate("MyBooks");
            }
            else if (navButton.Tag.Equals("MyBasket"))
            {
                _navigationService.Navigate("MyBasket");
            }
            //else if (navButton.Tag.Equals("Notifications"))
            //{
            //    _navigationService.Navigate("Notifications");
            //}
            else if (navButton.Tag.Equals("Support"))
            {
                EmailHelper.OpenEmailClientWithLitresInfo();
                CheckNavButton(_previousNavButton);
                return;
            }
            else if (navButton.Tag.Equals("Profile"))
            {
                ToUserInfo();
                if (SystemInfoHelper.IsDesktop())
                {
                    CheckNavButton(_previousNavButton);
                    return;
                }
            }
            else if (navButton.Tag.Equals("Genres"))
            {
                _navigationService.Navigate("ShopGenres");
            }
            else if (navButton.Tag.Equals("Popular"))
            {
                _navigationService.Navigate("ShopPopular");
            }
            else if (navButton.Tag.Equals("Novelty"))
            {
                _navigationService.Navigate("ShopNovelty");
            }
            else if (navButton.Tag.Equals("EditorsChoice"))
            {
                _navigationService.Navigate("ShopEditorsChoice"); 
            }
            else
            {
                _navigationService.Navigate(SystemInfoHelper.HasInternet() ? "ShopEditorsChoice" : "MyBooks");
            }

            if (_previousNavButton == null) _previousNavButton = navButton;
            if (!_previousNavButton.Equals(navButton)) _previousNavButton = navButton;
        }

        void CheckNavButton(RadioButton navButton, bool manual = true)
        {
            if (manual) _navButtonManualChecked = true;
            if(navButton != null) navButton.IsChecked = true;
        }

        private RadioButton _previousNavButton;
        private bool _navButtonManualChecked = false;

        private void ToUserInfo()
        {
            var creds =  _credentialsProvider.ProvideCredentials(CancellationToken.None);

            if (creds != null && creds.IsRealAccount)
            {
                if (SystemInfoHelper.IsDesktop())
                {
                    CurrentOpenedFrame = _navigationService.NavigateToFrame("UserInfo");
                    return;
                }
                _navigationService.Navigate("UserInfo", SystemInfoHelper.IsDesktop());
            }
            else
            {
                if (SystemInfoHelper.IsDesktop())
                {
                    CurrentOpenedFrame = _navigationService.NavigateToFrame("Authorization");
                    return;
                }
                _navigationService.Navigate("Authorization", SystemInfoHelper.IsDesktop());
            }
        }

        private void CloseCustomDialog_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            SubFrameDialog.Children.Remove(ContentDialogFrame);
            ContentDialogFrame = null;
            ((WindowsRTApplication) Application.Current).RootSubFrame = null;
            ContentDialogFrame = new Frame()
            {
                Width = 500,
                MinHeight = 200,
                MaxHeight = 700,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = new Thickness(0,0,0,0),
                Margin = new Thickness(0, 0, 0, 0),
                BorderThickness = new Thickness(1, 1, 1, 1),
                BorderBrush = new SolidColorBrush(Colors.Black)
            };
            SubFrameDialog.Children.Add(ContentDialogFrame);
            ((WindowsRTApplication) Application.Current).RootSubFrame = ContentDialogFrame;
            SubFrameDialog.Visibility = Visibility.Collapsed;
        }
        
        private void DropDownButton_OnClick(object sender, RoutedEventArgs e)
        {
            ControlPanel.Instance.OnDropDownMenuClick(sender, e);
        }

        private void SearchBox_OnQuerySubmitted(SearchBox sender, SearchBoxQuerySubmittedEventArgs args)
        {
            var searchStr = args.QueryText;
            Search(searchStr);
        }

        private void PhoneSearchButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            ((Button) sender).Visibility = Visibility.Collapsed;
            PhoneSearchBox.Focus(FocusState.Keyboard);
            if (!SystemInfoHelper.IsDesktop())
            {
                PhoneTopBarTitle.Visibility = Visibility.Collapsed;
                PhoneDropDownButton.Visibility = Visibility.Collapsed;
            }
        }

        private void PhoneSearchBox_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var phoneSearchBox = (SearchBox)sender;
            phoneSearchBox.Focus(FocusState.Keyboard);
        }

        private void SearchBox_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var phoneSearchBox = (SearchBox)sender;
            phoneSearchBox.Focus(FocusState.Keyboard);
        }

        private void OptionsDropDownButton_OnClick(object sender, RoutedEventArgs e)
        {
            ControlPanel.Instance.OnOptionsDropDownMenuClick(sender, e);
        }

        private void PhoneSearchBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (!SystemInfoHelper.IsDesktop())
            {
                PhoneSearchIconButton.Visibility = Visibility.Visible;
                if (ControlPanel.Instance.DropDownMenuItems.Count > 0)
                    PhoneDropDownButton.Visibility = Visibility.Visible;
            }
            PhoneSearchBox.QueryText = string.Empty;
            PhoneTopBarTitle.Visibility = Visibility.Visible;
        }

        private void Search(string searchString)
        {
            if (!ControlPanel.Instance.IsSearchPageOpened && !string.IsNullOrEmpty(searchString) && searchString.Length > 2)
            {
                _navigationService.Navigate("SearchResults", XParameters.Create("SearchText", searchString));
            }
        }

        private void PhoneSearchBox_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                var queryText = PhoneSearchBox.QueryText;
                this.Focus(FocusState.Programmatic);
                Search(queryText);
            }
        }

        private void ReadButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                var lastReaded = _dataCacheService.GetItem<Book>("lastreadedbook");
                if (lastReaded != null)
                {
                       _navigationService.Navigate("Reader", XParameters.Create("BookEntity", lastReaded));                  
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            PhonePresentDescriptionStackPanel.Visibility = Visibility.Collapsed;
        }
    }
}
