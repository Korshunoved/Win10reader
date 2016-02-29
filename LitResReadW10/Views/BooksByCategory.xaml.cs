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

using LitRes;
using LitRes.Models;
using LitRes.ValueConverters;
using LitRes.ViewModels;
using LitResReadW10.Controls;

namespace LitRes.Views
{
	[View("BooksByCategory")]
	[ViewParameter("category", typeof(int))]
	[ViewParameter("id", typeof(int),Required = false)]
	[ViewParameter("title", typeof(string),Required = false)]
	public partial class BooksByCategory : BooksByCategoryFitting
	{
	    private string _title;
		#region Constructors/Disposer
		public BooksByCategory()
		{
			InitializeComponent();
            Loaded += BooksByCategory_Loaded;
		}

        private void BooksByCategory_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ViewModel.LoadMoreCalled.Execute(null);
        }
        #endregion

        #region CreateDataSession
        protected override Digillect.Mvvm.Session CreateDataSession( DataLoadReason reason )
		{
            int param = ViewParameters.GetValue<int>("category");
		    ViewModel.BooksViewModelType = param;
            if (ViewParameters.Contains("id"))
            {
                int id = ViewParameters.GetValue<int>("id");
                ViewModel.GenreOrTagOrSeriaID = id;
            }

            if (ViewParameters.Contains("title"))
            {
                _title = ViewParameters.GetValue<string>("title");
            }


            switch ((BooksByCategoryViewModel.BooksViewModelTypeEnum)ViewModel.BooksViewModelType)
            {
                case BooksByCategoryViewModel.BooksViewModelTypeEnum.Interesting:
                    Analytics.Instance.sendMessage(Analytics.ViewInteresting);
                    break;
                case BooksByCategoryViewModel.BooksViewModelTypeEnum.Novelty:
                    Analytics.Instance.sendMessage(Analytics.ViewNew);
                    break;
                case BooksByCategoryViewModel.BooksViewModelTypeEnum.Popular:
                    Analytics.Instance.sendMessage(Analytics.ViewPop);
                    break;
                default:
                    break;
            }

			return base.CreateDataSession( reason );
		}
		#endregion

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (!string.IsNullOrEmpty(_title))
                ControlPanel.Instance.TopBarTitle = _title;
            else
                ControlPanel.Instance.TopBarTitle =
                    (string) (new EnumCategoryTitleConverter().Convert(ViewModel.BooksViewModelType, null, null));
            //if (NavigationContext.QueryString.ContainsKey("NavigatedFrom"))
            //{
            //    if (NavigationContext.QueryString["NavigatedFrom"].Equals("toast"))
            //    {                   
            //        if (NavigationContext.QueryString.ContainsKey(PushNotificationsViewModel.SPAMPACK_TAG))
            //            PushNotificationsViewModel.SendToastSpampack(NavigationContext.QueryString[PushNotificationsViewModel.SPAMPACK_TAG]);
            //    }
            //}
        }

	    private void CategoryBooks_OnLoadMore(object sender, EventArgs e)
	    {
            ViewModel.LoadMoreCalled.Execute(null);
        }

	    private void CategoryBooks_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
           if(CategoryBooks.SelectedItem != null) ViewModel.BookSelected.Execute(CategoryBooks.SelectedItem);  
        }

	    private Task CategoryBooks_OnMoreDataRequested(object sender, EventArgs e)
	    {
            ViewModel.LoadMoreCalled.Execute(null);
            return Task.Run(() => { });
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

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ChoosePaymentMethod")
            {
                ChoosePaymentMethod();
            }
        }

    }

    public class BooksByCategoryFitting : ViewModelPage<BooksByCategoryViewModel>
	{
	}
}