using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml;

namespace LitRes.Services
{
	internal class FileDownloadService : IFileDownloadService, IDisposable
	{
		private const string CacheFolderPath = "CachedFiles";
        
		private readonly IFileCacheService _fileCacheService;

		private readonly BackgroundWorker _worker;
		private readonly ManualResetEvent _waiter;

		private readonly List<string> _urls; 
		private readonly object _lockObject; 

		public FileDownloadService( IFileCacheService fileCacheService )
		{
			_fileCacheService = fileCacheService;

			_urls = new List<string>();
			_lockObject = new object();

			_waiter = new ManualResetEvent( false );

			_worker = new BackgroundWorker();
			_worker.DoWork += WorkerDoWork;
			_worker.WorkerSupportsCancellation = true;

			_worker.RunWorkerAsync();
		}

	    public async Task<Stream> DownloadFileSynchronouslyAndEncrypt(string url, string storedFilename, string password)
	    {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(storedFilename))
            {
                return null;
            }

            if (_fileCacheService.FileExists(storedFilename))
            {
                var file = await _fileCacheService.DecryptAndOpenFile(storedFilename, password, CancellationToken.None);

                return file;
            }

            var stream = await DownloadFile(url);
            stream.Seek(0, SeekOrigin.Begin);

            //var ms = new MemoryStream();
            //await stream.CopyToAsync(ms);

            //ms.Seek(0, SeekOrigin.Begin);
            stream.Seek(0, SeekOrigin.Begin);
            
           // Debug.WriteLine("MEMORY USAGE: {0} mb", DeviceStatus.ApplicationCurrentMemoryUsage / (1024 * 1024));

            await _fileCacheService.EncryptAndSaveFile(stream, storedFilename, password, CancellationToken.None);
            GC.Collect();
            //Debug.WriteLine("MEMORY USAGE: {0} mb", DeviceStatus.ApplicationCurrentMemoryUsage / (1024 * 1024));            
            stream = await _fileCacheService.DecryptAndOpenFile(storedFilename, password, CancellationToken.None);
            //Debug.WriteLine("MEMORY USAGE: {0} mb", DeviceStatus.ApplicationCurrentMemoryUsage / (1024 * 1024));

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
	    }

        public async Task<Stream> DownloadFileSynchronously(string url, string storedFilename)
	    {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(storedFilename))
            {
                return null;
            }

            if (_fileCacheService.FileExists(storedFilename))
            {
                var file = await _fileCacheService.OpenFile(storedFilename, CancellationToken.None);

                return file;
            }

            var stream = await DownloadFile(url);
            if (stream != null)
            {
                stream.Seek(0, SeekOrigin.Begin);

                var ms = new MemoryStream();
                await stream.CopyToAsync(ms);

                ms.Seek(0, SeekOrigin.Begin);
                stream.Seek(0, SeekOrigin.Begin);

                _fileCacheService.SaveFile(ms, storedFilename, CancellationToken.None);
            }
            return stream;
	    }

		public async Task<Stream> DownloadFileSynchronously( string url )
		{
			if( string.IsNullOrEmpty( url ) )
			{
				return null;
			}

			var filename = GetFilePathNameByUrl( url );

		    return await DownloadFileSynchronously(url, filename);
		}

		public void DownloadFileAsynchronously( string url )
		{
			if( !string.IsNullOrEmpty( url ) )
			{
				lock( _lockObject )
				{
					if( !_urls.Contains( url ) )
					{
						_urls.Add( url );
						_waiter.Set();
					}
				}
			}
		}

		public string GetFilePathNameByUrl( string url )
		{
			if( !string.IsNullOrEmpty( url ) )
			{
				string filename = MD5.GetMd5String( url );
				return Path.Combine( CacheFolderPath, filename );
			}

			return string.Empty;
		}

		public void Dispose()
		{
			_worker.CancelAsync();
			_waiter.Set();
		}

		async void WorkerDoWork( object sender, DoWorkEventArgs e )
		{
			while( !_worker.CancellationPending )
			{
				_waiter.WaitOne( 1000 );

				string url = string.Empty;
				lock( _lockObject )
				{
					if( _urls.Count > 0 )
					{
						url = _urls[0];
						_urls.RemoveAt( 0 );
					}
					if( _urls.Count == 0 )
					{
						_waiter.Reset();
					}
				}

				if( string.IsNullOrEmpty( url ) )
				{
					continue;
				}

				string filename = GetFilePathNameByUrl( url );
				if( !_fileCacheService.FileExists( filename ) )
				{
					Stream file = await DownloadFile( url );
					if( file != null )
					{
						file.Seek( 0, SeekOrigin.Begin );
						_fileCacheService.SaveFile( file, filename, CancellationToken.None );
					}
				}
			}
		}

		private async Task<Stream> DownloadFile( string url )
		{
			var request = (HttpWebRequest) WebRequest.Create( url );
			request.Method = "GET";

			return await GetDataAsync( request );
		}

		private async Task<Stream> GetDataAsync( HttpWebRequest request )
		{
			Task<WebResponse> task = Task.Factory.FromAsync(
				request.BeginGetResponse,
				asyncResult =>
				{
					try
					{
						return request.EndGetResponse( asyncResult );
					}
					catch( WebException )
					{
						return null;
					}
				},
				null );

			var returnTask = await task.ContinueWith( t =>
				   ReadStreamFromResponse( t.Result ) );

			return await returnTask;
		}

		private async Task<Stream> ReadStreamFromResponse( WebResponse result )
		{
			if( result == null )
			{
				return null;
			}

			MemoryStream ms = new MemoryStream();

			using( Stream stream = result.GetResponseStream() )
			{
				await stream.CopyToAsync( ms );

				await ms.FlushAsync();
			}

			return ms;
		}
	}
}
