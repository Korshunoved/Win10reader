using System;
using System.Threading;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Services;

namespace LitRes.ViewModels
{
    public class AskReviewViewModel : ViewModel
    {
        private readonly IDataCacheService _dataCacheService;
        private readonly INavigationService _navigationService;

        public AskReviewViewModel(IDataCacheService dataCacheService, INavigationService navigationService)
        {
            _dataCacheService = dataCacheService;
            _navigationService = navigationService;
        }

        public void AskLatter()
        {
            _dataCacheService.PutItem(DateTime.Now, "AskLaterDate", CancellationToken.None);
            _dataCacheService.PutItem(true, "AskLaterButtonPressed", CancellationToken.None);
        }

        public void DontAskMore()
        {
            _dataCacheService.PutItem(true, "DontAskMoreButtonPressed", CancellationToken.None);
            _dataCacheService.PutItem(DateTime.Now, "DontAskMoreDate", CancellationToken.None);
        }

        public async void Ratting(int starCount)
        {
            switch (starCount)
            {
                case 5:
                {
                    _dataCacheService.PutItem(true, "FiveStarRatingPressed", CancellationToken.None);
                    _dataCacheService.PutItem(DateTime.Now, "LastDateRattingPressed", CancellationToken.None);
                    var uriBing = new Uri(@"ms-windows-store://review/?ProductId=9wzdncrfhvzw");
                    await Windows.System.Launcher.LaunchUriAsync(uriBing);
                    break;
                }
                    
                case 4:
                {
                    _dataCacheService.PutItem(DateTime.Now, "LastDateRattingPressed", CancellationToken.None);
                    _dataCacheService.PutItem(true, "AnyStarRatingPressed", CancellationToken.None);
                    var uriBing = new Uri(@"ms-windows-store://review/?ProductId=9wzdncrfhvzw");
                    await Windows.System.Launcher.LaunchUriAsync(uriBing);
                    break;
                }
                default:
                {
                    _dataCacheService.PutItem(DateTime.Now, "LastDateRattingPressed", CancellationToken.None);
                    _dataCacheService.PutItem(true, "AnyStarRatingPressed", CancellationToken.None);
                    Analytics.Instance.sendMessage(Analytics.ActionRating13Star);
                    break;
                }
            }
        }

        public void Close()
        {
            _navigationService.Navigate("ShopEditorsChoice");
        }
    }
}
