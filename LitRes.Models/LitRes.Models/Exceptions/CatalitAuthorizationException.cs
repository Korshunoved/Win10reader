namespace LitRes.Exceptions
{
	public class CatalitAuthorizationException : CatalitException
	{
		public CatalitAuthorizationException( string message, int errorCode )
			: base( message, errorCode )
		{
		}
	}
}
