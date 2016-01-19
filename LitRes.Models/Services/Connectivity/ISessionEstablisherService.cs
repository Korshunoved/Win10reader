using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LitRes.Services.Connectivity
{
	public interface ISessionEstablisherService
	{
		Task<string> EstablishSession( bool forceNewSession, CancellationToken cancellationToken );
		void UpdateSid( string sid );

        Task ClearSid();
	}
}
