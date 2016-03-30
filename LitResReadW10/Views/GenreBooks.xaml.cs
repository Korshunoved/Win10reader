using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Digillect.Mvvm.UI;
using LitRes.Models;
using LitRes.ValueConverters;
using LitRes.ViewModels;
using LitResReadW10.Controls;
using LitResReadW10.Helpers;

namespace LitRes.Views
{
	[View( "GenreBooks" )]
	[ViewParameter( "Index", typeof(bool), Required = false)]
	public partial class GenreBooks : GenreBooksFitting
	{
	    private bool _isOpened = false;
	    private bool _popularBooksMode = true;
		#region Constructors/Disposer
		public GenreBooks()
		{
			InitializeComponent();
            //NavigationCacheMode = NavigationCacheMode.Enabled;
        }
        #endregion

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ControlPanel.Instance.TopBarTitle = ViewModel.Entity.Title;
            ControlPanel.Instance.DropDownButtonVisibility = Visibility.Collapsed;
            ControlPanel.Instance.DropDownMenuItems.Clear();

            var firstItem = new MenuFlyoutItem {Text = "Популярные"};
            firstItem.Click += (sender, args) =>
            {
                _popularBooksMode = true;
                PopularListView.ItemsSource = ViewModel.PopularBooks;
                PopularGridView.ItemsSource = ViewModel.PopularBooks;
            };
            var secondItem = new MenuFlyoutItem {Text = "Новые"};
            secondItem.Click += (sender, args) =>
            {
                _popularBooksMode = false;
                PopularListView.ItemsSource = ViewModel.NoveltyBooks;
                PopularGridView.ItemsSource = ViewModel.NoveltyBooks;
            };
            ControlPanel.Instance.DropDownMenuItems.Add(firstItem);
            ControlPanel.Instance.DropDownMenuItems.Add(secondItem);
            if (!SystemInfoHelper.HasInternet())
            {
                NoConnection.Visibility = Visibility.Visible;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ControlPanel.Instance.DropDownButtonVisibility = Visibility.Collapsed;
            ControlPanel.Instance.DropDownMenuItems.Clear();
            base.OnNavigatedFrom(e);
        }

        protected override void DeleteGenresPivotItem()
		{
			GenresListView.Visibility = Visibility.Collapsed;
            GenresGridView.Visibility = Visibility.Collapsed;
		}

	    private void GenresListView_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
	        var genre = GenresListView.SelectedItem as Genre;
	        OpenGenrePage(genre);
	    }

        private void GenresGridView_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var genre = e.ClickedItem as Genre;
            OpenGenrePage(genre);
        }

        void OpenGenrePage(Genre genre)
	    {
            if (genre != null) ViewModel.GenreSelected.Execute(genre);
        }

	    private void GenreRecomendedBooks_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
            if (PopularListView.SelectedItem != null) ViewModel.BookSelected.Execute(PopularListView.SelectedItem);
        }

        private void PopularGridView_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var book = e.ClickedItem as Models.Book;
            ViewModel.BookSelected.Execute(book);
        }

        private void ScrollViewer_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
	    {
            var sv = sender as ScrollViewer;

            if (sv != null)
            {
                var verticalOffsetValue = sv.VerticalOffset;
                var maxVerticalOffsetValue = sv.ExtentHeight - sv.ViewportHeight;
                if (maxVerticalOffsetValue < 0 || Math.Abs(verticalOffsetValue - maxVerticalOffsetValue) < 1.0)
                {
                    LoadMore();
                }
            }
        }

	    private void LoadMore()
	    {
	        if(_popularBooksMode) ViewModel.LoadMorePopularBooks.Execute(null);
            else ViewModel.LoadMoreNoveltyBooks.Execute(null);
        }

	    private void MinimizeMaximizeSubgenres(object sender, TappedRoutedEventArgs e)
	    {
            _isOpened = !_isOpened;

            GenresListStackPanel.Visibility = _isOpened? Visibility.Visible: Visibility.Collapsed;
	        MinimizerText.Text = _isOpened ? "Скрыть" : "Поджанры";
	    }

        private void BuyButton_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
            var button = sender as Button;
            if (button != null)
            {
                var book = button.DataContext as Models.Book;
                if (book != null && book.isFreeBook)
                    ViewModel.Read.Execute(book);
                else
                    ViewModel.BuyBook.Execute(book);
            }
        }
	}

	public class GenreBooksFitting : EntityPage<Genre, GenreBooksViewModel>
	{
        private bool isFirstNode;

		#region CreateDataSession
		protected override Digillect.Mvvm.Session CreateDataSession( DataLoadReason reason )
		{
			var index = ViewParameters.GetValue<bool>( "Index" );

			var session = base.CreateDataSession( reason );

            isFirstNode = index;

			ViewModel.PropertyChanged -= ViewModelPropertyChanged;
			ViewModel.PropertyChanged += ViewModelPropertyChanged;

			return session;
		}
		#endregion

		void ViewModelPropertyChanged( object sender, PropertyChangedEventArgs e )
		{
			if( e.PropertyName == "Entity" )
			{
				if( ViewModel.Entity?.Children != null && ViewModel.Entity.Children.Count == 0 )
				{
					DeleteGenresPivotItem();
				}
            }
            else if (e.PropertyName == "ChoosePaymentMethod")
            {
                ChoosePaymentMethod();
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
            storeButton.Tapped += (sender, args) => { ViewModel.BuyBookFromMicrosoft.Execute(null); dialog.Hide(); };
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

        protected virtual void DeleteGenresPivotItem()
		{
			
		}

        //protected virtual void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if ((sender as Pivot).SelectedIndex == 0) Analytics.Instance.sendMessage(isFirstNode? Analytics.ViewGenres1Pop: Analytics.ViewGenres2Pop);
        //    else if ((sender as Pivot).SelectedIndex == 1) Analytics.Instance.sendMessage(isFirstNode? Analytics.ViewGenres1New: Analytics.ViewGenres2New);
        //}
	}
}