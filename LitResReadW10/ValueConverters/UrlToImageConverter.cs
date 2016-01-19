using System;
using System.IO;
using System.IO.IsolatedStorage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Autofac;

using LitRes.Services;
using LitResReadW10;

namespace LitRes.ValueConverters
{
	public class UrlToImageConverter : ConverterBase<string, ImageSource>
	{
		private readonly IFileDownloadService _fileDownloadService;

        public delegate void UrlToImageConverterImageLoadedCallback(BitmapImage img);

        private UrlToImageConverterImageLoadedCallback onImgLoaded;
		public UrlToImageConverter()
		{
			_fileDownloadService = ((App) Application.Current).Scope.Resolve<IFileDownloadService>();
		}

        public UrlToImageConverter(UrlToImageConverterImageLoadedCallback callback)
        {
            onImgLoaded = callback;
            _fileDownloadService = ((App)Application.Current).Scope.Resolve<IFileDownloadService>();
        }

		public override object Convert( string value, object parameter, string language)
		{
			BitmapImage image = new BitmapImage();

		    if (value.Contains("http"))
		    {
		        _fileDownloadService.DownloadFileSynchronously(value).ContinueWith(t =>
		        {
		            if (t.Exception == null)
		            {
		                SetImage(t.Result, image);
		            }
		        });
		    }
		    else
		    {
		        try
		        {
		            using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
		            {
                        using (var fileStream = isf.OpenFile(value, FileMode.Open, FileAccess.Read, FileShare.Read))
		                {
                            SetImage(fileStream, image, false);
                        }                        
		            }
		        }
		        catch (Exception)
		        {
		            
		        }
		    }

		    return image;
		}

		private async void SetImage( Stream result, BitmapImage image, bool isAsync = true )
		{
			if( result != null && result.Length > 0 )
			{
                IRandomAccessStream inMemoryStream = new InMemoryRandomAccessStream();
                using (var inputStream = result.AsInputStream())
                {
                    await RandomAccessStream.CopyAsync(inputStream, inMemoryStream);
                }
                inMemoryStream.Seek(0);
                
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, (()=>
			    {
                    image.SetSource(inMemoryStream);
                    onImgLoaded?.Invoke(image);
                }));
               
                //if (onImgLoaded != null) onImgLoaded(image);

                //if (isAsync) Deployment.Current.Dispatcher.BeginInvoke( () => { image.SetSource( result ); if(onImgLoaded != null) onImgLoaded(image); });
                //else
                //{
                //    image.SetSource(result); 
                //    if (onImgLoaded != null) onImgLoaded(image);
                //}
			}
		}
	}
}
