using System.Threading;
using System.Threading.Tasks;

using RestSharp;

namespace LitRes.Services.Connectivity
{
	public interface IRestClientService
	{
        Task<IRestResponse> ProcessRequest(IRestRequest request, bool secureConnection, CancellationToken cancellationToken, string url = "win10-ebook.litres.ru");
		Task<IRestResponse> ProcessStaticRequest( IRestRequest request, CancellationToken cancellationToken );
        Task<IRestResponse> ProcessCustomRequest(IRestRequest request, string url, CancellationToken cancellationToken);
	}
}
