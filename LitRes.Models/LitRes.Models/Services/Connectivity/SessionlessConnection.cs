using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

using LitRes.Exceptions;
using LitRes.Models;

using RestSharp;
using System.Net;
using System.Text;
using LitRes.Models.JsonModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LitRes.Services.Connectivity
{
	public class SessionlessConnection : ISessionlessConnection
	{
		private static readonly IDictionary<string, Type> elementNameToExceptionMap = new Dictionary<string, Type>();

		private readonly IRestClientService _restClientService;
		private readonly IDeviceInfoService _deviceInfoService;

		#region Constructors/Disposer
		static SessionlessConnection()
		{
			elementNameToExceptionMap.Add( "catalit-authorization-failed", typeof( CatalitAuthorizationException ) );
			elementNameToExceptionMap.Add( "catalit-registration-failed", typeof( CatalitRegistrationException ) );
			elementNameToExceptionMap.Add( "catalit-unite-uisers-failed", typeof( CatalitUniteException ) );
			elementNameToExceptionMap.Add( "catalit-updateuser-failed", typeof( CatalitUpdateUserException ) );
			elementNameToExceptionMap.Add( "catalit-purchase-failed", typeof( CatalitPurchaseException ) );
            elementNameToExceptionMap.Add("catalit-paycard-processing-fail", typeof(CatalitPaycardProcessingException));            
			elementNameToExceptionMap.Add( "catalit-add-recenses-fail", typeof( CatalitAddRecenseException ) );
			elementNameToExceptionMap.Add( "catalit-activate-coupone-failed", typeof( CatalitActivateCouponeException ) );
			elementNameToExceptionMap.Add( "catalit-get-collection-book-failed", typeof( CatalitGetCollectionBooksException ) );
			elementNameToExceptionMap.Add( "catalit-inapp-processing-failed", typeof( CatalitInappProcessingFailedException ) );
			elementNameToExceptionMap.Add( "catalit-store-bookmarks-failed", typeof( CatalitBookmarksException ) );
			elementNameToExceptionMap.Add( "catalit-download-failed", typeof( CatalitDownloadException ) );
		}

		public SessionlessConnection( IRestClientService restClientService, IDeviceInfoService deviceInfoService )
		{
			_restClientService = restClientService;
			_deviceInfoService = deviceInfoService;
		}
		#endregion

		#region ISessionlessConnection Members
		public async Task<T> ProcessStaticRequest<T>( string method, CancellationToken cancellationToken )
		{
			return await ProcessInternalRequest<T>( method, false, true, cancellationToken );
		}

	    public async Task<T> ProcessStaticSecureRequest<T>(string method, CancellationToken cancellationToken)
	    {
            return await ProcessInternalRequest<T>(method, true, true, cancellationToken);
	    }

        public async Task<T> ProcessRequest<T>(string method, bool secureConnection, CancellationToken cancellationToken, IDictionary<string, object> parameters = null, ConnectivityRequestType requestType = ConnectivityRequestType.POST, string url = "win10-ebook.litres.ru", bool additionalParams = true)
		{
			return await ProcessInternalRequest<T>( method, secureConnection, false, cancellationToken, parameters, requestType, url, additionalParams);
		}

        public async Task<T> ProcessCustomRequest<T>(string url, string method, CancellationToken cancellationToken, IDictionary<string, object> parameters = null)
        {
            return await ProcessInternalCustomRequest<T>(url, method, cancellationToken, parameters);
        }

	    public async Task<T> ProcessJsonRequest<T>(string url, string method, CancellationToken cancellationToken, IDictionary<string, object> parameters = null)
	    {
            return await ProcessInternalJsonRequest<T>(url, method, cancellationToken, parameters);
        }

	    #endregion
        
        private async Task<T> ProcessInternalCustomRequest<T>(string url, string method, CancellationToken cancellationToken, IDictionary<string, object> parameters = null)
        {
            var urlProcessor = new Uri(url);
            string baseUrl = url.Substring(0, url.Length - urlProcessor.AbsolutePath.Length);

            var request = new RestRequest(urlProcessor.AbsolutePath);

            parameters = parameters ?? new Dictionary<string, object>();

            foreach (var parameter in parameters)
            {
                if (parameter.Value != null && parameter.Value is IList)
                {
                    foreach (var param in ((IList)parameter.Value))
                    {
                        request.AddParameter(parameter.Key, param ?? "");
                    }
                }
                else
                {
                    request.AddParameter(parameter.Key, parameter.Value ?? "");
                }
            }

            if (method.Equals("POST")) request.Method = Method.POST;
            else if (method.Equals("GET")) request.Method = Method.GET;
            else if (method.Equals("PUT")) request.Method = Method.PUT;
            else if (method.Equals("DELETE")) request.Method = Method.DELETE;
            
            var response = await _restClientService.ProcessCustomRequest(request, baseUrl, cancellationToken);
            var responseString = Encoding.UTF8.GetString(response.RawBytes);
            Debug.WriteLine("");
            Debug.WriteLine(responseString);
            Debug.WriteLine("");
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new TaskCanceledException("Request aborted");
            }

            if (responseString.Length == 0)
            {
                throw new Exception(responseString);
            }

            try
            {
                using (var textReader = new StringReader(responseString))
                {
                    var serializer = new XmlSerializer(typeof(T));

                    var ress = (T)serializer.Deserialize(textReader);
                    return ress;
                }
            }
            catch (Exception ex)
            {
                throw new CatalitParseException("Unable to parse XML response.", ex);
            }   
        }

        private async Task<T> ProcessInternalRequest<T>(string method, bool secureConnection, bool staticRequest, CancellationToken cancellationToken, IDictionary<string, object> parameters = null, ConnectivityRequestType requestType = ConnectivityRequestType.POST, string url = "win10-ebook.litres.ru", bool additionalParams = true)
		{
			string req = method;
			if( !staticRequest && requestType == ConnectivityRequestType.POST )
			{
				req += "\\";
			}

			var request = new RestRequest( req );

			parameters = parameters ?? new Dictionary<string, object>();
            if (additionalParams)
            {
                try
                {
                    parameters.Add("app", _deviceInfoService.AppId);
                }
                catch (Exception)
                {
                    // ignored
                }
                parameters.Add("currency", _deviceInfoService.Currency);
                //if (!parameters.ContainsKey("lfrom")) parameters.Add("lfrom", _deviceInfoService.WinMobileRefId);
                //if (!parameters.ContainsKey("pin")) parameters.Add("pin", _deviceInfoService.DeviceModel);
            }
            if(!staticRequest)
			{
				foreach( var parameter in parameters )
				{
					if( parameter.Value is IList )
					{
						foreach( var param in ( ( IList ) parameter.Value ) )
						{
							request.AddParameter( parameter.Key, param ?? "" );
						}
					}
                    else if (parameter.Key.Equals("search_types"))
                    {
                        if (parameter.Value != null)
                        {
                            var values = parameter.Value.ToString().Split(',');
                            foreach (var val in values)
                            {
                                request.AddParameter(parameter.Key, val ?? "");
                            }
                        }
                    }
					else
					{
						request.AddParameter( parameter.Key, parameter.Value ?? "" );
					}
				}
			}

            if (requestType == ConnectivityRequestType.POST)
            {
                request.Method = Method.POST;
            }
			if( staticRequest || requestType == ConnectivityRequestType.GET)
			{
				request.Method = Method.GET;
			}
            else if (requestType == ConnectivityRequestType.PUT)
            {
                request.Method = Method.PUT;
            }
            else if (requestType == ConnectivityRequestType.DELETE)
            {
                request.Method = Method.DELETE;
            }

			IRestResponse response;

			if( !staticRequest )
			{
				response = await _restClientService.ProcessRequest( request, secureConnection, cancellationToken, url );
			}
			else
			{
				response = await _restClientService.ProcessStaticRequest( request, cancellationToken );
			}

            if (response.ResponseStatus == ResponseStatus.Error)
            {
                throw new CatalitNoServerConnectionException("No server connection", new Exception("No server connection"));
            }

            var responseString = Encoding.UTF8.GetString(response.RawBytes);
            if ( typeof( T ) == typeof( RawFile ) )
			{
				try
				{
                    //Check catalit exceptions
					CheckException(responseString);
				}
				catch( CatalitParseException ex )
				{
                    Debug.WriteLine(ex.Message);
				}

				object rawFile = new RawFile { Raw = response.RawBytes, Zipped =  response.ContentType!=null && response.ContentType.ToString() == "application/zip" };
				return ( T ) rawFile;
			}
            if (typeof( T ) == typeof( string ))
            {
                try
                {
                    //Check catalit exceptions
                    CheckException(responseString);
                }
                catch (CatalitParseException ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                object resultString = responseString;
                return (T) resultString;
            }

            CheckException(responseString);

			try
			{
				using( var textReader = new StringReader(responseString) )
				{
					var serializer = new XmlSerializer( typeof( T ) );

					return ( T ) serializer.Deserialize( textReader );
				}
			}
			catch( Exception ex )
			{
				throw new CatalitParseException( "Unable to parse XML response.", ex );
			}
		}

        private async Task<T> ProcessInternalJsonRequest<T>(string url, string method, CancellationToken cancellationToken, IDictionary<string, object> parameters = null)
        {
            var urlProcessor = new Uri(url);
            string baseUrl = url.Substring(0, url.Length - urlProcessor.AbsolutePath.Length);

            var request = new RestRequest(urlProcessor.AbsolutePath);

            parameters = parameters ?? new Dictionary<string, object>();
            parameters.Add("app", _deviceInfoService.AppId);
            foreach (var parameter in parameters)
            {
                if (parameter.Value != null && parameter.Value is IList)
                {
                    foreach (var param in ((IList)parameter.Value))
                    {
                        request.AddParameter(parameter.Key, param ?? "");
                    }
                }
                else
                {
                    request.AddParameter(parameter.Key, parameter.Value ?? "");
                }
            }

            if (method.Equals("POST")) request.Method = Method.POST;
            else if (method.Equals("GET")) request.Method = Method.GET;
            else if (method.Equals("PUT")) request.Method = Method.PUT;
            else if (method.Equals("DELETE")) request.Method = Method.DELETE;

            var response = await _restClientService.ProcessCustomRequest(request, baseUrl, cancellationToken);
            var responseString = Encoding.UTF8.GetString(response.RawBytes);
            Debug.WriteLine("");
            Debug.WriteLine(responseString);
            Debug.WriteLine("");
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new TaskCanceledException("Request aborted");
            }

            if (responseString.Length == 0)
            {
                throw new Exception(responseString);
            }

            try
            {
                object result = JsonConvert.DeserializeObject<T>(responseString);
                return (T)result;
            }
            catch (Exception ex)
            {
                throw new CatalitParseException("Unable to parse JSON response.", ex);
            }
        }

        private void CheckException( string content )
		{
			CatalitException parsedException = null;

			try
			{
				using( var textReader = new StringReader( content ) )
				{
					using( XmlReader xmlReader = XmlReader.Create( textReader ) )
					{
						xmlReader.Read();

						if( xmlReader.Name == "xml" )
						{
							xmlReader.Read();
						}

						string elementName = xmlReader.Name;
						Type exceptionType = null;

						if( elementNameToExceptionMap.TryGetValue( elementName, out exceptionType ) )
						{
							int errorCode = 100;
							string message = "Unknown error";

							if( xmlReader.HasAttributes )
							{
								while( xmlReader.MoveToNextAttribute() )
								{
									if( xmlReader.Name == "coment" || xmlReader.Name == "comment" )
									{
										message = xmlReader.Value;
									}
									else if( xmlReader.Name == "error-code" )
									{
										errorCode = int.Parse( xmlReader.Value );
									}
									else if( xmlReader.Name == "error" )
									{
									    int errorCodeVal;
									    if (int.TryParse(xmlReader.Value, out errorCodeVal))
									    {
									        errorCode = errorCodeVal;
									    }
                                        else if (xmlReader.Value.GetType() == typeof(string)) message = xmlReader.Value;
									}
								}

								xmlReader.MoveToElement();
							}

							parsedException = ( CatalitException ) Activator.CreateInstance( exceptionType, message, errorCode );
						}
					}
				}

				if( parsedException == null )
				{
					return;
				}
			}
			catch( Exception ex )
			{
				throw new CatalitParseException( "Unable to parse XML response.", ex );
			}

			throw parsedException;
		}
	}
}