using System.Collections.Generic;

namespace LitRes.Services
{
	internal class BookReadingContextService : IBookReadingContextService
	{
		private readonly Dictionary<int, BookReadingContext> _cache = new Dictionary<int, BookReadingContext>();

		public void Clear()
		{
			_cache.Clear();
		}

		public bool HasContext(int bookId)
		{
			return _cache.ContainsKey(bookId);
		}

		public BookReadingContext GetContext(int bookId)
		{
			return _cache[bookId];
		}

		public void RemoveContext( int bookId )
		{
			if(HasContext( bookId ))
			{
				_cache.Remove( bookId );
			}
		}

		public void SetContext(int bookId, BookReadingContext item)
		{
			_cache[bookId] = item;
		}
	}
}
