using System;
using System.Text.RegularExpressions;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Autofac;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using Digillect.Mvvm.UI;

using LitRes.Exceptions;
using LitRes.Models;
using LitRes.Services;
using LitRes.ViewModels;
using LitResReadW10;
using LitResReadW10.Controls;
using LitResReadW10.Helpers;

namespace LitRes.Views
{
	[View( "Registration" )]
	[ViewParameter( "uri", typeof( string ), Required = false)]
    [ViewParameter("toCreateShopAccount", typeof(bool), Required = false)]
	public partial class Registration : RegistrationFitting
	{
		public static readonly DependencyProperty TranslateYProperty = DependencyProperty.Register( "TranslateY", typeof( double ), typeof( Registration ), new PropertyMetadata( 0d, OnRenderXPropertyChanged ) );

		private string _originalUri;
	    private bool _BackOnce;
        private INavigationService _navigationService = ((App)App.Current).Scope.Resolve<INavigationService>();

        #region Constructors/Disposer
        public Registration()
		{
			InitializeComponent();
            //NavigationCacheMode = NavigationCacheMode.Enabled;
            Loaded += PageLoaded;
		}
		#endregion

		#region Public Properties
		public double TranslateY
		{
			get { return ( double ) GetValue( TranslateYProperty ); }
			set { SetValue( TranslateYProperty, value ); }
		}
		#endregion

		private static void OnRenderXPropertyChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			( ( Registration ) d ).UpdateTopMargin( ( double ) e.NewValue );
		}

		private void PageLoaded( object sender, RoutedEventArgs e )
		{
			BindToKeyboardFocus();
		}

		private void BindToKeyboardFocus()
		{
			//var frame = Application.Current.RootVisual as PhoneApplicationFrame;
			//if( frame != null )
			//{
			//	var group = frame.RenderTransform as TransformGroup;
			//	if( group != null )
			//	{
			//		var translate = group.Children[0] as TranslateTransform;
			//		var translateYBinding = new Binding( "Y" );
			//		translateYBinding.Source = translate;
			//		SetBinding( TranslateYProperty, translateYBinding );
			//	}
			//}
		}

		private void UpdateTopMargin( double translateY )
		{
			LayoutRoot.Margin = new Thickness( 0, -translateY, 0, 0 );
		}

		private void TextBoxLostFocus( object sender, RoutedEventArgs e )
		{
			//MoveDown.Begin();
		}

		private void TextBoxGotFocus( object sender, RoutedEventArgs e )
		{
			//MoveUp.Begin();
		}

		#region CreateDataSession
		protected override Session CreateDataSession( DataLoadReason reason )
		{
			_originalUri = ViewParameters.GetValue<string>( "uri" ) ?? string.Empty;

            ViewModel.toCreateShopAccount = ViewParameters.GetValue<bool>("toCreateShopAccount");
           // if (ViewModel.toCreateShopAccount) toCreateShopAccountText.Visibility = Visibility.Visible;

			return base.CreateDataSession( reason );
		}
		#endregion

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!SystemInfoHelper.IsDesktop())
            {
                ControlPanel.Instance.TopBarTitle = "Профиль";
                ProfileTitle.Visibility = Visibility.Collapsed;
            }
            base.OnNavigatedTo(e);
            //if (NavigationContext.QueryString.ContainsKey("returnBackToBook"))
            //{
            //    _BackOnce = true;
            //}
        }

        private void TbLogin_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                tbPassword.Focus(FocusState.Keyboard);
            }
        }

        private void TbPassword_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                tbPhone.Focus(FocusState.Keyboard);
            }
        }

        private void TbPhone_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                SignUpButton.Focus(FocusState.Programmatic);
            }
        }

        #region View Events
        private async void SignUp( object sender, RoutedEventArgs eventArgs )
		{
			Focus(FocusState.Programmatic);

			#region Validate
			string caption = "Внимание";

			if( tbLogin.Text == string.Empty )
			{
				await new MessageDialog( "Поле Логин (email) обязательно для заполнения", caption).ShowAsync();
				tbLogin.Focus(FocusState.Keyboard);
				return;
			}

			if( tbPassword.Password == string.Empty )
			{
			    await new MessageDialog("Поле Пароль обязательно для заполнения", caption).ShowAsync();
				tbPassword.Focus(FocusState.Keyboard);
				return;
			}

			if( !Regex.IsMatch( tbLogin.Text, App.EmailRegexPattern, RegexOptions.IgnoreCase ) )
			{
                await new MessageDialog( "Неверный формат Email", caption).ShowAsync();
				tbLogin.Focus(FocusState.Keyboard);
				return;
			}

			if( tbPassword.Password.Length < App.PasswordLength )
			{
			    await new MessageDialog("Пароль не менее 3 символов", caption).ShowAsync();
				tbPassword.Focus(FocusState.Keyboard);
				return;
			}

			if( tbPhone.Text.Length > 0 && !Regex.IsMatch( tbPhone.Text, App.PhoneRegexPattern, RegexOptions.IgnoreCase ) )
			{
			    await new MessageDialog("Номер телефона должен быть в формате +7xxxxxxxxxx", caption).ShowAsync();
				tbPhone.Focus(FocusState.Keyboard);
				return;
			}
			#endregion

		    try
		    {
		        await ViewModel.Register(new CatalitCredentials
		        {
		            Login = tbLogin.Text,
		            Password = tbPassword.Password,
		            Phone = tbPhone.Text,
		            IsRealAccount = true
		        });
		    }
		    catch (CatalitRegistrationException)
		    {
		        return;
		    }
		    catch (Exception ex)
		    {
                await new MessageDialog("Ошибка сервера").ShowAsync();
		        return;
		    }

			if( ViewModel.Credential != null )
			{
                //if( !string.IsNullOrEmpty( _originalUri ) )
                //{
                //	try
                //	{
                //	    if (!_BackOnce)
                //	    {
                //	        NavigationService.Navigate(new Uri(_originalUri, UriKind.Relative));
                //	        NavigationService.RemoveBackEntry();
                //	        NavigationService.RemoveBackEntry();
                //	    }
                //	}
                //	catch( InvalidOperationException )
                //	{
                //	}
                //}
                //else
                //{
                //    if (!_BackOnce)
                //    {
                //        if ((from object Item in NavigationService.BackStack select Item).Count() > 1)
                //            NavigationService.RemoveBackEntry();				        
                //        NavigationService.GoBack();
                //    }
                //}
                //            if (_BackOnce) NavigationService.GoBack();

                var deviceInfo = ((App)App.Current).Scope.Resolve<IDeviceInfoService>();
                var isDesktop = !string.IsNullOrEmpty(deviceInfo.DeviceFamily) && deviceInfo.DeviceFamily.Equals("Windows.Desktop");

                _navigationService.ClearBackstack(isDesktop);
                _navigationService.Navigate("UserInfo", isDesktop);
            }
		}
		#endregion
	}

	public class RegistrationFitting : ViewModelPage<RegistrationViewModel>
	{
	}
}