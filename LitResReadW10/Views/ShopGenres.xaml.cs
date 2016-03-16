using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Autofac;
using Digillect.Mvvm.UI;
using LitRes.ViewModels;
using LitResReadW10.Controls;
using LitResReadW10.Helpers;

namespace LitResReadW10.Views
{
    [View("ShopGenres")]
    public sealed partial class ShopGenres : ShopGenresFitting
    {
        public ShopGenres()
        {
            InitializeComponent();
            SizeChanged += ShopGenres_SizeChanged;
            Loaded += ShopGenres_Loaded;
        }

        private void ShopGenres_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        void ShopGenres_Loaded(object sender, RoutedEventArgs e)
        {
            CheckWellcomeScreen();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.LoadMyBooks();
            if (!SystemInfoHelper.HasInternet())
            {
                NoConnection.Visibility = Visibility.Visible;
            }
        }

        private void CheckWellcomeScreen()
        {
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ControlPanel.Instance.TopBarTitle = "Жанры";
            base.OnNavigatedTo(e);

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
            if (GenresGridView.SelectedIndex != -1)
            {
                ViewModel.GenreSelected.Execute(GenresGridView.SelectedIndex);
                GenresGridView.SelectedIndex = -1;
            }
        }
    }

    public class ShopGenresFitting : ViewModelPage<MainViewModel>
    {
    }
}
