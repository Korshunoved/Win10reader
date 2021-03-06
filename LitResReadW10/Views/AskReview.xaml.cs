﻿using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Cimbalino.Toolkit.Extensions;
using Digillect.Mvvm.UI;
using LitRes.ViewModels;
using LitResReadW10;
using LitResReadW10.Helpers;

namespace LitRes.Views
{
    [View("AskReview")]
    public partial class AskReview : AskReviewFitting
    {
        public AskReview()
        {
            InitializeComponent();
            Loaded += PageLoaded;
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            //throw new System.NotImplementedException();
        }

        private void AskLatterButtonPressed(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.AskLatter();
            MainPage.Instance.CloseSubFrame();
        }

        private void DontAskMoreButtonPressed(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.DontAskMore();
            MainPage.Instance.CloseSubFrame();
        }

        private void RatingPressed(object sender, TappedRoutedEventArgs e)
        {
            sender = sender as Image;
            var name = sender?.GetPropertyValue("Name") as string;
            if (name == null) return;
            var count = int.Parse(name.Substring(name.Length - 1));
            if (count <= 3)
            {
                RateUsTextBlock.Visibility = Visibility.Collapsed;
                ThankYouTextBlock.Visibility = Visibility.Visible;
                ThankYouInfoTextBlock.Visibility = Visibility.Visible;
                AskLaterButton.Visibility = Visibility.Collapsed;
                DontAskMoreButton.Visibility = Visibility.Collapsed;
                WriteToSupportButton.Visibility = Visibility.Visible;
                CloseDialogButton.Visibility = Visibility.Visible;
            }
            else
            {
                MainPage.Instance.CloseSubFrame();
            }
            switch (count)
            {
                case 1:
                {
                    Star1.Visibility = Visibility.Collapsed;
                    NotEmptyStar1.Visibility = Visibility.Visible;
                    break;
                }
                case 2:
                {
                    Star1.Visibility = Visibility.Collapsed;
                    NotEmptyStar1.Visibility = Visibility.Visible;
                    Star2.Visibility = Visibility.Collapsed;
                    NotEmptyStar2.Visibility = Visibility.Visible;
                    break;
                }
                case 3:
                {
                    Star1.Visibility = Visibility.Collapsed;
                    NotEmptyStar1.Visibility = Visibility.Visible;
                    Star2.Visibility = Visibility.Collapsed;
                    NotEmptyStar2.Visibility = Visibility.Visible;
                    Star3.Visibility = Visibility.Collapsed;
                    NotEmptyStar3.Visibility = Visibility.Visible;
                    break;
                }
            }
            ViewModel.Ratting(count);
        }

        private void WriteToSupport(object sender, TappedRoutedEventArgs e)
        {
            EmailHelper.OpenEmailClientWithLitresInfo();
            MainPage.Instance.CloseSubFrame();
        }

        private void CloseDialogButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            MainPage.Instance.CloseSubFrame();
        }
    }
    public class AskReviewFitting : ViewModelPage<AskReviewViewModel>
    {
    }
}
