namespace LitRes.Services
{
	public interface IBookReadingContextService
	{
		void Clear();
		bool HasContext( int bookId );
		BookReadingContext GetContext( int bookId );
		void RemoveContext( int bookId );
		void SetContext( int bookId, BookReadingContext item );
	}
}
