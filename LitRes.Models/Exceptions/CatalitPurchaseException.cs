namespace LitRes.Exceptions
{
	public class CatalitPurchaseException : CatalitException
	{
		public CatalitPurchaseException(string message, int errorCode)
			: base( message, errorCode )
		{
		}
	}
}
