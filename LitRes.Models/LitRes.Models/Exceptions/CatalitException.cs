using System;

namespace LitRes.Exceptions
{
	public class CatalitException : Exception
	{
		#region Constructors/Disposer
		public CatalitException( string message, int errorCode ) : base( message )
		{
			ErrorCode = errorCode;
		}

		public CatalitException( string message, Exception innerException ) : base( message, innerException )
		{
			ErrorCode = 100;
		}

		protected CatalitException()
		{
			throw new NotImplementedException();
		}

		#endregion

		public int ErrorCode { get; set; }
	}
}
