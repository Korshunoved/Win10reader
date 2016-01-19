using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace LitRes.Services
{
    public interface IFileCacheService
    {
        bool FileExists(string path);
        bool FolderExists(string path);
        Task<Stream> OpenFile(string path, CancellationToken cancellationToken);
        void SaveFile(Stream file, string path, CancellationToken token);
        void ExtractFile(Stream file, string path, CancellationToken token);
        Task EncryptAndSaveFile(Stream file, string path, string password, CancellationToken token);
        Task<Stream> DecryptAndOpenFile(string path, string password, CancellationToken cancellationToken);
        void DeleteFile(string path);
        void ClearStorage(CancellationToken cancellationToken);
    }
}
