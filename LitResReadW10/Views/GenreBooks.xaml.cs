using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Digillect.Mvvm.UI;
using LitRes.Models;
using LitRes.ValueConverters;
using LitRes.ViewModels;
using LitResReadW10.Controls;

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
            ControlPanel.Instance.DropDownButtonVisibility = Visibility.Visible;
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