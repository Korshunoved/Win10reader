﻿using System;
using System.ComponentModel;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Digillect.Mvvm.UI;
using LitRes.Models;
using LitRes.ValueConverters;
using LitRes.ViewModels;
using LitResReadW10.Controls;
using LitResReadW10.Helpers;

namespace LitResReadW10.Views
{
    [View("ShopEditorsChoice")]
    public sealed partial class ShopEditorsChoice : ShopEditorsChoiceFitting
    {
        public ShopEditorsChoice()
        {
            InitializeComponent();
            SizeChanged += ShopEditorsChoice_SizeChanged;
            Loaded += ShopEditorsChoice_Loaded;
            NavigationCacheMode = NavigationCacheMode.Enabled;
        }

        private void ShopEditorsChoice_SizeChanged(object sender, SizeChangedEventArgs e)
        {            
        }

        void ShopEditorsChoice_Loaded(object sender, RoutedEventArgs e)
        {
            CheckWellcomeScreen();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.LoadMyBooks();
            ViewModel.LoadGiftInfo();          
            if (!SystemInfoHelper.HasInternet() && ViewModel?.InterestingBooks?.Count == 0)
            {
                NoConnection.Visibility = Visibility.Visible;
            }
        }

        private void CheckWellcomeScreen()
        {
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

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ChoosePaymentMethod")
            {
                ChoosePaymentMethod();
            }
            else if (e.PropertyName == "Banners")
            {
                var banners = new BannerControl(Banners, ViewModel.Banners, Frame);
            }
            else if (e.PropertyName == "PresentCovers")
            {
                if (!GiftInfo.GiftAvailable)
                {
                    FirstBookBrush.ImageSource = (BitmapImage)(new UrlToImageConverter().Convert(ViewModel.Covers[0], null, null));
                    var count = ViewModel.Covers.Count;
                    if (count > 1)
                        SecondBookBrush.ImageSource = (BitmapImage)(new UrlToImageConverter().Convert(ViewModel.Covers[1], null, null));
                    if (count > 2)
                        ThirdBookBrush.ImageSource = (BitmapImage)(new UrlToImageConverter().Convert(ViewModel.Covers[2], null, null));
                    PhoneGridBookGift.Visibility = Visibility.Visible;
                }
                else
                {
                    PhonePresentPopup.Visibility=Visibility.Visible;
                    PhoneGridBookGift.Visibility=Visibility.Collapsed;                    
                }
            }
            else if (e.PropertyName == "PresentOk")
            {
                MainPage.Instance.ShowMessageBox("Книга добавлена на ваш аккаунт в качестве подарка.");
            }
            else if (e.PropertyName == "PresentError")
            {
                MainPage.Instance.ShowMessageBox("Произошла ошибка добавления подарка.");
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ControlPanel.Instance.TopBarTitle = "Выбор редакции";
            base.OnNavigatedTo(e);
        }

        private void Body_Tap(object sender, TappedRoutedEventArgs e)
        {
            var listView = sender as ListView;
            if (listView?.SelectedItem != null)
            {
                var book = listView.SelectedItem as Book;
                ViewModel.BookSelected.Execute(book);
            }
        }

        private void Body_Click(object sender, ItemClickEventArgs e)
        {
            var book = e.ClickedItem as Book;
            ViewModel.BookSelected.Execute(book);
        }

        private void pageHeader_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void BuyButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var book = button.DataContext as Book;
                if (book == null)
                    return;
                if (book.IsFreeBook)
                {
                    if (book.InGifts != null && book.InGifts.Contains("1"))
                        ViewModel.BuyBook.Execute(book);
                    else
                        ViewModel.Read.Execute(book);
                }
                else
                {
                    ViewModel.BuyBook.Execute(book);
                }
            }
        }

        private void ReadButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var button = sender as Button;
            var book = button?.DataContext as Book;
            if (book == null)
                return;
            ViewModel.Read.Execute(book);
        }

        private void ScrollViewer_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var sv = sender as ScrollViewer;

            if (sv == null) return;
            var verticalOffsetValue = sv.VerticalOffset;
            var maxVerticalOffsetValue = sv.ExtentHeight - sv.ViewportHeight;
            if (maxVerticalOffsetValue < 0 || Math.Abs(verticalOffsetValue - maxVerticalOffsetValue) < 1.0)
            {
                ViewModel.LoadMoreInterestingBooks.Execute(null);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            PhoneGridBookGift.Visibility = Visibility.Collapsed;            
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            PhonePresentPopup.Visibility = Visibility.Collapsed;            
        }

        private void TextBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {            
            MainPage.Instance.DescriptionPanel.Visibility = Visibility.Visible;
        }

        private void GiftButton_OnClick(object sender, TappedRoutedEventArgs e)
        {
            var button = sender as Button;
            var book = button?.DataContext as Book;
            if (book == null)
                return;
            if (book.IsGiftBook)
            {
                ViewModel.GiftBook.Execute(book);
            }
        }
    }

    public class ShopEditorsChoiceFitting : ViewModelPage<MainViewModel>
    {
        
    }
}
