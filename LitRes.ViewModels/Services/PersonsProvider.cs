using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Digillect.Collections;
using LitRes.Models;
using LitRes.Services.Connectivity;

namespace LitRes.Services
{
	internal class PersonsProvider : IPersonsProvider
	{
		public const int SinglePersonsCount = 20;

		private readonly ICatalitClient _client;

		private XCollection<Person> _singlePerson;  

		#region Constructors/Disposer
		public PersonsProvider(ICatalitClient client)
		{
			_client = client;
		}
		#endregion

		public async Task<Person> GetPersonById(string personId, CancellationToken cancellationToken)
		{
			Person person = null;
			if (_singlePerson == null)
			{
				_singlePerson = new XCollection<Person>();
			}
			person = _singlePerson.FirstOrDefault(bk => bk.Id.Equals(personId));
			if (person != null)
			{
				//move to first
				_singlePerson.Remove(person);
				_singlePerson.Insert(0, person);
			}
			if (person == null)
			{
				Dictionary<string, object> parameters = new Dictionary<string, object>
						{							
							{"person", personId}								
						};
				var persons = await _client.GetPerson(parameters, cancellationToken);
				if (persons != null && persons.Persons != null && persons.Persons.Count > 0)
				{
					_singlePerson.Insert(0, persons.Persons[0]);
					if (_singlePerson.Count > SinglePersonsCount)
					{
						_singlePerson.RemoveAt(SinglePersonsCount);
					}
					return persons.Persons[0];
				}
			}
			return person;
		}

		public async Task<Person> GetPersonByName(string personName, CancellationToken cancellationToken)
		{
			Person person = null;
			if (_singlePerson == null)
			{
				_singlePerson = new XCollection<Person>();
			}
			person = _singlePerson.FirstOrDefault( bk => bk.Title.Main.Equals( personName ) );
			if (person != null)
			{
				//move to first
				_singlePerson.Remove(person);
				_singlePerson.Insert(0, person);
			}
			if (person == null)
			{
				var parameters = new Dictionary<string, object>
				{							
					{"search_person", personName}								
				};
				var persons = await _client.GetPerson(parameters, cancellationToken);
				if (persons?.Persons != null && persons.Persons.Count > 0)
				{
					_singlePerson.Insert(0, persons.Persons[0]);
					if (_singlePerson.Count > SinglePersonsCount)
					{
						_singlePerson.RemoveAt(SinglePersonsCount);
					}
					return persons.Persons[0];
				}
			}
			return person;
		}

        public async Task<XCollection<Person>> GetPersonsByName(string personName, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<string, object> { {"search_person", personName} };
            var persons = await _client.GetPerson(parameters, cancellationToken);
            return persons?.Persons;
        }
    }
}
