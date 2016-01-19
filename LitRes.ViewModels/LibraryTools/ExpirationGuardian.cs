using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Digillect.Collections;
using LitRes.Models;
using LitRes.Services;
using LitRes.Services.Connectivity;
using  Autofac;

namespace LitRes.LibraryTools
{
    public class ExpirationGuardian : IExpirationGuardian
    {
        #region Members
        private readonly IDataCacheService _dataCacheService;
        private readonly ICatalitClient _client;
        private readonly ICatalogProvider _catalogProvider;
        private readonly IBookProvider _bookProvider;
        private readonly List<IExpiredCallBack> _expiredCallBacks;
        private DispatcherTimer timer;
        #endregion

        #region Constructors
        public ExpirationGuardian(IDataCacheService dataCacheService, ICatalitClient client, ICatalogProvider catalogProvider, IBookProvider bookProvider)
        {
            _expiredCallBacks = new List<IExpiredCallBack>();
            _client = client;
            _dataCacheService = dataCacheService;
            _catalogProvider = catalogProvider;
            _bookProvider = bookProvider;

            Instance = this;
        }
        #endregion

        #region Properties
        public static IExpirationGuardian Instance { get; private set; }
        #endregion

        #region GuardianMethods
        public async void StartGuardian()
        {
            if (Debugger.IsAttached) Debug.WriteLine("StartGuardian");
            timer?.Stop();
            timer = new DispatcherTimer();
     
            var books = await _catalogProvider.GetMyBooksFromCache(CancellationToken.None);
            if(books == null) return;

            var nextBookTime = NextExpireDateTime(books);

            if (nextBookTime != DateTime.MaxValue)
            {
                var booksToExpire = getExpireBooks(books, nextBookTime);
               
                DateTime currentTime;
                try
                {
                    var serverTime = await _client.ServerTime(CancellationToken.None);
                    currentTime = serverTime.Time;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    currentTime = DateTime.Now;
                }

                if (nextBookTime > currentTime)
                {
                    timer.Interval = nextBookTime - currentTime;
                    if (Debugger.IsAttached) Debug.WriteLine("CheckExpirationWillBe");
                    timer.Tick += (s, e) =>
                    {                        
                        CheckExpiration(booksToExpire);
                    };
                    timer.Start();
                }
                else
                {                    
                    CheckExpiration(booksToExpire);
                }
            }
        }

        private async void CheckExpiration(XCollection<Book> booksToExpire)
        {
            if(Debugger.IsAttached) Debug.WriteLine("CheckExpirationDoing");
            var messageStr = new StringBuilder();

            foreach (var book in booksToExpire)
            {
                Notify(book);
             
                await _bookProvider.RemoveFullBookInLocalStorage(book);
                book.IsExpiredBook = true;
                
                messageStr.AppendLine(book.Description.Hidden.TitleInfo.BookTitle);
            }
            
            await _catalogProvider.ExpireBooks(booksToExpire);

            var books = await _catalogProvider.GetMyBooksFromCache(CancellationToken.None);
            if (books == null) books = booksToExpire;            
            else
            {
                booksToExpire.ForEach(expiredBook =>
                {
                    try
                    {
                        var bk = books.First(book => book.Id == expiredBook.Id);
                        bk.IsExpiredBook = true;
                    }
                    catch (Exception){}
                });
            }            

            _catalogProvider.SaveMyBooksToCache(books, CancellationToken.None);
            _catalogProvider.CheckBooks();

            if (messageStr.Length > 0) { 
                //Deployment.Current.Dispatcher.BeginInvoke(() =>
                //{
                //    MessageBox.Show(messageStr.ToString(), "Книги истекли", MessageBoxButton.OK);
                //});

                await new MessageDialog(messageStr.ToString(), "Книги истекли").ShowAsync();
            }
            var purchaseService = ((Digillect.Mvvm.UI.WindowsRTApplication)Application.Current).Scope.Resolve<IPurchaseServiceDecorator>();
            foreach (var bk in booksToExpire)
            {
                await purchaseService.RefreshPages(bk);
            }

            if(NextExpireDateTime(books) != DateTime.MaxValue) StartGuardian();
        }

        private DateTime NextExpireDateTime(XCollection<Book> books)
        {
            var nextBookTime = DateTime.MaxValue;

            books.ForEach(book =>
            {
                if (!string.IsNullOrEmpty(book.ExpiredDateStr) && !book.IsExpiredBook && book.ExpiredDate < nextBookTime)
                {
                    nextBookTime = book.ExpiredDate;
                }
            });

            return nextBookTime;
        }

        private XCollection<Book> getExpireBooks(XCollection<Book> books, DateTime nextBookTime)
        {
            var booksToExpire = new XCollection<Book>();
            books.ForEach(book =>
            {
                if (!string.IsNullOrEmpty(book.ExpiredDateStr) &&
                    !book.IsExpiredBook &&
                    book.ExpiredDate == nextBookTime)
                {
                    booksToExpire.Add(book);
                }
            });
            return booksToExpire;
        }

        public void AddBook(Book book)
        {           
            if (Debugger.IsAttached) Debug.WriteLine("AddBook and StartGuardian");
            StartGuardian();
        }
        #endregion

        #region ObserverMethods
        public void AddCallBack(IExpiredCallBack callBack)
        {
            if (Debugger.IsAttached) Debug.WriteLine("AddCallBack");
            _expiredCallBacks.Add(callBack);
        }
        public void RemoveCallBack(IExpiredCallBack callBack)
        {
            if (Debugger.IsAttached) Debug.WriteLine("RemovCallBack");
            _expiredCallBacks.Remove(callBack);
        }
        private void Notify(Book book)
        {
            if (Debugger.IsAttached) Debug.WriteLine("Notify");

            foreach (var expiredCallBack in _expiredCallBacks)
            {
                if (expiredCallBack != null)
                {
                    expiredCallBack.ExpiredCallBack(book);
                }
            }
        }
        #endregion
    }
}
