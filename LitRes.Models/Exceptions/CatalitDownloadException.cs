namespace LitRes.Exceptions
{
	public class CatalitDownloadException : CatalitException
	{
		public CatalitDownloadException(string message, int errorCode)
			: base( message, errorCode )
		{
		}
	}
}
