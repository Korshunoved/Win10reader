using System.Threading.Tasks;

using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Services;

namespace LitRes.ViewModels
{
	public class AddRecenseViewModel : ViewModel
	{
		private const string AddPersonRecensePart = "AddPersonRecense";
		private const string AddBookRecensePart = "AddBookRecense";

		private readonly IRecensesProvider _recensesProvider;
		private readonly INavigationService _navigationService;
	    private string _placeholderText;

	    public string RecensePlaceHolderText
	    {
	        get
	        {
	            return _placeholderText;
	        }
	        set
	        {
                SetProperty(ref _placeholderText, value, "RecensePlaceHolderText");
            }
	    }

	    #region Constructors/Disposer
        public AddRecenseViewModel(IRecensesProvider recensesProvider, INavigationService navigationService)
		{
			_navigationService = navigationService;
			_recensesProvider = recensesProvider;

            RegisterAction(AddPersonRecensePart).AddPart((session) => AddPersonRecense(session), (session) => true);
            RegisterAction(AddBookRecensePart).AddPart((session) => AddBookRecense(session), (session) => true);

		}
		#endregion

		#region AddPersonRecense
		public async Task AddPersonRecense( string message, string personUuid )
		{
			var session = new Session( AddPersonRecensePart );
			session.AddParameter( "message", message );
			session.AddParameter( "personUuid", personUuid );
			await Load( session );
		}

		private async Task AddPersonRecense( Session session )
		{
			string message = session.Parameters.GetValue<string>( "message" );
			string personUuid = session.Parameters.GetValue<string>( "personUuid" );

			await _recensesProvider.AddRecenseForPerson( message, personUuid, session.Token );
		}
		#endregion

		#region AddBookRecense
		public async Task AddBookRecense( string message, int bookId )
		{
			Session session = new Session( AddBookRecensePart );
			session.AddParameter( "message", message );
			session.AddParameter( "bookId", bookId );
			await Load( session );
		}

		private async Task AddBookRecense(Session session)
		{
			string message = session.Parameters.GetValue<string>( "message" );
			int bookId = session.Parameters.GetValue<int>( "bookId" );
		    
			await _recensesProvider.AddRecenseForBook( message, bookId, session.Token );
		}
		#endregion
	}
}
