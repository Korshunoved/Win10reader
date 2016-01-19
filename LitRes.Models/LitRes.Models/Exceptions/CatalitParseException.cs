using System;

namespace LitRes.Exceptions
{
	public class CatalitParseException : CatalitException
	{
		#region Constructors/Disposer
		public CatalitParseException( string message, Exception innerException )
			: base( message, innerException )
		{
		}
		#endregion
	}
}
