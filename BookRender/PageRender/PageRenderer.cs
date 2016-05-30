/*
 * Author: Vitaly Leschenko, CactusSoft (http://cactussoft.biz/), 2013
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using BookParser;
using BookParser.Common;
using BookParser.Data;
using BookParser.Models;
using BookParser.TextStructure;
using BookParser.Tokens;
using BookRender.RenderData;

namespace BookRender.PageRender
{
    public class PageRenderer
    {
        private readonly ContextBuilder _contextBuilder;

        public PageRenderer(List<BookImage> images)
        {
            _contextBuilder = new ContextBuilder(images);
        }

        public async Task<PageRenderData> RenderPageAsync(RenderPageRequest request)
        {
            var context = new PageRenderData
                          {
                              Page = request.Page,
                              Book = request.Book,
                              Bookmarks = request.Bookmarks,
                              PageSize = new Size(
                                  request.Panel.Width - AppSettings.Default.Margin.Left - AppSettings.Default.Margin.Right,
                                  request.Panel.Height - AppSettings.Default.Margin.Top - AppSettings.Default.Margin.Bottom),
                              OffsetX = (int) AppSettings.Default.Margin.Left,
                              OffsetY = (int) AppSettings.Default.Margin.Top,
                              Panel = request.Panel,
                              BackgroundBrush = AppSettings.Default.ColorScheme.BackgroundBrush,
                              LinkBrush =  AppSettings.Default.ColorScheme.LinkForegroundBrush.Color,
                              Links = request.Links,
                              Texts = request.Texts,
                          };

            await Task.Factory.StartNew(() => _contextBuilder.BuildContexts(context));
            
            Draw(context);

            return context;
        }

        private void Draw(PageRenderData context)
        {
            Panel panel = context.Panel;
            panel.Background = context.BackgroundBrush;
            panel.Children.Clear();

            foreach (BookmarkModel bookmarkModel in GetHighlights(context))
            {
                var highlight = RenderHighlight(bookmarkModel, panel, context);
                if (highlight != null)
                    panel.Children.Insert(0, highlight);
            }
            IEnumerable<WordRenderData> words = null;
            var found = false;
            foreach (WordRenderData wordContext in context.Words.Where(wordContext => wordContext.Text == "Полный"))
            {
                words = context.Words.SkipWhile(a => a != wordContext);
                var query = words.Aggregate("", (current, word) => current + word.Text).Replace("-",string.Empty);
                if (!query.Contains("Полный текст доступен на www.litres.ru")) continue;
                found = true;
                break;
            }
            if (found)
                context.Words.RemoveAll(a => words.Contains(a));

            foreach (var textBlock in context.Words.Select(RenderWordContext))
            {
                panel.Children.Add(textBlock);
            }
            foreach (var image in context.Images.Select(RenderImageContext))
            {
                panel.Children.Add(image);
            }
        }

        private static Image RenderImageContext(ImageRenderData imageRenderData)
        {
            var imageBytes = Convert.FromBase64String(imageRenderData.Base64String);
            var image = new BitmapImage();
            using (InMemoryRandomAccessStream ms = new InMemoryRandomAccessStream())
            {
                using (DataWriter writer = new DataWriter(ms.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes((byte[])imageBytes);
                    writer.StoreAsync().GetResults();
                }
                image.SetSource(ms);
            }

            return new Image
                        {
                            Margin = imageRenderData.Margin,
                            VerticalAlignment = VerticalAlignment.Top,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Source = image,
                            Width = imageRenderData.Width,
                            Height = imageRenderData.Height
                        };
        }

        private static TextBlock RenderWordContext(WordRenderData wordRenderData)
        {
            var textBlock = new TextBlock
                            {
                                Margin = new Thickness(wordRenderData.Left, wordRenderData.Top, 0.0, 0.0),
                                FontSize = wordRenderData.FontSize,
                                FontFamily = AppSettings.Default.FontSettings.FontFamily,
                                FontStyle = wordRenderData.Italic ? FontStyle.Italic : FontStyle.Normal,
                                FontWeight = wordRenderData.Bold ? FontWeights.Bold : FontWeights.Normal,
                                Foreground = wordRenderData.Color.HasValue
                                    ? new SolidColorBrush(wordRenderData.Color.Value)
                                    : AppSettings.Default.ColorScheme.TextForegroundBrush,
                                Text = wordRenderData.Text
                            };

            return textBlock;
        }

        private static Polygon RenderHighlight(BookmarkModel bookmarkModel, Panel canvas, PageRenderData context)
        {
            int tokenID = bookmarkModel.TokenID;
            TextRenderData firstHighlightedText = context.Texts.FirstOrDefault(t => t.TokenID == tokenID) ??
                                        (context.Texts).FirstOrDefault();

            if (firstHighlightedText == null) 
                return null;

            int endTokenID = bookmarkModel.EndTokenID;
            TextRenderData lastHighlightedText = context.Texts.LastOrDefault(t => t.TokenID == endTokenID) ??
                                                    (context.Texts).LastOrDefault();
            if (lastHighlightedText == null) 
                return null;

            Rect firtstRect = firstHighlightedText.Rect;
            Rect lastRect = lastHighlightedText.Rect;
            var pointCollection = SelectionHelper.GetSelectionPolygon(
                firtstRect, 
                lastRect,
                canvas.ActualWidth, 
                context.OffsetX,
                Convert.ToDouble(AppSettings.Default.FontSettings.FontInterval));

            var polygon = new Polygon
                              {
                                  Width = canvas.ActualWidth,
                                  Height = canvas.ActualHeight,
                                  Fill = new SolidColorBrush(ColorHelper.ToColor(bookmarkModel.Color)) {Opacity = 0.3},
                                  Points = pointCollection,
                                  Visibility = Visibility.Visible
                              };
            return polygon;
            
        }

        private static IEnumerable<BookmarkModel> GetHighlights(PageRenderData context)
        {
            int left = context.Page.FirstTokenID;

            int right = context.Page.LastTokenID;
            var lastTextToken = context.Page.Lines.OfType<TextTokenBlock>().LastOrDefault();
            if (lastTextToken != null)
            {
                var lastTextElement = lastTextToken.Inlines.OfType<TextElement>().LastOrDefault();
                if (lastTextElement != null)
                {
                    right = lastTextElement.TokenID;
                }
            }

            return context.Bookmarks.Where((t =>
                                              {
                                                  if (t.TokenID >= left && t.TokenID <= right)
                                                      return true;
                                                  if (t.TokenID < left && t.EndTokenID > right)
                                                      return true;
                                                  if (t.EndTokenID != -1 && t.EndTokenID >= left)
                                                      return t.EndTokenID <= right;
                                                  return false;
                                              })).ToList();
        }
    }
}