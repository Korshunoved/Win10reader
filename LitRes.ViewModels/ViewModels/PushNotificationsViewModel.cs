using LitRes.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;


namespace LitRes.ViewModels
{
    public class PushNotificationsViewModel
    {
        public const string SPAMPACK_TAG = "SPAMPACK";
        public const string TEXT1_TAG = "wp:Text1";
        public const string TEXT2_TAG = "wp:Text2";
        public const string PARAM_TAG = "wp:Param";

        private enum NotificationEventTags 
        {
            NotificationEventNone = 0,
            NotificationEventUnknown = 1,
            NotificationEventOpened = 2,
            NotificationEventComing = 3
        }

        private static INotificationsProvider _notificationsProvider;
        private static readonly PushNotificationsViewModel instance = new PushNotificationsViewModel();

        private PushNotificationsViewModel() { }

        public static PushNotificationsViewModel Instance
        {
             get{return instance;}
        }

        public INotificationsProvider NotificationsProvider
        {
            set { _notificationsProvider = value; }
        }
#warning PUSH_NOTIFICATIONS_VIEW_MODEL_SHOW_TOAST_NOT_IMPLEMENTED
        public void ShowToast(IDictionary<string,string> parametrs)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            
            //var screen = (Application.Current.RootVisual as Microsoft.Phone.Controls.PhoneApplicationFrame).CurrentSource;
            //if(screen.OriginalString.Contains("Reader")) return;

            //if (parametrs.ContainsKey(PARAM_TAG))
            //{
            //    var spamPackId = getSpamPackId(parametrs[PARAM_TAG]);
            //    if (spamPackId != string.Empty) SendToastSpampack(spamPackId, (int)NotificationEventTags.NotificationEventComing);
            //}

            //var toast = new ToastPrompt();
            //toast.MillisecondsUntilHidden = 15000;
            //if (parametrs.ContainsKey(TEXT1_TAG)) toast.Title = parametrs[TEXT1_TAG];
            //if (parametrs.ContainsKey(TEXT2_TAG)) toast.Message = parametrs[TEXT2_TAG];
            //if (parametrs.ContainsKey(PARAM_TAG)) toast.Tag = parametrs[PARAM_TAG];
            //toast.TextWrapping = TextWrapping.Wrap;
            //toast.TextOrientation = System.Windows.Controls.Orientation.Horizontal;
            //var bmp = new BitmapImage(new Uri("../Assets/ApplicationIcon.png", UriKind.RelativeOrAbsolute));
            //bmp.DecodePixelHeight = 32;
            //bmp.DecodePixelWidth = 32;
            //toast.ImageSource = bmp;
            //toast.Tap += onToastTap;
            //toast.Show();
        }

        public static void SendToastSpampack(string id, int eventId=2)
        {
            if (_notificationsProvider != null) _notificationsProvider.SendSpampack(id, eventId, CancellationToken.None);
        }

//#warning PUSH_NOTIFICATIONS_VIEW_MODEL_ON_TOAST_TAP_NOT_IMPLEMENTED
//        private void onToastTap(object sender, System.Windows.Input.GestureEventArgs e)
//        {
//            string navigateUri = ((ToastPrompt)sender).Tag.ToString();
//            (Application.Current.RootVisual as Microsoft.Phone.Controls.PhoneApplicationFrame).Navigate(new Uri(navigateUri, UriKind.RelativeOrAbsolute));            
//        }

        private string getSpamPackId(string url)
        {
            int index = url.IndexOf(SPAMPACK_TAG);
            if (index > -1)
            {
                int startIndex = index + SPAMPACK_TAG.Length + 1;
                var regExpMatch = new Regex(@"[+-]{0,1}\d{1,7}").Match(url.Substring(startIndex, url.Length - startIndex));
                if (regExpMatch.Success)
                {
                    return regExpMatch.Value;
                }
            }
            return string.Empty;
        }
    }
}
