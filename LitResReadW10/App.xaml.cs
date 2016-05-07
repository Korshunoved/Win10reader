using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using AppsFlyerLib;
using Autofac;
using BookParser;
using Digillect;
using Digillect.Mvvm.Services;
using Digillect.Mvvm.UI;
using LitRes;
using LitRes.Services;
using LitRes.Views;
using Microsoft.ApplicationInsights;
using Book = LitRes.Models.Book;

namespace LitResReadW10
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : WindowsRTApplication
    {
        internal const string EmailRegexPattern = @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$";
        internal const string PhoneRegexPattern = @"^\+7[0-9]{10,10}$";
        internal const int LoginLength = 3;
        internal const int PasswordLength = 3;
        public static string TileBookId;
        public static bool IsLaunched { get; set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            WindowsAppInitializer.InitializeAsync(
                WindowsCollectors.Metadata |
                WindowsCollectors.Session);
            InitializeComponent();
            Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            base.OnLaunched(e);
            IsLaunched = true;
            AppsFlyerLib.AppsFlyerTracker tracker = AppsFlyerLib.AppsFlyerTracker.GetAppsFlyerTracker();
            tracker.appId = "9wzdncrfhvzw";
            tracker.devKey = "8iAKRJCBJWsHtjSJiNZ6KQ";
            tracker.TrackAppLaunch();

            if (e.Arguments.Length > 0 && e.Arguments.Contains("secondary_tile_id"))
            {
              //  new MessageDialog(e.Kind.ToString()).ShowAsync();
                TileBookId = e.Arguments.Split('=')[1];
                var book = new Book {Id = int.Parse(TileBookId)};
                RootFrame.Navigate(typeof(Reader), XParameters.Create("BookEntity", book));
            }

            if (AppSettings.Default.ReaderOpen)
            {
                var book = new Book { Id = AppSettings.Default.LastBookId };
                RootFrame.Navigate(typeof(Reader), XParameters.Create("BookEntity", book));
            }
#if DEBUG
            if (Debugger.IsAttached)
            {
                //this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);
            if (args.Kind != ActivationKind.ToastNotification) return;
            if (!IsLaunched)
            {
                IsLaunched = true;
                RootFrame.Navigate(typeof (MainPage));
            }
            //NavigateRootFrame(e);
            var toastArgs = args as ToastNotificationActivatedEventArgs;
            if (toastArgs != null)
            {
                var argument = toastArgs.Argument;
                var arguments = argument.Split('&');
                var type = "";
                var action = "";
                var internalId = "";
                foreach (var s in arguments)
                {
                    if (s.Contains("type="))
                    {
                        type = s.Split('=')[1];
                    }
                    else if (s.Contains("action"))
                    {
                        action = s.Split('=')[1];
                    }
                    else if (s.Contains("internal_id="))
                    {
                        internalId = s.Split('=')[1];
                    }
                }
                if (internalId == "") return;
                switch (type)
                {
                    case "b":
                    {
                        switch (action)
                        {
                            case "read":
                            {
                                var book = new Book {Id = int.Parse(internalId)};
                                RootFrame.Navigate(typeof (Reader), XParameters.Create("BookEntity", book));
                                break;
                            }
                            case "about":
                            {
                                var book = new Book {Id = int.Parse(internalId)};
                                RootFrame.Navigate(typeof (LitRes.Views.Book), XParameters.Create("BookEntity", book));
                                break;
                            }
                            case "cart":
                            {
                                var book = new Book {Id = int.Parse(internalId)};
                                RootFrame.Navigate(typeof (LitRes.Views.Book), XParameters.Create("BookEntity", book));
                                break;
                            }
                            case "buy":
                            {
                                var book = new Book {Id = int.Parse(internalId)};
                                LitRes.Views.Book.NavigationReason = "buy";
                                RootFrame.Navigate(typeof (LitRes.Views.Book), XParameters.Create("BookEntity", book));
                                break;
                            }
                            default:
                            {
                                var book = new Book {Id = int.Parse(internalId)};
                                RootFrame.Navigate(typeof (LitRes.Views.Book), XParameters.Create("BookEntity", book));
                                break;
                            }
                        }
                        break;
                    }
                    case "a":
                    {
                        RootFrame.Navigate(typeof (Person), XParameters.Create("Id", internalId));
                        break;
                    }
                    default:
                    {
                        return;
                    }
                }
            }
            else if (AppSettings.Default.ReaderOpen)
            {
                var book = new Book { Id = AppSettings.Default.LastBookId };
                RootFrame.Navigate(typeof(Reader), XParameters.Create("BookEntity", book));
            }
        }

        protected override void NavigateRootFrame(LaunchActivatedEventArgs e)
        {
            RootFrame.Navigate(typeof(MainPage), e.Arguments);
        }


        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        #region IoC
        protected override void RegisterServices(ContainerBuilder builder)
        {
            base.RegisterServices(builder);

            builder.RegisterType<NetworkAvailabilityService>().As<INetworkAvailabilityService>().As<IStartable>().SingleInstance();

            builder.RegisterType<PurchaseServiceDecorator>().As<IPageDecorator>().As<IPurchaseServiceDecorator>().SingleInstance();
            builder.RegisterType<NetworkAvailabilityServiceDecorator>().As<IPageDecorator>().SingleInstance();
            builder.RegisterType<ViewModelExceptionHandlingService>().As<IViewModelExceptionHandlingService>().SingleInstance();
            builder.RegisterType<PushNotificationsService>().As<IPushNotificationsService>().As<IStartable>().SingleInstance();
            builder.RegisterType<IsolatedStorageDataCacheService>().As<IDataCacheService>();
            builder.RegisterType<IsolatedStorageFileCacheService>().As<IFileCacheService>();
            builder.RegisterType<InAppPurchaseService>().As<IInAppPurchaseService>().SingleInstance();
            builder.RegisterType<DeviceInfoService>().As<IDeviceInfoService>().SingleInstance();
            //builder.RegisterType<ApplicationSettingsService>().As<IApplicationSettingsService>().SingleInstance();

            builder.RegisterModule<ModelsModule>();
            builder.RegisterModule<ViewModelsModule>();
        }
        #endregion
    }
}
