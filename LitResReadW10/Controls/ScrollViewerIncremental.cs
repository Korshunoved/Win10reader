using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace LitResReadW10.Controls
{
    public class ScrollViewerIncremental : DependencyObject
    {
        public static readonly DependencyProperty LoadMoreProperty =
        DependencyProperty.RegisterAttached(
          "LoadMore",
          typeof(EventHandler),
          typeof(ScrollViewerIncremental),
          new PropertyMetadata(false)
        );

        public static void SetLoadMore(UIElement element, EventHandler value)
        {
            element.SetValue(LoadMoreProperty, value);
            var sv = element as ScrollViewer;
            if (sv != null)
            {
                sv.ViewChanged += (sender, args) =>
                {
                    var verticalOffsetValue = sv.VerticalOffset;
                    var maxVerticalOffsetValue = sv.ExtentHeight - sv.ViewportHeight;
                    if (maxVerticalOffsetValue < 0 || Math.Abs(verticalOffsetValue - maxVerticalOffsetValue) < 1.0)
                    {
                            
                        value.Invoke(sv, EventArgs.Empty);
                    }
                };
            }
        }
        public static EventHandler GetLoadMore(UIElement element)
        {
            return (EventHandler)element.GetValue(LoadMoreProperty);
        }
    }
}
