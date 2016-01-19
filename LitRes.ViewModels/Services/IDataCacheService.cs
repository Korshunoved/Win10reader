using System;
using System.Threading;
using System.Threading.Tasks;

namespace LitRes.Services
{
	public interface IDataCacheService
	{
		Task<DateTime> GetItemModificationDate(string name);
		//Task<T> GetItem<T>( string name, CancellationToken cancellationToken );
		T GetItem<T>( string name );
		void PutItem<T>( T item, string name, CancellationToken cancellationToken );
		void ClearCache( CancellationToken cancellationToken );
	}
}
