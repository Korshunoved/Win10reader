﻿using System;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Digillect;
using Digillect.Collections;
using LitRes;
using LitRes.Models;
using LitResReadW10.Helpers;
using Book = LitRes.Models.Book;

namespace LitResReadW10.Controls
{
    public class BannerControl : Control
    {    
        private readonly Frame _frame;
        private GridView _bannerCanv;

        public BannerControl(GridView bannerCanv, XCollection<Banner> banners, Frame frame)
        {
            _frame = frame;
            _bannerCanv = bannerCanv;
            bannerCanv.Items?.Clear();
            foreach (Banner banner in banners)
            {
                var bannerBitmap = new BitmapImage {UriSource = new Uri(banner.Image, UriKind.RelativeOrAbsolute)};
                var deviceWidth = Window.Current.CoreWindow.Bounds.Width;
                var bannerImage = !SystemInfoHelper.IsMobile() ? new Image
                {
                    Source = bannerBitmap,
                    Stretch = Stretch.Fill,
                    MaxHeight = bannerCanv.MaxHeight                    
                } : new Image
                {
                    Source = bannerBitmap,
                    Stretch = Stretch.Fill,
                    MaxWidth = deviceWidth
                };
                if (SystemInfoHelper.IsMobile())
                    bannerCanv.MaxHeight = bannerImage.MaxWidth/2.46;
                bannerImage.Tapped += OnBannerTap;                
                bannerImage.Tag = banner;

                bannerCanv.Visibility = Visibility.Visible;
                bannerImage.Visibility = Visibility.Visible;
                bannerCanv.Items?.Add(bannerImage);
            }            
        }

        private void OnBannerTap(object sender, TappedRoutedEventArgs e)
        {
            var banner = (Banner) ((Image) sender).Tag;

            switch (banner.Type)
            {
                case "author":
                    _frame.Navigate(typeof(Person), XParameters.Create("Id", banner.ContentId));                    
                    break;
                case "book":
                    var book = new Book { Id = int.Parse(banner.ContentId) };
                    _frame.Navigate(typeof(LitRes.Views.Book), XParameters.Create("BookEntity", book));
                    break;                    
                case "collection":
                    _frame.Navigate(typeof (LitRes.Views.BooksByCategory),
                        XParameters.Create("category", int.Parse(banner.ContentId)));
                    break;
                case "application":
                    var url = banner.ContentId;
                    Launch(url);
                    break;
            }
            _bannerCanv.SelectedItem = null;
            _bannerCanv.SelectedIndex = 0;
            Analytics.Instance.sendMessage(Analytics.ActionGotoBaner);
        }

        async void Launch(string url)
        {
            var uri = new Uri(url);

            var success = await Launcher.LaunchUriAsync(uri);

            if (success)
            {
                // URI launched
            }
            else
            {
                // URI launch failed
            }
        }
    }
}
