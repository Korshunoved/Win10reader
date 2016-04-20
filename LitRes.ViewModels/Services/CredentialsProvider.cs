using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;
using Cimbalino.Toolkit.Services;
using LitRes.Models;
using LitRes.Services.Connectivity;

namespace LitRes.Services
{
	internal class CredentialsProvider : ICredentialsProvider
	{
		const string CacheItemName = "credentials";
		private IDataCacheService _dataCacheService;

		#region Constructors/Disposer
		public CredentialsProvider(IDataCacheService dataCacheService)
		{
			_dataCacheService = dataCacheService;
		    //_applicationSettingsService = applicationSettingsService;
		}
        #endregion

        public static void DeleteDirectoryRecursively(IsolatedStorageFile storageFile, string directoryName)
        {
            var pattern = directoryName + @"\*";
            var files = storageFile.GetFileNames(pattern);
            foreach (var fileName in files)
            {
                storageFile.DeleteFile(Path.Combine(directoryName, fileName));
            }
            var dirs = storageFile.GetDirectoryNames(pattern);
            foreach (var dirName in dirs)
            {
                DeleteDirectoryRecursively(storageFile, Path.Combine(directoryName, dirName));
            }
            storageFile.DeleteDirectory(directoryName);
        }

        public async Task MigrateFromWp8ToWp10()
	    {
            var storage = IsolatedStorageFile.GetUserStoreForApplication();
	        if (storage.FileExists("__ApplicationSettings"))
	        {
	            try
	            {
	                using (var fileStream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync("__ApplicationSettings"))
	                {
	                    using (var streamReader = new StreamReader(fileStream))
	                    {
	                        var line = streamReader.ReadLine() ?? string.Empty;
	                        fileStream.Position = line.Length + Environment.NewLine.Length;
	                        var serializer = new DataContractSerializer(typeof (Dictionary<string, object>));
	                        var xmls = (Dictionary<string, object>) serializer.ReadObject(fileStream);

	                        foreach (var credentials in from xml in xmls
	                                                    where string.Equals(xml.Key, "credentials")
	                                                    let xmlSerializer = new XmlSerializer(typeof (CatalitCredentials))
	                                                    select (CatalitCredentials)xmlSerializer.Deserialize(new MemoryStream(Encoding.UTF8.GetBytes((string) xml.Value))))
	                        {
	                            RegisterCredentials(credentials, CancellationToken.None);
	                        }
	                    }
	                }
	                storage = IsolatedStorageFile.GetUserStoreForApplication();
	                storage.DeleteFile("__ApplicationSettings");
                    DeleteDirectoryRecursively(storage, "MyBooks");
	            }
	            catch
	            {
	                // ignored
	            }
	        }
	    }

	    public CatalitCredentials ProvideCredentials(CancellationToken cancellationToken)
	    {
            return  _dataCacheService.GetItem<CatalitCredentials>(CacheItemName);
	    }

        public void ForgetCredentialsRebill(CatalitCredentials credentials, CancellationToken cancellationToken)
        {
            credentials.CanRebill = "0";
            credentials.CreditCardLastNumbers = string.Empty;
            credentials.UserId = string.Empty;
            _dataCacheService.PutItem(credentials, CacheItemName, cancellationToken);
        }

		public void RegisterCredentials(CatalitCredentials credentials, CancellationToken cancellationToken)
		{
			_dataCacheService.PutItem( credentials, CacheItemName, cancellationToken );
		}

		public Task<bool> ShouldRetryAuthentication(CatalitCredentials credentials, CancellationToken cancellationToken)
		{
			return Task.FromResult(false);
		}
	}
}
