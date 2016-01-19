using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using RestSharp;

namespace LitRes.Services.Connectivity
{
	public static class RestClientExtensions
	{
		public static Task<IRestResponse> ExecuteRequestAsync( this RestClient client, IRestRequest request, CancellationToken cancellationToken )
		{
			var tcs = new TaskCompletionSource<IRestResponse>();

			var response = client.ExecuteAsync( request, r =>
			{
				if( r.ErrorException == null )
				{
					tcs.SetResult( r );
				}
				else
				{
					if( r.ErrorException is WebException && ((WebException) r.ErrorException).Status == WebExceptionStatus.RequestCanceled )
					{
						tcs.SetException( new OperationCanceledException() );
					}
					else
					{
						tcs.SetException( r.ErrorException );
					}
				}
			} );

			if( response != null && cancellationToken != CancellationToken.None )
			{
				cancellationToken.Register( () =>
				{
					response.Abort();
				} );
			}

			return tcs.Task;
		}

		public static Task<IRestResponse<T>> ExecuteRequestAsync<T>( this RestClient client, IRestRequest request, CancellationToken cancellationToken ) where T : new()
		{
			var tcs = new TaskCompletionSource<IRestResponse<T>>();

			var response = client.ExecuteAsync<T>( request, r =>
			{
				if( r.ErrorException == null )
				{
					tcs.SetResult( r );
				}
				else
				{
					if( r.ErrorException is WebException && ((WebException) r.ErrorException).Status == WebExceptionStatus.RequestCanceled )
					{
						tcs.SetException( new OperationCanceledException() );
					}
					else
					{
						tcs.SetException( r.ErrorException );
					}
				}
			} );

			if( response != null && cancellationToken != CancellationToken.None )
			{
				cancellationToken.Register( () =>
				{
					response.Abort();
				} );
			}

			return tcs.Task;
		}
	}
}
