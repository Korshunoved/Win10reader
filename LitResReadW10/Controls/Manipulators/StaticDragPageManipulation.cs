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

using Windows.UI.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using BookParser;

namespace LitResReadW10.Controls.Manipulators
{
    public class StaticDragPageManipulation : DragManipulationBase
    {
        public StaticDragPageManipulation(ThreePagePanel bookView, FlippingMode mode) : base(bookView, mode)
        {
        }

        protected override void ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            double totalManipulation = (e.Position.X + e.Velocities.Linear.X/20);
            FlipDirection flipDirection = totalManipulation > DRAG_THRESHOLD
                ? FlipDirection.Backward
                : totalManipulation < -DRAG_THRESHOLD ? FlipDirection.Forward : FlipDirection.Revert;

            DoFlip(flipDirection);
        }

        protected override void DoFlip(FlipDirection flipDirection)
        {
            if (flipDirection != FlipDirection.Revert)
            {
                UnregisterManipulations();

                TurnPage(flipDirection == FlipDirection.Forward);
            }
        }

        protected override void ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // Do nothing
        }

        public override void UpdatePanelsVisibility()
        {
            Canvas.SetZIndex(GetCurrentPagePanel(), 10);
            Canvas.SetZIndex(GetPrevPagePanel(), 0);
            Canvas.SetZIndex(GetNextPagePanel(), 0);

            ResetTranslations();

            base.UpdatePanelsVisibility();
        }

        protected override void ResetTranslations()
        {
            ApplyToEachPanel(p => ((TranslateTransform) p.RenderTransform).X = 0);
        }
    }
}