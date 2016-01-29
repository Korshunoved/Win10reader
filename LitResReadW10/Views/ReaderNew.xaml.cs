using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Autofac;
using Digillect.Mvvm;
using Digillect.Mvvm.UI;
using LitRes.Services;
using LitRes.ViewModels;
using LitRes.Views;
//using LitRes.LiveTileAgent;
using FB2Library;
using LitRes.BookReader;
using LitRes.Helpers;


namespace LitResReadW10.Views
{
    [View("ReaderNew")]
    [ViewParameter("bookmark", Required = false)]
    [ViewParameter("chapter", Required = false)]
    [ViewParameter("BookEntity", typeof(LitRes.Models.Book), Required = true)]
    [ViewParameter("filetoken", Required = false)]

    public partial class ReaderNew
    {
        private BookReadingContext _cacheItem;
        private int _moveCount;

        public ReaderNew()
        {
            InitializeComponent();
           // LoadBook();
          //  LoadFb2File(new Uri("ms-appx:///Assets/Book.fb2", UriKind.Absolute).ToString());
            Loaded += ReaderLoaded;
        }

        #region ReaderLoaded
        async void ReaderLoaded(object sender, RoutedEventArgs e)
        {
            _moveCount = 0;
            await ViewModel.LoadSettings();
            var folder = ViewModel.BookFolderName;
       //    FB2Library.FB2File book = new FB2File();
            BookReadingContext book = new BookReadingContext();
           
           // book.Load();
        }
        #endregion

        #region CreateDataSession
        protected override Session CreateDataSession(DataLoadReason reason)
        {
            ViewModel.Id = ViewParameters.GetValue<int>("id");
            ViewModel.FileToken = ViewParameters.GetValue<string>("filetoken");

            ViewModel.ReaderSettings.PropertyChanged -= ReaderSettingsUpdated;
            ViewModel.ReaderSettings.PropertyChanged += ReaderSettingsUpdated;

          //  ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
           // ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.DeffaultSettings = (ResolutionHelper.isFullHD) ? ReaderSettingsViewModel.DeffaultSettingsType.DeffaultSettingsTypeHD : ReaderSettingsViewModel.DeffaultSettingsType.DeffaultSettingsTypeNormal;
            Debug.WriteLine("Reader CreateDataSession");

            return base.CreateDataSession(reason);
        }
        #endregion


        #region ReaderSettingsUpdated
        void ReaderSettingsUpdated(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("OnUpdated"))
            {
               // Center.Visibility = Visibility.Visible;
               // ApplyReaderSettings();
            }
        }
        #endregion

        private async void LoadBook()
        {
            // Create or overwrite file target file in local app data folder
            var fileToWrite = await ApplicationData.Current.LocalFolder.CreateFileAsync("Book.fb2", CreationCollisionOption.ReplaceExisting);
            // Open file in application package
            var fileToRead = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Book.fb2", UriKind.Absolute));

            byte[] buffer = new byte[1024];
            using (BinaryWriter fileWriter = new BinaryWriter(await fileToWrite.OpenStreamForWriteAsync()))
            {
                using (BinaryReader fileReader = new BinaryReader(await fileToRead.OpenStreamForReadAsync()))
                {
                    long readCount = 0;
                    while (readCount < fileReader.BaseStream.Length)
                    {
                        int read = fileReader.Read(buffer, 0, buffer.Length);
                        readCount += read;
                        fileWriter.Write(buffer, 0, read);
                    }
                }
            }
        }

        private async void LoadFb2File(string fileName)
        {
            try
            {

                Stream s = File.Open("Assets/Book.fb2", FileMode.Open);
                ReadFB2FileStream(s);

            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
        }

        private void ReadFB2FileStream(Stream s)
        {
            XmlReaderSettings settings = new XmlReaderSettings
            {
                // ValidationType = ValidationType.None,
                // ProhibitDtd = false
            };
            XDocument fb2Document = null;
            using (XmlReader reader = XmlReader.Create(s, settings))
            {
                fb2Document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
            }
            FB2File file = new FB2File();
            
            try
            {
                file.Load(fb2Document, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Error loading file : {0}", ex.Message));
            }
        }

        /* public async void LoadBook()
         {
             var orient = DisplayInformation.GetForCurrentView().CurrentOrientation;
             if (_cacheItem == null)
             {
                 var cache = ((App)Application.Current).Scope.Resolve<IBookReadingContextService>();
                 if (cache.HasContext(ViewModel.Entity.Id) &&
                     cache.GetContext(ViewModel.Entity.Id).Orientation == orient)
                 {
                     _cacheItem = cache.GetContext(ViewModel.Entity.Id);
                     _cacheItem.Reader.OnMoveTo = Reader_onMoveTo;
                 }
                 else
                 {
                     cache.Clear();


                         if (ViewModel != null)
                         {
                             //AddBuySection(ViewModel.Entity.Price);
                            // var reader = new Athenaeum.BookReader(new DrawingContext(), ViewModel.Document,
                            // bounds);
                           //  _cacheItem = new BookReadingContext { Reader = reader };
                             cache.SetContext(ViewModel.Entity.Id, _cacheItem);
                             _cacheItem.Reader.OnMoveTo = Reader_onMoveTo;
                         }

                 }
             }
         }*/

        /*  async void Reader_onMoveTo(object sender)
          {
              if (sender is BookChaptersViewModel)
              {
                 // await ReformatBook();
              }
          }*/

        /*   private async Task ReformatBook(DisplayOrientations pageOrient = 0)
           {
               if (ViewModel.Entity.TypeBook == Models.Book.BookType.Pdf)
               {
                  // ShowChaptersButton.IsEnabled = ToBookmarksButton.IsEnabled = AllBookmarksButton.IsEnabled = false;
                  // ReformatPdfBook(pageOrient);
               }
               else
               {
                   await ReformatFb2Book(pageOrient);
               }
           }*/
    }
}
