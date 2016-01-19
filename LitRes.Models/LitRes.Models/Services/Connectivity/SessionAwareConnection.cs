using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LitRes.Exceptions;
using LitRes.Models;

namespace LitRes.Services.Connectivity
{
	public class SessionAwareConnection : ISessionAwareConnection
	{
		private const string SkipAuthRetryMethod = "catalit_authorise";

		private readonly ISessionlessConnection _connection;
		private readonly ISessionEstablisherService _sessionEstablisherService;

		public SessionAwareConnection( ISessionlessConnection connection, ISessionEstablisherService sessionEstablisherService )
		{
			_connection = connection;
			_sessionEstablisherService = sessionEstablisherService;
		}

        public async Task<T> ProcessRequest<T>(string method, bool secureConnection, bool sessionRequired, CancellationToken cancellationToken, IDictionary<string, object> parameters = null, ConnectivityRequestType requestType = ConnectivityRequestType.POST, string url = "wp8-ebook.litres.ru", bool additionalParams = true)
		{
			Dictionary<string, object> parametersWithSession = null;

			T response = default( T );
			int retries = 2;
			bool forceEstablishSession = false;

			while( retries-- > 0 )
			{
				if ( sessionRequired || forceEstablishSession )
				{
					var sessionId = await _sessionEstablisherService.EstablishSession( forceEstablishSession, cancellationToken );

				    parametersWithSession = parameters != null ? new Dictionary<string, object>( parameters ) : new Dictionary<string, object>();

				    parametersWithSession.Add( "sid", sessionId );

				    Debug.WriteLine(String.Empty);
                    Debug.WriteLine("SID = {0} ", sessionId, null);
				}

				try
				{
					response = await _connection.ProcessRequest<T>( method, secureConnection, cancellationToken, parametersWithSession ?? parameters,  requestType, url, additionalParams);
				}
				catch( CatalitAuthorizationException )
				{
					if (method == SkipAuthRetryMethod)
					{
						throw;
					}
					forceEstablishSession = true;
				    parametersWithSession = null;
					continue;
				}

				if( response is UserInformation )
				{
					_sessionEstablisherService.UpdateSid( ( response as UserInformation ).SessionId );
				}

				if( response is UniteInformation )
				{
					_sessionEstablisherService.UpdateSid( (response as UniteInformation).SessionId );
				}

				return response;
			}

			throw new CatalitAuthorizationException( "Authorization failed.", 100 );
		}

		public async Task<T> ProcessStaticRequest<T>( string method, CancellationToken cancellationToken )
		{
			return await _connection.ProcessStaticRequest<T>( method, cancellationToken );
		}

	    public async Task<T> ProcessStaticSecureRequest<T>(string method, CancellationToken cancellationToken)
	    {
            return await _connection.ProcessStaticSecureRequest<T>(method, cancellationToken);

	    }

        public async Task<T> ProcessCustomRequest<T>(string url, string method, CancellationToken cancellationToken, IDictionary<string, object> parameters = null, bool sessionRequired = false)           
        {
            if (sessionRequired)
            {
                var sessionId = await _sessionEstablisherService.EstablishSession(false, cancellationToken);
                if(parameters != null) parameters["sid"] = sessionId;
            }
            return await _connection.ProcessCustomRequest<T>(url, method, cancellationToken, parameters);
        }

        public async Task<T> ProcessJsonRequest<T>(string url, string method, CancellationToken cancellationToken, IDictionary<string, object> parameters = null, bool sessionRequired = false)
        {
            if (sessionRequired)
            {
                var sessionId = await _sessionEstablisherService.EstablishSession(false, cancellationToken);
                if (parameters != null) parameters["sid"] = sessionId;
            }
            return await _connection.ProcessJsonRequest<T>(url, method, cancellationToken, parameters);
        }
    }
}
