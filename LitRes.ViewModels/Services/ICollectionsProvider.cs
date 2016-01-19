using System.Threading;
using System.Threading.Tasks;
using Digillect.Collections;
using Collection = LitRes.Models.CollectionsResponse.Collection;

namespace LitRes.Services
{
	public interface ICollectionsProvider
	{
		Task<XCollection<Collection>> ProvideMyCollections(CancellationToken cancellationToken);
		Task<Collection> GetCollectionById(int collectionId, CancellationToken cancellationToken);
	}
}
