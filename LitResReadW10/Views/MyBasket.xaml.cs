using System;
using System.ComponentModel;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using BookParser.Extensions;
using Digillect.Mvvm.UI;
using LitRes.Models;
using LitRes.ViewModels;
using LitResReadW10.Controls;
using LitResReadW10.Helpers;

namespace LitResReadW10.Views
{
    [View("MyBasket")]
    public partial class MyBasket :  MyBasketFitting
    {
        public MyBasket()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled;
            Loaded += MyBasket_Loaded;
        }

        void MyBasket_Loaded(object sender, RoutedEventArgs e)
        {
            if (!SystemInfoHelper.HasInternet())
            {
                NoConnection.Visibility = Visibility.Visible;
                MyBooksEmptyStackPanel.Visibility = Visibility.Collapsed;
                return;
            }
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.LoadMyBooks();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ControlPanel.Instance.TopBarTitle = "Отложенные";
            base.OnNavigatedTo(e);
        }

        private void MyBasketListView_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (MyBasketListView.SelectedItem != null) ViewModel.BookSelected.Execute(MyBasketListView.SelectedItem);
        }

        private void pageHeader_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ChoosePaymentMethod")
            {
                ChoosePaymentMethod();
            }
        }

        private void MyBasketGridView_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var book = e.ClickedItem as Book;
            ViewModel.BookSelected.Execute(book);
        }

        private void BuyButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var book = button.DataContext as LitRes.Models.Book;
                ViewModel.BuyBook.Execute(book);
            }
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
                TextWrapping = TextWrapping.Wrap,
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
            //storeButton.Tapped += (sender, args) => { ViewModel.BuyBookFromMicrosoft.Execute(null); dialog.Hide(); };
            panel.Children.Add(storeButton);

            panel.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 10),
                Text = "Внимание! К цене будет добавлена комисия Windows Store.",

                TextWrapping = TextWrapping.Wrap,
            });
            dialog.Content = panel;
            await dialog.ShowAsync();
        }
    }

    public class MyBasketFitting : ViewModelPage<MyBooksViewModel>
    {
    }
}
