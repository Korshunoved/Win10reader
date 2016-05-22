using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace LitResReadW10.Controls
{
    public class LoopItemsPanel : Panel
    {
        // hack to animate a value easy
        private readonly Slider _sliderHorizontal;
        private readonly TimeSpan _animationDuration = TimeSpan.FromMilliseconds(1200);
        // separating offset
        private double _offsetSeparator;

        // item height. must be 1d to fire first arrangeoverride
        private double _itemWidth = 1d;

        // true when arrange override is ok
        private bool _templateApplied;
        private int _currentIndex;

        public LoopItemsPanel()
        {
            ManipulationMode = (ManipulationModes.TranslateX | ManipulationModes.TranslateInertia);
            ManipulationDelta += OnManipulationDelta;
            ManipulationCompleted += OnManipulationCompleted;
            Tapped += OnTapped;            
            _sliderHorizontal = new Slider
            {
                SmallChange = 0.0000000001,
                Minimum = double.MinValue,
                Maximum = double.MaxValue,
                StepFrequency = 0.0000000001
            };
            _sliderHorizontal.ValueChanged += OnHorizontalOffsetChanged;

            var waitingTimer = new DispatcherTimer {Interval = new TimeSpan(0, 0, 1)};
            waitingTimer.Tick += WaitingTimerOnTick;
            waitingTimer.Start();
            
        }       

        private void WaitingTimerOnTick(object sender, object o)
        {
            if (Children.Count <= 0) return;
            var rnd = new Random();
            var index = rnd.Next(0, Children.Count - 1);
            _currentIndex = 0;
            var item = Children[index];            
            var rect =
                item.TransformToVisual(this)
                    .TransformBounds(new Rect(0, 0, item.DesiredSize.Width, item.DesiredSize.Height));
           // ScrollToSelectedIndex(item, rect, false);
            var timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 5) };
            timer.Tick += TimerOnTick;
            timer.Start();
            var waitingTimer = sender as DispatcherTimer;
            if (waitingTimer == null) return;
            waitingTimer.Stop();
            waitingTimer.Tick -= WaitingTimerOnTick;
        }

        private void TimerOnTick(object sender, object o)
        {           
            _currentIndex++;
            if (_currentIndex >= Children.Count) _currentIndex = 0;
            var item = Children[_currentIndex];
            var rect =
                item.TransformToVisual(this)
                    .TransformBounds(new Rect(0, 0, item.DesiredSize.Width, item.DesiredSize.Height));
            ScrollToSelectedIndex(item, rect, true);
        }

        private void OnHorizontalOffsetChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            UpdatePositions(e.NewValue - e.OldValue);
        }

        private void OnTapped(object sender, TappedRoutedEventArgs args)
        {
            if (Children == null || Children.Count == 0)
                return;

            var positionX = args.GetPosition(this).X;

            foreach (var child in Children)
            {
                var rect =
                    child.TransformToVisual(this)
                        .TransformBounds(new Rect(0, 0, child.DesiredSize.Width, child.DesiredSize.Height));

                if (!(positionX >= rect.X) || !(positionX <= (rect.X + rect.Height))) continue;

                // scroll to Selected
                ScrollToSelectedIndex(child, rect, true);

                break;
            }
        }


        /// <summary>
        /// Get Items Source count items
        /// </summary>
        /// <returns></returns>
        private int GetItemsCount()
        {
            return Children?.Count ?? 0;
        }


        /// <summary>
        /// On manipulation delta
        /// </summary>
        private void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (e == null)
                return;

            var translation = e.Delta.Translation;
            UpdatePositions(translation.X/2);
        }

        private void OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            //if (e == null)
            //    return;

            //var translation = e.Position;
            //this.UpdatePositions(translation.X*2);
        }


        private void ScrollToSelectedIndex(UIElement selectedItem, Rect rect, bool useAnimation)
        {
            if (!_templateApplied)
                return;

            // Apply Transform
            TranslateTransform compositeTransform = (TranslateTransform) selectedItem.RenderTransform;

            if (compositeTransform == null)
                return;

            var centerTopOffset = (ActualWidth/2d) - (_itemWidth)/2d;

            var deltaOffset = centerTopOffset - rect.X;

            if (useAnimation) UpdatePositionsWithAnimation(compositeTransform.X, compositeTransform.X + deltaOffset);
            else UpdatePositions(compositeTransform.X + deltaOffset);
        }

        /// <summary>
        /// Updating with an animation (after a tap)
        /// </summary>
        private void UpdatePositionsWithAnimation(Double fromOffset, Double toOffset)
        {
            var storyboard = new Storyboard();

            var animationSnap = new DoubleAnimation
            {
                EnableDependentAnimation = true,
                From = fromOffset,
                To = toOffset,
                Duration = _animationDuration,
                EasingFunction = new ExponentialEase {EasingMode = EasingMode.EaseInOut}
            };

            storyboard.Children.Add(animationSnap);

            Storyboard.SetTarget(animationSnap, _sliderHorizontal);
            Storyboard.SetTargetProperty(animationSnap, "Value");

            _sliderHorizontal.ValueChanged -= OnHorizontalOffsetChanged;
            _sliderHorizontal.Value = fromOffset;
            _sliderHorizontal.ValueChanged += OnHorizontalOffsetChanged;

            storyboard.Begin();
        }

        /// <summary>
        /// Updating position
        /// </summary>
        private void UpdatePositions(Double offsetDelta)
        {
            Double maxLogicalHeight = GetItemsCount()*_itemWidth;

            // Reaffect correct offsetSeparator
            _offsetSeparator = (_offsetSeparator + offsetDelta)%maxLogicalHeight;

            // Get the correct number item
            Int32 itemNumberSeparator = (Int32) (Math.Abs(_offsetSeparator)/_itemWidth);

            Int32 itemIndexChanging;
            Double offsetAfter;
            Double offsetBefore;

            if (_offsetSeparator > 0)
            {
                itemIndexChanging = GetItemsCount() - itemNumberSeparator - 1;
                offsetAfter = _offsetSeparator;

                if (_offsetSeparator%maxLogicalHeight == 0)
                    itemIndexChanging++;

                offsetBefore = offsetAfter - maxLogicalHeight;
            }
            else
            {
                itemIndexChanging = itemNumberSeparator;
                offsetBefore = _offsetSeparator;
                offsetAfter = maxLogicalHeight + offsetBefore;
            }

            // items that must be before
            UpdatePosition(itemIndexChanging, GetItemsCount(), offsetBefore);

            // items that must be after
            UpdatePosition(0, itemIndexChanging, offsetAfter);
        }

        /// <summary>
        /// Translate items to a new offset
        /// </summary>
        private void UpdatePosition(Int32 startIndex, Int32 endIndex, Double offset)
        {
            for (Int32 i = startIndex; i < endIndex; i++)
            {
                UIElement loopListItem = Children[i];

                // Apply Transform
                TranslateTransform compositeTransform = (TranslateTransform) loopListItem.RenderTransform;

                if (compositeTransform == null)
                    continue;
                compositeTransform.X = offset;
            }
        }

        /// <summary>
        /// Arrange all items
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            // Clip to ensure items dont override container
            Clip = new RectangleGeometry {Rect = new Rect(0, 0, finalSize.Width, finalSize.Height)};

            Double positionLeft = 0d;

            // Must Create looping items count
            foreach (UIElement item in Children)
            {
                if (item == null)
                    continue;

                Size desiredSize = item.DesiredSize;

                if (double.IsNaN(desiredSize.Width) || double.IsNaN(desiredSize.Height)) continue;

                // Get rect position
                var rect = new Rect(positionLeft, 0, desiredSize.Width, desiredSize.Height);
                item.Arrange(rect);

                // set internal CompositeTransform to handle movement
                TranslateTransform compositeTransform = new TranslateTransform();
                item.RenderTransform = compositeTransform;


                positionLeft += desiredSize.Width;
            }

            _templateApplied = true;

            return finalSize;
        }

        /// <summary>
        /// Measure items 
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size s = base.MeasureOverride(availableSize);

            // set good cliping
            Clip = new RectangleGeometry {Rect = new Rect(0, 0, s.Width, s.Height)};

            // Measure all items
            foreach (UIElement container in Children)
            {
                container.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

                if (_itemWidth != container.DesiredSize.Width)
                    _itemWidth = container.DesiredSize.Width;
            }

            return (s);
        }
    }
}
