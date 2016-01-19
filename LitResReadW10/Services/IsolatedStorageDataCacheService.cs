using System;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;
using Digillect.Collections;
using LitRes.Models;

namespace LitRes.Services
{
	public class IsolatedStorageDataCacheService : IDataCacheService
	{
		private static readonly object LockObject = new object();

		public async Task<DateTime> GetItemModificationDate(string name)
		{
			return await Task.Run( () =>
				{
				    using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
				    {
				        return !isf.FileExists(name) ? DateTime.MinValue : isf.GetCreationTime(name).DateTime;
				    }
				} );
		}

		//public async Task<T> GetItem<T>(string name, CancellationToken cancellationToken)
		//{
		//	return await Task.Run(() => GetItem<T>( name ), cancellationToken);
		//}

		public T GetItem<T>(string name)
		{
		    using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
		    {
		        if (!isf.FileExists(name)) return default(T);

		        using (var file = isf.OpenFile(name, FileMode.Open, FileAccess.Read))
		        {
		            using (var ms = new MemoryStream())
		            {
		                file.CopyTo(ms);
		                ms.Seek(0, SeekOrigin.Begin);
		                var xml = new XmlSerializer(typeof (T));
		                using (TextReader tr = new StreamReader(ms))
		                {
		                    var obj = xml.Deserialize(tr);

		                    if (obj is T)
		                    {
		                        return (T) obj;
		                    }
		                }
		            }
		        }
                isf.Dispose();
		    }
            return default(T);
		}

		public void PutItem<T>(T item, string name, CancellationToken cancellationToken)
		{
            //Task task = new Task(() => PutItem(item, name), cancellationToken);

            //task.Start();
		    PutItem(item, name);
		}

		private void PutItem<T>(T item, string name)
		{
			lock (LockObject)
			{
                try
                {
                    var xml = new XmlSerializer( typeof( T ) );
                    byte[] buffer = null;
			        if( item != null  )
			        {
				        using( var ms = new MemoryStream() )
				        {
					        xml.Serialize( ms, item );
					        ms.Seek( 0, SeekOrigin.Begin );
                            buffer = new byte[ms.Length];
                            ms.Read(buffer,0,buffer.Length);
				        }
			        }

                    if(buffer == null) return;
                
                    if (IsFileExists(name)) DeleteFile(name);
                    CreateFile(name, buffer);
                }
			    catch (Exception ex)
			    {
			        Debug.WriteLine(ex.Message);
			    }
            }
        }

	    private bool IsFileExists(string name)
	    {
	        bool isFileExists = false;
	        using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
	        {
	            try
	            {
	                isFileExists = isf.FileExists(name);
	                isf.Dispose();
	            }
	            catch (Exception ex)
	            {
	                Debug.WriteLine(ex.Message);
	            }
	        }
	        return isFileExists;
	    }

	    private void DeleteFile(string name)
	    {
            using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    isf.DeleteFile(name);
                    isf.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        private void CreateFile(string name, byte[] buffer)
        {
            using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    using (var newFile = isf.CreateFile(name))
                    {
                        newFile.Write(buffer, 0, buffer.Length);
                        newFile.Flush();
                        newFile.Dispose();
                    }
                    isf.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        public void ClearCache(CancellationToken cancellationToken)
        {
            lock (LockObject)
            {
                var existsBooks = GetItem<XCollection<Book>>("ExistBooks");
                bool isNotFirstRun = ApplicationData.Current.LocalSettings.Values.ContainsKey("isWelcomeScreenShowed");
                ApplicationData.Current.LocalSettings.Values.Clear();
                ClearAllConfigFiles();
                if (isNotFirstRun)
                {
                    ApplicationData.Current.LocalSettings.Values.Add("isWelcomeScreenShowed", true);
                    //if (existsBooks == null || existsBooks.Count == 0) IsolatedStorageSettings.ApplicationSettings.Save();
                }

                if (existsBooks != null && existsBooks.Count > 0)
                {
                    PutItem(existsBooks, "ExistBooks");
                    //IsolatedStorageSettings.ApplicationSettings.Save();
                }
            }
        }

        private void ClearAllConfigFiles()
        {
            using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var fileNames = isf.GetFileNames();
                foreach (var fileName in fileNames)
                {
                    if (isf.FileExists(fileName))
                    {
                        try
                        {
                            isf.DeleteFile(fileName);
                        }
                        catch (Exception ex)
                        {
                            ex = ex;
                            Debug.WriteLine(ex.Message);
                        }
                    }
                }
                isf.Dispose();
            }
        }
    }
}