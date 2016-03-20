using System;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Autofac;
using Digillect;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using Digillect.Mvvm.UI;

using LitRes.Models;
using LitRes.Services;
using LitRes.ViewModels;
using LitResReadW10;
using LitResReadW10.Controls;
using LitResReadW10.Helpers;

namespace LitRes.Views
{
	[View( "Authorization" )]
	//[ViewParameter( "uri", typeof( string ) , Required = false)]
	public partial class Authorization : AuthorizationFitting
	{
		private string _originalUri;
        private INavigationService _navigationService = ((App)App.Current).Scope.Resolve<INavigationService>();

        #region Constructors/Disposer
        public Authorization()
		{
			InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled;
            Loaded += PageLoaded;
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
        }


        private void PageLoaded( object sender, RoutedEventArgs e )
        {
            ViewModel.LoadCredential(new Session());
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
		    ControlPanel.Instance.TopBarVisibility = Visibility.Visible;
		}

		private void TextBoxGotFocus( object sender, RoutedEventArgs e )
		{
            //	MoveUp.Begin();
            ControlPanel.Instance.TopBarVisibility = Visibility.Collapsed;
        }

		private async void Registration_Click(object sender, RoutedEventArgs eventArgs)
        {
            if (!SystemInfoHelper.HasInternet())
            {
                string caption = "Внимание";
                string required = "Проверьте интернет-соединение";
                await new MessageDialog(required, caption).ShowAsync();                
                return;
            }
            Focus(FocusState.Programmatic);
            _navigationService.Navigate( "Registration", SystemInfoHelper.IsDesktop());
		}

		private void TbLogin_OnKeyUp( object sender, KeyRoutedEventArgs e )
		{
			if( e.Key == VirtualKey.Enter )
			{
			    tbPassword.Focus(FocusState.Keyboard);
			}
		}

		private void TbPassword_OnKeyUp( object sender, KeyRoutedEventArgs e )
		{
			if( e.Key == VirtualKey.Enter)
			{
                SignIn( sender, new RoutedEventArgs());
			}
		}

		#region View Events
		private async void SignIn( object sender, RoutedEventArgs eventArgs )
		{		 
                    
			Focus(FocusState.Keyboard);

			string caption = "Внимание";
			string required = "Все поля обязательны для заполнения";

            if (!SystemInfoHelper.HasInternet())
            {
                caption = "Внимание";
                required = "Проверьте интернет-соединение";
                await new MessageDialog(required, caption).ShowAsync();                
                return;
            }

            if ( tbLogin.Text == string.Empty )
			{
				await new MessageDialog( required, caption).ShowAsync();
				tbLogin.Focus(FocusState.Keyboard);
				return;
			}

			if( tbPassword.Password == string.Empty )
			{
                await new MessageDialog( required, caption).ShowAsync();
				tbPassword.Focus(FocusState.Keyboard);
				return;
			}

			if( tbLogin.Text.Length < App.LoginLength )
			{
			    await new MessageDialog("Логин не менее 3 символов", caption).ShowAsync();
				tbLogin.Focus(FocusState.Keyboard);
				return;
			}

			if( tbPassword.Password.Length < App.PasswordLength )
			{
                await new MessageDialog( "Пароль не менее 3 символов", caption).ShowAsync();
				tbPassword.Focus(FocusState.Keyboard);
				return;
			}

			try
			{
				await ViewModel.Login( new CatalitCredentials { Login = tbLogin.Text, Password = tbPassword.Password } );
			}
			catch( Exception e)
			{
                await new MessageDialog("Вы ввели неверный логин или пароль", caption).ShowAsync();
                return;
			}

			if( ViewModel.Credential != null )
			{
				//if( !string.IsNullOrEmpty( _originalUri ) )
				//{
				//	try
				//	{
				//		NavigationService.Navigate( new Uri( _originalUri, UriKind.Relative ) );
				//		NavigationService.RemoveBackEntry();
				//	}
				//	catch( InvalidOperationException )
				//	{
				//	}
				//}
				//else
				//{
				//	NavigationService.GoBack();
				//}

			    var deviceInfo = ((App) App.Current).Scope.Resolve<IDeviceInfoService>();
			    var isDesktop = !string.IsNullOrEmpty(deviceInfo.DeviceFamily) && deviceInfo.DeviceFamily.Equals("Windows.Desktop");

                _navigationService.ClearBackstack(isDesktop);
                _navigationService.Navigate("UserInfo", isDesktop);
            }
		}
		#endregion

		#region CreateDataSession
		protected override Session CreateDataSession( DataLoadReason reason )
		{
			//_originalUri = ViewParameters.GetValue<string>( "uri" ) ?? string.Empty;

			return base.CreateDataSession( reason );
		}
		#endregion
	}

	public class AuthorizationFitting : ViewModelPage<AuthorizationViewModel>
	{
	}
}