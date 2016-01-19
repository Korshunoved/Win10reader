using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Digillect.Collections;
using LitRes.Models;
using LitRes.Services.Connectivity;
using ICSharpCode.SharpZipLib.Zip;

namespace LitRes.Services
{
    internal class BookProvider : IBookProvider
    {
        const string CatalogPath = "MyBooks";
        const string StorageSettingName = "ExistBooks";
        const string DKey = "D(Fdskfd9i34987w7r7*8sd-hfuUF*73rksdf#E(DFijF(D*]${";

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

        public async Task<string> GetTrialBook(Book book, CancellationToken token)
        {
            var exists = await GetExistBooks(CancellationToken.None);
            var exist = exists.FirstOrDefault(x => x.Id == book.Id);

            if (exist != null)
            {
                var document = await GetFullBook(exist, token);

                if (document != null)
                {
                    return document;
                }
            }

            var bookname = book.Id.ToString(CultureInfo.InvariantCulture) + ".trial";
            string id = string.Format("{0:00000000}", book.Id);
            string url = string.Format("trials/{0}/{1}/{2}/{3}.jbk", id.Substring(0, 2), id.Substring(2, 2), id.Substring(4, 2), id);

            return await GetDocument(
                cancellationToken => _awareConnection.ProcessStaticSecureRequest<RawFile>(url, cancellationToken),
                token,
                Path.Combine(CatalogPath, book.Id.ToString(CultureInfo.InvariantCulture)), Path.Combine(CatalogPath, bookname)
                );
        }

        public async Task<string> GetFullBook(Book book, CancellationToken token)
        {
            var document = await GetDocument(async cancellationToken =>
            {
                var parameters = new Dictionary<string, object>
                        {
                            { "art", book.Id },
                            { "type", "jbk" }
                        };

                if (book.IsUnpackedDrm)//book.Id == 6883340)
                {
                    var timeResp = await _awareConnection.ProcessRequest<ServerTimeResponse>("catalit_browser", false, true, token, new Dictionary<string, object> { { "art", 0 } });
                    var umd5 = MD5.GetMd5String($"{timeResp.UnixTime}:{book.Description.Hidden.DocumentInfo.Id}:{"6193449b38c80b4f50bc3bdab32c61a8c95068bb"}");
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
                    var umd5 = MD5.GetMd5String($"{timeResp.UnixTime}:{book.Description.Hidden.DocumentInfo.Id}:{DKey}");
                    parameters = new Dictionary<string, object>
                    {
                        {"uuid", book.Description.Hidden.DocumentInfo.Id},
                        {"libapp", _deviceInfoService.LibAppId},
                        {"timestamp", timeResp.UnixTime},
                        {"umd5", umd5}
                    };                     
                }
                if (book.isFreeBook) Analytics.Instance.sendMessage(Analytics.ActionGetFree);
                return await _awareConnection.ProcessRequest<RawFile>("catalit_download_book", true, true, token, parameters);
            },
                token,
                Path.Combine(CatalogPath, book.Id.ToString(CultureInfo.InvariantCulture)));

            if (!string.IsNullOrEmpty(document))
            {
                var exists = await GetExistBooks(CancellationToken.None);

                if (exists.All(x => x.Id != book.Id))
                {
                    exists.Insert(0, book);
                    _dataCacheService.PutItem(exists, StorageSettingName, CancellationToken.None);
                }
            }

            return document;
        }

        public async Task<string> GetDocument(Func<CancellationToken, Task<RawFile>> loader, CancellationToken token, params string[] paths)
        {
            string document = (from path in paths where _fileCacheService.FolderExists(path) select Path.GetFileName(path)).FirstOrDefault();

            if (!string.IsNullOrEmpty(document))
            {
                return document;
            }

            RawFile file = await loader(token);

            if (file.Raw != null)
            {
                using (var originalStream = new MemoryStream(file.Raw))
                {
                    {
                        try
                        {
                            var zipInputStream = new ZipInputStream(originalStream);
                            var zipEntry = zipInputStream.GetNextEntry();
                            if (zipEntry != null)
                            {
                                originalStream.Seek(0, SeekOrigin.Begin);
                                _fileCacheService.ExtractFile(originalStream, paths.Last(), token);
                            }

                            document = Path.GetFileName(paths.Last());
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            originalStream.Flush();
                        }
                    }
                }
            }

            return document;
        }

        public bool FullBookExistsInLocalStorage(int bookId)
        {
            var path = Path.Combine(CatalogPath, bookId.ToString(CultureInfo.InvariantCulture));
            return _fileCacheService.FolderExists(path);
        }

        public bool TrialBookExistsInLocalStorage(int bookId)
        {
            var path = Path.Combine(CatalogPath, bookId.ToString(CultureInfo.InvariantCulture) + ".trial");
            return _fileCacheService.FolderExists(path);
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
