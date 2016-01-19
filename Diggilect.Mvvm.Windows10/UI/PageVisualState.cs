namespace Digillect.Mvvm.UI
{
	/// <summary>
	///     Specifies the page view states.
	/// </summary>
	public enum PageVisualState
	{
		/// <summary>
		///     The current app's view is in full-screen (has no snapped app adjacent to it), and has changed to landscape orientation.
		/// </summary>
		FullScreenLandscape,

		/// <summary>
		///     The current app's view has been snapped.
		/// </summary>
		Snapped,

		/// <summary>
		///     The current app's view is in full-screen (has no snapped app adjacent to it), and has changed to portrait orientation.
		/// </summary>
		FullScreenPortrait
	}
}
