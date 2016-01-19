using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Digillect.Mvvm.Services;
using Digillect.Mvvm.UI;
using LitRes.LibraryTools;
using LitRes.ViewModels;
using LitRes.Views;
using Digillect.Mvvm;
using LitRes.Models;
using LitRes.Services.Connectivity;

namespace LitRes.Services
{
	public class PurchaseServiceDecorator : IPageDecorator, IPurchaseServiceDecorator
	{
		private readonly ICatalogProvider _catalogProvider;
		private readonly INotificationsProvider _notificationsProvider;
        private readonly IBookProvider _bookProvider;
        private readonly ICredentialsProvider _credentialsProvider;
	    private readonly IExpirationGuardian _expirationGuardian;
		private readonly List<Page> _pages;

		#region Constructors/Disposer
        public PurchaseServiceDecorator(IExpirationGuardian expirationGuardian, INotificationsProvider notificationsProvider, ICatalogProvider catalogProvider, IBookProvider bookProvider, ICredentialsProvider credentialsProvider)
		{
			_catalogProvider = catalogProvider;
			_notificationsProvider = notificationsProvider;
            _bookProvider = bookProvider;
            _credentialsProvider = credentialsProvider;
            _expirationGuardian = expirationGuardian;
			_pages = new List<Page>();
		}
		#endregion

		#region Decorator methods
		public void AddDecoration(WindowsRTPage page )
		{
			if(!_pages.Contains( page ))
			{
				_pages.Add( page );
			}
		}

		public void RemoveDecoration(WindowsRTPage page )
		{
			if(_pages.Contains( page ))
			{
				_pages.Remove( page );
			}
		}
		#endregion

        public async void UpdateBook(Models.Book currentBook, bool withDownload = false)
		{
            if (!CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
            {
               await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High,
                    (() => UpdateBook(currentBook, withDownload)));
            }
            else
            {
                await _catalogProvider.DeleteBook(currentBook, true);
                var book = await _catalogProvider.GetBookOnline(currentBook.Id, CancellationToken.None);
                if (null == book) book = await _catalogProvider.GetBook(currentBook.Id, CancellationToken.None);

                await _catalogProvider.AddToMyBooks(book, CancellationToken.None);
                _expirationGuardian.AddBook(book);

                if (withDownload)
                {
                    var credentials = _credentialsProvider.ProvideCredentials(CancellationToken.None);
                    var exist = _bookProvider.FullBookExistsInLocalStorage(book.Id);

                    if (credentials != null || !exist)
                    {
                        try
                        {
                            await _bookProvider.GetFullBook(book, CancellationToken.None);
                        }
                        catch (Exception e)
                        {
                           await new MessageDialog("Невозможно загрузить книгу").ShowAsync();
                        }
                    }
                }
                await _notificationsProvider.RefreshNotifications(CancellationToken.None);

                if (
                    _catalogProvider.GetBookByCollectionCache(
                        (int)BooksByCategoryViewModel.BooksViewModelTypeEnum.NokiaCollection, book.Id) != null)
                {
                    _catalogProvider.ClearBooksCollectionCache((int)BooksByCategoryViewModel.BooksViewModelTypeEnum.NokiaCollection);
                }
                await RefreshPages(book);

                var creds = _credentialsProvider.ProvideCredentials(CancellationToken.None);
                if (creds != null)
                {
                    creds.PurchasedBooksCount += 1;
                    _credentialsProvider.RegisterCredentials(creds, CancellationToken.None);
                }

                if (!string.IsNullOrEmpty(book.SelfService) && !string.IsNullOrEmpty(book.ExpiredDateStr))
                {
                    await new MessageDialog(string.Format("Книга \"" + book.Description.Hidden.TitleInfo.BookTitle + "\" получена до {0}", book.ExpiredDate.ToString("d MMMM yyyy"))).ShowAsync();
                }
                else if (_catalogProvider.GetBookByCollectionCache((int)BooksByCategoryViewModel.BooksViewModelTypeEnum.NokiaCollection, book.Id) == null)
                {
                    await new MessageDialog("Книга \"" + book.Description.Hidden.TitleInfo.BookTitle + "\" куплена").ShowAsync();
                }
                else
                {
                    await new MessageDialog("Книга \"" + book.Description.Hidden.TitleInfo.BookTitle + "\" получена").ShowAsync();
                }
            }
        }

        public async void UpdateBookFailed(Models.Book bookId)
        {
            if (!CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
            {
                await
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High,
                        () => { UpdateBook(bookId, false); });
            }
            else
            {
                var book = await _catalogProvider.GetBook(bookId.Id, CancellationToken.None);

                await new MessageDialog("Ошибка покупки \"" + book.Description.Hidden.TitleInfo.BookTitle + "\". При необходимости обратитесь в службу поддержки").ShowAsync();
            }
        }

	    public async void UpdateAccountDeposit()
	    {
            await RefreshAccountPage();
            await new MessageDialog("Пополнение счёта завершено.").ShowAsync();
        }

	    public async void UpdateAccountDepositFailed()
	    {
            if (!CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
            {
                await
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High,
                        UpdateAccountDeposit);
            }
            else
            {
                await new MessageDialog("Ошибка пополнения счёта. При необходимости обратитесь в службу поддержки").ShowAsync();
            }
        }

        public async Task RefreshAccountPage()
        {
            foreach (var page in _pages)
            {
                if (page is AccountInfo)
                {
                    var model = ((AccountInfo)page).ViewModel;
                    model.ReloadInfo.Execute(null);
                    break;
                }
            }
        }

        public async Task RefreshPages(Models.Book book)
	    {
            foreach (var page in _pages)
            {
                if (page is Main)
                {
                    MainViewModel model = ((Main)page).ViewModel;
                    model.LoadMyBooks();
                    model.RefreshOtherBooks(new Session());
                }
                if (page is BooksByCategory)
                {
                    BooksByCategoryViewModel model = ((BooksByCategory)page).ViewModel;
                    model.RefreshBook(book);
                }
                if (page is GenreBooks)
                {
                    GenreBooksViewModel model = ((GenreBooks)page).ViewModel;
                    model.RefreshBook(book);
                }
                if (page is SearchResults)
                {
                    SearchViewModel model = ((SearchResults)page).ViewModel;
                    model.RefreshBook(book);
                }
                else if (page is MyBooks)
                {
                    MyBooksViewModel model = ((MyBooks)page).ViewModel;
                    model.Reload();
                }
                else if (page is Views.Person)
                {
                    PersonViewModel model = ((Views.Person)page).ViewModel;
                    if (!model.AddedToNotifications)
                    {
                        model.ChangeNotificationStatus();
                    }
                }
                else if (page is Views.Book)
                {
                    BookViewModel model = ((Views.Book)page).ViewModel;

                    await model.UpdateBookAfterPurchasing();
                }
                else if (page is Reader)
                {
                    ((Reader)page).UpdateBook();
                }
            }
        }
	}
}
