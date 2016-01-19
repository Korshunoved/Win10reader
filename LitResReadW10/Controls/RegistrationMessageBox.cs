using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LitRes.Helpers;
using LitRes.Resources;
using Microsoft.Phone.Controls;

namespace LitRes.Controls
{
    public class RegistrationMessageBox
    {
        #region Fields and Constructor

        private readonly CustomMessageBox _messageBox;

        public static bool IsOpened { get; private set; }

        public RegistrationMessageBox()
        {
            _messageBox = new CustomMessageBox
            {
                Background = new SolidColorBrush(Colors.Transparent)
            };

            IsOpened = false;

            var grid = new Grid
            {
                Height = 760,
                Background = new SolidColorBrush(Colors.White),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(12, ResolutionHelper.CurrentResolution == Resolutions.HD720p ? 34: 24, 24, 0)
            };

            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });

            var title = new TextBlock
            {
                Text = AppResources.RegistrationMessageBoxCaption,
                FontFamily = new FontFamily("Segoe WP SemiBold"),
                FontSize = 28,
                Foreground = new SolidColorBrush(Colors.Black),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 22, 0, 0)
            };

            var message = new TextBlock
            {
                Text = AppResources.RegistrationMessageBoxMessage,
                FontFamily = new FontFamily("Segoe WP"),
                FontSize = 20,
                Foreground = new SolidColorBrush(Colors.Black),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 0)
            };

            var helpGrid = new Grid
            {
                Margin = new Thickness(0, 30, 0, 0)
            };

            helpGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(106, GridUnitType.Pixel) });
            helpGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });
            helpGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
            helpGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
            helpGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });

            var image1 = new Image
            {
                Source = new BitmapImage
                {
                    UriSource = new Uri(@"\Assets\RegistrationHelpIcon1.png", UriKind.RelativeOrAbsolute),
                    CreateOptions = BitmapCreateOptions.None
                },
                Width = 52,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(30, 0, 0, 0)
            };

            var image2 = new Image
            {
                Source = new BitmapImage
                {
                    UriSource = new Uri(@"\Assets\RegistrationHelpIcon2.png", UriKind.RelativeOrAbsolute),
                    CreateOptions = BitmapCreateOptions.None
                },
                Width = 61,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(30, 30, 0, 0)
            };

            var image3 = new Image
            {
                Source = new BitmapImage
                {
                    UriSource = new Uri(@"\Assets\RegistrationHelpIcon3.png", UriKind.RelativeOrAbsolute),
                    CreateOptions = BitmapCreateOptions.None
                },
                Width = 42,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(30, 30, 0, 0)
            };

            var helpText1 = new TextBlock
            {
                Text = AppResources.RegistrationMessageBoxHelpText1,
                FontFamily = new FontFamily("Segoe WP"),
                FontSize = 18,
                Foreground = new SolidColorBrush(Colors.Black),
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                LineHeight = 24,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -6, 0, 0)
            };

            var helpText2 = new TextBlock
            {
                Text = AppResources.RegistrationMessageBoxHelpText2,
                FontFamily = new FontFamily("Segoe WP"),
                FontSize = 18,
                Foreground = new SolidColorBrush(Colors.Black),
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                LineHeight = 24,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 22, 0, 0)
            };

            var helpText3 = new TextBlock
            {
                Text = AppResources.RegistrationMessageBoxHelpText3,
                FontFamily = new FontFamily("Segoe WP"),
                FontSize = 18,
                Foreground = new SolidColorBrush(Colors.Black),
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                LineHeight = 24,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 22, 0, 0)
            };

            helpGrid.Children.Add(image1);
            helpGrid.Children.Add(image2);
            helpGrid.Children.Add(image3);
            helpGrid.Children.Add(helpText1);
            helpGrid.Children.Add(helpText2);
            helpGrid.Children.Add(helpText3);

            Grid.SetColumn(image1, 0);
            Grid.SetColumn(image2, 0);
            Grid.SetColumn(image3, 0);
            Grid.SetColumn(helpText1, 1);
            Grid.SetColumn(helpText2, 1);
            Grid.SetColumn(helpText3, 1);

            Grid.SetRow(image1, 0);
            Grid.SetRow(image2, 1);
            Grid.SetRow(image3, 2);
            Grid.SetRow(helpText1, 0);
            Grid.SetRow(helpText2, 1);
            Grid.SetRow(helpText3, 2);

            var loginButton = new Button
            {
                Content = AppResources.LoginButtonText2,
                FontSize = 21,
                Height = 72,
                Style = Application.Current.Resources["LitResButtonStyle"] as Style,
                Margin = new Thickness(18, 18, 18, 0)
            };

            loginButton.Click += LoginButtonOnClick;

            var registerButton = new Button
            {
                Content = AppResources.RegisterButtonText2,
                FontSize = 21,
                Height = 72,
                Style = Application.Current.Resources["LitResButtonStyle"] as Style,
                Margin = new Thickness(18, -3, 18, 0)
            };

            registerButton.Click += RegisterButtonOnClick;

            var cancelButton = new Button
            {
                Content = AppResources.DoNotRegisterButtonText,
                FontSize = 21,
                Height = 72,
                Style = Application.Current.Resources["ImageButtonStyle"] as Style,
                Margin = new Thickness(18, -8, 18, 0)
            };

            cancelButton.Click += CancelButtonOnClick;

            grid.Children.Add(title);
            grid.Children.Add(message);
            grid.Children.Add(helpGrid);
            grid.Children.Add(loginButton);
            grid.Children.Add(registerButton);
            grid.Children.Add(cancelButton);

            Grid.SetRow(title, 0);
            Grid.SetRow(message, 1);
            Grid.SetRow(helpGrid, 2);
            Grid.SetRow(loginButton, 3);
            Grid.SetRow(registerButton, 4);
            Grid.SetRow(cancelButton, 5);

            _messageBox.Content = grid;
        }

        #endregion

        #region Events

        public event Action LoginButtonClicked;
        public event Action RegisterButtonClicked;
        public event Action CancelButtonClicked;

        #endregion

        #region Public Methods

        public void Show()
        {
            _messageBox.Show();

            IsOpened = true;
        }

        public void Dismiss()
        {
            _messageBox.Dismiss();

            IsOpened = false;
        }

        #endregion

        #region Private Methods

        private void LoginButtonOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            if (LoginButtonClicked != null)
            {
                LoginButtonClicked();
            }

            Dismiss();
        }

        private void RegisterButtonOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            if (RegisterButtonClicked != null)
            {
                RegisterButtonClicked();
            }

            Dismiss();
        }

        private void CancelButtonOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            if (CancelButtonClicked != null)
            {
                CancelButtonClicked();
            }

            Dismiss();
        }

        #endregion
    }
}
