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
using System.Net.NetworkInformation;

using Windows.Networking.Connectivity;
using Windows.UI.Core;

using Autofac;

namespace Digillect.Mvvm.Services
{
	/// <summary>
	/// Windows 8 implementation of <see cref="INetworkAvailabilityService"/>.
	/// </summary>
	public sealed class NetworkAvailabilityService : INetworkAvailabilityService, IStartable
	{
		/// <summary>
		/// Perform once-off startup processing.
		/// </summary>
		public void Start()
		{
			NetworkAvailable = true;

			NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;

			NetworkInformation_NetworkStatusChanged( this );
		}

		private void NetworkInformation_NetworkStatusChanged( object sender )
		{
			var profile = NetworkInformation.GetInternetConnectionProfile();

			var oldNetworkAvailable = NetworkAvailable;

			NetworkAvailable = profile != null;

			if( NetworkAvailable != oldNetworkAvailable && NetworkAvailabilityChanged != null )
				NetworkAvailabilityChanged( this, EventArgs.Empty );
		}

		/// <summary>
		/// Gets a value indicating whether network connection is available.
		/// </summary>
		/// <value>
		/// <c>true</c> if network connection available; otherwise, <c>false</c>.
		/// </value>
		public bool NetworkAvailable { get; private set; }
		/// <summary>
		/// Occurs when network availability changed.
		/// </summary>
		public event EventHandler NetworkAvailabilityChanged;
	}
}
