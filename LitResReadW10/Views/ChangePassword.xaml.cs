using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Digillect.Mvvm.UI;

using LitRes.Models;
using LitRes.ViewModels;
using LitResReadW10;


namespace LitRes.Views
{
	[View( "ChangePassword" )]
	public partial class ChangePassword : ChangePasswordFitting
	{
		public static readonly DependencyProperty TranslateYProperty = DependencyProperty.Register( "TranslateY", typeof( double ), typeof( ChangePassword ), new PropertyMetadata( 0d, OnRenderXPropertyChanged ) );

		#region Constructors/Disposer
		public ChangePassword()
		{
			InitializeComponent();

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
			( ( ChangePassword ) d ).UpdateTopMargin( ( double ) e.NewValue );
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

		private void TextBoxLostFocus( object sender, RoutedEventArgs e )
		{
			MoveDown.Begin();
		}

		private void TextBoxGotFocus( object sender, RoutedEventArgs e )
		{
			MoveUp.Begin();
		}

		private void UpdateTopMargin( double translateY )
		{
			LayoutRoot.Margin = new Thickness( 0, -translateY, 0, 0 );
		}

		#region View Events
		private async void appbarSave_Click( object sender, EventArgs e )
		{
			Focus(FocusState.Programmatic);

			#region Validate
			string caption = "Внимание";
			string required = "Все поля обязательны для заполнения";
			string minimum = "Пароль должен быть длиной не менее 3х символов";

			if( tbOldPassword.Text == string.Empty )
			{
				//MessageBox.Show( required, caption, MessageBoxButton.OK );
				tbOldPassword.Focus(FocusState.Keyboard);
				return;
			}

			if( tbNewPassword.Text == string.Empty )
			{
				//MessageBox.Show( required, caption, MessageBoxButton.OK );
				tbNewPassword.Focus(FocusState.Keyboard);
				return;
			}
			if( tbNewPassword.Text.Length < 3 )
			{
				//MessageBox.Show( minimum, caption, MessageBoxButton.OK );
				tbNewPassword.Focus(FocusState.Keyboard);
				return;
			}

			if( tbOldPassword.Text.Length < App.PasswordLength )
			{
				//MessageBox.Show( "Неверный старый пароль", caption, MessageBoxButton.OK );
				tbOldPassword.Focus(FocusState.Keyboard);
				return;
			}

			if( tbOldPassword.Text.Length < App.PasswordLength )
			{
				//MessageBox.Show( "Пароль не менее 3 символов", caption, MessageBoxButton.OK );
				tbOldPassword.Focus(FocusState.Keyboard);
				return;
			}
			#endregion

			try
			{
				if( tbOldPassword.Text != ViewModel.Credentials.Password )
				{
					//MessageBox.Show( "Пароль не совпадает с существующим", caption, MessageBoxButton.OK );
				}
				else
				{
					UserInformation userInfo;
					if( ViewModel.UserInformation is UniteInformation )
					{
						userInfo = new UniteInformation();
					}
					else
					{
						userInfo = new UserInformation();
					}

					userInfo.Update( ViewModel.UserInformation );
					userInfo.NewPassword = tbNewPassword.Text;

					await ViewModel.ChangePassword( userInfo );

					//MessageBox.Show( "Данные успешно изменены" );
				}
			}
			catch( Exception )
			{
			}
		}
		#endregion
	}

	public class ChangePasswordFitting : ViewModelPage<ChangePasswordViewModel>
	{
	}
}