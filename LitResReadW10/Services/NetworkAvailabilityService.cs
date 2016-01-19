using System;
using System.Net.NetworkInformation;
using Digillect.Mvvm.Services;
using Microsoft.Phone.Net.NetworkInformation;
using Autofac;

namespace LitRes.Services
{
	//ToDo: Update servise in mvvm base library
	/// <summary>
	/// Custom implementation of <see cref="Digillect.Mvvm.Services.INetworkAvailabilityService"/> for Windows Phone 7/8.
	/// </summary>
	public sealed class NetworkAvailabilityService : INetworkAvailabilityService, IStartable
	{
		#region Constructors/Disposer
		/// <summary>
		/// Initializes a new instance of the NetworkAvailabilityService class.
		/// </summary>
		public NetworkAvailabilityService()
		{
		}
		#endregion

		#region Start
		/// <summary>
		/// Initializes a new instance of the <see cref="NetworkAvailabilityService"/> class.
		/// </summary>
		public void Start()
		{
			NetworkAvailable = true;

			DeviceNetworkInformation.NetworkAvailabilityChanged += DeviceNetworkInformation_NetworkAvailabilityChanged;

			DeviceNetworkInformation_NetworkAvailabilityChanged( null, null );
		}
		#endregion

		#region Update
		/// <summary>
		/// Update to current network state.
		/// </summary>
		public void Update()
		{
			DeviceNetworkInformation_NetworkAvailabilityChanged( null, null );
		}
		#endregion

		/// <summary>
		/// Gets a value indicating whether network connection is available.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if network connection available; otherwise, <c>false</c>.
		/// </value>
		public bool NetworkAvailable { get; private set; }
		/// <summary>
		/// Occurs when network availability changed.
		/// </summary>
		public event EventHandler NetworkAvailabilityChanged;

		void DeviceNetworkInformation_NetworkAvailabilityChanged(object sender, NetworkNotificationEventArgs e)
		{
			var oldNetworkAvailable = NetworkAvailable;

			NetworkAvailable = DeviceNetworkInformation.IsNetworkAvailable;

			if (NetworkAvailable != oldNetworkAvailable && NetworkAvailabilityChanged != null)
			{
				NetworkAvailabilityChanged(this, EventArgs.Empty);
			}
		}
	}
}