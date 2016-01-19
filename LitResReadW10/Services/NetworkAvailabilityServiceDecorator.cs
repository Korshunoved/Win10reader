using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Digillect.Mvvm.Services;
using Digillect.Mvvm.UI;
using LitRes.Controls;
using LitRes.Views;

namespace LitRes.Services
{
	public class NetworkAvailabilityServiceDecorator : IPageDecorator
	{
		private readonly INetworkAvailabilityService _networkAvailabilityService;
		private readonly List<Page> _pages;

		#region Constructors/Disposer
		public NetworkAvailabilityServiceDecorator(INetworkAvailabilityService networkAvailabilityService)
		{
			_networkAvailabilityService = networkAvailabilityService;
			_networkAvailabilityService.NetworkAvailabilityChanged += NetworkAvailabilityChanged;

			_pages = new List<Page>();
		}
		#endregion

		#region Decorator methods
		public void AddDecoration( WindowsRTPage page )
		{
			if(!_pages.Contains( page ))
			{
				_pages.Add( page );
			}
			foreach(var phoneApplicationPage in _pages)
			{
				SetCurrentNetworkState( phoneApplicationPage );
			}
		}

		public void RemoveDecoration(WindowsRTPage page )
		{
			if(_pages.Contains( page ))
			{
				_pages.Remove( page );
			}
			foreach(var phoneApplicationPage in _pages)
			{
				SetCurrentNetworkState( phoneApplicationPage );
			}
		}
		#endregion

		private void NetworkAvailabilityChanged(object sender, EventArgs e)
		{
			foreach (var page in _pages)
			{
				//Deployment.Current.Dispatcher.BeginInvoke( ( Action<WindowsRTPage> ) SetCurrentNetworkState, page );
			}
		}

		private void SetCurrentNetworkState( Page page )
		{
			if(VisualTreeHelper.GetChildrenCount( page ) > 0)
			{
				Object grid = VisualTreeHelper.GetChild( page, 0 );
				int childCount = VisualTreeHelper.GetChildrenCount( grid as FrameworkElement );
				if (childCount > 0)
				{
					Type t = typeof ( PageHeader );
					for (int i = 0; i < childCount; i++)
					{
						Object element = VisualTreeHelper.GetChild( grid as FrameworkElement, i );
						if (element.GetType() == t)
						{
							PageHeader header = ( PageHeader ) element;
							header.NetworkAvailability = !_networkAvailabilityService.NetworkAvailable;
						}
					}
				}
			}
		}
	}
}
