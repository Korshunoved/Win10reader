using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Digillect.Collections;
using LitRes.Services.Connectivity;
using Collection = LitRes.Models.CollectionsResponse.Collection;

namespace LitRes.Services
{
	internal class CollectionsProvider : ICollectionsProvider
	{
		private readonly ICatalitClient _client;

		private XCollection<Collection> _collections;

		#region Constructors/Disposer
		public CollectionsProvider(ICatalitClient client)
		{
			_client = client;
		}
		#endregion

		public async Task<XCollection<Collection>> ProvideMyCollections( CancellationToken cancellationToken )
		{
			if (_collections == null)
			{
				Dictionary<string, object> parameters = new Dictionary<string, object>
						{									
							{"my_active", "1"},							
						};
				var collections = await _client.ProvideMyCollections(parameters, cancellationToken);
				_collections = collections.Collections;
			}

			return _collections;
		}

		public async Task<Collection> GetCollectionById( int collectionId, CancellationToken cancellationToken )
		{
			var collections = await ProvideMyCollections( cancellationToken );
			Collection collection = null;

			if (collections != null)
			{
				collection = collections.FirstOrDefault( x => x.Id.Equals( collectionId ) );
			}

			return collection;
		}
	}
}
