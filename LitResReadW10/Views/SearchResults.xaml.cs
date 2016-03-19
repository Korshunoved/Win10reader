using System;
using System.ComponentModel;
using System.Diagnostics;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Digillect;
using Digillect.Mvvm;
using Digillect.Mvvm.UI;
using LitRes.Models.JsonModels;
using LitRes.ViewModels;
using LitResReadW10.Controls;
using LitResReadW10.Helpers;

namespace LitRes.Views
{
	[View( "SearchResults" )]
	[ViewParameter("SearchText")]
	public partial class SearchResults : SearchResultsFitting
	{
		public string SearchText
		{
			get;
			set;
		}

        public string LastSearch { get; set; }

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
	        var xParameters = (XParameters) e.Parameter;
	        if (xParameters != null) SearchText = xParameters.GetValue<string>("SearchText");
	        ControlPanel.Instance.TopBarTitle = "Результаты поиска";
            ControlPanel.Instance.IsSearchPageOpened = true;
            ControlPanel.Instance.PhoneSearchBox.QuerySubmitted += PhoneSearchBox_QuerySubmitted;
            ControlPanel.Instance.PhoneSearchBox.KeyUp += PhoneSearchBox_KeyUp;
            if (!SystemInfoHelper.HasInternet())
            {
                NoConnection.Visibility = Visibility.Visible;
                return;
            }
            if (e.NavigationMode == NavigationMode.New) SearchBooks();
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
                if (LastSearch != SearchText)
	                ViewModel.SearchQuery = SearchText;
	            await ViewModel.SearchBooks();
	            LastSearch = SearchText;
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
            else if (e.PropertyName == "ChoosePaymentMethod")
            {
                ChoosePaymentMethod();
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

            //var storeButton = new Button
            //{
            //    Margin = new Thickness(0, 10, 0, 5),
            //    HorizontalAlignment = HorizontalAlignment.Stretch,
            //    Content = $"оплатить через Windows Store",
            //    BorderThickness = new Thickness(2),
            //    BorderBrush = new SolidColorBrush(Colors.Gray),
            //    Background = new SolidColorBrush(Colors.Transparent)
            //};
            //storeButton.Tapped += (sender, args) => { ViewModel.BuyBookFromMicrosoft.Execute(null); dialog.Hide(); };
            //panel.Children.Add(storeButton);

            //panel.Children.Add(new TextBlock
            //{
            //    Margin = new Thickness(0, 0, 0, 10),
            //    Text = "Внимание! К цене будет добавлена комисия Windows Store.",

            //    TextWrapping = TextWrapping.Wrap,
            //});
            dialog.Content = panel;
            await dialog.ShowAsync();
        }
    }

	public class SearchResultsFitting : ViewModelPage<SearchViewModel>
	{
	}

}   