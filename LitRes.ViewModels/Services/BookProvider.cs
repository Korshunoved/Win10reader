﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BookParser;
using BookParser.Common.ExtensionMethods;
using BookParser.Models;
using BookParser.Parsers;
using BookRender.Tools;
using Digillect.Collections;
using ICSharpCode.SharpZipLib;
using LitRes.Models;
using LitRes.Services.Connectivity;
using ICSharpCode.SharpZipLib.Zip;

namespace LitRes.Services
{
    internal class BookProvider : IBookProvider
    {
        const string CatalogPath = "MyBooks/";
        const string StorageSettingName = "ExistBooks";
        const string DKey = "D(Fdskfd9i34987w7r7*8sd-hfuUF*73rksdf#E(DFijF(D*]${";
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

       public async Task<FictionBook.Document> GetTrialBook(Book book, CancellationToken token)
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
            string url = string.Format("trials/{0}/{1}/{2}/{3}.fb2.zip", id.Substring(0, 2), id.Substring(2, 2), id.Substring(4, 2), id);

            return await GetDocument(
                cancellationToken => _awareConnection.ProcessStaticSecureRequest<RawFile>(url, cancellationToken),
                token,
                book
                );
        }

        public async Task<FictionBook.Document> GetFullBook(Book book, CancellationToken token)
        {
            var document = await GetDocument(async cancellationToken =>
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
                            //{"file", book.Files.FullPdfFile.Id},
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
                if (book.isFreeBook) Analytics.Instance.sendMessage(Analytics.ActionGetFree);
                return await _awareConnection.ProcessRequest<RawFile>("catalit_download_book", true, true, token, parameters);
            },
                token,
                book);

            if (document != null)
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

        public async Task<FictionBook.Document> GetDocument(Func<CancellationToken, Task<RawFile>> loader,
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
                return null;
            }
            if (file?.Raw == null) return null;
            SaveToFile(PrepareFilePath(book, IsolatedStorageFile.GetUserStoreForApplication()), file.Raw);
            ParseBook(book);

            return null;
        }

        private void ParseBook(Book item)
        {
            try
            {
                using (var storeForApplication = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var bookStorageFileStream = new IsolatedStorageFileStream(CreateBookPath(item), FileMode.Open, storeForApplication))
                    {
                        var previewGenerator = BookFactory.GetPreviewGenerator(item.TypeBook.ToString(), item.BookTitle, bookStorageFileStream);
                        var bookSummary = previewGenerator.GetBookPreview();
                        SaveBook(item, bookSummary, previewGenerator, storeForApplication);
                    }
                }
            }
            catch (SharpZipBaseException)
            {
                //SetItemStatus(item, DownloadStatus.Error);
            }
            catch (Exception)
            {
               // SetItemStatus(item, DownloadStatus.Error);
            }
        }

        private static string PrepareFilePath(Book item, IsolatedStorageFile storage)
        {
            if (!storage.DirectoryExists(item.Id.ToString()))
            {
                storage.CreateDirectory(Path.Combine(CatalogPath,item.Id.ToString()));
            }

            var bookPath = CreateBookPath(item);
            if (storage.FileExists(bookPath))
            {
                storage.DeleteFile(bookPath);
            }

            return bookPath;
        }

        private static string CreateBookPath(Book item)
        {
            return Path.Combine(CatalogPath + item.Id + ModelConstants.BOOK_FILE_DATA_PATH);
        }

        private static string CreateImagesPath(Book item)
        {
            return Path.Combine(CatalogPath + item.Id + ModelConstants.BOOK_IMAGES_FILE_NAME);
        }

        private void SaveBook(Book item, BookSummary bookSummary, IBookSummaryParser previewGenerator, IsolatedStorageFile storeForApplication)
        {
            using (var imageStorageFileStream = new IsolatedStorageFileStream(CreateImagesPath(item), FileMode.Create, storeForApplication))
            {
                previewGenerator.SaveImages(imageStorageFileStream);
            }

           // previewGenerator.SaveCover(item.BookID.ToString());

            var book = CreateBook(item, bookSummary);

            try
            {
               // _bookService.Add(book);
                TokensTool.SaveTokens(book, previewGenerator);
                book.Hidden = book.Trial;
               // _bookService.Save(book);                
            }
            catch (Exception)
            {
                //_bookService.Remove(book.BookID);
                throw;
            }
            AppSettings.Default.CurrentBook = book;
        }

        private static BookModel CreateBook(Book item, BookSummary bookSummary)
        {
            var book = new BookModel
            {
                BookID = item.Id.ToString(),
                Title = bookSummary.Title.SafeSubstring(1024),
                Author = bookSummary.AuthorName.SafeSubstring(1024),
                Type = item.Type,
                Hidden = true,
                Trial = bookSummary.IsTrial,
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

        public MemoryStream Encrypt(MemoryStream dataToEncrypt, string password, string salt = "jklkljasb)0_3;22A,xA")
        {
            MemoryStream memoryStream = null;
            memoryStream = new MemoryStream();

            do
            {
                var data = new byte[1024];
                int readBytes = dataToEncrypt.Read(data, 0, 1024);
                if (readBytes != 0)
                {

                }
                else
                {
                    break;
                }

            } while (true);

            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        public MemoryStream Decrypt(MemoryStream dataToDecrypt, string password, string salt = "jklkljasb)0_3;22A,xA")
        {
            MemoryStream memoryStream = null;

            memoryStream = new MemoryStream();

            byte[] data = dataToDecrypt.ToArray();

            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
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
