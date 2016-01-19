using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Digillect.Collections;

namespace LitResReadW10.Controls
{
    public class ControlPanel: INotifyPropertyChanged
    {
        private string _topBarTitle;
        private Visibility _topBarVisibility;
        private Visibility _paneVisibility;
        private Visibility _dropDownButtonVisibility;
        private Visibility _optionsDropDownButtonVisibility;
        private List<MenuFlyoutItem> _menuFlyoutItems;
        private List<MenuFlyoutItem> _optionsMenuFlyoutItems;
        private double _compactPaneLength;
        private bool _isSearchPageOpened;

        public ControlPanel()
        {
            TopBarVisibility = Visibility.Visible;
            PaneVisibility = Visibility.Visible;
            DropDownButtonVisibility = Visibility.Collapsed;
            OptionsDropDownButtonVisibility = Visibility.Collapsed;
            TopBarTitle = "Магазин";
            StatusBarEnable = true;
            _menuFlyoutItems = new List<MenuFlyoutItem>();
            _optionsMenuFlyoutItems = new List<MenuFlyoutItem>();
            _isSearchPageOpened = false;
        }
        
        public void OnOptionsDropDownMenuClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var dropdownButton = sender as Button;
            if (dropdownButton != null && _optionsMenuFlyoutItems.Count > 0)
            {
                var flayoutRoot = new MenuFlyout();
                foreach (var menuFlyoutItem in _optionsMenuFlyoutItems)
                {
                    flayoutRoot.Items?.Add(menuFlyoutItem);
                }

                dropdownButton.Flyout = flayoutRoot;
                dropdownButton.Flyout.Placement = FlyoutPlacementMode.Bottom;
                dropdownButton.Flyout.ShowAt(dropdownButton);
            }
        }

        public void OnDropDownMenuClick(object sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                var dropdownButton = sender as Button;
                if (dropdownButton != null && _menuFlyoutItems.Count > 0)
                {
                    var flayoutRoot = new MenuFlyout();
                    foreach (var menuFlyoutItem in _menuFlyoutItems)
                    {
                        flayoutRoot.Items?.Add(menuFlyoutItem);
                    }
                    
                    dropdownButton.Flyout = flayoutRoot;
                    dropdownButton.Flyout.Placement = FlyoutPlacementMode.Bottom;
                    dropdownButton.Flyout.ShowAt(dropdownButton);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public static ControlPanel Instance { get; } = new ControlPanel();

        public List<MenuFlyoutItem> DropDownMenuItems
        {
            get { return _menuFlyoutItems; }
            set
            {
                _menuFlyoutItems = value;
                RaisePropertyChanged(() => this.DropDownMenuItems);
            }
        }

        public List<MenuFlyoutItem> OptionsDropDownMenuItems
        {
            get { return _optionsMenuFlyoutItems; }
            set
            {
                _optionsMenuFlyoutItems = value;
                RaisePropertyChanged(() => this.OptionsDropDownMenuItems);
            }
        }

        public Visibility TopBarVisibility
        {
            get { return _topBarVisibility; }
            set
            {
                _topBarVisibility = value;
                RaisePropertyChanged(() => this.TopBarVisibility);
            }
        }

        public SearchBox PhoneSearchBox { get; set; }

        public bool IsSearchPageOpened
        {
            get { return _isSearchPageOpened; }
            set
            {
                _isSearchPageOpened = value;
                RaisePropertyChanged(() => this.IsSearchPageOpened);
            }
        }

        public Visibility PaneVisibility
        {
            get { return _paneVisibility; }
            set
            {
                _paneVisibility = value;
                RaisePropertyChanged(() => this.PaneVisibility);

                if (value == Visibility.Visible) CompactPaneLength = 48;
                else CompactPaneLength = 0;
            }
        }

        public double CompactPaneLength
        {
            get
            {
                return _compactPaneLength;
            }
            set
            {
                _compactPaneLength = value;
                RaisePropertyChanged(() => this.CompactPaneLength);
            }
        }

        public string TopBarTitle
        {
            get
            {
                return _topBarTitle;
            }
            set
            {
                if (value == null) return;
                _topBarTitle = value;
                RaisePropertyChanged(() => this.TopBarTitle);
            }
        }

        public Visibility DropDownButtonVisibility
        {
            get { return _dropDownButtonVisibility; }
            set
            {
                _dropDownButtonVisibility = value;
                RaisePropertyChanged(() => this.DropDownButtonVisibility);
            }
        }

        public Visibility OptionsDropDownButtonVisibility
        {
            get { return _optionsDropDownButtonVisibility; }
            set
            {
                _optionsDropDownButtonVisibility = value;
                RaisePropertyChanged(() => this.OptionsDropDownButtonVisibility);
            }
        }

        public bool StatusBarEnable
        {
            set
            {
                if (value) ShowStatusBar();
                else HideStatusBar();
            }
        }

        public void ReaderMode()
        {
            TopBarVisibility = Visibility.Collapsed;
            PaneVisibility = Visibility.Collapsed;
            StatusBarEnable = false;
        }

        public void NormalMode()
        {
            TopBarVisibility = Visibility.Visible;
            PaneVisibility = Visibility.Visible;
            StatusBarEnable = true;
        }

        private void ShowStatusBar()
        {
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                StatusBar.GetForCurrentView().BackgroundColor = Color.FromArgb(255, 235, 234, 228);
                StatusBar.GetForCurrentView().ForegroundColor = Colors.Black;
                StatusBar.GetForCurrentView().BackgroundOpacity = 1;
            }
        }

        private void HideStatusBar()
        {
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
               //Task.Run(async () => await StatusBar.GetForCurrentView().HideAsync());
            }
        }

        void RaisePropertyChanged<T>(Expression<Func<T>> raiser)
        {
            var e = PropertyChanged;
            if (e != null)
            {
                var propName = ((MemberExpression)raiser.Body).Member.Name;
                e(this, new PropertyChangedEventArgs(propName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
