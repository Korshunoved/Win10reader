namespace LitRes.Exceptions
{
	public class CatalitActivateCouponeException : CatalitException
	{
		public CatalitActivateCouponeException(string message, int errorCode)
			: base( message, errorCode )
		{
		}
	}
}
