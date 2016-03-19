using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Autofac;
using Digillect.Mvvm.Services;
using Digillect.Mvvm.UI;
using LitRes;
using LitRes.Services;
using LitRes.Views;
using Microsoft.QueryStringDotNET;

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

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
                Microsoft.ApplicationInsights.WindowsCollectors.Metadata |
                Microsoft.ApplicationInsights.WindowsCollectors.Session);
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            base.OnLaunched(e);
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                //this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            // TODO: Initialize root frame just like in OnLaunched

            // Handle toast activation
            if (args.Kind == ActivationKind.ToastNotification)
            {
                var toastArgs = args as ToastNotificationActivatedEventArgs;

                // Get arguments corresponding to this activation;
                // When tapping the body of the toast caused this activation, the app receives the value of “launch” property of ;
                // When the activation is caused by using tapping on an action inside the toast, the app receives the value of “arguments” property of ; 
                var arguments = toastArgs.Argument;

                // User input from <input> can be retrieved using the UserInput property. The UserInput is a ValueSet and the key is the pre-defined id attribute in the <input> element in the payload.
                RootFrame.Navigate(typeof(MyBooks));

                // Navigate accordingly
            }

            // TODO: Handle other types of activation
        }

        protected override void NavigateRootFrame(LaunchActivatedEventArgs e)
        {
            RootFrame.Navigate(typeof(MainPage), e.Arguments);
        }

       
        void HandleNavigationFailed(NavigationFailedEventArgs eventArgs)
        {
            base.HandleNavigationFailed(eventArgs);
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

            builder.RegisterModule<ModelsModule>();
            builder.RegisterModule<ViewModelsModule>();
        }
        #endregion
    }
}
