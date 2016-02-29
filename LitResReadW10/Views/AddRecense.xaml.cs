using System;
using Windows.UI.Popups;
using Digillect.Mvvm;
using Digillect.Mvvm.UI;
using LitRes.Exceptions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Autofac;
using Digillect.Mvvm.Services;
using LitRes.ViewModels;
using LitResReadW10;
using LitResReadW10.Controls;
using LitResReadW10.Helpers;

namespace LitRes.Views
{
	[View( "AddRecense" )]
	[ViewParameter( "bookId", Required = false)]
	[ViewParameter( "personId", Required = false)]
	public partial class AddRecense : AddRecenseFitting
	{
		private int _bookId;
		private string _personUuid;
        private INavigationService _navigationService = ((App)App.Current).Scope.Resolve<INavigationService>();

        #region Constructors/Disposer
        public AddRecense()
		{
			InitializeComponent();

            Loaded += AddRecense_Loaded;
		}

        void AddRecense_Loaded(object sender, RoutedEventArgs e)
        {
            Analytics.Instance.sendMessage(Analytics.ActionWriteReview);

            if (_bookId != 0)
            {
                ViewModel.RecensePlaceHolderText = "Эта книга...";
            }
            else if (!string.IsNullOrEmpty(_personUuid))
            {
                ViewModel.RecensePlaceHolderText = "Этот автор...";
            }
        }
		#endregion

		#region CreateDataSession
		protected override Session CreateDataSession( DataLoadReason reason )
		{
		    if (ViewParameters.Contains("bookId"))
		    {
		        _bookId = Convert.ToInt32( ViewParameters.GetValue<string>( "bookId" ) );
		    }
			else if (ViewParameters.Contains("personId"))
			{
			    _personUuid = ViewParameters.GetValue<string>( "personId" );
            }

			return base.CreateDataSession( reason );
		}
		#endregion

	    protected override void OnNavigatedTo(NavigationEventArgs e)
	    {
	        base.OnNavigatedTo(e);
	        ControlPanel.Instance.TopBarTitle = "Новый отзыв";
	    }

	    private async void AddRecenseButton_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
            var caption = "Внимание";
            var required = "Для отправки необходимо заполнить рецензию";

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
            _navigationService.GoBack();
        }

	    private void RecenseTextBlock_GotFocus(object sender, RoutedEventArgs e)
	    {
	        if (!SystemInfoHelper.IsDesktop())
	        {
	            TypeWriterImage.Visibility = Visibility.Collapsed;
	            MainStackPanel.VerticalAlignment = VerticalAlignment.Stretch;
	            TextBlock.Margin = new Thickness(0, 5, 0, 15);
	        }
	    }

        private void RecenseTextBlock_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!SystemInfoHelper.IsDesktop())
            {
                TypeWriterImage.Visibility = Visibility.Visible;
                MainStackPanel.VerticalAlignment = VerticalAlignment.Center;
                TextBlock.Margin = new Thickness(0, 20, 0, 15);
            }
        }
    }

    public class AddRecenseFitting : ViewModelPage<AddRecenseViewModel>
	{
	}
}