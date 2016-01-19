namespace LitRes.Exceptions
{
	public class CatalitAddRecenseException : CatalitException
	{
		public CatalitAddRecenseException(string message, int errorCode)
			: base( message, errorCode )
		{
		}
	}
}
