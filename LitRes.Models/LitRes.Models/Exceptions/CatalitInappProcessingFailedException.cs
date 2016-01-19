namespace LitRes.Exceptions
{
	public class CatalitInappProcessingFailedException : CatalitException
	{
		public CatalitInappProcessingFailedException( string message, int errorCode )
			: base( message, errorCode )
		{
		}
	}
}
