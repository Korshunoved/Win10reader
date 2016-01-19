using System;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Digillect.Collections;
using Digillect.Mvvm.UI;
using LitRes.Models;
using LitRes.ValueConverters;

namespace LitRes.Controls
{
    class BannerControl:    Object
    {
        private double posX;
        private int loadedImagesCounter;
        private int iteration;
        private Image BannerImage;
        private Image BannerImage2;
        private XCollection<Banner> Banners;
        private DispatcherTimer timer;
        private BitmapImage temp;
        private bool isEnabled;

        public bool Enabled
        {
            get
            {
                return isEnabled;
            }
            set
            {
                isEnabled = value;
                BannerImage.Visibility = (Visibility)new ObjectToVisibilityConverter().Convert(isEnabled, null, null, string.Empty);
                BannerImage2.Visibility = BannerImage.Visibility;

                if (Enabled) Continue();
                else Pause();
            }
        }

        public BannerControl(Image bannerImage, Image bannerImage2, XCollection<Banner> Banners)
        {
            posX = 0.0;
            loadedImagesCounter = 0;
            iteration = 0;
            this.BannerImage = bannerImage;
            this.BannerImage2 = bannerImage2;
            this.Banners = Banners;
            
            //  BannerImage.Children.Clear();

            bannerImage.Tapped += onBannerTap;
            bannerImage.ImageOpened += Banner_Image_Opened;
            bannerImage.Tag = Banners[iteration];
            var img = new BitmapImage(new Uri(Banners[iteration].Image, UriKind.RelativeOrAbsolute));
            bannerImage.CacheMode = new BitmapCache();
            bannerImage.Source = img;
            ++iteration;
            //foreach (Banner banner in Banners)
            //{
            //    var BannerBitmap = new BitmapImage();
            //    BannerBitmap.UriSource = new Uri(banner.Image, UriKind.RelativeOrAbsolute);

            //    var BannerImage = new Image();
            //    BannerImage.Source = BannerBitmap;       
            //    BannerImage.Stretch = Stretch.Fill;
            //    //TiltEffect.SetIsTiltEnabled(BannerImage, true);                
            //    BannerImage.Tapped += onBannerTap;
            //    BannerImage.ImageOpened += Banner_Image_Opened;
            //    BannerImage.Tag = banner;

            //    BannerImage.Visibility = Visibility.Visible;
            //    BannerImage.Visibility = Visibility.Collapsed;
            //    BannerImage.Children.Add(BannerImage);
            //}
        }
        public void Pause()
        {
            if (timer != null && timer.IsEnabled) timer.Stop();
        }
        public void Continue()
        {
            if (timer != null && !timer.IsEnabled) timer.Start();
            else
            {
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(10000);
                timer.Tick += (s, e) =>
                {
                    StartAnimation();
                };
                timer.Start();
              }
        }
        private void StartAnimation()
        {

            //if (loadedImagesCounter > 1 && iteration == loadedImagesCounter - 1)
            //{
            //    iteration = 0;

            //    List<Image> tmpLst = new List<Image>(BannerImage.Children.Count);
            //    tmpLst.Add((Image)BannerImage.Children.Last());
            //    for (int i = 0; i < BannerImage.Children.Count - 1; ++i) tmpLst.Add(((Image)BannerImage.Children[i]));
            //    BannerImage.Children.Clear();
            //    foreach (Image img in tmpLst) BannerImage.Children.Add(img);

            //    posX = 0.0;
            //    foreach (Image img in BannerImage.Children)
            //    {
            //        img.RenderTransform = new TranslateTransform();
            //        Canvas.SetLeft(img, posX);
            //        posX += BannerImage.ActualWidth;

            //    }
            //}

            //Storyboard storyboard = new Storyboard();
            //foreach (Image img in BannerImage.Children)
            //{

            //    TranslateTransform trans = img.RenderTransform as TranslateTransform;
            //    DoubleAnimation anima1 = new DoubleAnimation();
            //    anima1.By = -BannerImage.ActualWidth;
            //    anima1.Duration = new TimeSpan(0, 0, 1);
            //    Storyboard.SetTarget(anima1, trans);
            //    Storyboard.SetTargetProperty(anima1, new
            //    PropertyPath(TranslateTransform.XProperty));
            //    storyboard.Children.Add(anima1);
            //}

            //storyboard.Completed += Story_Completed;
            //storyboard.Begin();
            //++iteration;
  
            //if (iteration > 0)
            {
                //Canvas.SetLeft(BannerImage.Children[iteration],Canvas.GetLeft(BannerImage.Children[(iteration+1)%loadedImagesCounter]));
                //Canvas.SetLeft(BannerImage.Children[(iteration + 1) % loadedImagesCounter], 0);
                iteration = (iteration) % Banners.Count;
                
                BannerImage.Tag = Banners[iteration];
                BannerImage2.Source = BannerImage.Source;
                BannerImage.Source = new BitmapImage(new Uri(Banners[iteration].Image, UriKind.RelativeOrAbsolute));
                
                ++iteration;
            }
        }

#warning BANNER_CONTROL_onBanerTap_NOT_IMPLEMENTED
        private void onBannerTap(object sender, TappedRoutedEventArgs e)
        {
            var banner = (Banner)((Image)sender).Tag;

            string pathForView = string.Concat("/Views/Book.xaml?NavigatedFrom=toast&id=", banner.ContentId);
            if (banner.Type == "author") pathForView = string.Concat("/Views/Person.xaml?NavigatedFrom=toast&id=", banner.ContentId);
            else if (banner.Type == "collection") pathForView = string.Concat("/Views/BooksByCategory.xaml?NavigatedFrom=toast&category=", banner.ContentId);//string.Concat("/Views/BooksByCategory.xaml?category=", banner.ContentId);
            else if (banner.Type == "application")  pathForView = string.Concat("/Views/Web.xaml?NavigatedFrom=toast&id=", banner.ContentId);

            Analytics.Instance.sendMessage(Analytics.ActionGotoBaner);

            Uri navigateUri = new Uri(pathForView, UriKind.RelativeOrAbsolute);
            //(Application.Current.RootVisual as Microsoft.Phone.Controls.PhoneApplicationFrame).Navigate(navigateUri);
        }

        private void Banner_Image_Opened(object sender, RoutedEventArgs ee)
        {
            if (!Enabled) return;

            BannerImage.Visibility = Visibility.Visible;
            var snd = (Image)sender;
            BannerImage.Source = snd.Source;
            BannerImage2.Source = null;

            //double pw = (double)((BitmapImage)snd.Source).PixelWidth;
            //double ph = (double)((BitmapImage)snd.Source).PixelHeight;
            //snd.Width = BannerImage.ActualWidth;
            ////snd.Height = 132;
            ////snd.Height = ph / (pw / BannerImage.ActualWidth);
            //double height = ph / (pw / BannerImage.ActualWidth);
            //if (height > BannerImage.ActualHeight) height = BannerImage.ActualHeight;
            //snd.Height = height;
            //Canvas.SetLeft(snd, posX);
            //posX += BannerImage.ActualWidth+50;
            //snd.Visibility = Visibility.Visible;
            //snd.RenderTransform = new TranslateTransform();

            //++iteration;

           // if (loadedImagesCounter == Banners.Count)
            
            Continue();
        }
        private void Story_Completed(object sender, EventArgs e){}
    }
}
