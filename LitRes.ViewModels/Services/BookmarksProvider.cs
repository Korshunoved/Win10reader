using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Digillect.Collections;
using Digillect.Mvvm.Services;

using LitRes.Exceptions;
using LitRes.Models;
using LitRes.Services.Connectivity;

namespace LitRes.Services
{
	internal class BookmarksProvider : IBookmarksProvider
	{
		const string BookmarksCacheItemName = "bookmarks";

		private readonly ICatalitClient _client;
		private readonly IDataCacheService _dataCacheService;
		private readonly INetworkAvailabilityService _networkAvailabilityService;

		private string _lock;

		#region Constructors/Disposer
		public BookmarksProvider(ICatalitClient client, IDataCacheService dataCacheService, INetworkAvailabilityService networkAvailabilityService)
		{
			_client = client;
			_dataCacheService = dataCacheService;
			_networkAvailabilityService = networkAvailabilityService;

			_lock = string.Empty;
		}
		#endregion

        public async Task<XCollection<Bookmark>> GetBookmarksByDocumentId(string bookId, CancellationToken cancellationToken, bool isOnlyCurrentPosition = false)
        {
            var bookmarkGroup = isOnlyCurrentPosition ? "0" : "0,1";
			var parameters = new Dictionary<string, object>
				{
					{"set_lock", 1},
					{"uuid", bookId},
                    {"group", bookmarkGroup},
				};

			BookmarksResponse response = null;
			Exception exception = null;
			try
			{
				response = await _client.GetBookmarks(parameters, cancellationToken);
			}
			catch( Exception e)
			{
				exception = e;
			}

			if( response != null )
			{
				_lock = response.LockId;

			    try
			    {
                    var allbookmarks = _dataCacheService.GetItem<XCollection<Bookmark>>(BookmarksCacheItemName) ?? new XCollection<Bookmark>();

			        if (allbookmarks != null)
			        {
			            allbookmarks.RemoveAll(bk => bk.Group.Equals("0"));
			        }

                    foreach (var bookmark in response.Bookmarks)
                    {
                        if (allbookmarks.FirstOrDefault(x => x.Id == bookmark.Id) == null)
                        {
                            allbookmarks.Add(bookmark);
                        }
                    }


			        var bookmarksForBook = allbookmarks.Derive(bk => bk.ArtId == bookId);
                    //bookmarksForBook.OrderBy(Bk => Bk.Selection);

                    //Check for errors

			        foreach (var bookmark in bookmarksForBook)
			        {
			            if (string.IsNullOrEmpty(bookmark.Class)) bookmark.Class = null;
			        }

                    _dataCacheService.PutItem(allbookmarks, BookmarksCacheItemName, cancellationToken);
              //      response.Bookmarks = allbookmarks.Derive(bk => bk.ArtId == bookId);

                    return bookmarksForBook;
			    }
			    catch (Exception ex){}
			}		
			return null;
		}

		public async Task<XCollection<Bookmark>> GetLocalBookmarksByDocumentId( string bookId, CancellationToken cancellationToken )
		{		
            var bookmarks =  _dataCacheService.GetItem<XCollection<Bookmark>>( BookmarksCacheItemName );
		    return bookmarks != null ? bookmarks.Derive(bk => bk.ArtId == bookId) : null;
		}

		public async Task<Bookmark> GetCurrentBookmarkByDocumentId( string bookId, bool local, CancellationToken cancellationToken )
		{
			XCollection<Bookmark> bookmarks = null;

			if( local )
			{
				bookmarks = await GetLocalBookmarksByDocumentId( bookId, cancellationToken );
			}
			else if( _networkAvailabilityService.NetworkAvailable )
			{
				bookmarks = await GetBookmarksByDocumentId( bookId, cancellationToken, true );
			}

			if( bookmarks != null )
			{
				var current = bookmarks.FirstOrDefault( x => x.Group == "0" );

				return current;
			}

			return null;
		}

		public void SetCurrentBookmarkByDocumentId( string bookId, Bookmark bookmark, CancellationToken cancellationToken )
		{
			if( _networkAvailabilityService.NetworkAvailable == true)
			{
				var task = new Task( async () =>
									{
										try
										{
											await AddBookmark( bookmark, cancellationToken );
										}
										catch( Exception )
										{
											
										}
									} );
				task.Start();
			}
		}

		public async Task<XCollection<Bookmark>> GetBookmarks( CancellationToken cancellationToken )
		{
			var parameters = new Dictionary<string, object>
				{
					{ "set_lock", 1 }
				};

			BookmarksResponse response = null;
			Exception exception = null;
			try
			{
				response = await _client.GetBookmarks( parameters, cancellationToken );
			}
			catch( Exception e )
			{
				exception = e;
			}

			if( response != null )
			{
				_lock = response.LockId;
				_dataCacheService.PutItem( response.Bookmarks, BookmarksCacheItemName, cancellationToken );
			}

			XCollection<Bookmark> bookmarks = null;
			if( response == null )
			{
				bookmarks =  _dataCacheService.GetItem<XCollection<Bookmark>>( BookmarksCacheItemName );
			}
			else
			{
				bookmarks = response.Bookmarks;
			}

			if( bookmarks == null && exception != null )
			{
				throw exception;
			}

			return bookmarks;
		}

		public async Task RemoveBookmarks( XCollection<Bookmark> bookmarks, CancellationToken cancellationToken )
		{
			if( bookmarks != null && bookmarks.Count > 0 )
			{
				string id = bookmarks[0].ArtId;

				XCollection<Bookmark> existbookmarks = await GetBookmarksByDocumentId( id, cancellationToken ) ?? (await GetLocalBookmarksByDocumentId(id, cancellationToken ) ?? new XCollection<Bookmark>());

				if( existbookmarks.Count > 0 )
				{
					for( int i = existbookmarks.Count - 1; i >= 0; i-- )
					{
						if( bookmarks.Any( x => x.Id == existbookmarks[i].Id ) )
						{
							existbookmarks.RemoveAt( i );
						}
					}
				}

				BookmarksResponse request = new BookmarksResponse();
				request.Bookmarks = existbookmarks;

				await SaveBookmarks( id, request, cancellationToken );
			}
		}

		public async Task AddBookmark( Bookmark bookmark, CancellationToken cancellationToken )
		{
			if (bookmark != null)
			{
				XCollection<Bookmark> bookmarks = await GetBookmarksByDocumentId( bookmark.ArtId, cancellationToken ) ?? new XCollection<Bookmark>();
                if (bookmarks.Count == 0) bookmarks = await GetLocalBookmarksByDocumentId(bookmark.ArtId, cancellationToken) ?? bookmarks;
			    
				if( bookmark.Group == "0" )
				{
					bookmarks.RemoveAll( x => x.Group == "0" );
				}

				bookmarks.RemoveAll( x => x.Selection == bookmark.Selection && x.Group == bookmark.Group );

				bookmarks.Insert( 0, bookmark );

				BookmarksResponse request = new BookmarksResponse();
				request.Bookmarks = bookmarks;

				await SaveBookmarks( bookmark.ArtId, request, cancellationToken );
			}
		}

	    private string CreateXmlFromRequest(BookmarksResponse request)
	    {
            request.LockId = _lock;
            //foreach (var bookmark in request.Bookmarks)
            //{
            //    bookmark.NoteText = new Note {Text = "12345"};
            //    bookmark.Percent = null;
            //}
            var serializer = new XmlSerializer(typeof(BookmarksResponse));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "http://www.gribuser.ru/xml/fictionbook/2.0/markup");
            namespaces.Add("fb", "http://www.gribuser.ru/xml/fictionbook/2.0");
            
            string data;
            using (var ms = new MemoryStream())
            {
                var xmlWriter = XmlWriter.Create(ms, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true, Encoding = Encoding.UTF8 });
                serializer.Serialize(xmlWriter, request, namespaces);
                ms.Seek(0, SeekOrigin.Begin);
                var sr = new StreamReader(ms);
                data = sr.ReadToEnd();
            }

            //XDocument document = new XDocument();
            //using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            //{
            //    using (var reader = XmlReader.Create(stream))
            //    {
            //        reader.MoveToContent();
            //        document.Add(XElement.ReadFrom(reader));
            //    }
            //}           
            //data = document.ToString(SaveOptions.DisableFormatting);
            data = data.Replace("\r\n", string.Empty);
	    //    data = EscapeUnicode(data);
	        return data;
	    }

        private static string EscapeUnicode(string input)
        {
            StringBuilder sb = new StringBuilder(input.Length);
            foreach (char ch in input)
            {
                if (ch <= 0x7f)
                    sb.Append(ch);
                else
                    sb.AppendFormat(CultureInfo.InvariantCulture, "\\u{0:x4}", (int)ch);
            }
            return sb.ToString();
        }

		private async Task SaveBookmarks( string artId, BookmarksResponse request, CancellationToken cancellationToken )
		{
		    var data = CreateXmlFromRequest(request);
			bool needNewLock = false;
            
			while( true )
			{
			    try
			    {
			        if (needNewLock || string.IsNullOrEmpty(_lock))
			        {
			            await GetBookmarksByDocumentId(artId, cancellationToken);
			        }

			        var parameters = new Dictionary<string, object>
			        {
			            {"data", data},
			            {"uuid", artId},
			            {"lock_id", _lock},
			            {"group", "0,1"},
			        };

			        await _client.AddBookmark(parameters, cancellationToken);
			    }
			    catch (CatalitBookmarksException e)
			    {
			        if ((e.ErrorCode == 4 || e.ErrorCode == 1) && !needNewLock)
			        {
			            needNewLock = true;
			            continue;
			        }
			        throw;
			    }
			    catch (Exception ex)
			    {
			        
			    }
				break;
			}

            XCollection<Bookmark> allbookmarks = _dataCacheService.GetItem<XCollection<Bookmark>>(BookmarksCacheItemName);
            allbookmarks = allbookmarks ?? new XCollection<Bookmark>();

            var existbookmarks = allbookmarks.Derive(art => art.ArtId == artId);
            var newbookmarks = request.Bookmarks.Derive(art => art.ArtId == artId);
            if (existbookmarks != null && newbookmarks != null)
		    {
                if (existbookmarks.Count != 0)
		        {
                    for (int i = existbookmarks.Count - 1; i >= 0; i--)
                    {
                        if (newbookmarks.FirstOrDefault(x => x.Id == existbookmarks[i].Id) == null)
                        {
                            allbookmarks.Remove(existbookmarks[i]);
                        }
                    }
		        }
		    }

            foreach (var bookmark in request.Bookmarks)
            {
                if (allbookmarks.FirstOrDefault(x => x.Id == bookmark.Id) == null)
                {
                    allbookmarks.Add(bookmark);
                }
            }
            _dataCacheService.PutItem(allbookmarks, BookmarksCacheItemName, cancellationToken);		    
		}

		public void Clear()
		{
			
		}
	}
}
