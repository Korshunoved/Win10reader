namespace LitRes.Exceptions
{
	public class CatalitUniteException : CatalitException
	{
		public CatalitUniteException(string message, int errorCode)
			: base( message, errorCode )
		{
		}
	}
}
