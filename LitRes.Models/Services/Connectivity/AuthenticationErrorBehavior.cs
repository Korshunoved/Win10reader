using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LitRes.Services.Connectivity
{
	public enum AuthenticationErrorBehavior
	{
		/// <summary>
		/// Authentication error should be ignored.
		/// </summary>
		Ignore,
		/// <summary>
		/// Retry authentication with (possibly) modified credentials.
		/// </summary>
		Retry,
		/// <summary>
		/// Report an error.
		/// </summary>
		Fail,
	}
}
