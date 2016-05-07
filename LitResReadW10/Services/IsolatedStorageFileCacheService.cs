using System;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.VirtualFileSystem;
using ICSharpCode.SharpZipLib.Zip;
using LitResReadW10.Crypto;
using FileAttributes = ICSharpCode.SharpZipLib.VirtualFileSystem.FileAttributes;

namespace LitRes.Services
{
    class IsolatedStorageFileCacheService : IFileCacheService
    {
        public bool FileExists(string path)
        {
            using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                return isf.FileExists(path);
            }
        }

        public bool FolderExists(string path)
        {
            using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                return isf.DirectoryExists(path);
            }
        }

        public void ClearStorage(CancellationToken cancellationToken)
        {
            using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (isf.DirectoryExists("CachedFiles"))
                {
                    var files = isf.GetFileNames("CachedFiles\\*");

                    foreach (var file in files)
                    {
                        var path = Path.Combine("CachedFiles", file);
                        if (isf.FileExists(path)) isf.DeleteFile(path);
                    }
                }

                if (isf.DirectoryExists("MyBooks"))
                {
                    var bookfolders = isf.GetDirectoryNames("MyBooks\\*");

                    foreach (var folder in bookfolders)
                    {
                        var pattern = "MyBooks\\" + folder + "\\*";
                        var bookFiles = isf.GetFileNames(pattern);
                        foreach (var bookFile in bookFiles)
                        {
                            var bookPath = Path.Combine("MyBooks\\" + folder + "\\" + bookFile);
                            if (isf.FileExists(bookPath))
                            {
                                try
                                {
                                    isf.DeleteFile(bookPath);
                                }
                                catch (Exception e)
                                {
                                    Debug.Write(e);
                                }
                            }
                        }
                        var path = Path.Combine("MyBooks", folder);
                        if (isf.DirectoryExists(path)) isf.DeleteDirectory(path);
                    }
                }
            }
        }

        public void DeleteFile(string path)
        {
            try
            {
                using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (isf.DirectoryExists(path)) DeleteDirectoryRecursively(isf, path);
                    else isf.DeleteFile(path);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void DeleteDirectoryRecursively(IsolatedStorageFile storageFile, String dirName)
        {
            string pattern = dirName + @"\*";
            string[] files = storageFile.GetFileNames(pattern);
            foreach (var fName in files)
            {
                storageFile.DeleteFile(Path.Combine(dirName, fName));
            }
            string[] dirs = storageFile.GetDirectoryNames(pattern);
            foreach (var dName in dirs)
            {
                DeleteDirectoryRecursively(storageFile, Path.Combine(dirName, dName));
            }
            storageFile.DeleteDirectory(dirName);
        }

        public async Task<Stream> OpenFile(string path, CancellationToken cancellationToken)
        {            
            using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var inputStream = isf.OpenFile(path, FileMode.Open, FileAccess.Read))
                {
                    var outputStream = new MemoryStream();

                    await inputStream.CopyToAsync(outputStream, 4096, cancellationToken);
                    await outputStream.FlushAsync(cancellationToken);

                    outputStream.Seek(0, SeekOrigin.Begin);

                    return outputStream;
                }
            }
        }

        public void SaveFile(Stream streamToSave, string path, CancellationToken cancellationToken)
        {
            if (streamToSave == null) { return; }

            Task.Run(() =>
            {
                try
                {
                    using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        var directoryPath = Path.GetDirectoryName(path);

                        if (!isf.DirectoryExists(directoryPath))
                        {
                            isf.CreateDirectory(directoryPath);
                        }
                        
                        using (var fileStream = isf.OpenFile(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                        {
                
                            streamToSave.CopyTo(fileStream, 2048);
                           
                            fileStream.Flush();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                finally
                {
                    streamToSave.Dispose();
                }
            }, cancellationToken);
        }

        public async Task EncryptAndSaveFile(Stream streamToSave, string path, string password, CancellationToken cancellationToken)
        {
            if (streamToSave == null) { return; }

            await Task.Run(() =>
            {
                try
                {
                    using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        var directoryPath = Path.GetDirectoryName(path);

                        if (!isf.DirectoryExists(directoryPath))
                        {
                            isf.CreateDirectory(directoryPath);
                        }

                        using (var fileStream = isf.OpenFile(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                        {
                            Encrypt(streamToSave, fileStream, password);
                            fileStream.Flush();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                finally
                {
                    streamToSave.Dispose();
                }
            }, cancellationToken);
        }

        public async Task<Stream> DecryptAndOpenFile(string path, string password, CancellationToken cancellationToken)
        {
            using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var inputStream = isf.OpenFile(path, FileMode.Open, FileAccess.Read))
                {
                    try
                    {
                        //var outputStream = new MemoryStream();

                        //await inputStream.CopyToAsync(outputStream, 4096, cancellationToken);
                        //await outputStream.FlushAsync(cancellationToken);

                        //outputStream.Seek(0, SeekOrigin.Begin);

                        return Decrypt(inputStream, password);
                    }
                    catch (Exception ex)
                    {
                        ex = ex;
                    }
                    return null;
                }
            }
        }
#warning ENCRYPT_DOSENT_IMPLEMENT
        public void Encrypt(Stream dataToEncrypt, IsolatedStorageFileStream fileStream, string password, string salt = "jklkljasb)0_3;22A,xA")
        {
            //AesManaged aes = null;
            //CryptoStream cryptoStream = null;
            //try
            //{
            //    Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt));
            //    aes = new AesManaged();
            //    aes.Key = rfc2898.GetBytes(aes.KeySize / 8);
            //    aes.IV = rfc2898.GetBytes(aes.BlockSize / 8);
            //    cryptoStream = new CryptoStream(fileStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            //    do
            //    {
            //        var data = new byte[1024];
            //        int readBytes = dataToEncrypt.Read(data, 0, 1024);
            //        if (readBytes != 0)
            //        {
            //            cryptoStream.Write(data, 0, readBytes);
            //        }
            //        else
            //        {
            //            break;
            //        }

            //    } while (true);
            //    cryptoStream.FlushFinalBlock();
            //}
            //catch (Exception ex)
            //{
            //    ex = ex;
            //}
            //finally
            //{
            //    if (aes != null)
            //        aes.Clear();
            //}
        }
#warning DECRYPT_DOSENT_IMPLEMENT
        public Stream Decrypt(Stream dataToDecrypt, string password, string salt = "jklkljasb)0_3;22A,xA")
        {
            return dataToDecrypt;
            //AesManaged aes = null;
            //MemoryStream memoryStream = null;
            //CryptoStream cryptoStream = null;

            //try
            //{
            //    Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt));

            //    aes = new AesManaged();
            //    aes.Key = rfc2898.GetBytes(aes.KeySize / 8);
            //    aes.IV = rfc2898.GetBytes(aes.BlockSize / 8);

            //    memoryStream = new MemoryStream();
            //    cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write);
            //    //byte[] data = ((MemoryStream) dataToDecrypt).ToArray();
            //    //cryptoStream.Write(data, 0, data.Length);

            //    //cryptoStream.FlushFinalBlock();

            //    do
            //    {
            //        var data = new byte[1024];
            //        int readBytes = dataToDecrypt.Read(data, 0, 1024);
            //        if (readBytes != 0)
            //        {
            //            cryptoStream.Write(data, 0, readBytes);
            //            data = null;
            //        }
            //        else
            //        {
            //            break;
            //        }

            //    } while (true);
            //    cryptoStream.FlushFinalBlock();

            //    memoryStream.Seek(0, SeekOrigin.Begin);
            //    return memoryStream;
            //}
            //catch (Exception ex)
            //{
            //    ex = ex;
            //}
            //finally
            //{
            //    if (aes != null)
            //        aes.Clear();
            //}
            //return null;
        }

        public void ExtractFile(Stream file, string path, CancellationToken token)
        {
            VFS.SetCurrent(new UwpFileSystem());
            FastZip fastZip = new FastZip();

            var extractToDir = Path.Combine(ApplicationData.Current.LocalFolder.Path, path);
            fastZip.ExtractZip(file, extractToDir, FastZip.Overwrite.Always, (name => true),null,null,true,false);
            EncryptAllBookDataInDirectory(extractToDir, token);
        }

        private void EncryptAllBookDataInDirectory(string extractToDir, CancellationToken token)
        {
            Task.WaitAll(Task.Run(async () =>
            {
                try
                {
                    var folder = await StorageFolder.GetFolderFromPathAsync(extractToDir);
                    var files = await folder.GetFilesAsync();
                    foreach (var storageFile in files)
                    {
                        IBuffer encodedBuffer = null;
                        using (var originalFileStream = await storageFile.OpenReadAsync())
                        {
                            var dataReader = new DataReader(originalFileStream);
                            var streamSize = (uint)originalFileStream.Size;
                            await dataReader.LoadAsync(streamSize);
                            var buffer = dataReader.ReadBuffer(streamSize);
                            encodedBuffer = EncryptionProvider.Encrypt(buffer);
                        }

                        if (encodedBuffer != null)
                        {
                            var fileName = storageFile.Name;
                            var encryptedFile = await folder.CreateFileAsync($"tmp_{fileName}");

                            using (var encryptedFileStream = await encryptedFile.OpenStreamForWriteAsync())
                            {
                                var dataToWrite = encodedBuffer.ToArray();
                                await encryptedFileStream.WriteAsync(dataToWrite, 0, dataToWrite.Length, token);
                            }

                            await storageFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                            await encryptedFile.RenameAsync(fileName, NameCollisionOption.ReplaceExisting);
                            Debug.WriteLine($"{fileName} ENCRYPTED");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }, token));
        }

        class UwpFileSystem : IVirtualFileSystem
        {
            class ElementInfo : IVfsElement
            {
                protected FileSystemInfo Info;
                public ElementInfo(FileSystemInfo info)
                {
                    Info = info;
                }

                public string Name
                {
                    get { return Info.Name; }
                }

                public bool Exists
                {
                    get { return Info.Exists; }
                }

                public FileAttributes Attributes
                {
                    get
                    {
                        FileAttributes attrs = 0;
                        if (Info.Attributes.HasFlag(Windows.Storage.FileAttributes.Normal)) attrs |= FileAttributes.Normal;
                        if (Info.Attributes.HasFlag(Windows.Storage.FileAttributes.ReadOnly)) attrs |= FileAttributes.ReadOnly;
                      //  if (Info.Attributes.HasFlag(Windows.Storage.FileAttributes.Hidden)) attrs |= FileAttributes.Hidden;
                        if (Info.Attributes.HasFlag(Windows.Storage.FileAttributes.Directory)) attrs |= FileAttributes.Directory;
                        if (Info.Attributes.HasFlag(Windows.Storage.FileAttributes.Archive)) attrs |= FileAttributes.Archive;

                        return attrs;
                    }
                }

                public DateTime CreationTime
                {
                    get { return Info.CreationTime; }
                }

                public DateTime LastAccessTime
                {
                    get { return Info.LastAccessTime; }
                }

                public DateTime LastWriteTime
                {
                    get { return Info.LastWriteTime; }
                }
            }
            class DirInfo : ElementInfo, IDirectoryInfo
            {
                public DirInfo(DirectoryInfo dInfo)
                    : base(dInfo)
                {
                }
            }
            class FilInfo : ElementInfo, IFileInfo
            {
                protected FileInfo FInfo { get { return (FileInfo)Info; } }
                public FilInfo(FileInfo fInfo)
                    : base(fInfo)
                {
                }
                public long Length
                {
                    get { return FInfo.Length; }
                }
            }

            public System.Collections.Generic.IEnumerable<string> GetFiles(string directory)
            {
                using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    return isf.GetFileNames(directory);
                }
            }

            public System.Collections.Generic.IEnumerable<string> GetDirectories(string directory)
            {
                using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    return isf.GetDirectoryNames(directory);
                }
            }

            public string GetFullPath(string path)
            {
                return Path.GetFullPath(path);
            }

            public IDirectoryInfo GetDirectoryInfo(string directoryName)
            {
                return new DirInfo(new DirectoryInfo(directoryName));
            }

            public IFileInfo GetFileInfo(string filename)
            {
                return new FilInfo(new FileInfo(filename));
            }

            public void SetLastWriteTime(string name, DateTime dateTime)
            {
               // File.SetLastWriteTime(name, dateTime);
            }

            public void SetAttributes(string name, FileAttributes attributes)
            {
                
                //Windows.Storage.FileAttributes attrs = 0;
                //if (attributes.HasFlag(FileAttributes.Normal)) attrs |= Windows.Storage.FileAttributes.Normal;
                //if (attributes.HasFlag(FileAttributes.ReadOnly)) attrs |= Windows.Storage.FileAttributes.ReadOnly;
                //if (attributes.HasFlag(FileAttributes.Hidden)) attrs |= Windows.Storage.FileAttributes.Hidden;
                //if (attributes.HasFlag(FileAttributes.Directory)) attrs |= Windows.Storage.FileAttributes.Directory;
                //if (attributes.HasFlag(FileAttributes.Archive)) attrs |= Windows.Storage.FileAttributes.Archive;
               
                //File.SetAttributes(name, attrs);
            }


            public void CreateDirectory(string directory)
            {
                using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if(!isf.DirectoryExists(directory)) isf.CreateDirectory(directory);
                }
            }

            public string GetTempFileName()
            {
                return Path.GetTempFileName();
            }

            public void CopyFile(string fromFileName, string toFileName, bool overwrite)
            {
                using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    isf.CopyFile(fromFileName, toFileName, overwrite);
                }
            }

            public void MoveFile(string fromFileName, string toFileName)
            {
                using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    isf.MoveFile(fromFileName, toFileName);
                }
            }

            public void DeleteFile(string fileName)
            {
                using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    isf.DeleteFile(fileName);
                }
            }

            public VfsStream CreateFile(string filename)
            {
                using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    return new VfsProxyStream(isf.CreateFile(filename), filename);
                }
            }

            public VfsStream OpenReadFile(string filename)
            {
                using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    return new VfsProxyStream(isf.OpenFile(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.Read),filename);
                }
            }

            public VfsStream OpenWriteFile(string filename)
            {
                using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    return new VfsProxyStream(isf.OpenFile(filename, FileMode.Open, FileAccess.Write, FileShare.Write), filename);
                }
            }

            public string CurrentDirectory
            {
                get { return ApplicationData.Current.LocalFolder.Path; }
            }

            public char DirectorySeparatorChar
            {
                get { return Path.DirectorySeparatorChar; }
            }
        }
    }
}
