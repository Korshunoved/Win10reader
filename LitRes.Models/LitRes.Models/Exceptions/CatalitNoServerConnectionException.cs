using System;

namespace LitRes.Exceptions
{
	public class CatalitNoServerConnectionException : CatalitException
	{
		#region Constructors/Disposer
		public CatalitNoServerConnectionException( string message, Exception innerException )
			: base( message, innerException )
		{
		}
		#endregion
	}
}
