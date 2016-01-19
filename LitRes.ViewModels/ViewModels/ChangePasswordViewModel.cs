using System.Threading.Tasks;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;

using LitRes.Models;
using LitRes.Services;
using LitRes.Services.Connectivity;

namespace LitRes.ViewModels
{
	public class ChangePasswordViewModel : ViewModel
	{
		public const string LoadUserInfoPart = "LoadProfile";
		public const string LoadCredentialsPart = "LoadCredentials";
		public const string ChangePasswordPart = "ChangePassword";
		public const string ChangePasswordParameter = "ChangePassword";

		private readonly IProfileProvider _profileProvider;
		private readonly ICredentialsProvider _credentialsProvider;
		private readonly INavigationService _navigationService;

		private UserInformation _userInformation;
		private CatalitCredentials _credentials;

		#region Public Properties
		public UserInformation UserInformation
		{
			get { return _userInformation; }
			set { SetProperty( ref _userInformation, value, "UserInformation" ); }
		}

		public CatalitCredentials Credentials
		{
			get { return _credentials; }
			set { SetProperty( ref _credentials, value, "Credentials" ); }
		}
		#endregion

		#region Constructors/Disposer
		public ChangePasswordViewModel( IProfileProvider profileProvider, ICredentialsProvider credentialsProvider, INavigationService navigationService )
		{
			_navigationService = navigationService;
			_profileProvider = profileProvider;
			_credentialsProvider = credentialsProvider;

            RegisterAction(ChangePasswordPart).AddPart((session) => ChangePasswordProceed(session), (session) => session.Parameters.GetValue<UserInformation>(ChangePasswordParameter, null) != null);
            RegisterAction(LoadCredentialsPart).AddPart((session) => LoadCredentialsProceed(session), (session) => session.Parameters.GetValue<UserInformation>(ChangePasswordParameter, null) == null);
            RegisterAction(LoadUserInfoPart).AddPart((session) => LoadUserInfoProceed(session), (session) => session.Parameters.GetValue<UserInformation>(ChangePasswordParameter, null) == null);
		}
		#endregion

		#region ChangePassword
		public async Task ChangePassword( UserInformation information )
		{
			Session session = new Session( ChangePasswordPart );
			session.AddParameter( ChangePasswordParameter, information );
			await Load( session );
		}
		#endregion

		#region ChangePasswordProceed
		private async Task ChangePasswordProceed( Session session )
		{
			UserInformation uinfo = session.Parameters.GetValue<UserInformation>( ChangePasswordParameter );
			await _profileProvider.ChangeUserInfo( uinfo, session.Token );
			var creds =  _credentialsProvider.ProvideCredentials( session.Token );
			creds.Password = uinfo.NewPassword;
			_credentialsProvider.RegisterCredentials( creds, session.Token );
			Credentials = creds;
		}
		#endregion

		#region LoadUserInfoProceed
		private async Task LoadUserInfoProceed( Session session )
		{
			var userInfo = await _profileProvider.GetUserInfo( session.Token );
			UserInformation = userInfo;
		}
		#endregion

		#region LoadCredentialsProceed
		private async Task LoadCredentialsProceed( Session session )
		{
			var credentials =  _credentialsProvider.ProvideCredentials( session.Token );
			Credentials = credentials;
		}
		#endregion
	}
}
