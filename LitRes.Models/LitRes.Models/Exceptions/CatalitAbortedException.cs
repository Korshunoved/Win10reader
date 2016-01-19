using System;

namespace LitRes.Exceptions
{
	public class CatalitAbortedException : CatalitException
	{
		#region Constructors/Disposer
		public CatalitAbortedException( string message, Exception innerException )
			: base( message, innerException )
		{
		}
		#endregion
	}
}
