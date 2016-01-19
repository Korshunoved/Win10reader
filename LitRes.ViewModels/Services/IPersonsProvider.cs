using System.Threading;
using System.Threading.Tasks;
using Digillect.Collections;
using LitRes.Models;

namespace LitRes.Services
{
	public interface IPersonsProvider
	{
		Task<Person> GetPersonById(string personId, CancellationToken cancellationToken);
		Task<Person> GetPersonByName(string personName, CancellationToken cancellationToken);
		Task<XCollection<Person>> GetPersonsByName(string personName, CancellationToken cancellationToken);
	}
}
