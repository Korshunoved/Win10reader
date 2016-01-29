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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Windows.UI.Core;

using Autofac;

using Digillect.Mvvm.UI;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

using Digillect.Runtime.Serialization;

namespace Digillect.Mvvm.Services
{
	/// <summary>
	///     Windows phone implementation of <see cref="Digillect.Mvvm.Services.INavigationService" />
	/// </summary>
	internal sealed class NavigationService : IWindowsRTNavigationService, IStartable, IBreadcrumbService
	{
		private readonly INavigationHandler[] _navigationHandlers;
		private readonly List<NavigationSnapshot> _navigationSnapshots = new List<NavigationSnapshot>();
		private readonly Stack<Breadcrumb> _breadcrumbs = new Stack<Breadcrumb>();
		private readonly IViewDiscoveryService _viewDiscoveryService;

		private readonly Dictionary<string, ViewDescriptor> _views = new Dictionary<string, ViewDescriptor>( StringComparer.OrdinalIgnoreCase );

		private bool _initialized;
		private bool _navigationIsInProgress;

        #region Constructors/Disposer
        /// <summary>
        ///     Initializes a new instance of the <see cref="NavigationService" /> class.
        /// </summary>
        /// <param name="viewDiscoveryService">The view discovery service.</param>
        /// <param name="navigationHandlers">The navigation handlers.</param>
        public NavigationService(IViewDiscoveryService viewDiscoveryService, IEnumerable<INavigationHandler> navigationHandlers)
        {
            _viewDiscoveryService = viewDiscoveryService;
            _navigationHandlers = navigationHandlers.ToArray();
        }
        #endregion

        #region IStartable Members
        /// <summary>
        ///     Perform once-off startup processing.
        /// </summary>
        public void Start()
        {         
			var viewTypes = _viewDiscoveryService.GetViewTypes();

			foreach( var type in viewTypes )
			{
				var viewAttribute = type.GetTypeInfo().GetCustomAttributes( typeof( ViewAttribute ), true ).Cast<ViewAttribute>().First();
				var viewName = viewAttribute.Name ?? type.Name;

				var descriptor = new ViewDescriptor
				{
					Name = viewName,
					Type = type,
				};
			    try
			    {
                    _views.Add(viewName, descriptor);
                }
			    catch (Exception)
			    {
			        //
			    }
				
			}

			var app = (WindowsRTApplication) Application.Current;

			if( app.RootFrame == null )
			{
				Window.Current.Activated += Window_Activated;
			}
			else
			{
				app.RootFrame.Navigated += RootFrame_Navigated;
			}
		}

		private void Window_Activated( object sender, WindowActivatedEventArgs e )
		{
			var app = (WindowsRTApplication) Application.Current;

			if( app.RootFrame != null )
			{
				app.RootFrame.Navigated += RootFrame_Navigated;
				Window.Current.Activated -= Window_Activated;
			}
		}
		#endregion

		#region IWindowsRTNavigationService Members

	    /// <summary>
	    ///     Navigates to the specified view.
	    /// </summary>
	    /// <param name="viewName">Name of the view.</param>
	    /// <param name="openInSubFrame"></param>
	    public void Navigate( string viewName, bool openInSubFrame = false)
		{
			Navigate( viewName, null, openInSubFrame);
		}

		/// <summary>
		///     Navigates to the specified view with parameters.
		/// </summary>
		/// <param name="viewName">Name of the view.</param>
		/// <param name="parameters">The parameters.</param>
		/// <exception cref="System.ArgumentNullException">viewName</exception>
		/// <exception cref="System.ArgumentException">viewName</exception>
		public async void Navigate( string viewName, XParameters parameters, bool openInSubFrame = false)
		{
			var context = new NavigationContext { ViewName = viewName, Parameters = parameters };

			foreach( var handler in _navigationHandlers )
			{
				if( await handler.HandleNavigation( context ) )
				{
					break;
				}
			}

			if( context.Cancel )
			{
				return;
			}

			ViewDescriptor descriptor;

			if( !_views.TryGetValue( context.ViewName, out descriptor ) )
			{
				throw new ArgumentException( String.Format( "View with name '{0}' is not registered.", context.ViewName ), "viewName" );
			}
		    if (openInSubFrame)
		    {
		       var subFrame = ((WindowsRTApplication) Application.Current).RootSubFrame;
                subFrame.Navigate(descriptor.Type, context.Parameters);
                ((UIElement)subFrame.Parent).Visibility = Visibility.Visible;
		        //FlyoutBase.ShowAttachedFlyout(((WindowsRTApplication)Application.Current).RootFrame);
		    }
		    else
		    {
                ((WindowsRTApplication)Application.Current).RootFrame.Navigate(descriptor.Type, context.Parameters);
            }
		    
            //if (openInSubFrame) ((WindowsRTApplication)Application.Current).RootSubFrame.Navigate(descriptor.Type, context.Parameters);
            //else ((WindowsRTApplication)Application.Current).RootFrame.Navigate(descriptor.Type, context.Parameters);
        }

		/// <summary>
		///     Navigated to the previous view, if any.
		/// </summary>
		public void GoBack(bool backInSubFrame = false)
		{
			if( !_navigationIsInProgress )
			{
				_navigationIsInProgress = true;

			    var frame = ((WindowsRTApplication) Application.Current).RootFrame;
                var subFrame = ((WindowsRTApplication)Application.Current).RootSubFrame;
                if (frame != null && frame.CanGoBack && !backInSubFrame) frame.GoBack();
			    if (subFrame != null && subFrame.CanGoBack && backInSubFrame) subFrame.GoBack();
                
                _navigationIsInProgress = false;
			}
		}

	    public bool CanGoBack(bool backInSubFrame = false)
	    {
            var frame = backInSubFrame ? ((WindowsRTApplication)Application.Current).RootSubFrame : ((WindowsRTApplication)Application.Current).RootFrame;
            return frame.CanGoBack;
        }

	    public void RemoveBackEntry(bool backInSubFrame = false)
	    {
            var frame = backInSubFrame? ((WindowsRTApplication)Application.Current).RootSubFrame : ((WindowsRTApplication)Application.Current).RootFrame;
           

            var count = frame.BackStack.Count;
            if(count > 0) frame.BackStack.RemoveAt(count-1);
        }

        /// <summary>
        ///     Navigates back to the specified view or first view if not found.
        /// </summary>
        /// <param name="viewName">Name of the view.</param>
        public void GoBack( string viewName )
		{
			if( !_navigationIsInProgress )
			{
				_navigationIsInProgress = true;

				var backStacks = ((WindowsRTApplication) Application.Current).RootFrame.BackStack;
				var journalEntries = backStacks.ToArray();

				ViewDescriptor descriptor;

				if( !_views.TryGetValue( viewName, out descriptor ) )
				{
					throw new ViewNavigationException( String.Format( "View '{0}' is not registered.", viewName ) );
				}

				var breadcrumb = _breadcrumbs.Count > 0 ? _breadcrumbs.Pop() : null;

				for( int i = journalEntries.Count() - 1; i > 0; i-- )
				{
					if( journalEntries[i].SourcePageType != descriptor.Type )
					{
						backStacks.Remove( journalEntries[i] );
						PopBreadcrumb( journalEntries[i].SourcePageType );
					}
					else
					{
						break;
					}
				}

				if( breadcrumb != null )
				{
					_breadcrumbs.Push( breadcrumb );
				}

				_navigationIsInProgress = false;
				GoBack();
			}
		}

	    public void ClearBackstack(bool clearSubFrame = false)
	    {
            if (!_navigationIsInProgress)
            {
                _navigationIsInProgress = true;
                if (clearSubFrame)
                {
                    var subFrame = ((WindowsRTApplication) Application.Current).RootSubFrame;
                    subFrame?.BackStack?.Clear();
                }
                else
                {
                    var frame = ((WindowsRTApplication) Application.Current).RootFrame;
                    frame?.BackStack?.Clear();
                }
                _navigationIsInProgress = false;
            }
        }

		public object CreateSnapshot()
		{
			return CreateSnapshot( null, null );
		}

		public object CreateSnapshot( Action<object> guard )
		{
			return CreateSnapshot( guard, null );
		}

		public object CreateSnapshot( Action<object> guard, object tag )
		{
			var breadcrumb = _breadcrumbs.Peek();

			var snapshot = new NavigationSnapshot
				{
					Breadcrumb = breadcrumb,
					Guard = guard, 
					Tag = tag
				};

			_navigationSnapshots.Add( snapshot );

			return snapshot;
		}

		public bool RollbackSnapshot( object snapshotId )
		{
			return RollbackSnapshot( snapshotId, null, null );
		}

		public bool RollbackSnapshot( object snapshotId, string viewName, XParameters parameters )
		{
			var snapshot = snapshotId as NavigationSnapshot;

			if( snapshot == null )
			{
				throw new ArgumentException( "Invalid snapshot.", "snapshotId" );
			}

			// Rewind journal
			var app = (WindowsRTApplication) Application.Current;
			var numberOfEntriesToRemove = 0;
			var sourceViewIsFoundInBackStack = false;

			foreach( var entry in _breadcrumbs )
			{
				if( entry.Equals( snapshot.Breadcrumb ) )
				{
					sourceViewIsFoundInBackStack = true;
					break;
				}

				numberOfEntriesToRemove++;
			}

			if( sourceViewIsFoundInBackStack )
			{
				// TODO: Check whether Navigated event is fired on each GoBack
				while( numberOfEntriesToRemove-- > 0 )
				{
					//var breadcrumb = _breadcrumbs.Peek();
					app.RootFrame.GoBack();

					//ProcessSnapshots( entry.Source );
				}

				if( viewName != null )
				{
					Navigate( viewName, parameters );
				}
				/*
				else
				{
					app.RootFrame.GoBack();
				}
				*/

				return true;
			}

			return false;
		}

		public void RestoreStateOrGoToLandingView( string view )
		{
			RestoreStateOrGoToLandingView( view, null );
		}

		public void RestoreStateOrGoToLandingView( string view, XParameters parameters )
		{
			if( !RestoreBreadcrumbs() )
			{
				Navigate( view, parameters );
			}
		}

		/// <summary>
		///     Saves the state.
		/// </summary>
		public void SaveState()
		{
			SaveBreadcrumbs();
		}
		#endregion

		#region Event handlers
		private void RootFrame_Navigated( object sender, NavigationEventArgs e )
		{
			if( !_initialized )
			{
				CompleteInitialization( e );
			}

			if( e.NavigationMode == NavigationMode.Back )
			{
				var breadcrumb = _breadcrumbs.Pop();

				ProcessSnapshots( breadcrumb );
			}
			else if( e.NavigationMode == NavigationMode.New )
			{
			    var breadcrumb = new Breadcrumb(e.SourcePageType, XParameters.Empty);
                _breadcrumbs.Push(breadcrumb);
                //_breadcrumbs.Push(new Breadcrumb(e.SourcePageType, e.Parameter as XParameters));
                //  _breadcrumbs.Push(new Breadcrumb(e.SourcePageType, XParameters.Empty));
            }
		}
		#endregion

		private void ProcessSnapshots( Breadcrumb breadcrumb )
		{
			if( _navigationSnapshots.Count > 0 )
			{
				var snapshots = _navigationSnapshots.Where( snapshot => snapshot.Breadcrumb.Equals( breadcrumb ) ).ToList();

				foreach( var snapshot in snapshots )
				{
					if( snapshot.Guard != null )
					{
						snapshot.Guard( snapshot.Tag );
					}

					_navigationSnapshots.Remove( snapshot );
				}
			}
		}

		private void CompleteInitialization( NavigationEventArgs e )
		{
			_initialized = true;
		}

		#region Breadcrumbs
		public Breadcrumb PeekBreadcrumb( Type pageType )
		{
			if( _breadcrumbs.Count == 0 )
			{
				return null;
			}

			var bc = _breadcrumbs.Peek();

			if( bc.Type == pageType )
			{
				return bc;
			}

			return null;
		}

		public Breadcrumb PopBreadcrumb( Type pageType )
		{
			if( _breadcrumbs.Count == 0 )
			{
				return null;
			}

			var bc = _breadcrumbs.Peek();

			if( bc.Type == pageType )
			{
				_breadcrumbs.Pop();

				return bc;
			}

			return null;
		}

		public void PushBreadcrumb( Type type, XParameters parameters = null )
		{
			_breadcrumbs.Push( new Breadcrumb( type, parameters ) );
		}

		private bool RestoreBreadcrumbs( bool clearOnSuccess = true )
		{
			if( !ApplicationData.Current.RoamingSettings.Values.ContainsKey( "Breadcrumbs" ) )
			{
				return false;
			}

			var unwind = new Stack<Breadcrumb>();
			string state;

			try
			{
				var history = (string) ApplicationData.Current.RoamingSettings.Values["Breadcrumbs"];

				using( var stream = new MemoryStream( Convert.FromBase64String( history ) ) )
				{
					var reader = new BinaryReader( stream );

					state = reader.ReadString();
					var count = reader.ReadInt32();

					while( count-- > 0 )
					{
						var pageTypeName = reader.ReadString();
						var hasParameters = reader.ReadBoolean();
						var pageType = Type.GetType( pageTypeName );
						XParameters parameters = null;

						if( hasParameters )
						{
							parameters = XParametersSerializer.Deserialize( reader );
						}

						var breadcrumb = new Breadcrumb( pageType, parameters );

						unwind.Push( breadcrumb );
					}
				}

				if( unwind.Count == 0 )
				{
					return false;
				}
			}
			catch( IOException )
			{
				return false;
			}

			foreach( var bc in unwind )
			{
				_breadcrumbs.Push( bc );
			}

			IsUnwinding = true;

			((WindowsRTApplication) Application.Current).RootFrame.SetNavigationState( state );

			IsUnwinding = false;

			if( clearOnSuccess )
			{
				ApplicationData.Current.RoamingSettings.Values["Breadcrumbs"] = null;
			}

			return true;
		}

		private void SaveBreadcrumbs()
		{
			string history = null;

			try
			{
				using( var stream = new MemoryStream() )
				{
					var writer = new BinaryWriter( stream );

					writer.Write( ((WindowsRTApplication) Application.Current).RootFrame.GetNavigationState() );
					writer.Write( _breadcrumbs.Count );

					foreach( var breadcrumb in _breadcrumbs )
					{
						writer.Write( breadcrumb.Type.AssemblyQualifiedName );
						writer.Write( breadcrumb.Parameters != null );

						if( breadcrumb.Parameters != null )
						{
							XParametersSerializer.Serialize( writer, breadcrumb.Parameters );
						}
					}

					writer.Flush();

					history = Convert.ToBase64String( stream.ToArray() );
				}
			}
			catch( IOException )
			{
			}

			ApplicationData.Current.RoamingSettings.Values["Breadcrumbs"] = history;
		}
		#endregion

		#region Internal properties
		public bool IsUnwinding { get; private set; }
		#endregion

		#region Nested type: NavigationSnapshot
		private class NavigationSnapshot
		{
			#region Public Properties
			public Breadcrumb Breadcrumb { get; set; }
			public Action<object> Guard { get; set; }
			public object Tag { get; set; }
			#endregion
		}
		#endregion

		#region Nested type: ViewDescriptor
		private class ViewDescriptor
		{
			#region Public Properties
			public string Name { get; set; }
			public Type Type { get; set; }
			#endregion
		}
		#endregion
	}
}