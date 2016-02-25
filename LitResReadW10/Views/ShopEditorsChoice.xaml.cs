using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Digillect.Mvvm.UI;
using LitRes.ViewModels;
using LitResReadW10.Controls;


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
        }

        private void ShopEditorsChoice_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        void ShopEditorsChoice_Loaded(object sender, RoutedEventArgs e)
        {
            CheckWellcomeScreen();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.LoadMyBooks();
        }

        private void CheckWellcomeScreen()
        {
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

        }

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
                var book = listView.SelectedItem as LitRes.Models.Book;
                ViewModel.BookSelected.Execute(book);
            }
        }

        private void Body_Click(object sender, ItemClickEventArgs e)
        {
            var book = e.ClickedItem as LitRes.Models.Book;
            ViewModel.BookSelected.Execute(book);
        }

        private void pageHeader_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }

    public class ShopEditorsChoiceFitting : ViewModelPage<MainViewModel>
    {
        
    }
}
