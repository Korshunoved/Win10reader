using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace LitResReadW10.ValueConverters
{
    public class CacheImageFileConverter
    {
        private static IsolatedStorageFile _storage = IsolatedStorageFile.GetUserStoreForApplication();
        private const string imageStorageFolder = "TempImages";

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string path = value as string;
            if (String.IsNullOrEmpty(path)) return null;
            Uri imageFileUri = new Uri(path);
            if (imageFileUri.Scheme == "http" || imageFileUri.Scheme == "https")
            {
                //if (!Phone.GetIsNetworkAvailable())
                {
                    return _storage.FileExists(GetFileNameInIsolatedStorage(imageFileUri)) ? ExtractFromLocalStorage(imageFileUri) : LoadDefaultIfPassed(imageFileUri, (parameter ?? string.Empty).ToString());
                }
               // else
                {
                   // return DownloadFromWeb(imageFileUri);
                }
            }
            else
            {
                BitmapImage bm = new BitmapImage(imageFileUri);
                return bm;
            }
        }

        private static object LoadDefaultIfPassed(Uri imageFileUri, string defaultImagePath)
        {
            string defaultImageUri = (defaultImagePath ?? string.Empty).ToString();
            if (!string.IsNullOrEmpty(defaultImageUri))
            {
                BitmapImage bm = new BitmapImage(new Uri(defaultImageUri, UriKind.Relative));         //Load default Image
                return bm;
            }
            else
            {
                BitmapImage bm = new BitmapImage(imageFileUri);
                return bm;
            }
        }

        //private static object DownloadFromWeb(Uri imageFileUri)
        //{
        //    WebClient m_webClient = new WebClient();                                //Load from internet
        //    BitmapImage bm = new BitmapImage();

        //    m_webClient.OpenReadCompleted += (o, e) =>
        //    {
        //        if (e.Error != null || e.Cancelled) return;
        //        WriteToIsolatedStorage(IsolatedStorageFile.GetUserStoreForApplication(), e.Result, GetFileNameInIsolatedStorage(imageFileUri));
        //        bm.SetSource(e.Result);
        //        e.Result.Close();
        //    };
        //    m_webClient.OpenReadAsync(imageFileUri);
        //    return bm;
        //}

        private static object ExtractFromLocalStorage(Uri imageFileUri)
        {
            string isolatedStoragePath = GetFileNameInIsolatedStorage(imageFileUri);       //Load from local storage
            using (var sourceFile = _storage.OpenFile(isolatedStoragePath, FileMode.Open, FileAccess.Read))
            {
                BitmapImage bm = new BitmapImage();                
               // bm.SetSource(sourceFile);
                return bm;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static void WriteToIsolatedStorage(IsolatedStorageFile storage, System.IO.Stream inputStream, string fileName)
        {
            IsolatedStorageFileStream outputStream = null;
            try
            {
                if (!storage.DirectoryExists(imageStorageFolder))
                {
                    storage.CreateDirectory(imageStorageFolder);
                }
                if (storage.FileExists(fileName))
                {
                    storage.DeleteFile(fileName);
                }
                outputStream = storage.CreateFile(fileName);
                byte[] buffer = new byte[32768];
                int read;
                while ((read = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    outputStream.Write(buffer, 0, read);
                }
                outputStream.Dispose();
            }
            catch
            {
                //We cannot do anything here.
                outputStream?.Dispose();
            }
        }

        /// <summary>
        /// Gets the file name in isolated storage for the Uri specified. This name should be used to search in the isolated storage.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        public static string GetFileNameInIsolatedStorage(Uri uri)
        {
            return imageStorageFolder + "\\" + uri.AbsoluteUri.GetHashCode() + ".img";
        }

    }
}
