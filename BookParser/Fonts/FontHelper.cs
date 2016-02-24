
using System;
using System.Collections.Generic;
using System.Threading;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using BookParser.Common.ExtensionMethods;

namespace BookParser.Fonts
{
    public class FontHelper : IFontHelper
    {
        private Dictionary<long, Size> _cache = new Dictionary<long, Size>();
        public FontFamily FontFamily { get; set; }

        public FontHelper(string family)
        {
            FontFamily = new FontFamily(family);
        }

        public Size GetSize(char c, double fontSize, bool bold = false, bool italic = false)
        {
            long hash = GetHash(c, fontSize, bold, italic);
            if (!_cache.ContainsKey(hash))
            {
                lock (this)
                {
                    if (!_cache.ContainsKey(hash))
                        _cache[hash] = InternalGetSize(c, fontSize, bold, italic);
                }
            }
            return _cache[hash];
        }

        private static long GetHash(char c, double fontSize, bool bold, bool italic)
        {
            return ((long)c << 16) + ((((long)(fontSize * 5) << 1) + (bold ? 1L : 0L) << 1) + (italic ? 1L : 0L));
        }

        private Size InternalGetSize(char c, double fontSize, bool bold, bool italic)
        {
            var @event = new AutoResetEvent(false);
            var size = new Size();
            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () => {
                        var textBlock = new TextBlock
                        {
                            Text = Convert.ToString(c),
                            FontSize = fontSize,
                            FontFamily = FontFamily,
                            FontStyle = italic ? FontStyle.Italic : FontStyle.Normal,
                            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal
                        };
                        textBlock.Measure(new Size(1024.0, 1024.0));
                        size = new Size(textBlock.ActualWidth, textBlock.ActualHeight);
                        @event.Set();
                    });
            @event.WaitOne();
            return size;
        }
    }
}