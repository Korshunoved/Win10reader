#region Copyright (c) 2011-2014 Gregory Nickonov and Andrew Nefedkin (Actis® Wunderman)
// Copyright (c) 2011-2014 Gregory Nickonov and Andrew Nefedkin (Actis® Wunderman).
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
#endregion

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Autofac;

using Digillect.Mvvm.Services;

using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Digillect.Mvvm.UI
{
	/// <summary>
	///     Base class for Windows Metro applications.
	/// </summary>
	public abstract class WindowsRTApplication : Application
	{
		private readonly Stack<Breadcrumb> _breadcrumbs = new Stack<Breadcrumb>();

		#region Constructors/Disposer
		/// <summary>
		///     Initializes a new instance of the <see cref="WindowsRTApplication" /> class.
		/// </summary>
		[SuppressMessage( "Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors" )]
		protected WindowsRTApplication()
		{
			//InitializeIoC();

			Suspending += ( s, e ) => HandleSuspension( e );
            UnhandledException += WindowsRTApplication_UnhandledException;
		}

	    protected virtual void NavigateRootFrame(LaunchActivatedEventArgs e)
	    {
	    }

        private void WindowsRTApplication_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Debug.WriteLine(e.Message);
        }
        #endregion

        #region Public properties
        /// <summary>
        ///     Gets the application root frame.
        /// </summary>
        public Frame RootFrame { get; set; }

        public Frame RootSubFrame { get; set; }

		/// <summary>
		///     Gets the lifetime scope used to create components.
		/// </summary>
		/// <value>
		///     The scope.
		/// </value>
		public ILifetimeScope Scope { get; private set; }
		#endregion

		#region Events and event raisers
		/// <summary>
		///     Invoked when the application is launched. Override this method to perform application initialization and to display initial content in the associated Window.
		/// </summary>
		/// <param name="args">Event data for the event.</param>
		protected override void OnLaunched( LaunchActivatedEventArgs args )
		{
            //RootFrame = CreateRootFrame();

            //InitializeIoC();
            //HandleLaunch( args );

            //RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            //Window.Current.Content = RootFrame;
            //Window.Current.Activate();


            

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (RootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                RootFrame = CreateRootFrame();
                InitializeIoC();
                
                HandleLaunch(args);
                RootFrame.NavigationFailed += OnNavigationFailed;

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = RootFrame;
            }
            NavigateRootFrame(args);
            // Ensure the current window is active
            Window.Current.Activate();

        }

		protected override void OnSearchActivated( SearchActivatedEventArgs args )
		{
			base.OnSearchActivated( args );

			if( args.Kind == ActivationKind.Search )
			{
				RootFrame = CreateRootFrame();

				InitializeIoC();
				HandleSearchActivated( args );

				RootFrame.NavigationFailed += OnNavigationFailed;

				Window.Current.Content = RootFrame;
				Window.Current.Activate();
			}
		}
		#endregion

		#region Event handlers
	    void OnNavigationFailed( object sender, NavigationFailedEventArgs e )
		{
			HandleNavigationFailed( e );
		}
		#endregion

		#region Protected methods
		/// <summary>
		///     Creates application root frame. By default creates instance of <see cref="Windows.UI.Xaml.Controls.Frame" />, override
		///     to create instance of other type.
		/// </summary>
		/// <returns>application frame.</returns>
		protected virtual Frame CreateRootFrame()
		{
			return new Frame();
		}

		/// <summary>
		///     Handles the launch.
		/// </summary>
		/// <param name="eventArgs">
		///     The <see cref="LaunchActivatedEventArgs" /> instance containing the event data.
		/// </param>
		protected virtual void HandleLaunch( LaunchActivatedEventArgs eventArgs )
		{
		}

		/// <summary>
		///     Handles the search activated.
		/// </summary>
		/// <param name="eventArgs">
		///     The <see cref="SearchActivatedEventArgs" /> instance containing the event data.
		/// </param>
		protected virtual void HandleSearchActivated( SearchActivatedEventArgs eventArgs )
		{
		}

		/// <summary>
		///     Executes when navigation has been failed. Override to provide your own handling.
		/// </summary>
		/// <param name="eventArgs">
		///     The <see cref="Windows.UI.Xaml.Navigation.NavigationFailedEventArgs" /> instance containing the event data.
		/// </param>
		protected virtual void HandleNavigationFailed( NavigationFailedEventArgs eventArgs )
		{
		}

		/// <summary>
		///     Handles the suspension.
		/// </summary>
		/// <param name="eventArgs">
		///     The <see cref="SuspendingEventArgs" /> instance containing the event data.
		/// </param>
		protected virtual void HandleSuspension( SuspendingEventArgs eventArgs )
		{
		}

		/// <summary>
		///     Called to registers services in container.
		/// </summary>
		/// <param name="builder">The builder.</param>
		protected virtual void RegisterServices( ContainerBuilder builder )
		{
			builder.RegisterModule<WindowsRTModule>();
		}
		#endregion

		#region Miscellaneous
		private void InitializeIoC()
		{
			var builder = new ContainerBuilder();

			RegisterServices( builder );

			Scope = builder.Build();
		}
		#endregion
	}
}