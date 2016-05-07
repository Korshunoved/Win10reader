﻿using System;
using System.ComponentModel;
using Digillect.Mvvm.UI;
using LitRes.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using LitResReadW10.Controls;

namespace LitRes.Views
{
	[View( "MyBooks" )]
	public partial class MyBooks : MyBooksFitting
	{
		private bool _reloadMyBooks;
	    private bool _booksByTimeMode = true;

		#region Constructors/Disposer
		public MyBooks()
		{
			InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled;
            Loaded += MyBooks_Loaded;           
		}

        void MyBooks_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
            ViewModel.LoadMyBooks();
            MyBooksEmptyStackPanel.Visibility = ViewModel.BooksByTime.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            Analytics.Instance.sendMessage(Analytics.ViewMyBooks);
        }

	    private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	    {
	        if (e.PropertyName.Contains("BooksByTime"))
	        {
	            MyBooksEmptyStackPanel.Visibility = ViewModel.BooksByTime.Count > 0
	                ? Visibility.Collapsed
	                : Visibility.Visible;
	        }
	    }

	    protected override void OnNavigatedTo(NavigationEventArgs e)
	    {
            ControlPanel.Instance.TopBarTitle = "Мои книги";
            ControlPanel.Instance.DropDownButtonVisibility = Visibility.Collapsed;

            ControlPanel.Instance.DropDownMenuItems.Clear();

            var firstItem = new MenuFlyoutItem { Text = "Последние" };
            firstItem.Click += (sender, args) =>
            {
                _booksByTimeMode = true;
                MyBooksListView.ItemsSource = ViewModel.BooksByTime;
                MyBooksGridView.ItemsSource = ViewModel.BooksByTime;
            };
            var secondItem = new MenuFlyoutItem { Text = "По автору" };
            secondItem.Click += (sender, args) =>
            {
                _booksByTimeMode = false;
                MyBooksListView.ItemsSource = ViewModel.BooksByAuthors;
                MyBooksGridView.ItemsSource = ViewModel.BooksByAuthors;
            };
            ControlPanel.Instance.DropDownMenuItems.Add(firstItem);
            ControlPanel.Instance.DropDownMenuItems.Add(secondItem);

            base.OnNavigatedTo(e);
	    }

	    protected override void OnNavigatedFrom(NavigationEventArgs e)
	    {
            ControlPanel.Instance.DropDownMenuItems.Clear();
            ControlPanel.Instance.DropDownButtonVisibility = Visibility.Collapsed;
            base.OnNavigatedFrom(e);            
	    }

	    #endregion

	    private void MyBooksListView_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
            if (MyBooksListView.SelectedItem != null) ViewModel.BookSelected.Execute(MyBooksListView.SelectedItem);
        }

	    private void MyBooksGridView_OnItemClick(object sender, ItemClickEventArgs e)
	    {
            var book = e.ClickedItem as Models.Book;
            ViewModel.BookSelected.Execute(book);
        }

	    private void DownloadButton_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
	        throw new System.NotImplementedException();
	    }

	    private void FragmentButton_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
	        throw new System.NotImplementedException();
	    }

	    private void ReadButton_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
	        var button = sender as Button;
	        var book = button?.DataContext as Models.Book;
	        if (book == null)
	            return;
	        ViewModel.Read.Execute(book);
	    }

	    private void FreeButton_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
	        throw new System.NotImplementedException();
	    }
    }

    public class MyBooksFitting : ViewModelPage<MyBooksViewModel>
	{
	}
}