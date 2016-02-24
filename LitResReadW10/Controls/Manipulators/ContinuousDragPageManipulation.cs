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

using System;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using BookParser;
using LitResReadW10.Common;

namespace LitResReadW10.Controls.Manipulators
{
    public class ContinuousDragPageManipulation : DragManipulationBase
    {
        public ContinuousDragPageManipulation(ThreePagePanel bookView, FlippingMode mode) : base(bookView, mode)
        {
        }

        protected override void ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            double xVelocity = e.Velocities.Linear.X > 0 ? e.Velocities.Linear.X : 0;

            double totalManipulation = (e.Position.X + xVelocity/20);
            FlipDirection flipDirection = totalManipulation > DRAG_THRESHOLD
                ? FlipDirection.Backward
                : totalManipulation < -DRAG_THRESHOLD ? FlipDirection.Forward : FlipDirection.Revert;

            DoFlip(flipDirection);
        }

        protected override void DoFlip(FlipDirection flipDirection)
        {
            double currentTranslationX = ((TranslateTransform) GetCurrentPagePanel().RenderTransform).X;

            double leftToAnimate;
            switch (flipDirection)
            {
                case FlipDirection.Forward:
                    leftToAnimate = -(Screen.Width + currentTranslationX);
                    break;
                case FlipDirection.Backward:
                    leftToAnimate = Screen.Width - currentTranslationX;
                    break;
                case FlipDirection.Revert:
                    leftToAnimate = -currentTranslationX;
                    break;

                default:
                    throw new NotSupportedException();
            }

            var storyboard = new Storyboard();
            foreach (Panel panel in Panels)
            {
                var doubleTranslateAnimation = new DoubleAnimation
                                               {
                                                   By = leftToAnimate,
                                                   Duration = new Duration(TimeSpan.FromMilliseconds(250)),
                                                   EasingFunction = new SineEase()
                                               };

                Storyboard.SetTarget(doubleTranslateAnimation, panel.RenderTransform);
                Storyboard.SetTargetProperty(doubleTranslateAnimation, new PropertyPath("X").ToString());

                storyboard.Children.Add(doubleTranslateAnimation);
            }

            if (flipDirection != FlipDirection.Revert)
            {
                UnregisterManipulations();

                storyboard.Completed += delegate { TurnPage(flipDirection == FlipDirection.Forward); };
            }

            storyboard.Begin();
        }

        protected override void ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            ApplyToEachPanel(p =>
                             {
                                 var transform = (TranslateTransform) p.RenderTransform;
                                 transform.X += e.Delta.Translation.X;
                             });
        }


        protected override void ResetTranslations()
        {
            ((TranslateTransform) GetCurrentPagePanel().RenderTransform).X = 0;
            ((TranslateTransform) GetNextPagePanel().RenderTransform).X = Screen.Width;
            ((TranslateTransform) GetPrevPagePanel().RenderTransform).X = -Screen.Width;
        }

        public override void UpdatePanelsVisibility()
        {
            Canvas.SetZIndex(GetCurrentPagePanel(), 10);
            Canvas.SetZIndex(GetPrevPagePanel(), 0);
            Canvas.SetZIndex(GetNextPagePanel(), 0);

            ResetTranslations();

            base.UpdatePanelsVisibility();
        }
    }
}