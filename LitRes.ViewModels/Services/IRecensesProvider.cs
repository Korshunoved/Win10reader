using System.Threading;
using System.Threading.Tasks;
using Digillect.Collections;
using LitRes.Models;

namespace LitRes.Services
{
	public interface IRecensesProvider
	{
		Task<XCollection<Recense>> GetRecenses(int bookId, CancellationToken cancellationToken);
		Task<XCollection<Recense>> GetRecensesForPerson(string personId, CancellationToken cancellationToken);
		Task AddRecenseForPerson( string message, string personId, CancellationToken cancellationToken );
		Task AddRecenseForBook( string message, int bookId, CancellationToken cancellationToken );
	}
}
