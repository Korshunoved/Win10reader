using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LitRes.Services.Connectivity
{
    public enum ConnectivityRequestType
    {
        POST,
        GET,
        PUT,
        DELETE
    }

	public interface ISessionAwareConnection
	{
		/// <summary>
		/// Executes request and managing client-server session if needed.
		/// </summary>
		/// <typeparam name="T">Response type.</typeparam>
		/// <param name="method">Server method.</param>
		/// <param name="secureConnection">If <c>true</c> HTTPS connection will be used, otherwise plain old HTTP.</param>
		/// <param name="sessionRequired">If <c>true</c> session will be enforced for this connection.</param>
		/// <param name="cancellationToken">Token to cancel asynchronous request.</param>
		/// <param name="parameters">Request parameters.</param>
		/// <returns>Server response as object of correct type.</returns>
        Task<T> ProcessRequest<T>(string method, bool secureConnection, bool sessionRequired, CancellationToken cancellationToken, IDictionary<string, object> parameters = null, ConnectivityRequestType requestType = ConnectivityRequestType.POST, string url = "wp8-ebook.litres.ru", bool additionalParams = true);

		/// <summary>
		/// Executes request for special uri without any parameters.
		/// </summary>
		/// <typeparam name="T">Response type.</typeparam>
		/// <param name="method">Server method.</param>
		/// <param name="cancellationToken">Token to cancel asynchronous request.</param>
		/// <returns>Server response as object of correct type.</returns>
		Task<T> ProcessStaticRequest<T>( string method, CancellationToken cancellationToken );
        Task<T> ProcessStaticSecureRequest<T>(string method, CancellationToken cancellationToken);

        Task<T> ProcessCustomRequest<T>(string url, string method, CancellationToken cancellationToken, IDictionary<string, object> parameters = null, bool sessionRequired = false);        
        Task<T> ProcessJsonRequest<T>(string url, string method, CancellationToken cancellationToken, IDictionary<string, object> parameters = null, bool sessionRequired = false);        
	}
}
