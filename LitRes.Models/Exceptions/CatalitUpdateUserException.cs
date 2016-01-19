namespace LitRes.Exceptions
{
	public class CatalitUpdateUserException : CatalitException
	{
		public CatalitUpdateUserException(string message, int errorCode)
			: base( message, errorCode )
		{
		}
	}
}
