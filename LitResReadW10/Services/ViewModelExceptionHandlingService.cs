using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using Digillect.Mvvm.UI;

using LitRes.Exceptions;
using LitResReadW10;

namespace LitRes.Services
{
	public class ViewModelExceptionHandlingService : IViewModelExceptionHandlingService
	{
		public bool HandleException( ViewModel viewModel, Session session, Exception ex )
		{
			string message = ex.Message;
			if (ex is CatalitParseException)
			{
				//message = "Проблемы с серверным соединением.\nПопробуйте позже.";
				message = string.Empty;
			}
			else if (ex is CatalitRegistrationException)
			{
				int error = (( CatalitRegistrationException ) ex).ErrorCode;
				switch (error)
				{
					case 1:
						message = "Запрошенный логин уже занят";
						break;
					case 2:
						message = "Логин не указан";
						break;
					case 3:
						message = "Пароль не указан";
						break;
					case 4:
						message = "E-mail некорректный";
						break;
					case 5:
						message = "Слишком много регистраций с этого IP, попробуйте немного позднее";
						break;
					case 6:
						message = "E-mail уже принадлежит другому пользователю";
						break;
					case 7:
						message = "Повтор пароля не совпадает с паролем";
						break;
					case 8:
						message = "Некорректный номер телефона";
						break;
					case 9:
						message = "Номер телефона уже используется";
						break;
					default:
						message = "Неизвестная ошибка регистрации";
						break;
				}
			}
			else if (ex is CatalitAuthorizationException)
			{
				message = "Неправильно введены логин и пароль.";
			}
			else if (ex is CatalitException)
			{
				
			}
			else if (ex is WebException)
			{
				message = "Проблема с интернет-соединением.\nПопробуйте позже.";
			}
			else
			{
				if( !Debugger.IsAttached )
				{
					//BugSense.BugSenseHandler.Instance.LogException( ex );
				}
			}

			if( !( ex is TaskCanceledException ) && !string.IsNullOrEmpty( message ) )
			{
				bool showMessage = true;
				
				var currentPage = ( ( Page ) ( ( ( WindowsRTApplication ) App.Current ).RootFrame.Content ) );

				var prop = currentPage.GetType().GetProperty( "ViewModel" );

				if( prop != null )
				{
					var viewmodel = prop.GetValue( currentPage );

					if( viewmodel.GetType() != viewModel.GetType() )
					{
						showMessage = false;
					}
				}

				if( showMessage )
				{
				    new MessageDialog(message).ShowAsync();
				    //   new Task( async() => await new MessageDialog(message).ShowAsync() ).Start();
				    //(new Task( () => Deployment.Current.Dispatcher.BeginInvoke( () => MessageBox.Show( message ) ) )).Start();
				}
			}

			return false;
		}
	}
}
