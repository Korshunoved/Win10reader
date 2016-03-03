using System;
using System.ComponentModel;
using System.Diagnostics;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Digillect.Mvvm;
using Digillect.Mvvm.UI;
using LitRes.ValueConverters;
using LitRes.Models;
using LitRes.ViewModels;
using LitResReadW10.Controls;
using LitRes.Exceptions;
using LitResReadW10.Helpers;

namespace LitRes.Views
{
	[View("Book")]
    [ViewParameter("BookEntity", typeof(Models.Book), Required = false)]
    [ViewParameter("hidden", typeof(string),Required = false)]
	public partial class Book : BookFitting
	{        
        private DispatcherTimer _timer;
	    private bool _animInProgress;
		#region Constructors/Disposer
		public Book()
		{
			InitializeComponent();
		}
		#endregion

		#region CreateDataSession
		protected override Session CreateDataSession( DataLoadReason reason )
		{
			ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
			ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            if (!string.IsNullOrEmpty(ViewParameters.GetValue<string>("hidden")))
		    {
		        ViewModel.IsHiddenBook = true;
		    }

            return base.CreateDataSession( reason );  
		}
		#endregion

        protected override async void OnDataLoadComplete(Session session)
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

            _timer.Tick += RefreshTimer;
            _timer.Start();
        }

	    private void InitTagsAndGenres()
	    {
            if (ViewModel.BookGenres.Count > 0)
            {
                var genreButtonStyle = CurrentApplication.Resources["TagButtonStyle"] as Style;
                foreach (var bookGenre in ViewModel.BookGenres)
                {
                    var genreButton = new Button
                    {
                        DataContext = bookGenre,
                        Content = bookGenre.Title.ToUpper(),
                        Margin = new Thickness(5),
                        Style = genreButtonStyle,
                    };
                    genreButton.Tapped += GenreButton_Tapped;
                  
                    GenresVariableSizedWrapGrid.Children.Add(genreButton);
                }
            }
        }

        private void GenreButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var genre = ((Button) sender).DataContext;
            ViewModel.TagSelected.Execute(genre);
        }

        public void RefreshTimer(object sender, object e)
        {
        }

        private void UpdateSelfServiceUI()
        {
            if (!ViewModel.Entity.isFreeBook && !string.IsNullOrEmpty(ViewModel.Entity.SelfService))
            {
         //       RequestButton.Content = "запросить";

                if (!ViewModel.Entity.IsMyBook || (ViewModel.Entity.IsMyBook && (ViewModel.Entity.SelfServiceMyRequest.Equals("1") || ViewModel.Entity.IsExpiredBook)))
                {
      //              SelfServiceBlock.Visibility = Visibility.Visible;
                }
                else
                {
    //                SelfServiceBlock.Visibility = Visibility.Collapsed;
                }

    //            SelfServiceBlockText.Visibility = Visibility.Visible;

                if (ViewModel.Entity.SelfServiceMyRequest.Equals("0"))
                {
                    if (ViewModel.Entity.SelfService.Equals("delayed"))
                    {
        //                SelfServiceBlockText.Text = "Свободных экземпляров нет.";
                    }
                    else if (ViewModel.Entity.SelfService.Equals("instant"))
                    {
//SelfServiceBlockText.Text = "Есть свободный экземпляр.";
                    }
                    else if (ViewModel.Entity.SelfService.Equals("impossible"))
                    {
             //           RequestButton.IsEnabled = false;
         //               SelfServiceBlockText.Text = "Книга недоступна.";

                        if (ViewModel.UserInformation.AccountType != (int)AccountTypeEnum.AccountTypeLibrary)
                        {
            //                SelfServiceBlock.Visibility = Visibility.Collapsed;
                        }
                    }
                }
                else
                {
         //           SelfServiceBlockText.Text = "Запрос в библиотеку отправлен.";
        //          RequestButton.Content = "отменить";
                }
            }
        }

		private void MainPivotSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
 
		    if (PivotControl.SelectedItem is PivotItem)
		    {
		        var currentPivotItem = (PivotItem) PivotControl.SelectedItem;
		        if (currentPivotItem.Tag is string)
		        {
		            if (((string) currentPivotItem.Tag).Equals("1990564"))
		            {
		                CheckOptionsButton();
                        ViewModel.LoadRecenses();
                        Analytics.Instance.sendMessage(Analytics.ViewRecenses);
                    }
		            else
		            {
                        ControlPanel.Instance.OptionsDropDownButtonVisibility = Visibility.Collapsed;
                    }
                }
		    }
        }
      
		private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Entity")
			{
			    var bookOpacityConverter = Resources["BookOpacityConverter"] as BookOpacityConverter;
                if (bookOpacityConverter != null) bookOpacityConverter.EntityId = ViewModel.Entity.Id;
            }
            else if (e.PropertyName == "ChoosePaymentMethod")
            {
                ChoosePaymentMethod();
            }
            else if (e.PropertyName == "BuyBookStart")
            {
                Analytics.Instance.sendMessage(Analytics.ActionBuyFullCard);
            }
            else if (e.PropertyName == "UpdateSelfServiceUI")
            {
                UpdateSelfServiceUI();
            }
            else if (e.PropertyName == "BookLoaded")
            {
                if (ViewModel.Entity.IsMyBook && !ViewModel.Entity.IsLocal)
                {
                    if (!string.IsNullOrEmpty(ViewModel.Entity.ExpiredDateStr) && !ViewModel.Entity.IsExpiredBook)
                    {
            //            ExpiredTimeImage.Visibility = Visibility.Visible;
           //             NotExpiredTextBlock.Visibility = Visibility.Visible;
          //              DataText.Text = ViewModel.Entity.ExpiredDate.ToString("d MMMM yyyy");
                    }
                    else if (ViewModel.Entity.IsExpiredBook)
                    {
           //             ExpiredTimeImage.Visibility = Visibility.Visible;
           //             ExpiredTextBlock.Visibility = Visibility.Visible;
                    }
                }
                if (ViewModel.Entity.IsLocal)
                {
                    CleanPivotItemByTag("1990564");
                    CleanPivotItemByTag("1990565");
                    CleanPivotItemByTag("1990566");
                    CleanPivotItemByTag("1990567");
                }
            }
            else if (e.PropertyName == "GenresLoaded")
            {
                InitTagsAndGenres();
            }
            else if (e.PropertyName == "ReadWithBooksLoaded")
            {
                if (ViewModel.ReadWithBooks != null && ViewModel.ReadWithBooks.Count == 0)
                {
                    CleanPivotItemByTag("1990566");
                }
            }
            else if (e.PropertyName == "SequenceBooksLoaded")
            {
                if (ViewModel.SequenceBooks == null || ViewModel.SequenceBooks.Count == 0)
                {
                    CleanPivotItemByTag("1990565");
                }
            }
            else if (e.PropertyName == "HasRelationBook")
            {
            }
            else if (e.PropertyName == "RecensesLoaded")
            {
                CheckOptionsButton();
            }
        }

	    private void CheckOptionsButton()
	    {
            if (ViewModel.RecenseExist)
            {
                ControlPanel.Instance.OptionsDropDownButtonVisibility = Visibility.Visible;
                try
                {
                    ControlPanel.Instance.OptionsDropDownMenuItems.Clear();

                    var firstItem = new MenuFlyoutItem
                    {
                        Text = "Оставить отзыв",
                        Command = ViewModel.WriteRecenseSelected
                    };
                    ControlPanel.Instance.OptionsDropDownMenuItems.Add(firstItem);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

	    private void CleanPivotItemByTag(string tag)
	    {
          
	        if (PivotControl.Items != null)
	            foreach (var piv in PivotControl.Items)
	            {
	                var pItem = (piv as PivotItem);
	                if (pItem != null && pItem.Tag != null)
	                {
	                    var s = pItem.Tag as string;
	                    if (s != null && s.Equals(tag))
	                    {
	                        PivotControl.Items.Remove(piv);
	                        break;
	                    }
	                }
	            }
	    }

		private void RecenseText_Tap(object sender, TappedRoutedEventArgs e)
		{
			var tb = sender as TextBlock;

			if( tb.MaxHeight == 2048 )
			{
				tb.MaxHeight = 160;
			}
			else
			{
				tb.MaxHeight = 2048;
			}
		}
        
	    protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ControlPanel.Instance.TopBarTitle = "Книга";
            base.OnNavigatedTo(e);

            ViewModel.UpdateButtons();

            Analytics.Instance.sendMessage(Analytics.ViewBookcard);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (_timer != null) _timer.Stop();         

            ControlPanel.Instance.OptionsDropDownButtonVisibility = Visibility.Collapsed;
            if (!SystemInfoHelper.IsDesktop())
            {
                FullImagePanel.Visibility = Visibility.Collapsed;
                ControlPanel.Instance.TopBarVisibility = Visibility.Visible;
                StatusBar statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                statusBar.ShowAsync();
            }

        }
        
        private void RequestButton_Tap(object sender, TappedRoutedEventArgs e)
        {
            if(ViewModel.Entity.SelfService.Equals("delayed") && !ViewModel.Entity.SelfServiceMyRequest.Equals("1"))
            {
              
            }
            else 
            {
                ViewModel.SelfServiceRequest.Execute(null);
            }
        }

        private void SelfServiceRequest2_tap(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.SelfServiceRequest.Execute(null);
        }

	    private void PivotItemSequence_OnLoaded(object sender, RoutedEventArgs e)
	    {
            if (ViewModel.Entity!=null && ViewModel.Entity.Description.Hidden.TitleInfo.Sequence == null)
            {
                PivotControl.Items?.Remove(sender);
            }
	    }

	    private async void CoverOnTap(object sender, TappedRoutedEventArgs e)
	    {
            if (!_animInProgress)
            {
               // if (!SystemInfoHelper.IsDesktop())
                {
                    var image = sender as Image;
                    if (image != null)
                        FullImageCover.Source = image.Source;
                    FullImagePanel.Visibility = Visibility.Visible;
                    ControlPanel.Instance.TopBarVisibility = Visibility.Collapsed;
                    StatusBar statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                    await statusBar.HideAsync();

                }
                //_animInProgress = true;	            
                //if (!HideCover()) ShowCover();
            }
	    }

	    void ShowCover()
	    {
       
	    }

	    bool HideCover()
	    {
           
	        return false;
	    }

        private void CoverRiseCompleted(object sender, object e)
        {           
            _animInProgress = false;
        }

        private void CoverDownCompleted(object sender, object e)
        {            
            
        }
        
	    private async void onBuyBookTaped(object sender, TappedRoutedEventArgs e)
	    {
            ViewModel.BuyBook.Execute(null);
	    }

	    private void ReadWithBooks_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
	        var listView = (ListView) sender;
	        var book = (Models.Book) listView.SelectedItem;
            if (book != null && book.Id != ViewModel.Entity.Id)
	        {
	            ViewModel.BookSelected.Execute(listView.SelectedItem);
	        }
	    }

	    private async void AddRecenseButton_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
            var caption = "Внимание";
            var required = "Для отправки необходимо заполнить рецензию";
	        var _bookId = ViewModel.Entity.Id;
	        string _personUuid = string.Empty;

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
                  //  await ViewModel.AddPersonRecense(RecenseTextBlock.Text, _personUuid);
                }
                else if (_bookId > 0)
                {
                    await ViewModel.AddBookRecense(RecenseTextBlock.Text, _bookId);
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

	    private void AuthorOnTapped_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
	        if (ViewModel.Entity.Description?.Hidden != null)
	        {
	            var author = ViewModel.Entity.Description.Hidden.TitleInfo?.Author[0];
	            ViewModel.AuthorSelected.Execute(author);
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
                Margin = new Thickness(0,10,0,10),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Content = $"пополнить счет на {price} руб.",
                BorderThickness = new Thickness(2),
                BorderBrush = new SolidColorBrush(Colors.Gray),
                Background = new SolidColorBrush(Colors.Transparent)
            };
	        creditButton.Tapped += (sender, args) => { ViewModel.RunCreditCardPaymentProcess.Execute(null); dialog.Hide(); };
            panel.Children.Add(creditButton);

            var storeButton = new Button
            {
                Margin = new Thickness(0, 10, 0, 5),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Content = $"оплатить через Windows Store",
                BorderThickness = new Thickness(2),
                BorderBrush = new SolidColorBrush(Colors.Gray),
                Background = new SolidColorBrush(Colors.Transparent)
            };
            storeButton.Tapped += (sender, args) => { ViewModel.BuyBookFromMicrosoft.Execute(null); dialog.Hide(); };
            panel.Children.Add(storeButton);

            panel.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 10),
                Text = "Внимание! К цене будет добавлена комисия Windows Store.",

                TextWrapping = TextWrapping.Wrap,
            });
            dialog.Content = panel;
            await dialog.ShowAsync();
	    }

	    private async void FullImagePanel_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
          //  if (!SystemInfoHelper.IsDesktop())
            {
                FullImagePanel.Visibility = Visibility.Collapsed;
                ControlPanel.Instance.TopBarVisibility = Visibility.Visible;
                StatusBar statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                await statusBar.ShowAsync();
            }
        }
	}

	public class BookFitting : EntityPage<Models.Book, BookViewModel>
	{}
}