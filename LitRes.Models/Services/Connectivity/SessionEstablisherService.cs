using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LitRes.Exceptions;
using LitRes.Models;

namespace LitRes.Services.Connectivity
{
	public class SessionEstablisherService : ISessionEstablisherService
	{
		private static readonly object _lock = new object();

		private readonly ISessionlessConnection _connection;
		private readonly ICredentialsProvider _credentialsProvider;
		private string _sessionId;
		private TaskCompletionSource<string> _taskCompletionSource;

		#region Constructors/Disposer
		public SessionEstablisherService( ISessionlessConnection connection, ICredentialsProvider credentialsProvider )
		{
			_connection = connection;
			_credentialsProvider = credentialsProvider;

		}
		#endregion

		public Task<string> EstablishSession( bool forceNewSession, CancellationToken cancellationToken )
		{		    
			if( _sessionId != null && !forceNewSession )
			{
				return Task.FromResult( _sessionId );
			}

			lock( _lock )
			{
				if( _sessionId != null && !forceNewSession )
				{
					return Task.FromResult( _sessionId );
				}

				if( _taskCompletionSource != null )
				{
					return _taskCompletionSource.Task;
				}

				_sessionId = null;
				_taskCompletionSource = new TaskCompletionSource<string>();
			}

			var ltcs = _taskCompletionSource;
			Task.Factory.StartNew( async () =>
			{
				string result = null;
				Exception exception = null;

				try
				{
					if( !string.IsNullOrEmpty( _sessionId ) )
					{
						result = _sessionId;
					}
					else
					{
                        if (forceNewSession) await ClearSid();
						result = await SessionEstablisher( cancellationToken );
						_sessionId = result;
					}
				}
				catch( Exception ex )
				{
					exception = ex;
				}

				var tcs = _taskCompletionSource;

				lock( _lock )
				{
					_taskCompletionSource = null;
				}

				if( exception != null )
				{
					tcs.SetException( exception );
				}
				else
				{
					tcs.SetResult( result );
				}
			} , cancellationToken);

			return ltcs.Task;
		}

		public void UpdateSid( string sid )
		{
			_sessionId = sid;
		}

        public async Task ClearSid()
	    {
            var credentials = await _credentialsProvider.ProvideCredentials(CancellationToken.None);
            if (credentials != null)
            {
                credentials.Sid = null;
                _credentialsProvider.RegisterCredentials(credentials, CancellationToken.None);
            }
            _sessionId = null;
	    }

		private async Task<string> SessionEstablisher( CancellationToken cancellationToken )
		{
			string sessionId = null;

			var credentials = await _credentialsProvider.ProvideCredentials( cancellationToken );

			if (credentials != null)
			{
			    if (!string.IsNullOrEmpty(credentials.Sid)) sessionId = credentials.Sid;

				while (sessionId == null)
				{
					var parameters = new Dictionary<string, object>();                    
					parameters.Add("login", credentials.Login);
					parameters.Add("pwd", credentials.Password);

					UserInformation userInformation = null;

					try
					{
						userInformation = await _connection.ProcessRequest<UserInformation>("catalit_authorise", false, cancellationToken, parameters);
					}
					catch (CatalitAuthorizationException e)
					{
						_credentialsProvider.RegisterCredentials( null, cancellationToken );
					}

					if (userInformation == null)
					{
						var shouldRetry = await _credentialsProvider.ShouldRetryAuthentication(credentials, cancellationToken);

						if (!shouldRetry)
						{
							throw new CatalitAuthorizationException("Authentication failed.", 100);
						}
					}
					else
					{
						sessionId = userInformation.SessionId;
					    credentials.Sid = sessionId;
                        _credentialsProvider.RegisterCredentials(credentials, cancellationToken);
					}
				}
			}
			else
			{
				throw new CatalitNoCredentialException();
			}
			
			return sessionId;
		}
	}
}
