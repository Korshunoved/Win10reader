using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using BookParser;
using BookParser.Common.ExtensionMethods;
using BookParser.Models;
using BookParser.Parsers;
using BookRender.Tools;
using Digillect.Collections;
using ICSharpCode.SharpZipLib;
using LitRes.Models;
using LitRes.Services.Connectivity;

namespace LitRes.Services
{
    internal class BookProvider : IBookProvider
    {
        const string CatalogPath = "MyBooks/";
        const string StorageSettingName = "ExistBooks";
        const string DKey1 = "Z2OD63E9885BBE98";
        const string DKey2 = "rDB90j7h2ZxhEUCR";
        const string DKey3 = "937vCj55CR864pVe";
        const string DKey4 = "AsrYN477I_O";

        private readonly ISessionAwareConnection _awareConnection;
        private readonly IFileCacheService _fileCacheService;
        private readonly IDataCacheService _dataCacheService;
        private readonly IDeviceInfoService _deviceInfoService;
        private readonly IFileDownloadService _fileDownloadService;
        private readonly ISessionEstablisherService _sessionEstablisherService;
        private XCollection<Book> _existBooks;

        #region Constructors/Disposer
        public BookProvider(ISessionAwareConnection awareConnection, IDataCacheService dataCacheService, IFileCacheService fileCacheService, IDeviceInfoService deviceInfoService, IFileDownloadService fileDownloadService, ISessionEstablisherService sessionEstablisherService)
        {
            _fileCacheService = fileCacheService;
            _dataCacheService = dataCacheService;
            _awareConnection = awareConnection;
            _deviceInfoService = deviceInfoService;
            _fileDownloadService = fileDownloadService;
            _sessionEstablisherService = sessionEstablisherService;
        }
        #endregion

       public async Task GetTrialBook(Book book, CancellationToken token)
        {
            var exists = await GetExistBooks(CancellationToken.None);
            var exist = exists.FirstOrDefault(x => x.Id == book.Id);

            if (exist != null)
            {
                await GetFullBook(exist, token);
            }

            var bookname = book.Id.ToString(CultureInfo.InvariantCulture) + ".trial";
            string id = string.Format("{0:00000000}", book.Id);
            string url = string.Format("trials/{0}/{1}/{2}/{3}.fb2.zip", id.Substring(0, 2), id.Substring(2, 2), id.Substring(4, 2), id);

            await GetDocument(
                cancellationToken => _awareConnection.ProcessStaticSecureRequest<RawFile>(url, cancellationToken),
                token,
                book
                );
        }

        public async Task GetFullBook(Book book, CancellationToken token)
        {
            await GetDocument(async cancellationToken =>
            {
                var parameters = new Dictionary<string, object>
                        {
                            { "art", book.Id },
                            { "type", "fb2.zip" }
                        };

                if (book.IsUnpackedDrm)//book.Id == 6883340)
                {
                    var timeResp = await _awareConnection.ProcessRequest<ServerTimeResponse>("catalit_browser", false, true, token, new Dictionary<string, object> { { "art", 0 } });
                    var umd5 = MD5.GetMd5String(string.Format("{0}:{1}:{2}", timeResp.UnixTime, book.Description.Hidden.DocumentInfo.Id, "6193449b38c80b4f50bc3bdab32c61a8c95068bb"));
                    parameters = new Dictionary<string, object>
                        {
                            {"uuid", book.Description.Hidden.DocumentInfo.Id},
                            {"libapp", 6},
                            {"timestamp", timeResp.UnixTime},
                            {"umd5", umd5}
                        };
                }
                else if ((!book.IsExpiredBook && !string.IsNullOrEmpty(book.ExpiredDateStr)))
                {
                    var timeResp = await _awareConnection.ProcessRequest<ServerTimeResponse>("catalit_browser", false, true, token, new Dictionary<string, object> { { "art", 0 } });
                    var umd5 = MD5.GetMd5String(string.Format("{0}:{1}:{2}", timeResp.UnixTime, book.Description.Hidden.DocumentInfo.Id, (1078 % (2 * 5)).ToString() + DKey1 + DKey2 + DKey3 + DKey4 + (68103 / 987).ToString()));
                    parameters = new Dictionary<string, object>
                    {
                        {"uuid", book.Description.Hidden.DocumentInfo.Id},
                        {"libapp", _deviceInfoService.LibAppId},
                        {"timestamp", timeResp.UnixTime},
                        {"umd5", umd5}
                    };
                }
                if (book.IsFreeBook) Analytics.Instance.sendMessage(Analytics.ActionGetFree);
                return await _awareConnection.ProcessRequest<RawFile>("catalit_download_book", true, true, token, parameters);
            },
                token,
                book);
          
            var exists = await GetExistBooks(CancellationToken.None);

            if (exists.All(x => x.Id != book.Id))
            {
                exists.Insert(0, book);
                _dataCacheService.PutItem(exists, StorageSettingName, CancellationToken.None);
            }
        }

        private static void SaveToFile(string path, byte[] bytes)
        {
            Stream stream = new MemoryStream(bytes);
            using (var storeForApplication = IsolatedStorageFile.GetUserStoreForApplication())
            {
                stream.Position = 0L;
                using (var storageFileStream = storeForApplication.OpenFile(path, FileMode.CreateNew, FileAccess.Write))
                {
                    stream.CopyTo(storageFileStream);
                }
            }
        }

        public async Task GetDocument(Func<CancellationToken, Task<RawFile>> loader,
            CancellationToken token, Book book)
        {
            RawFile file = null;
            try
            {
                file = await loader(token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return;
            }
            if (file?.Raw == null) return;
            SaveToFile(PrepareFilePath(book, IsolatedStorageFile.GetUserStoreForApplication()), file.Raw);
            ParseBook(book);
        }

        public IBookSummaryParser GetBookFromStorage(Book item, bool isTrial)
        {
            var previewGenerator = GetSummaryParser(item, isTrial);
            var bookSummary = previewGenerator.GetBookPreview();
            var book = CreateBook(item, bookSummary);
            book.LoadInfo(book.GetFolderPath() + "/bookinfo");
            var chapters = ToolsRepository.GetChapters(book.BookID, book.GetChaptersPath());     
            AppSettings.Default.Chapters = chapters;
            var anchors = ToolsRepository.GetAnchors(book.BookID, book.GetAnchorsPath());
            AppSettings.Default.Anchors = anchors;
            ReplaceBookInSettings(book);
            return previewGenerator;
        }

        public IBookSummaryParser PreviewGenerator { get; set; }
        public IBookSummaryParser GetSummaryParser(Book item, bool isTrial)
        {
            var bookFolder = item.IsMyBook || item.IsFreeBook ? item.Id.ToString() : item.Id + ".trial";
            var bookStorageFileStream =
                new IsolatedStorageFileStream(isTrial ? CreateTrialBookPath(item) : CreateBookPath(bookFolder), FileMode.Open,
                    IsolatedStorageFile.GetUserStoreForApplication());
            var previewGenerator = PreviewGenerator ?? BookFactory.GetPreviewGenerator(item.TypeBook.ToString(), item.BookTitle, bookStorageFileStream);
            return previewGenerator;
        }

        private void ParseBook(Book item)
        {
            var bookFolder = item.IsMyBook || item.IsFreeBook ? item.Id.ToString() : item.Id + ".trial";
            try
            {
                using (var storeForApplication = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var bookStorageFileStream = new IsolatedStorageFileStream(CreateBookPath(bookFolder), FileMode.Open, storeForApplication))
                    {
                        var previewGenerator = PreviewGenerator ?? BookFactory.GetPreviewGenerator(item.TypeBook.ToString(), item.BookTitle, bookStorageFileStream);
                        var bookSummary = previewGenerator.GetBookPreview();
                        SaveBook(item, bookSummary, previewGenerator, storeForApplication);
                    }
                }
            }
            catch (SharpZipBaseException)
            {
                //SetItemStatus(item, DownloadStatus.Error);
            }
            catch (Exception e)
            {
               // SetItemStatus(item, DownloadStatus.Error);
            }
        }

        private static string PrepareFilePath(Book item, IsolatedStorageFile storage)
        {
            if (item.IsMyBook || item.IsFreeBook)
            {
                if (!storage.DirectoryExists(item.Id.ToString()))
                {
                    storage.CreateDirectory(Path.Combine(CatalogPath, item.Id.ToString()));
                }
            }
            else
            {
                if (!storage.DirectoryExists(item.Id + ".trial"))
                {
                    storage.CreateDirectory(Path.Combine(CatalogPath, item.Id + ".trial"));
                }
            }

            var bookPath = item.IsMyBook || item.IsFreeBook ? CreateBookPath(item.Id.ToString()) : CreateBookPath(item.Id + ".trial");
            if (storage.FileExists(bookPath))
            {
                storage.DeleteFile(bookPath);
            }

            return bookPath;
        }

        private static string CreateBookPath(string folderName)
        {
            return Path.Combine(CatalogPath + folderName + ModelConstants.BookFileDataPath);
        }

        private static string CreateTrialBookPath(Book item)
        {
            return Path.Combine(CatalogPath + item.Id +".trial" + ModelConstants.BookFileDataPath);
        }

        private static string CreateImagesPath(string folderName)
        {
            return Path.Combine(CatalogPath + folderName + ModelConstants.BookImagesFileName);
        }

        private void SaveBook(Book item, BookSummary bookSummary, IBookSummaryParser previewGenerator, IsolatedStorageFile storeForApplication)
        {
            var bookFolder = item.IsMyBook || item.IsFreeBook ? item.Id.ToString() : item.Id + ".trial";
            using (var imageStorageFileStream = new IsolatedStorageFileStream(CreateImagesPath(bookFolder), FileMode.Create, storeForApplication))
            {
                previewGenerator.SaveImages(imageStorageFileStream);
            }

           // previewGenerator.SaveCover(item.BookID.ToString());

            var book = CreateBook(item, bookSummary);

            try
            {
                //_dataCacheService.PutItem(book, book.BookID, CancellationToken.None);
                TokensTool.SaveTokens(book, previewGenerator);
                book.Hidden = book.Trial;
               // _bookService.Save(book);                
            }
            catch (Exception)
            {
                //_bookService.Remove(book.BookID);
                throw;
            }
            ReplaceBookInSettings(book);
        }

        private void ReplaceBookInSettings(BookModel book)
        {
            var tmpBook = AppSettings.Default.CurrentBook;
            AppSettings.Default.CurrentBook = book;
            if (tmpBook != null && tmpBook.BookID != book.BookID)
                AppSettings.Default.CurrentTokenOffset = 0;
        }

        private static BookModel CreateBook(Book item, BookSummary bookSummary)
        {
            var trial = !(item.IsFreeBook || item.IsMyBook);
            var book = new BookModel
            {
                BookID = item.Id.ToString(),
                Title = bookSummary.Title.SafeSubstring(1024),
                Author = bookSummary.AuthorName.SafeSubstring(1024),
                Type = item.Type,
                Hidden = trial,
                Trial = trial,
                Deleted = false,
                CreatedDate = DateTime.Now.ToFileTimeUtc(),
                UniqueID = bookSummary.UniqueId.SafeSubstring(1024),
                Description = bookSummary.Description,
                Language = bookSummary.Language,
                Url = item.Url,
               // CatalogItemId = item.CatalogItemId
            };

            return book;
        }

        public bool FullBookExistsInLocalStorage(int bookId)
        {
            var path = Path.Combine(CatalogPath + bookId + ModelConstants.BookFileDataPath); //
            return _fileCacheService.FileExists(path);
        }

        public bool TrialBookExistsInLocalStorage(int bookId)
        {
            var path = Path.Combine(CatalogPath + bookId + ".trial" + ModelConstants.BookFileDataPath);            
            return _fileCacheService.FileExists(path);
        }

        public async Task RemoveFullBookInLocalStorage(Book book)
        {
            var path = Path.Combine(CatalogPath, book.Id.ToString(CultureInfo.InvariantCulture));
            _fileCacheService.DeleteFile(path);
            
            if (_existBooks == null) await GetExistBooks(CancellationToken.None);
            try
            {
                _existBooks.Remove(_existBooks.First(bk => bk.Id == book.Id));
            }
            catch (Exception) { }
            _dataCacheService.PutItem(_existBooks, StorageSettingName, CancellationToken.None);

            var coverPath = string.Format("cover{0}.jpg", book.Id.ToString(CultureInfo.InvariantCulture));
            coverPath = Path.Combine(CatalogPath, coverPath);
            if (_fileCacheService.FileExists(coverPath))
            {
                _fileCacheService.DeleteFile(coverPath);
            }
        }

        public void RemoveTrialBookInLocalStorage(Book book)
        {
            var path = Path.Combine(CatalogPath, book.Id.ToString(CultureInfo.InvariantCulture) + ".trial");
            _fileCacheService.DeleteFile(path);
            _existBooks.Remove(book);
        }

        public async Task ClearLibrariesBooks(CancellationToken token)
        {
            var books = await GetExistBooks(token);
            if (books != null && books.Count > 0)
            {
                var booksWillBeClear = new XCollection<Book>();
                foreach (var book in books)
                {
                    if (!string.IsNullOrEmpty(book.ExpiredDateStr)) booksWillBeClear.Add(book);
                }
                if (booksWillBeClear.Count > 0)
                {
                    foreach (var book in booksWillBeClear)
                    {
                        await RemoveFullBookInLocalStorage(book);
                    }
                }
            }
        }

        public async Task ClearNotLoadedBooks(CancellationToken token)
        {
            var exists = await GetExistBooks(token);
            if (exists != null && exists.Count > 0)
            {
                var booksWillBeClear = new XCollection<Book>();
                foreach (var book in exists)
                {
                    if (!FullBookExistsInLocalStorage(book.Id)) booksWillBeClear.Add(book);
                }
                if (booksWillBeClear.Count > 0)
                {
                    foreach (var book in booksWillBeClear)
                    {
                        exists.Remove(book);
                    }
                    _dataCacheService.PutItem(exists, StorageSettingName, CancellationToken.None);
                }
            }
        }

        public async Task<XCollection<Book>> GetExistBooks(CancellationToken token)
        {
            if (_existBooks == null)
            {
                _existBooks =  _dataCacheService.GetItem<XCollection<Book>>(StorageSettingName);
                _existBooks = _existBooks ?? new XCollection<Book>();
            }

            return _existBooks;
        }

        public async Task UpdateExistBook(Book book, CancellationToken token)
        {
            var exists = await GetExistBooks(token);
            var bk = exists.FirstOrDefault((x => x.Id == book.Id));
            if (bk != null)
            {
                exists.Remove(bk);
                exists.Add(book);
                _dataCacheService.PutItem(exists, StorageSettingName, CancellationToken.None);
            }
        }
    }
}
