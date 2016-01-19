using System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Autofac;
using Digillect.Mvvm.Services;
using Digillect.Mvvm.UI;
using LitRes.Models;
using LitRes.ViewModels;

namespace LitRes.Views
{
	[View( "Search" )]
	public partial class Search : SearchFitting
	{
		public const string tbSearchDefault = "введите название книги или автора";

		#region Constructors/Disposer
		public Search()
		{
			InitializeComponent();

			//Loaded += Search_Loaded;
		}
		#endregion

		//#region View Events

		//private async void tbSearch_KeyDown(object sender, KeyEventArgs e)
		//{
		//	if( e == null || e.Key == Key.Enter || e.PlatformKeyCode == 0x0A )
		//	{
		//		if ( tbSearch.Text == string.Empty )
		//		{
		//			MessageBox.Show(tbSearchDefault, "Внимание", MessageBoxButton.OK);
		//			return;
		//		}

		//		//ViewModel.AddToHistory( new Models.SearchQuery { Date = DateTime.Now, SearchString = tbSearch.Text } );

		//		//Check if found something and cache results
		//		bool found = await ViewModel.SearchBooks( tbSearch.Text );
		//		if(found)
		//		{
					
		//			Scope.Resolve<INavigationService>().Navigate("SearchResults", Digillect.Mvvm.Parameters.From("searchText", tbSearch.Text));
		//		}
		//		else
		//		{
		//			MessageBox.Show("По данному запросу ничего не найдено", "Внимание", MessageBoxButton.OK);
		//		}
		//	}
		//}
		//#endregion

		//void Search_Loaded( object sender, RoutedEventArgs e )
		//{
		//	tbSearch.Focus();
  //          Analytics.Instance.sendMessage(Analytics.ViewSearch);
		//}

		//private void SearchQueries_Tap(object sender, GestureEventArgs e)
		//{
		//	var searchQuery = SearchQueries.SelectedItem as SearchQuery;

		//	if(searchQuery != null)
		//	{
		//		tbSearch.Text = searchQuery.SearchString;
		//		tbSearch.Focus();
		//		tbSearch.Select(tbSearch.Text.Length, 0);
		//		tbSearch_KeyDown( sender, null );
		//	}
		//}

		//private void RemoveQuery_Click( object sender, RoutedEventArgs e )
		//{
		//	var searchQuery = (sender as FrameworkElement).DataContext as SearchQuery;

		//	if( searchQuery != null )
		//	{
		//		ViewModel.RemoveFromHistory( searchQuery );
		//	}
		//}
	}

	public class SearchFitting : ViewModelPage<SearchHistoryViewModel>
	{
	}
}