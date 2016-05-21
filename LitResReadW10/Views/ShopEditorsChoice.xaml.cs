﻿using System;
using System.ComponentModel;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Digillect.Mvvm.UI;
using LitRes.Models;
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
            NavigationCacheMode = NavigationCacheMode.Disabled;
        }

        private void ShopEditorsChoice_SizeChanged(object sender, SizeChangedEventArgs e)
        {            
        }

        void ShopEditorsChoice_Loaded(object sender, RoutedEventArgs e)
        {
            CheckWellcomeScreen();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.LoadMyBooks();
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
                //var loopPanel = GetLoopItemsPanel(Banners);
                //loopPanel.GetRandomItem();
                //GetRandomBanner();
                //DispatcherTimer timer = new DispatcherTimer();
                //timer.Interval = new TimeSpan(0,0,4);
                //timer.Tick += TimerOnTick;                
                //timer.Start();
            }
        }

        //private void GetRandomBanner()
        //{
        //    var rnd = new Random();
        //    if (Banners.Items == null) return;
        //    var idx = rnd.Next(0, Banners.Items.Count - 1);
        //    i = idx;
        //    _currentItem = Banners.Items[idx];
        //    Banners.SelectedItem = _currentItem;
        //}

        //private int i;
        //private object _currentItem;
        //private void TimerOnTick(object sender, object o)
        //{
        //    var timer = sender as DispatcherTimer;
        //    var seconds = timer?.Interval.Seconds;
        //    if (seconds >= 4)
        //    {
        //        i++;
        //        Debug.Assert(Banners.Items != null, "Banners.Items != null");
        //        if (i >= Banners.Items.Count) i = -1;
        //        timer.Stop();
        //        timer.Start();
        //    }

        //}


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
    }

    public class ShopEditorsChoiceFitting : ViewModelPage<MainViewModel>
    {
        
    }
}
