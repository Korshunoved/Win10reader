using System;
using System.Diagnostics;
using Digillect.Mvvm;
using Digillect.Mvvm.UI;

using LitRes.Models;
using LitRes.Services;
using LitRes.ViewModels;

using System.ComponentModel;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace LitRes.Views
{
	[View( "AccountInfo" )]
	public partial class AccountInfo : AccountInfoFitting
	{

	    private bool inapShowed;

		#region Constructors/Disposer
        public AccountInfo()
		{
			InitializeComponent();

			Loaded += PageLoaded;
		}
		#endregion

        protected override Session CreateDataSession(DataLoadReason reason)
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            return base.CreateDataSession(reason);
        }

		private void PageLoaded( object sender, RoutedEventArgs e )
		{
		}

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {            
            base.OnNavigatedFrom(e);
            
            if(e.Uri.OriginalString.Contains("ListPickerPage")) inapShowed = true;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                Debug.WriteLine("Back to account info");
                ViewModel.ReloadInfo.Execute(null);
            }
            if (inapShowed) 
            {
                msPopup.IsOpen = true;
                inapShowed = false;
            }
            base.OnNavigatedTo(e);
        }

        //protected override void OnBackKeyPress(CancelEventArgs e)
        //{
        //    if (mainPopup.IsOpen || msPopup.IsOpen)
        //    {
        //        hideMainPopup(true);
        //        hideMSPopup(true);
        //        e.Cancel = true;
        //    }
        //    else
        //    {
        //        base.OnBackKeyPress(e);
        //    }
        //}

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("UserInfoLoaded"))
            {
                if (ViewModel.UserInformation != null)
                {
                    AddMoney.Visibility = Visibility.Visible;
                    AccountMoneyText.Text = ViewModel.UserInformation.Account.ToString("### ### ###");
                    if (ViewModel.UserInformation.Account < 1.0) AccountMoneyText.Text = "0";

                    if (!string.IsNullOrEmpty(ViewModel.UserInformation.CanRebill) && !ViewModel.UserInformation.CanRebill.Equals("0") && !string.IsNullOrEmpty(ViewModel.LastNumbers))
                    {
                        BlockWithData.Visibility = Visibility.Visible;
                        CreditCardBlock.Visibility = Visibility.Visible;
                        CreditCardText.Text = string.Format("xxxx-xxxx-xxxx-{0}",ViewModel.LastNumbers);
                    }

                    if (!string.IsNullOrEmpty(ViewModel.UserInformation.Phone))
                    {
                        BlockWithData.Visibility = Visibility.Visible;
                        PhoneNumberBlock.Visibility = Visibility.Visible;

                        if (ViewModel.UserInformation.Phone.Contains("+7")) ViewModel.UserInformation.Phone = ViewModel.UserInformation.Phone.Remove(0, 2);
                        else if (ViewModel.UserInformation.Phone.Length > 10)
                            ViewModel.UserInformation.Phone = ViewModel.UserInformation.Phone.Substring(ViewModel.UserInformation.Phone.Length - 10, 10);

                        MobilePhoneText.Text = string.Format("+7 {0}", long.Parse(ViewModel.UserInformation.Phone).ToString("(###) ###-##-##"));
                    }

                    if (BlockWithData.Visibility == Visibility.Collapsed) BlockWithoutData.Visibility = Visibility.Visible;
                    else ClearDataButton.Visibility = Visibility.Visible;
                }
            }
            else if (e.PropertyName.Equals("UserInfoCleared"))
            {
                BlockWithData.Visibility = Visibility.Collapsed;
                BlockWithoutData.Visibility = Visibility.Visible;
            }
            else if (e.PropertyName.Equals("LockClearDataButton"))
            {
                ClearDataButton.IsEnabled = false;
            }
        }

        private void AddMoneyTap(object sender, RoutedEventArgs e)
        {
            showMainPopup();
        }

        private void Body_Tap(object sender, RoutedEventArgs e)
        {
            if (mainPopup.IsOpen)
            {
                hideMainPopup(true);
            }
            else if (msPopup.IsOpen)
            {
                hideMSPopup(true);
            }
        }

        private void showPopup(Popup pop, Canvas popRoot)
        {
            pop.IsOpen = true;
            var storyboard = new Storyboard();
            var translateTransformTop = popRoot.RenderTransform as TranslateTransform;
            var topAnimation = new DoubleAnimation();
            topAnimation.To = 0;
            var duration = new TimeSpan(0, 0, 0, 0, 400);
            topAnimation.Duration = duration;
            Storyboard.SetTarget(topAnimation, translateTransformTop);
            Storyboard.SetTargetProperty(topAnimation, "YProperty");

            storyboard.Children.Add(topAnimation);
            storyboard.Begin();
        }

        private void hidePopup(Canvas pop, double pos, EventHandler func, int time = 400)
        {
            var storyboard = new Storyboard();
            var translateTransformTop = pop.RenderTransform as TranslateTransform;
            var topAnimation = new DoubleAnimation();
            topAnimation.To = pos;
            var duration = new TimeSpan(0, 0, 0, 0, time);
            topAnimation.Duration = duration;
            Storyboard.SetTarget(topAnimation, translateTransformTop);
            Storyboard.SetTargetProperty(topAnimation, "YProperty");
            topAnimation.Completed += (sender, e) => { func.Invoke(sender, null); };

            storyboard.Children.Add(topAnimation);
            storyboard.Begin();
        }

        private void showMainPopup()
        {
            Fade.Visibility = Visibility.Visible;
            Animatio1n_FadeIn.Begin();
            showPopup(mainPopup, mainPopupRoot);            
        }

        private void hideMainPopup(bool widthFade = false)
        {
            if (widthFade) Animation1_FadeOut.Begin();
            else Fade.Visibility = Visibility.Collapsed;
            hidePopup(mainPopupRoot, -250, (object sender, EventArgs e) => { Debug.WriteLine("Back Main Anim Popup end"); mainPopup.IsOpen = false; }, widthFade?400:1);
        }

        private void showMSPopup()
        {
            Fade2.Visibility = Visibility.Visible;
            Fade2.Opacity = 0.6;
            showPopup(msPopup, msPopupRoot);            
        }

	    private void hideMSPopup(bool widthFade = false)
	    {
            if (widthFade) Animation2_FadeOut.Begin();
	        else Fade.Visibility = Visibility.Collapsed;
            hidePopup(msPopupRoot, -360, (object sender, EventArgs e) => { Debug.WriteLine("Back MS Anim Popup end"); msPopup.IsOpen = false; });
	    }

        private void msBuyMenu(object sender, RoutedEventArgs e)
        {
            hideMainPopup(false);
            showMSPopup();
        }

	    private void LitresStore_OnTap(object sender, RoutedEventArgs e)
	    {
            hideMainPopup(false);
	        Launcher.LaunchUriAsync(
	            new Uri(string.Format("http://www.pda.litres.ru/pages/put_money_on_account/?sid={0}",
	                ViewModel.UserInformation.SessionId)));
        
	    }

	    private void MsBuy_OnTap(object sender, RoutedEventArgs e)
	    {
            hideMSPopup(false);
	        var index = DepositsPicker.SelectedIndex;
            ViewModel.AddToDeposit.Execute((DepositType)index);
	    }
	}

    public class AccountInfoFitting : ViewModelPage<AccountInfoViewModel>
	{
	}
}