namespace LitRes.Exceptions
{
	public class CatalitPaycardProcessingException : CatalitException
	{
        public CatalitPaycardProcessingException(string message, int errorCode)
			: base( message, errorCode )
		{
		}
	}
}
