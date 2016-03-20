using System;
using System.ComponentModel;
using System.Diagnostics;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Digillect.Mvvm.UI;
using LitRes.Exceptions;
using LitRes.ViewModels;
using LitResReadW10.Controls;

namespace LitRes.Views
{
	[View( "Person" )]
	[ViewParameter( "Id", Required = false)]
	[ViewParameter( "PersonName", Required = false)]
	public partial class Person : PersonFitting
	{
		#region Constructors/Disposer
		public Person()
		{
			InitializeComponent();
            Loaded += Person_Loaded;
		}

        private void Person_Loaded(object sender, RoutedEventArgs e)
        {
            ControlPanel.Instance.OptionsDropDownButtonVisibility = Visibility.Visible;
            try
            {
                ControlPanel.Instance.OptionsDropDownMenuItems.Clear();

                var firstItem = new MenuFlyoutItem
                {
                    Text = "Подписаться",
                };
                firstItem.Tapped += (o, args) =>
                {
                    Analytics.Instance.sendMessage(Analytics.ActionSubscribe);
                    ViewModel.ChangeNotificationStatus();
                };

                var secondItem = new MenuFlyoutItem
                {
                    Text = "Оставить отзыв",
                    Command = ViewModel.WriteRecenseSelected
                };
                
                ControlPanel.Instance.OptionsDropDownMenuItems.Add(firstItem);
                ControlPanel.Instance.OptionsDropDownMenuItems.Add(secondItem);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        #endregion

        #region CreateDataSession
        protected override System.Threading.Tasks.Task<Digillect.Mvvm.Session> CreateDataSessionAsync( DataLoadReason reason )
		{
			ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
			ViewModel.PropertyChanged += ViewModel_PropertyChanged;

			ViewModel.SessionComplete -= ViewModel_SessionComplete;
			ViewModel.SessionComplete += ViewModel_SessionComplete;

			if( !string.IsNullOrEmpty( ViewParameters.GetValue<string>( "PersonName" ) ) )
			{
				return ViewModel.LoadByName( ViewParameters.GetValue<string>( "PersonName" ) );
			}

            return ViewModel.LoadById(ViewParameters.GetValue<string>("Id"));
		}

        protected override Digillect.Mvvm.Session CreateDataSession(DataLoadReason reason)
        {
            return null;
        }

        void ViewModel_SessionComplete( object sender, Digillect.Mvvm.SessionEventArgs e )
		{
			
		}
		#endregion

		#region View Events

		private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "AddedToNotifications")
			{
				
			}
			if (e.PropertyName == "Entity")
			{
			    ControlPanel.Instance.TopBarTitle = string.Format("{0}", ViewModel.Entity?.Title?.Main);
			}
            if (e.PropertyName == "ChoosePaymentMethod")
            {
                ChoosePaymentMethod();
            }
        }

	    protected override void OnNavigatedFrom(NavigationEventArgs e)
	    {
            ControlPanel.Instance.OptionsDropDownButtonVisibility = Visibility.Collapsed;
            base.OnNavigatedFrom(e);
	    }

	    protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            //if (NavigationContext.QueryString.ContainsKey("NavigatedFrom"))
            //{
            //    if (NavigationContext.QueryString["NavigatedFrom"].ToString().Equals("toast"))
            //    {
            //        if (NavigationContext.QueryString.ContainsKey(PushNotificationsViewModel.SPAMPACK_TAG))
            //            PushNotificationsViewModel.SendToastSpampack(NavigationContext.QueryString[PushNotificationsViewModel.SPAMPACK_TAG]);
            //    }
            //}
        }

		#endregion

	    private void AuthorBooks_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
	        ViewModel.BookSelected.Execute((sender as ListView).SelectedItem);
	    }

	    private void PivotControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	    {

        }

        private async void AddRecenseButton_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
            var caption = "Внимание";
            var required = "Для отправки необходимо заполнить рецензию";
            var _personUuid = ViewModel.Entity.Id;

            if (RecenseTextBlock.Text == string.Empty)
            {
                await new MessageDialog(required, caption).ShowAsync();
                RecenseTextBlock.Focus(FocusState.Pointer);
                return;
            }

            if (RecenseTextBlock.Text.Length < 140)
            {
                await new MessageDialog("Извините, но минимальная длина отзыва 140 символов.").ShowAsync();
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(_personUuid))
                {
                     await ViewModel.AddPersonRecense(RecenseTextBlock.Text, _personUuid);
                }

                Analytics.Instance.sendMessage(Analytics.ActionWriteReviewOk);
            }
            catch (CatalitAuthorizationException)
            {
                await new MessageDialog("Неверный логин или пароль", caption).ShowAsync();
                return;
            }
            await new MessageDialog("Ваш отзыв отправлен и ожидает модерации", "Спасибо").ShowAsync();
            RecenseTextBlock.Text = string.Empty;
            PivotControl.SelectedIndex = 0;
        }

	    private void RecenseText_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
            TextBlock tb = sender as TextBlock;
            if(tb == null) return;

            if (tb.MaxHeight == 2048)
            {
                tb.MaxHeight = 160;
            }
            else
            {
                tb.MaxHeight = 2048;
            }
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

	public class PersonFitting : ViewModelPage<PersonViewModel>
	{
	}
}