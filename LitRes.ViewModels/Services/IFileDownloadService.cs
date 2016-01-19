using System;
using System.IO;
using System.Threading.Tasks;

namespace LitRes.Services
{
	public interface IFileDownloadService
	{
		Task<Stream> DownloadFileSynchronously( string url );
        Task<Stream> DownloadFileSynchronously(string url, string storedFilename);
        Task<Stream> DownloadFileSynchronouslyAndEncrypt(string url, string storedFilename, string password);

		void DownloadFileAsynchronously( string url );
		string GetFilePathNameByUrl( string url );
	}
}
