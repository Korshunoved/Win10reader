using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using LitRes.Exceptions;
using LitRes.Models;
using LitRes.Services.Connectivity;

namespace LitRes.Services
{
	internal class ProfileProvider : IProfileProvider
	{
		private readonly ICatalitClient _client;
		private readonly IDeviceInfoService _deviceInfoService;

		public UserInformation UserInformation { get; private set; }

		#region Constructors/Disposer
        public ProfileProvider(ICatalitClient client, IDeviceInfoService deviceInfoService)
		{
			_client = client;
			_deviceInfoService = deviceInfoService;
		}
		#endregion

		public async Task<UserInformation> Authorize( CatalitCredentials credentials, CancellationToken cancellationToken )
		{
			if( credentials != null )
			{
			    var encodedPass = WebUtility.UrlEncode(credentials.Password);
				var parameters = new Dictionary<string, object>
						{								
							{"login", credentials.Login},									
							{"pwd", credentials.Password}										
						};
                if(!string.IsNullOrEmpty(credentials.Sid)) parameters.Add("sid", credentials.Sid);
				var result = await _client.Authorize( parameters, cancellationToken );
				if( result != null )
				{
					UserInformation = result;
                    
					//Activate Coupone if nokia
					if( _deviceInfoService.IsNokiaDevice )
					{
						try
						{
                            await ActivateNokiaCoupone(result.SessionId, CancellationToken.None);
						}
						catch( CatalitActivateCouponeException )
						{
						}
					}

					return result;
				}
			}

			throw new CatalitNoCredentialException();
		}

		public async Task<UserInformation> GetUserInfo( CancellationToken cancellationToken, bool deffaultFromTheWeb = false )
		{
            if (deffaultFromTheWeb) UserInformation = null;
			var result = UserInformation ?? await _client.GetUserInfo( cancellationToken );

			if( result != null )
			{
				UserInformation = result;
				return result;
			}

			throw new CatalitNoCredentialException();
		}

        public async Task<UserInformation> Register(CatalitCredentials credentials, CancellationToken cancellationToken, CatalitCredentials oldCredentials = null, bool additionalParams = true)
		{
			if( credentials != null )
			{
				var parameters = new Dictionary<string, object>
						{								
							{"new_login", credentials.Login},								
							{"new_pwd1", credentials.Password},								
							{"phone", credentials.Phone}							
						};

				if( credentials.IsRealAccount )
				{
					parameters.Add( "mail", credentials.Login );
				}

				//result contains only sid
				var result = await _client.Register( parameters, cancellationToken, additionalParams );
				if( result != null )
				{
					UserInformation = result;

                    //Activate Coupone if nokia
                    if (_deviceInfoService.IsNokiaDevice)
                    {
                        try
                        {
                            await ActivateNokiaCoupone(result.SessionId, CancellationToken.None);
                        }
                        catch (CatalitActivateCouponeException)
                        {
                        }
                    }
                    //if (oldCredentials != null)
                    //{
                    //    if (!oldCredentials.IsRealAccount)
                    //    {
                    //        try
                    //        {
                    //            await ActivateRegistrationCoupone(result.SessionId, CancellationToken.None);
                    //        }
                    //        catch (CatalitActivateCouponeException)
                    //        {
                    //        }
                    //    }
                    //}
					return result;
				}
			}

            throw new CatalitNoCredentialException();
		}

		public async Task<CatalitCredentials> RegisterDefault( CancellationToken cancellationToken )
		{
		    string login = "wp10_879879789" + _deviceInfoService.DeviceId;//.Substring( 0, 16 );

			var creds = new CatalitCredentials {
				IsRealAccount = false,
				Password = "wp8litres",
				Login = login
			};

			bool relogin = false;
		    try
		    {
		       var userInfo = await Register(creds, cancellationToken, additionalParams: false);
               creds.Sid = userInfo.SessionId;
		    }
		    catch (CatalitRegistrationException e)
		    {
		        //already exists
		        if (e.ErrorCode == 1 || e.ErrorCode == 100)
		        {
		            relogin = true;
		        }
		        else
		        {
		            throw;
		        }
		    }
		    catch (Exception ex)
		    {
		        ex = ex;
                throw;
		    }

			if( relogin )
			{
			    try
			    {
			        var userInfo = await Authorize(creds, cancellationToken);
			        creds.Sid = userInfo.SessionId;
			    }
			    catch (Exception ex)
			    {
			        var stackTace = ex.StackTrace;
			    }
			}

			UserInformation = null;

			return creds;
		}

		public async Task ChangeUserInfo( UserInformation userInfo, CancellationToken cancellationToken )
		{
			if( userInfo != null )
			{
				var parameters = new Dictionary<string, object>
						{
							{"user-id", userInfo.UserId},
							{"first-name", userInfo.FirstName},
							{"last-name", userInfo.LastName},
							{"middle-name", userInfo.MiddleName},
							{"mail", userInfo.Email},
							{"www", userInfo.Web},
							{"phone", userInfo.Phone},
							{"city", userInfo.City},
                            {"curpass", userInfo.OldPassword},
						};
                
				if( !string.IsNullOrEmpty( userInfo.NewPassword ) )
				{
					parameters.Add( "new_pwd1", userInfo.NewPassword );
				}

			    await _client.ChangeUserInfo( parameters, cancellationToken );
				UserInformation = userInfo;
			}
			else
			{
				throw new CatalitNoCredentialException();
			}
		}

		public async Task<UserInformation> MergeAccounts( string userAccountSid, CatalitCredentials mergedAccount, CancellationToken cancellationToken )
		{
			if( mergedAccount != null )
			{
				var parameters = new Dictionary<string, object>
						{
							{"sid", userAccountSid},
							{"user_login", mergedAccount.Login},
							{"user_passwd", mergedAccount.Password},
						};
				var userInfo = await _client.MergeAccounts( parameters, cancellationToken );
				UserInformation = userInfo;
				return userInfo;
			}

            throw new CatalitNoCredentialException();
		}

		private async Task ActivateNokiaCoupone( string sid, CancellationToken cancellationToken )
		{
			var parameters = new Dictionary<string, object>
						{
#if DEBUG
							{ "code", "2058005753530018" },
#else                   
							{ "code", "2058005753530018" },
#endif
							{ "sid", sid }
						};
			await _client.ActivateCoupone( parameters, CancellationToken.None );
		}

        private async Task ActivateRegistrationCoupone(string sid, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<string, object>
						{
							{ "code", "0576982139344614" },
							{ "sid", sid }
						};
            await _client.ActivateCoupone(parameters, cancellationToken);
        }
	}
}
