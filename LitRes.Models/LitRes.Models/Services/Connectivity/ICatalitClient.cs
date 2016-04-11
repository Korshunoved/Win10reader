using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Digillect.Collections;
using LitRes.Models;
using LitRes.Models.JsonModels;
using Genre = LitRes.Models.Genre;

namespace LitRes.Services.Connectivity
{
	public interface ICatalitClient
	{
		Task<Genre> GetGenres( CancellationToken cancellationToken );
        Task<BannersResponse> GetBanners(IDictionary<string, object> parameters, CancellationToken cancellationToken);
        Task<CatalogSearchResponse> SearchCatalog(IDictionary<string, object> parameters, CancellationToken cancellationToken, string url = "win10-ebook.litres.ru", bool sid = true);
        Task<Rootobject> SearchAll(IDictionary<string, object> parameters, CancellationToken cancellationToken, string url = "win10-ebook.litres.ru", bool sid = true);
	    Task<CatalogSearchResponse> SearchAudioCatalog(IDictionary<string, object> parameters,CancellationToken cancellationToken, string url = "win10-ebook.litres.ru", bool sid = true);
		Task<CatalogSearchResponse> GetMyBooks( IDictionary<string, object> parameters, CancellationToken cancellationToken );
        Task<CatalogSearchResponse> GetBooksInBasket(IDictionary<string, object> parameters, CancellationToken cancellationToken);
        Task<AddRecenseResponse> AddRecense( IDictionary<string, object> parameters, CancellationToken cancellationToken );
		Task<RecensesResponse> GetRecenses( IDictionary<string, object> parameters, CancellationToken cancellationToken );
		Task<PersonsResponse> GetPerson( IDictionary<string, object> parameters, CancellationToken cancellationToken );
		Task<UserInformation> Authorize( IDictionary<string, object> parameters, CancellationToken cancellationToken );
		Task<UserInformation> GetUserInfo( CancellationToken cancellationToken );
        Task<UserInformation> Register(IDictionary<string, object> parameters, CancellationToken cancellationToken, bool additionalParams = true);
		Task<UniteInformation> MergeAccounts(IDictionary<string, object> parameters, CancellationToken cancellationToken);
		Task<UpdateUserResponse> ChangeUserInfo( IDictionary<string, object> parameters, CancellationToken cancellationToken );
		Task<CollectionsResponse> ProvideMyCollections( IDictionary<string, object> parameters, CancellationToken cancellationToken );
		Task<BookmarksResponse> GetBookmarks( IDictionary<string, object> parameters, CancellationToken cancellationToken );
		Task AddBookmark( IDictionary<string, object> parameters, CancellationToken cancellationToken );
        Task<PurchaseResponse> PurchaseBook(IDictionary<string, object> parameters, CancellationToken cancellationToken, bool isHidden = false);
        Task<LitresPurchaseResponse> LitresPurchaseBook(IDictionary<string, object> parameters, CancellationToken cancellationToken, bool isHidden = false);
        Task<PurchaseStateResponse> CheckPurchaseState(IDictionary<string, object> parameters, CancellationToken cancellationToken);
        Task<MobileCommerceResponse> MobileCommerceInit(IDictionary<string, object> parameters, CancellationToken cancellationToken);
        Task<CreditCardInitResponse> CreditCardInit(IDictionary<string, object> parameters, CancellationToken cancellationToken);
        Task<ProcessingResponse> ProcessingServerRequest(string url, string method, IDictionary<string, object> parameters, CancellationToken cancellationToken);
        Task<SmsResponse> GetSmsPaymentInfo(IDictionary<string, object> parameters, CancellationToken cancellationToken);
		Task<NotificationsResponce> Subscriptions( IDictionary<string, object> parameters, CancellationToken cancellationToken );
		Task<RawFile> SubscribeDevice( IDictionary<string, object> parameters, CancellationToken cancellationToken );
        Task<RawFile> SendSpampack(IDictionary<string, object> parameters, CancellationToken cancellationToken);
		Task<ActivateCouponeResponse> ActivateCoupone( IDictionary<string, object> parameters, CancellationToken cancellationToken );
		Task<TakeCollectionBookResponse> TakeBookFromCollectionBySubscription( IDictionary<string, object> parameters, CancellationToken cancellationToken );
        Task<RawFile> SelfServiceRequest(IDictionary<string, object> parameters, bool isCancel, CancellationToken cancellationToken);
	    Task<ServerTimeResponse> ServerTime(CancellationToken cancellationToken);
        Task<PurgeRebillsResponse> PurgeRebils(CancellationToken cancellationToken);
	}
}
