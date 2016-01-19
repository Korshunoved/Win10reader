using System.Threading.Tasks;
using Digillect.Collections;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Services;
using Collection = LitRes.Models.CollectionsResponse.Collection;

namespace LitRes.ViewModels
{
	public class CollectionsViewModel : ViewModel
	{
		public const string MainPart = "Main";

		private readonly ICollectionsProvider _collectionsProvider;
		private readonly INavigationService _navigationService;

		private bool _loaded;

		#region Public Properties
		public XCollection<Collection> Collections { get; private set; }
		#endregion

		#region Constructors/Disposer
		public CollectionsViewModel( ICollectionsProvider collectionsProvider, INavigationService navigationService )
		{
			_collectionsProvider = collectionsProvider;
			_navigationService = navigationService;

			Collections = new XCollection<Collection>();

			RegisterPart( MainPart, ( session, part ) => LoadCollections( session ), ( session, part ) => !_loaded );
		}
		#endregion

		#region Load
		public async Task Load()
		{
			Session session = new Session();
			await Load( session );
		}
		#endregion

		#region LoadCollections
		private async Task LoadCollections( Session session )
		{
			var collections = await _collectionsProvider.ProvideMyCollections(session.Token);

			_loaded = true;

			Collections.BeginUpdate();
			Collections.Clear();
			Collections.AddRange(collections);
			Collections.EndUpdate();
		}
		#endregion
	}
}
