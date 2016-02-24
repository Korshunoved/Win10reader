/*
 * Author: CactusSoft (http://cactussoft.biz/), 2013
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA
 * 02110-1301, USA.
 */

using Windows.Graphics.Display;
using Windows.UI.Xaml.Controls;

namespace LitResReadW10.Common
{
    public class Screen
    {
        private static Frame _frame;

        private const ResolutionScale WVGA_SCALE_FACTOR = ResolutionScale.Scale100Percent;
        private const ResolutionScale WXGA_SCALE_FACTOR = ResolutionScale.Scale160Percent;
        private const ResolutionScale _720P_SCALE_FACTOR = ResolutionScale.Scale150Percent;

        private static readonly ResolutionScale ScaleFactor = DisplayInformation.GetForCurrentView().ResolutionScale; //Application.Current.Host.Content.ScaleFactor / 100d;

        //480x800
        public static bool IsWVGA
        {
            get
            {
                return DisplayInformation.GetForCurrentView().ResolutionScale == WVGA_SCALE_FACTOR;
            }
        }

        //768x1280
        public static bool IsWXGA
        {
            get
            {
                return DisplayInformation.GetForCurrentView().ResolutionScale == WXGA_SCALE_FACTOR;
            }
        }

        //720x1280 pixels
        public static bool Is720p
        {
            get
            {
                return DisplayInformation.GetForCurrentView().ResolutionScale == _720P_SCALE_FACTOR;
            }
        }

        public static double RoundScalePixel(double value)
        {
            //TODO: fix
            return 0;
            // return (int)(value * ScaleFactor.) / ScaleFactor;
        }


        public static void Init(Frame frame)
        {
            _frame = frame;
        }

        public static Frame Frame
        {
            get
            {
                return _frame;
            }
        }

        public static double Width
        {
            get
            {
                return DisplayInformation.GetForCurrentView().CurrentOrientation == DisplayOrientations.Portrait
                           ? DisplayInformation.GetForCurrentView().RawDpiX
                           : DisplayInformation.GetForCurrentView().RawDpiY;
            }
        }

        public static double Height
        {
            get
            {
                return DisplayInformation.GetForCurrentView().CurrentOrientation != DisplayOrientations.Portrait
                          ? DisplayInformation.GetForCurrentView().RawDpiX
                          : DisplayInformation.GetForCurrentView().RawDpiY;
            }
        }
    }
}
