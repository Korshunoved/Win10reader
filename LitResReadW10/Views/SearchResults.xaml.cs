using System;
using System.ComponentModel;
using System.Diagnostics;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Digillect.Mvvm;
using Digillect.Mvvm.UI;
using LitRes.Models.JsonModels;
using LitRes.ViewModels;
using LitResReadW10.Controls;
using LitResReadW10.Helpers;

namespace LitRes.Views
{
	[View( "SearchResults" )]
	[ViewParameter( "SearchText" )]
	public partial class SearchResults : SearchResultsFitting
	{
		public string SearchText
		{
			get;
			set;
		}

		#region Constructors/Disposer
		public SearchResults()
		{
			InitializeComponent();
		    NavigationCacheMode = NavigationCacheMode.Enabled;
            ControlPanel.Instance.IsSearchPageOpened = true;
        }
		#endregion

	    protected override void OnNavigatedTo(NavigationEventArgs e)
	    {
            base.OnNavigatedTo(e);
            ControlPanel.Instance.TopBarTitle = "Результаты поиска";
            ControlPanel.Instance.IsSearchPageOpened = true;
            ControlPanel.Instance.PhoneSearchBox.QuerySubmitted += PhoneSearchBox_QuerySubmitted;
            ControlPanel.Instance.PhoneSearchBox.KeyUp += PhoneSearchBox_KeyUp;
            if(e.NavigationMode == NavigationMode.New) SearchBooks();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ControlPanel.Instance.IsSearchPageOpened = false;
            ControlPanel.Instance.PhoneSearchBox.QuerySubmitted -= PhoneSearchBox_QuerySubmitted;
            ControlPanel.Instance.PhoneSearchBox.KeyUp -= PhoneSearchBox_KeyUp;
            if (e.NavigationMode == NavigationMode.Back)
            {
                ControlPanel.Instance.PhoneSearchBox.QueryText = string.Empty;
                ResetPageCache();
            }
         
            base.OnNavigatedFrom(e);
        }

        private void ResetPageCache()
        {
            var frame = ((WindowsRTApplication) Application.Current).RootFrame;
            var cacheSize = frame.CacheSize;
            frame.CacheSize = 0;
            frame.CacheSize = cacheSize;
        }

        private void PhoneSearchBox_QuerySubmitted(SearchBox sender, SearchBoxQuerySubmittedEventArgs args)
        {
            Debug.WriteLine($"PhoneSearchBox_QuerySubmitted: {args.QueryText}");
            Search(args.QueryText);
        }

	    void Search(string queryText)
	    {
            //GenresAndTagsVariableSizedWrapGrid.Children.Clear();

	        ViewModel.SearchQuery = queryText;
            SearchBooks();
        }

        private async void SearchBooks()
	    {
	        try
	        {
	            await ViewModel.SearchBooks();
	        }
	        catch (Exception ex)
	        {
	            Debug.WriteLine(ex.Message);
	        }
	    }

        private void PhoneSearchBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                if (!SystemInfoHelper.IsDesktop())
                {
                    var queryText = ControlPanel.Instance.PhoneSearchBox.QueryText;
                    this.Focus(FocusState.Programmatic);
                    Search(queryText);
                }
            }
        }
        #region CreateDataSession
        protected override Session CreateDataSession( DataLoadReason reason )
		{
			SearchText = ViewParameters.GetValue<string>( "SearchText" );
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.SearchQuery = SearchText;

			return base.CreateDataSession( reason );
		}
        #endregion

	    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
	    {
	        if (e.PropertyName == "TagsAndGenresLoaded")
	        {
                InitTagsAndGenres();
	        }
            else if (e.PropertyName == "Found")
            {
                NotFoundStackPanel.Visibility = Visibility.Collapsed;
                QueryResultTextBox.Visibility = Visibility.Visible;
                MainScrollViewer.Visibility = Visibility.Visible;
            }
            else if (e.PropertyName == "NotFound")
            {
                MainScrollViewer.Visibility = Visibility.Collapsed;
                NotFoundStackPanel.Visibility = Visibility.Visible;
                QueryResultTextBox.Visibility = Visibility.Collapsed;
            }
        }

	    private void InitTagsAndGenres()
	    {
	        GenresAndTagsListView.ItemsSource = ViewModel.TagsAndGenresCollection;
	        //if (ViewModel.TagsAndGenresCollection.Count > 0)
	        //{
	        //    GenresAndTagsVariableSizedWrapGrid.Children.Clear();

	        //    var genreButtonStyle = CurrentApplication.Resources["TagButtonStyle"] as Style;

	        //    foreach (var tagOrGenre in ViewModel.TagsAndGenresCollection)
	        //    {
	        //        var genreButton = new Button
	        //        {
	        //            DataContext = tagOrGenre,
	        //            Content = (tagOrGenre is Genre) ? ((Genre)tagOrGenre).name.ToUpper() : ((Tag)tagOrGenre).name.ToUpper(),
	        //            Margin = new Thickness(5),
	        //            //Style = genreButtonStyle,
	        //            HorizontalAlignment = HorizontalAlignment.Stretch
	        //        };
	        //        genreButton.Tapped += GenreButton_Tapped;
	        //        GenresAndTagsVariableSizedWrapGrid.Children.Add(genreButton);
	        //    }
	        //}
	    }

        private void FoundedBooks_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
	        var foundedBooks = (ListView) sender;
	        ViewModel.BookSelected.Execute(foundedBooks.SelectedItem);
	    }

	    private void MoreOtherBooks_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
	        ViewModel.ShowAllBooks();
	    }

	    private void PersonsResult_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
	        ViewModel.PersonSelected.Execute((sender as ListView).SelectedItem);
        }

	    private void MorePersons_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
            ViewModel.ShowAllPersons();
        }

	    private void SequencesResult_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
            ViewModel.SequenceSelected.Execute((sender as ListView).SelectedItem);
        }

	    private void MoreSequences_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
	        ViewModel.ShowAllSequences();
	    }

	    private void BestResult_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
	        OpenPageByObject((sender as ListView).SelectedItem);
        }
        
	    private void OpenPageByObject(object sender)
	    {
            if (sender is Models.Book)
            {
                ViewModel.BookSelected.Execute(sender);
            }
            else if (sender is Models.Person)
            {
                ViewModel.PersonSelected.Execute(sender);
            }
            else if (sender is Genre)
            {
                ViewModel.GenreSelected.Execute(sender);
            }
            else if (sender is Tag)
            {
                ViewModel.TagSelected.Execute(sender);
            }
            else if (sender is Models.Book.SequenceInfo)
            {
                ViewModel.SequenceSelected.Execute(sender);
            }
        }

        //private void GenreButton_Tapped(object sender, TappedRoutedEventArgs e)
        //{
        //    OpenPageByObject(sender);
        //}

	    //private void CollectionsResult_OnTapped(object sender, TappedRoutedEventArgs e)
	    //{
     //       ViewModel.CollectionSelected.Execute((sender as ListView).SelectedItem);
     //   }

	    //private void MoreCollections_OnTapped(object sender, TappedRoutedEventArgs e)
	    //{
	    //    ViewModel.ShowAllCollections();
	    //}
	    private void GenresAndTagsListView_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
            OpenPageByObject(GenresAndTagsListView.SelectedItem);
        }
	}

	public class SearchResultsFitting : ViewModelPage<SearchViewModel>
	{
	}

}   