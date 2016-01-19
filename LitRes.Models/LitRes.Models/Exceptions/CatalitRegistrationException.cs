namespace LitRes.Exceptions
{
	public class CatalitRegistrationException : CatalitException
	{
		public CatalitRegistrationException(string message, int errorCode)
			: base( message, errorCode )
		{
		}
	}
}
