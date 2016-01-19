using System;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using LitResReadW10;

namespace LitRes.Helpers
{
	public enum Resolutions { WVGA, WXGA, HD720p, HD1080p };

	public static class ResolutionHelper
	{
		private static bool IsWvga
		{
			get
			{
                
				return DisplayInformation.GetForCurrentView().LogicalDpi == 100;
			}
		}

		private static bool IsWxga
		{
			get
			{
				return DisplayInformation.GetForCurrentView().LogicalDpi == 160;
			}
		}

		private static bool Is720p
		{
			get
			{
				return DisplayInformation.GetForCurrentView().LogicalDpi == 150;
			}
		}

	    public static bool isFullHD
	    {
	        get
	        {
	            try
	            {
                    //DisplayInformation.GetForCurrentView().LogicalDpi
                    //var resolution = (Size)DeviceExtendedProperties.GetValue("PhysicalScreenResolution");
                    //if (resolution.Width == 1080.0 && resolution.Height == 1920.0) return true;
	            }
	            catch (Exception ex)
	            {
	                return false;
	            }

	            return false;
	        }
	    }

		public static Resolutions CurrentResolution
		{
			get
			{
				if ( IsWvga )
					return Resolutions.WVGA;
				else if ( IsWxga )
					return Resolutions.WXGA;
				else if ( Is720p )
					return Resolutions.HD720p;
				else
					return  Resolutions.WXGA;
			}
		}

		public static int GetActualHeight()
		{
			return (int) Window.Current.Bounds.Height;
		}

		public static int GetActualWidth()
		{
			return (int) Window.Current.Bounds.Width;
		}

	}
}
