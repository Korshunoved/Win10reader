using System.Threading.Tasks;
using Digillect.Collections;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Models;
using LitRes.Services;

namespace LitRes.ViewModels
{
	public class SearchHistoryViewModel : ViewModel
	{
		private const string QueryParameter = "query";

		private const string LoadHistoryPart = "LoadHistory";
		private const string LoadBooksPart = "LoadBooks";
		private const string AddToHistoryPart = "AddToHistory";
		private const string RemoveFromHistoryPart = "RemoveFromHistory";

		private readonly ISearchHistoryProvider _searchHistoryProvider;
		private readonly ICatalogProvider _catalogProvider;
		private readonly INavigationService _navigationService;

		#region Public Properties
		public XCollection<SearchQuery> SearchQueries { get; private set; }
		public bool Founded { get; private set; }
		#endregion

		#region Constructors/Disposer
		public SearchHistoryViewModel( ISearchHistoryProvider searchHistoryProvider, ICatalogProvider catalogProvider, INavigationService navigationService )
		{
			_searchHistoryProvider = searchHistoryProvider;
			_catalogProvider = catalogProvider;
			_navigationService = navigationService;

            RegisterAction(LoadHistoryPart).AddPart((session) => LoadHistory(session), (session) => true);
            RegisterAction(LoadBooksPart).AddPart((session) => SearchBooks(session), (session) => true);
            RegisterAction(AddToHistoryPart).AddPart((session) => AddToHistory(session), (session) => true);
            RegisterAction(RemoveFromHistoryPart).AddPart((session) => RemoveFromHistory(session), (session) => true);
            
            SearchQueries = new XCollection<SearchQuery>();
		}
		#endregion

		#region SearchBooks
		public async Task<bool> SearchBooks( string searchString )
		{
			Session session = new Session( LoadBooksPart );
			session.AddParameter( "search", searchString );
			await Load( session );
			return Founded;
		}

		private async Task SearchBooks( Session session )
		{
			string search = session.Parameters.GetValue<string>( "search" );
			var searchBooks = await _catalogProvider.SearchBooks( 0, search, session.Token );

			if(searchBooks != null && searchBooks.Count > 0)
			{
				Founded = true;
			}
			else
			{
				Founded = false;
			}
		}
		#endregion

		#region AddToHistory
		public Task AddToHistory( SearchQuery query )
		{
			Session session = new Session("AddToHistory");
			session.AddParameter( QueryParameter, query );
			return Load( new Session() );
		}
		#endregion

		#region RemoveFromHistory
		public Task RemoveFromHistory( SearchQuery query )
		{
			Session session = new Session("RemoveFromHistoryPart");
			session.AddParameter( QueryParameter, query );
			return Load( new Session() );
		}
		#endregion

		#region LoadHistory
		private async Task LoadHistory( Session session )
		{
			var history = await _searchHistoryProvider.GetHistory( session.Token );
			if( history != null )
			{
				SearchQueries.Update( history );
			}
		}
		#endregion

		#region AddToHistory
		private async Task AddToHistory( Session session )
		{
			var query = session.Parameters.GetValue<SearchQuery>( QueryParameter );
			await _searchHistoryProvider.AddToHistory( query, session.Token );
			var history = await _searchHistoryProvider.GetHistory( session.Token );
			if( history != null )
			{
				SearchQueries.Update( history );
			}
		}
		#endregion

		#region AddToHistory
		private async Task RemoveFromHistory( Session session )
		{
			var query = session.Parameters.GetValue<SearchQuery>( QueryParameter );
			await _searchHistoryProvider.RemoveFromHistory( query, session.Token );

			var history = await _searchHistoryProvider.GetHistory( session.Token );

			if( history != null )
			{
				SearchQueries.Update( history );
			}
		}
		#endregion
	}
}
