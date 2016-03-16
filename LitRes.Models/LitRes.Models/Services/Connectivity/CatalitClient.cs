using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Digillect.Collections;
using LitRes.Models;
using LitRes.Models.JsonModels;
using Genre = LitRes.Models.Genre;

namespace LitRes.Services.Connectivity
{
	public class CatalitClient : ICatalitClient
	{
		private readonly ISessionAwareConnection _connection;

		#region Constructors/Disposer
		public CatalitClient( ISessionAwareConnection connection )
		{
			_connection = connection;
		}
		#endregion

		public Task<Genre> GetGenres( CancellationToken cancellationToken )
		{
			return _connection.ProcessRequest<Genre>( "catalit_genres", false, false, cancellationToken );
		}

        public Task<BannersResponse> GetBanners(IDictionary<string, object> parameters, CancellationToken cancellationToken)
		{
            return _connection.ProcessRequest<BannersResponse>("catalit_banners", false, false, cancellationToken, parameters);
		}

        public Task<CatalogSearchResponse> SearchCatalog(IDictionary<string, object> parameters, CancellationToken cancellationToken, string url = "wp8-ebook.litres.ru", bool sid = true)
		{
			return _connection.ProcessRequest<CatalogSearchResponse>("catalit_browser", false, sid, cancellationToken, parameters, ConnectivityRequestType.GET, url, true);
		}

        public Task<Rootobject> SearchAll(IDictionary<string, object> parameters, CancellationToken cancellationToken, string url = "wp8-ebook.litres.ru", bool sid = true)
        {
            return _connection.ProcessJsonRequest<Rootobject>(url, "GET", cancellationToken, parameters, false);
        }

        public Task<CatalogSearchResponse> SearchAudioCatalog(IDictionary<string, object> parameters, CancellationToken cancellationToken, string url = "wp8-ebook.litres.ru", bool sid = true)
        {
            return _connection.ProcessRequest<CatalogSearchResponse>("catalit_browser", false, sid, cancellationToken, parameters, ConnectivityRequestType.POST, url);
        }

		public Task<CatalogSearchResponse> GetMyBooks(IDictionary<string, object> parameters, CancellationToken cancellationToken)
		{
			return _connection.ProcessRequest<CatalogSearchResponse>("catalit_browser", false, true, cancellationToken, parameters);
		}

		public Task<RecensesResponse> GetRecenses(IDictionary<string, object> parameters, CancellationToken cancellationToken)
		{
			return _connection.ProcessRequest<RecensesResponse>("catalit_get_recenses", false, false, cancellationToken, parameters);
		}

		public Task<AddRecenseResponse> AddRecense(IDictionary<string, object> parameters, CancellationToken cancellationToken)
		{
			return _connection.ProcessRequest<AddRecenseResponse>("catalit_add_recense", false, true, cancellationToken, parameters);
		}

		public Task<PersonsResponse> GetPerson(IDictionary<string, object> parameters, CancellationToken cancellationToken)
		{
			return _connection.ProcessRequest<PersonsResponse>("catalit_persons", false, false, cancellationToken, parameters);
		}

		public Task<UserInformation> Authorize(IDictionary<string, object> parameters, CancellationToken cancellationToken)
		{
			return _connection.ProcessRequest<UserInformation>("catalit_authorise", false, false, cancellationToken, parameters, ConnectivityRequestType.GET);
		}

		public Task<UserInformation> GetUserInfo(CancellationToken cancellationToken)
		{
			return _connection.ProcessRequest<UserInformation>("catalit_authorise", false, true, cancellationToken);
		}

        public Task<UserInformation> Register(IDictionary<string, object> parameters, CancellationToken cancellationToken, bool additionalParams = true)
		{
			return _connection.ProcessRequest<UserInformation>("catalit_register_user", false, false, cancellationToken, parameters, additionalParams: additionalParams);
		}

		public Task<UpdateUserResponse> ChangeUserInfo(IDictionary<string, object> parameters, CancellationToken cancellationToken)
		{
			return _connection.ProcessRequest<UpdateUserResponse>("catalit_update_user", false, true, cancellationToken, parameters);
		}

		public Task<CollectionsResponse> ProvideMyCollections(IDictionary<string, object> parameters, CancellationToken cancellationToken)
		{
			return _connection.ProcessRequest<CollectionsResponse>("catalit_collections", false, true, cancellationToken, parameters);
		}

		public Task<BookmarksResponse> GetBookmarks(IDictionary<string, object> parameters, CancellationToken cancellationToken)
		{
			return _connection.ProcessRequest<BookmarksResponse>("catalit_load_bookmarks", false, true, cancellationToken, parameters);
		}

		public Task AddBookmark(IDictionary<string, object> parameters, CancellationToken cancellationToken)
		{
            return _connection.ProcessRequest<AddBookmarkResponse>("catalit_store_bookmarks", false, true, cancellationToken, parameters, url: "www.litres.ru");
		}

		public Task<UniteInformation> MergeAccounts(IDictionary<string, object> parameters, CancellationToken cancellationToken)
		{
			return _connection.ProcessRequest<UniteInformation>("catalit_unite_user", false, false, cancellationToken, parameters);
		}

        public Task<PurchaseResponse> PurchaseBook(IDictionary<string, object> parameters, CancellationToken cancellationToken, bool isHidden = false)
		{
            if (isHidden) return _connection.ProcessRequest<PurchaseResponse>("purchase_book_inapp", false, true, cancellationToken, parameters, ConnectivityRequestType.POST, "wp8-ebook-hidden.litres.ru");
            return _connection.ProcessRequest<PurchaseResponse>("purchase_book_inapp", false, true, cancellationToken, parameters);
		}

        public Task<LitresPurchaseResponse> LitresPurchaseBook(IDictionary<string, object> parameters, CancellationToken cancellationToken, bool isHidden = false)
        {
            if (isHidden) return _connection.ProcessRequest<LitresPurchaseResponse>("purchase_book", false, true, cancellationToken, parameters, ConnectivityRequestType.POST, "wp8-ebook-hidden.litres.ru");
            return _connection.ProcessRequest<LitresPurchaseResponse>("purchase_book", false, true, cancellationToken, parameters);
        }

        public Task<MobileCommerceResponse> MobileCommerceInit(IDictionary<string, object> parameters, CancellationToken cancellationToken) 
        {
            return _connection.ProcessRequest<MobileCommerceResponse>("catalit_mcommerce_init", false, true, cancellationToken, parameters);
        }

        public Task<CreditCardInitResponse> CreditCardInit(IDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            return _connection.ProcessRequest<CreditCardInitResponse>("catalit_credit_card_init", true, true, cancellationToken, parameters, ConnectivityRequestType.POST, "robot.litres.ru");
        }

        public Task<SmsResponse> GetSmsPaymentInfo(IDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            return _connection.ProcessRequest<SmsResponse>("sms_billing_publ_info", false, false, cancellationToken, parameters);
        }

		public Task<PurchaseStateResponse> CheckPurchaseState(IDictionary<string, object> parameters, CancellationToken cancellationToken)
		{
			return _connection.ProcessRequest<PurchaseStateResponse>("catalit_payorder_check_state", false, true, cancellationToken, parameters);
		}

		public Task<NotificationsResponce> Subscriptions(IDictionary<string, object> parameters, CancellationToken cancellationToken)
		{
			return _connection.ProcessRequest<NotificationsResponce>("catalit_author_subscr", false, true, cancellationToken, parameters);
		}

		public Task<RawFile> SubscribeDevice(IDictionary<string, object> parameters, CancellationToken cancellationToken)
		{
			return _connection.ProcessRequest<RawFile>("fake", false, true, cancellationToken, parameters);
		}

        public Task<RawFile> SendSpampack(IDictionary<string, object> parameters, CancellationToken cancellationToken)
		{
			return _connection.ProcessRequest<RawFile>("fake", false, true, cancellationToken, parameters);
		}

		public Task<ActivateCouponeResponse> ActivateCoupone(IDictionary<string, object> parameters, CancellationToken cancellationToken)
		{
			return _connection.ProcessRequest<ActivateCouponeResponse>("catalit_activate_coupone", false, false, cancellationToken, parameters);
		}

		public Task<TakeCollectionBookResponse> TakeBookFromCollectionBySubscription(IDictionary<string, object> parameters, CancellationToken cancellationToken)
		{
			return _connection.ProcessRequest<TakeCollectionBookResponse>("catalit_get_collection_book", false, true, cancellationToken, parameters);
		}

        public Task<ProcessingResponse> ProcessingServerRequest(string url, string method, IDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            return _connection.ProcessCustomRequest<ProcessingResponse>(url, method, cancellationToken, parameters);
        }

        public Task<RawFile> SelfServiceRequest(IDictionary<string, object> parameters, bool isCancel, CancellationToken cancellationToken)
        {
            return _connection.ProcessRequest<RawFile>(isCancel ? "catalit_cancel_request_libbook" : "catalit_request_libbook", false, true, cancellationToken, parameters);
        }

	    public Task<ServerTimeResponse> ServerTime(CancellationToken cancellationToken)
	    {
            return _connection.ProcessRequest<ServerTimeResponse>("catalit_browser", false, true, cancellationToken, new Dictionary<string, object> { { "art", 0 } });
	    }

	    public Task<PurgeRebillsResponse> PurgeRebils(CancellationToken cancellationToken)
	    {
            return _connection.ProcessRequest<PurgeRebillsResponse>("catalit_purge_rebills", false, true, cancellationToken);
	    }
	}
}
