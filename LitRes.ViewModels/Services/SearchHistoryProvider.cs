using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Digillect.Collections;
using LitRes.Models;

namespace LitRes.Services
{
	internal class SearchHistoryProvider : ISearchHistoryProvider
	{
		const string CacheItemName = "searchhistory";

		public const int HistoryCount = 30;

		private readonly IDataCacheService _dataCacheService;

		private XCollection<SearchQuery> _searchQueries;

		#region Constructors/Disposer
		public SearchHistoryProvider(IDataCacheService dataCacheService)
		{
			_dataCacheService = dataCacheService;
		}
		#endregion

		public async Task<XCollection<SearchQuery>> GetHistory(CancellationToken cancellationToken)
		{
			if (_searchQueries == null)
			{
				_searchQueries =  _dataCacheService.GetItem<XCollection<SearchQuery>>(CacheItemName);
			}

			return _searchQueries ?? (_searchQueries = new XCollection<SearchQuery>());
		}

		public async Task AddToHistory(SearchQuery query, CancellationToken cancellationToken)
		{
			//if any call add to history without initialization
			await GetHistory(cancellationToken);

			//move to top
			var exits = _searchQueries.FirstOrDefault( x => x.SearchString.ToLower().Equals( query.SearchString.ToLower() ) );
			if (exits != null)
			{
				_searchQueries.Remove( exits );
			}

			_searchQueries.Insert(0, query);

			if (_searchQueries.Count > HistoryCount)
			{
				_searchQueries = new XCollection<SearchQuery>(_searchQueries.Take(HistoryCount));
			}

			_dataCacheService.PutItem(_searchQueries, CacheItemName, cancellationToken);
		}

		public async Task RemoveFromHistory( SearchQuery query, CancellationToken cancellationToken )
		{
			//if any call add to history without initialization
			await GetHistory( cancellationToken );

			//move to top
			var exits = _searchQueries.FirstOrDefault( x => x.SearchString.ToLower().Equals( query.SearchString.ToLower() ) );
			if( exits != null )
			{
				_searchQueries.Remove( exits );
			}
			
			_dataCacheService.PutItem( _searchQueries, CacheItemName, cancellationToken );
		}
	}
}
