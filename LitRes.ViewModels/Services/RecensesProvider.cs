using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Digillect.Collections;
using LitRes.Models;
using LitRes.Services.Connectivity;

namespace LitRes.Services
{
	internal class RecensesProvider : IRecensesProvider
	{
		private readonly ICatalitClient _client;

		#region Constructors/Disposer
		public RecensesProvider(ICatalitClient client)
		{
			_client = client;
		}
		#endregion

		public async Task<XCollection<Recense>> GetRecenses(int bookId, CancellationToken cancellationToken)
		{
			var parameters = new Dictionary<string, object>
						{								
							{"art", bookId}									
						};
			var recenses = await _client.GetRecenses(parameters, cancellationToken);
			return recenses.RecensesInfo.Recenses;
		}

		public async Task<XCollection<Recense>> GetRecensesForPerson(string personId, CancellationToken cancellationToken)
		{
			Dictionary<string, object> parameters = new Dictionary<string, object>
						{								
							{"person", personId}									
						};
			var recenses = await _client.GetRecenses(parameters, cancellationToken);
			return recenses.RecensesInfo.Recenses;
		}

		public async Task AddRecenseForPerson( string message, string personUuid, CancellationToken cancellationToken )
		{
			var parameters = new Dictionary<string, object>
						{								
							{"author", personUuid},									
							{"message", message}									
						};
			await _client.AddRecense( parameters, cancellationToken );
		}

		public async Task AddRecenseForBook( string message, int bookId, CancellationToken cancellationToken )
		{
			var parameters = new Dictionary<string, object>
						{								
							{"art", bookId},									
							{"message", message}									
						};
			await _client.AddRecense(parameters, cancellationToken);
		}
	}
}
