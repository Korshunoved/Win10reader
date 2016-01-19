using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Navigation;

using Digillect.Mvvm.Services;
using LitRes.Services.Connectivity;

namespace LitRes.Services
{
	public class NavigationServiceContext : INavigationServiceContext
	{
		private ICredentialsProvider _credentialsProvider;
		private IBookProvider _bookProvider;

		private List<Uri> _needAnyAuthorizationViewCollection; 
		private List<Uri> _needUserAuthorizationViewCollection;

		private Uri _authorizationUri;

		public NavigationServiceContext(ICredentialsProvider credentialsProvider, IBookProvider bookProvider)
		{
			_authorizationUri = new Uri( "/Views/Authorization.xaml", UriKind.Relative );

			_credentialsProvider = credentialsProvider;
			_bookProvider = bookProvider;

			_needAnyAuthorizationViewCollection = new List<Uri>();
			_needAnyAuthorizationViewCollection.Add( new Uri( "/Views/MyBooks.xaml", UriKind.Relative ) );
			_needAnyAuthorizationViewCollection.Add( new Uri( "/Views/Bookmarks.xaml", UriKind.Relative ) );
			_needAnyAuthorizationViewCollection.Add( new Uri( "/Views/NotificationsEdit.xaml", UriKind.Relative ) );

			_needUserAuthorizationViewCollection = new List<Uri>();
			_needUserAuthorizationViewCollection.Add( new Uri( "/Views/UserInfo.xaml", UriKind.Relative ) );
		}

		public async void Navigate( Uri uri )
		{
			bool ignoreRestrictions = false;

			//little hack fo MyBooks
			if( uri.ToString() == "/Views/MyBooks.xaml" )
			{
				var exists = await _bookProvider.GetExistBooks( CancellationToken.None );

				if( exists != null && exists.Count > 0 )
				{
					ignoreRestrictions = true;
				}
			}

			if (_needUserAuthorizationViewCollection.Contains(uri) && !ignoreRestrictions)
			{
				var credentials = await _credentialsProvider.ProvideCredentials( CancellationToken.None );
				if (credentials == null || !credentials.IsRealAccount)
				{
					try
					{
						Uri navigate = new Uri( _authorizationUri.ToString() + "?uri=" + uri.ToString(), UriKind.Relative );
						(( App ) App.Current).RootFrame.Navigate( navigate );
					}
					catch (InvalidOperationException)
					{
					}
					return;
				}
			}
			else if (_needAnyAuthorizationViewCollection.Contains(uri) && !ignoreRestrictions)
			{
				var credentials = await _credentialsProvider.ProvideCredentials( CancellationToken.None );
				if (credentials == null)
				{
					try
					{
						Uri navigate = new Uri( _authorizationUri.ToString() + "?uri=" + uri.ToString(), UriKind.Relative );
						(( App ) App.Current).RootFrame.Navigate( navigate );
					}
					catch (InvalidOperationException)
					{
					}
					return;
				}
			}

			try
			{
				var mainpart = uri.OriginalString.Split( '?' )[0];

				bool exist = ( ( App ) App.Current ).RootFrame.BackStack.Any( x => x.Source.OriginalString.Split( '?' )[0] == mainpart );

				bool iscurrent = ( ( App ) App.Current ).RootFrame.CurrentSource.OriginalString.Split( '?' )[0] == mainpart;

				if( exist )
				{
					JournalEntry entry = ( ( App ) App.Current ).RootFrame.RemoveBackEntry();
					while( entry.Source.OriginalString.Split( '?' )[0] != mainpart )
					{
						entry = ( ( App ) App.Current ).RootFrame.RemoveBackEntry();
					}
				}

				((App) App.Current).RootFrame.Navigate( uri );

				if( iscurrent )
				{
					( ( App ) App.Current ).RootFrame.RemoveBackEntry();
				}
			}
			catch( InvalidOperationException )
			{
			}
		}

		public void GoBack()
		{
			
		}

		public void GoBack( string viewName )
		{
			
		}

		public Assembly GetMainAssemblyContainingViews()
		{
			return GetType().Assembly;
		}

		public string GetRootNamespace()
		{
			return "LitRes";
		}
	}
}
