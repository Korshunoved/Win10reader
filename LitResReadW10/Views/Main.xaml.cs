using System;
using System.ComponentModel;
using Digillect.Mvvm.UI;
using LitRes.ViewModels;
using Autofac;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using LitResReadW10;
using LitResReadW10.Controls;

namespace LitRes.Views
{
	[View( "Main" )]
	public partial class Main : MainFitting
	{
        #region Constructors/Disposer
        public Main()
		{
            InitializeComponent();
            SizeChanged += Main_SizeChanged;
            
            if (!ApplicationData.Current.LocalSettings.Values.Keys.Contains("isWelcomeScreenShowed"))
            {             
                ApplicationData.Current.LocalSettings.Values.Add("isWelcomeScreenShowed", true);
               
                //TODO: Show Welcome Screen
            }
            
            Loaded += Main_Loaded;
		}

        private void Main_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }
        #endregion

        void Main_Loaded(object sender, RoutedEventArgs e)
		{
            CheckWellcomeScreen();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.LoadMyBooks();
		}

#warning CheckWellcomeScreen_NOT_IMPLEMENTED
        private void CheckWellcomeScreen()
        {
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            
        }
        
        private void Body_Tap(object sender, TappedRoutedEventArgs e)
        {
            var listView = sender as ListView;
            if (listView?.SelectedItem != null)
            {
                var book = listView.SelectedItem as Models.Book;
                ViewModel.BookSelected.Execute(book);
            }
        }

	    private void Body_Click(object sender, ItemClickEventArgs e)
        {
            var book = e.ClickedItem as Models.Book;
            ViewModel.BookSelected.Execute(book);
        }


        private async void Delete_Tap(object sender, TappedRoutedEventArgs e)
        {
            var book = (sender as FrameworkElement).DataContext as Models.Book;
            var catalogProvider = ((App)Application.Current).Scope.Resolve<LitRes.Services.ICatalogProvider>();

            await catalogProvider.DeleteBook(book);
            await ViewModel.LoadMyBooks();           
        }

	    protected override void OnNavigatedTo(NavigationEventArgs e)
	    {
	        ControlPanel.Instance.TopBarTitle = "Магазин";
			base.OnNavigatedTo( e );
            
		}

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        private void pageHeader_Loaded(object sender, RoutedEventArgs e)
        {

        }

	    private void GenresListView_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
	        ViewModel.GenreSelected.Execute(GenresListView.SelectedIndex);
	    }

	    private void GenresGridView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	    {
            if(GenresGridView.SelectedIndex != -1)
	        {
	            ViewModel.GenreSelected.Execute(GenresGridView.SelectedIndex);
	            GenresGridView.SelectedIndex = -1;
	        }
	    }
	}

    public class MainFitting : ViewModelPage<MainViewModel>
	{
	}
}