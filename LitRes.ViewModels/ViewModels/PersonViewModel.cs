﻿using System.Threading.Tasks;
using Digillect;
using Digillect.Collections;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;

using LitRes.Models;
using LitRes.Services;
using LitRes.Services.Connectivity;

namespace LitRes.ViewModels
{
	public class PersonViewModel : ViewModel
	{
		private const string LoadMorePart = "LoadMore";
		private const string LoadMainPart = "LoadMain";
		private const string ChangeNotificationStatusPart = "ChangeNotificationStatus";
        private const string AddPersonRecensePart = "AddPersonRecense";

        private readonly ICredentialsProvider _credentialsProvider;
		private readonly IPersonsProvider _personsProvider;
		private readonly ICatalogProvider _catalogProvider;
		private readonly IRecensesProvider _recensesProvider;
		private readonly INotificationsProvider _notificationsProvider;
		private readonly INavigationService _navigationService;

		private bool _loaded;
		private bool _isBioExists;
		private bool _addedToNotification;
		private Person _entity;

		private bool _isEndOfList;
		private bool _accountExist;

        #region Public Properties

        
        public bool IsBioExists
        {
            get { return _isBioExists; }
            private set { SetProperty(ref _isBioExists, value, "IsBioExists"); }
        }

        public Person Entity
		{
			get { return _entity; }
			private set { SetProperty( ref _entity, value, "Entity" ); }
		}

		public bool AccountExist
		{
			get { return _accountExist; }
			private set { SetProperty( ref _accountExist, value, "AccountExist" ); }
		}

		public bool AddedToNotifications
		{
			get { return _addedToNotification; }
			private set { SetProperty( ref _addedToNotification, value, "AddedToNotifications" ); }
		}
		public XCollection<Book> PersonBooks { get; private set; }
		public XCollection<Recense> PersonRecenses { get; private set; }
		public RelayCommand<Book> BookSelected { get; private set; }
		public RelayCommand WriteRecenseSelected { get; private set; }
		public RelayCommand LoadMoreCalled { get; private set; }
		#endregion

		#region Constructors/Disposer
		public PersonViewModel(ICredentialsProvider credentialsProvider, IPersonsProvider personsProvider, ICatalogProvider catalogProvider, INavigationService navigationService, IRecensesProvider recensesProvider, INotificationsProvider notificationsProvider)
		{
			_credentialsProvider = credentialsProvider;
			_personsProvider = personsProvider;
			_catalogProvider = catalogProvider;
			_navigationService = navigationService;
			_recensesProvider = recensesProvider;
			_notificationsProvider = notificationsProvider;

			PersonBooks = new XCollection<Book>();
			PersonRecenses = new XCollection<Recense>();

            RegisterAction(LoadMorePart).AddPart(session => LoadPersonBooks(session, Entity), session => true);
            RegisterAction(ChangeNotificationStatusPart).AddPart(session => ChangeNotificationStatus(session), session => true);
            RegisterAction(LoadMainPart).AddPart(session => LoadPerson(session), session => true);
            RegisterAction(AddPersonRecensePart).AddPart((session) => AddPersonRecense(session), (session) => true);

            LoadMoreCalled = new RelayCommand( () => LoadMore(), () => true );
			BookSelected = new RelayCommand<Book>( NavigateToBook, book => book != null );
			WriteRecenseSelected = new RelayCommand( () => _navigationService.Navigate( "AddRecense", XParameters.Create( "personId", Entity.Id ) ), () => Entity != null );
		}
		#endregion

		#region NavigateToBook
		private void NavigateToBook( Book book )
		{
			if( book != null && !book.IsEmptyElement )
			{
				_navigationService.Navigate( "Book", XParameters.Create( "BookEntity", book ) );
			}
		}
		#endregion

		#region ChangeNotificationStatus
		public async void ChangeNotificationStatus()
		{
			Session session = new Session( ChangeNotificationStatusPart );
			session.AddParameter( "person", Entity );
			await Load( session );
		}

		private async Task ChangeNotificationStatus(Session session)
		{
			if (AddedToNotifications)
			{
				await DeleteFromNotificationsProceed( session );
			}
			else
			{
				await AddToNotificationsProceed( session );
			}
			
		}
		#endregion

		#region LoadByName
		public Task<Session> LoadByName( string personName )
		{
			Session session = new Session( LoadMainPart );
			session.AddParameter( "personName", personName );

			return Load( session );
		}
		#endregion

		#region LoadById
		public Task<Session> LoadById( string id )
		{
			Session session = new Session( LoadMainPart );
			session.AddParameter( "id", id );            
			return Load( session );
		}
		#endregion

		#region LoadMore
		public Task LoadMore()
		{
			Session session = new Session( LoadMorePart );

			return Load( session );
		}
		#endregion

		#region LoadPersonInfo
		private async Task LoadPersonInfo(Session session, Person person)
		{
			Task loadPersonRecenses = LoadPersonRecenses( session, person );
			Task loadPersonBooks = LoadPersonBooks( session, person );
			Task loadNotificationStatus = LoadNotificationStatus( session, person );
			await Task.WhenAll( loadPersonRecenses, loadPersonBooks, loadNotificationStatus );
		}
		#endregion

		#region AddToNotificationsProceed
		private async Task AddToNotificationsProceed(Session session)
		{
			Person person = session.Parameters.GetValue<Person>( "person", null );

			var notification = await _notificationsProvider.GetNotificationByAuthor( person, session.Token );

			if (notification == null && person != null)
			{
				await _notificationsProvider.AddNotification( person.Id, session.Token );
			}

			notification = await _notificationsProvider.GetNotificationByAuthor( person, session.Token );
			AddedToNotifications = notification != null;
		}
		#endregion

		#region DeleteFromNotificationsProceed
		private async Task DeleteFromNotificationsProceed(Session session)
		{
			Person person = session.Parameters.GetValue<Person>( "person", null );

			var notification = await _notificationsProvider.GetNotificationByAuthor( person, session.Token );

			if (notification != null)
			{
				await _notificationsProvider.DeleteNotifications( new XCollection<Notification> { notification }, session.Token );
			}

			notification = await _notificationsProvider.GetNotificationByAuthor( person, session.Token );
			AddedToNotifications = notification != null;
		}
		#endregion

		#region LoadPerson
		private async Task LoadPerson(Session session)
		{
			AccountExist = (_credentialsProvider.ProvideCredentials(session.Token)) != null;

			Person person = null;

			string name = session.Parameters.GetValue<string>( "personName" );
			if (!string.IsNullOrEmpty( name ))
			{
				person = await _personsProvider.GetPersonByName( name, session.Token );
			}

			string id = session.Parameters.GetValue<string>( "id" );
			if (!string.IsNullOrEmpty( id ))
			{
				person = await _personsProvider.GetPersonById( id, session.Token );
			}

			Entity = person;
		    if (Entity?.TextDescription?.Text == null)
		    {
		        IsBioExists = false;
		    }
		    else
		    {
		        IsBioExists = true;
		    }

			_loaded = true;

			if (person != null)
			{
				await LoadPersonInfo(session, person);
			}
		}
		#endregion

		#region LoadPersonRecenses
		private async Task LoadPersonRecenses(Session session, Person person)
		{
			if ( person != null )
			{
				var recenses = await _recensesProvider.GetRecensesForPerson(person.Id, session.Token);

				PersonRecenses.Update(recenses);
			}
		}
		#endregion

		#region LoadNotificationStatus
		private async Task LoadNotificationStatus(Session session, Person person)
		{
			if ( person != null )
			{
				var notification = await _notificationsProvider.GetNotificationByAuthor( person, session.Token );

				AddedToNotifications = notification != null;
			}
		}
		#endregion

		#region LoadPersonBooks
		private async Task LoadPersonBooks(Session session, Person person)
		{
			if ( person != null )
			{
				var booksCount = PersonBooks.Count > 0 ? PersonBooks.Count - 1 : 0;

				var authorBooks = await _catalogProvider.GetBooksByAuthor(booksCount, person.Id, session.Token);

				if( authorBooks != null && authorBooks.Count > 0 )
				{
					if( PersonBooks.Count > 0 && PersonBooks[PersonBooks.Count - 1] == null )
					{
						PersonBooks.RemoveAt( PersonBooks.Count - 1 );
					}

					if( authorBooks.Count <= PersonBooks.Count )
					{
						_isEndOfList = true;
					}

					PersonBooks.Update( authorBooks );

					//PersonBooks.Add( new Book { IsEmptyElement = true } );
				}
			}
		}
        #endregion

        #region AddPersonRecense
        public async Task AddPersonRecense(string message, string personUuid)
        {
            var session = new Session(AddPersonRecensePart);
            session.AddParameter("message", message);
            session.AddParameter("personUuid", personUuid);
            await Load(session);
        }

        private async Task AddPersonRecense(Session session)
        {
            string message = session.Parameters.GetValue<string>("message");
            string personUuid = session.Parameters.GetValue<string>("personUuid");

            await _recensesProvider.AddRecenseForPerson(message, personUuid, session.Token);
        }
        #endregion
    }
}
