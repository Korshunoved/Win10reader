namespace LitRes.Exceptions
{
	public class CatalitGetCollectionBooksException : CatalitException
	{
		public CatalitGetCollectionBooksException(string message, int errorCode)
			: base( message, errorCode )
		{
		}
	}
}
