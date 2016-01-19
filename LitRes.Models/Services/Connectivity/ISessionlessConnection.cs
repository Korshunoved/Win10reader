using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LitRes.Services.Connectivity
{
	public interface ISessionlessConnection
	{
        Task<T> ProcessRequest<T>(string method, bool secure, CancellationToken cancellationToken, IDictionary<string, object> parameters = null, ConnectivityRequestType requestType = ConnectivityRequestType.POST, string url = "wp8-ebook.litres.ru", bool additionalParams = true);
		Task<T> ProcessStaticRequest<T>( string method, CancellationToken cancellationToken );
        Task<T> ProcessStaticSecureRequest<T>(string method, CancellationToken cancellationToken);
        Task<T> ProcessCustomRequest<T>(string url, string method, CancellationToken cancellationToken, IDictionary<string, object> parameters = null);
	}
}
