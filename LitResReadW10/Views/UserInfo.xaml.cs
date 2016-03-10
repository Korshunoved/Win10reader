using System;
using System.Text.RegularExpressions;
using Digillect.Mvvm;
using Digillect.Mvvm.UI;

using LitRes.Models;
using LitRes.ViewModels;

using System.ComponentModel;
using System.Diagnostics;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Autofac;
using Digillect.Mvvm.Services;
using LitResReadW10;
using LitResReadW10.Controls;
using LitResReadW10.Helpers;

namespace LitRes.Views
{
	[View( "UserInfo" )]
	public partial class UserInfo : UserInfoFitting
	{
		public static readonly DependencyProperty TranslateYProperty = DependencyProperty.Register( "TranslateY", typeof( double ), typeof( UserInfo ), new PropertyMetadata( 0d, OnRenderXPropertyChanged ) );
        public string UserInfoTitle => SystemInfoHelper.IsDesktop() ? "Профиль" : string.Empty;
        private INavigationService _navigationService = ((App)App.Current).Scope.Resolve<INavigationService>();
      
        #region Constructors/Disposer
        public UserInfo()
		{
			InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled;
            Loaded += PageLoaded;
            
		}

        #endregion

        protected override Session CreateDataSession(DataLoadReason reason)
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            return base.CreateDataSession(reason);
        }

		#region Public Properties
		public double TranslateY
		{
			get { return ( double ) GetValue( TranslateYProperty ); }
			set { SetValue( TranslateYProperty, value ); }
		}
		#endregion

		private static void OnRenderXPropertyChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			( ( UserInfo ) d ).UpdateTopMargin( ( double ) e.NewValue );
		}

		private void PageLoaded( object sender, RoutedEventArgs e )
		{
            if (!SystemInfoHelper.IsDesktop()) ControlPanel.Instance.TopBarTitle = "Профиль";
            BindToKeyboardFocus();
		    ViewModel.LoadUserInfoProceed();
		}

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("GetOutOfHere"))
            {

                _navigationService.Navigate("Authorization", SystemInfoHelper.IsDesktop());
                _navigationService.ClearBackstack(SystemInfoHelper.IsDesktop());

            }
            else if (e.PropertyName == "UserInfoLoaded")
            {
                if (ViewModel.UserInformation.AccountType == (int)AccountTypeEnum.AccountTypeLibrary)
                {
                 //   EditableBlock.Visibility = Visibility.Collapsed;
                   // ShopEnableButton.Visibility = Visibility.Visible;
                }
            }
           
        }

		private void BindToKeyboardFocus()
		{
		    Focus(FocusState.Programmatic);
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
		//	MoveDown.Begin();
		}

		private void TextBoxGotFocus( object sender, RoutedEventArgs e )
		{
			//MoveUp.Begin();
		}

	    //private void Hyperlink_OnClick(Hyperlink sender, HyperlinkClickEventArgs args)
	    //{
     //       ViewModel.ShowChangePassword.Execute(SystemInfoHelper.IsDesktop());
     //   }

	    private async void SaveInfo(object sender, RoutedEventArgs e)
	    {
            Focus(FocusState.Programmatic);

            #region Validate
            string caption = "Внимание";
            if (tbLogin.Text == string.Empty)
            {
                await new MessageDialog("Поле Email обязательно для заполнения", caption).ShowAsync();
                tbLogin.Focus(FocusState.Keyboard);
                return;
            }

            if (!Regex.IsMatch(tbLogin.Text, App.EmailRegexPattern, RegexOptions.IgnoreCase))
            {
                await new MessageDialog("Неверный формат Email", caption).ShowAsync();
                tbLogin.Focus(FocusState.Keyboard);
                return;
            }

            if (tbPhone.Text.Length > 0 && !Regex.IsMatch(tbPhone.Text, App.PhoneRegexPattern, RegexOptions.IgnoreCase))
            {
                await new MessageDialog("Номер телефона должен быть в формате +7xxxxxxxxxx", caption).ShowAsync();
                tbPhone.Focus(FocusState.Keyboard);
                return;
            }
            #endregion

            try
            {
                UserInformation userInfo;
                if (ViewModel.UserInformation is UniteInformation)
                {
                    userInfo = new UniteInformation();
                }
                else
                {
                    userInfo = new UserInformation();
                }

                userInfo.Update(ViewModel.UserInformation);

                bool newMail = userInfo.Email != tbLogin.Text;

                userInfo.Email = tbLogin.Text;
                userInfo.Phone = tbPhone.Text;

                await ViewModel.ChangeUserInfo(userInfo);

                if (newMail)
                {
                    await new MessageDialog("Для успешного изменения данных перейдите по ссылке, которая пришла Вам на электронную почту", caption).ShowAsync();
                }
                else
                {
                    await new MessageDialog("Данные успешно изменены", caption).ShowAsync();
                }
            }
            catch (Exception)
            {
            }
        }

	    private async void Logout(object sender, RoutedEventArgs e)
	    {
            await ViewModel.Logout();
        }
	}

	public class UserInfoFitting : ViewModelPage<ChangeUserInfoViewModel>
	{
	}
}