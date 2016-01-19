using System.Threading;
using System.Threading.Tasks;
using Digillect.Collections;
using LitRes.Models;

namespace LitRes.Services
{
	public interface ISearchHistoryProvider
	{
		Task<XCollection<SearchQuery>> GetHistory(CancellationToken cancellationToken);
		Task AddToHistory(SearchQuery query, CancellationToken cancellationToken);
		Task RemoveFromHistory( SearchQuery query, CancellationToken cancellationToken );
	}
}
