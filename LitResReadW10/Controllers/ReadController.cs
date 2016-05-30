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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using BookParser;
using BookParser.Data;
using BookParser.Models;
using BookParser.TextStructure;
using BookRender.PageRender;
using BookRender.Tools;

namespace LitResReadW10.Controllers
{
    public class ReadController
    {
        private readonly BookModel _bookModel;
        private readonly PageCompositor _pageLoader;

        private PageInfo _currentPage;
        private PageInfo _prevPage;
        private PageInfo _nextPage;
        private readonly IBookView _bookView;
        private readonly int _offset;
        private readonly List<BookImage> _images;

        public ReadController(IBookView page, BookModel book, string bookId, int offset = 0)
        {
            var data = new BookData(bookId);
            _bookView = page;
            _offset = offset;
            _bookModel = book;
            _images = data.LoadImages().ToList();
            _pageLoader = new PageCompositor(_bookModel, (AppSettings.Default.FontSettings.FontSize), new Size(page.GetSize().Width - AppSettings.Default.Margin.Left - AppSettings.Default.Margin.Right, page.GetSize().Height - AppSettings.Default.Margin.Top - AppSettings.Default.Margin.Bottom), _images);
            BookId = bookId;
        }

        public string BookId { get; private set; }

        public bool IsFirst => _prevPage == null;

        public bool IsLast => _nextPage == null;

        public int CurrentPage => (int) Math.Ceiling((double) (_currentPage.FirstTokenID + 1)/AppSettings.WordsPerPage);

        public int TotalPages => (int) Math.Ceiling((double) _bookModel.TokenCount/AppSettings.WordsPerPage - 1);

        public int Offset => _currentPage.FirstTokenID;

        public async Task ShowNextPage()
        {
            var page = _nextPage ?? await PrepareNextPage();
            if (page != null)
            {
                _prevPage = _currentPage;
                _currentPage = page;
                _nextPage = null;
                _bookView.SwapNextWithCurrent();

                await PrepareNextPage();

                if (_prevPage == null)
                    await PreparePrevPage();
            }
        }

        public async Task ShowPrevPage()
        {            
            var page = _prevPage ?? await PreparePrevPage();
            if (page != null)
            {
                _nextPage = _currentPage;
                _currentPage = page;
                _prevPage = null;
                _bookView.SwapPrevWithCurrent();

                await PreparePrevPage();
            }
        }

        public async Task<PageInfo> PreparePrevPage()
        {
            if (_currentPage == null)
                return null;

            string startText = string.Empty;
            if (_currentPage != null)
            {
                startText = _currentPage.StartText;
            }

            var page = await _pageLoader.GetPreviousPageAsync(_currentPage.FirstTokenID, startText);
            if (page == null || page.FirstTokenID < 0)
                return null;

            _prevPage = page;

            var bgBuilder = new PageRenderer(_images);
            _bookView.PreviousTexts.Clear();
            _bookView.PreviousLinks.Clear();
            await bgBuilder.RenderPageAsync(new RenderPageRequest()
            {
                Page = page,
                Panel = _bookView.GetPrevPagePanel(),
                Texts = _bookView.PreviousTexts,
                Links=  _bookView.PreviousLinks,
                Book = _bookModel,
                Bookmarks = _bookView.Bookmarks
            });

            return page;
        }

        public async Task<PageInfo> PrepareNextPage()
        {
            string lastText = string.Empty;
            if (_currentPage != null)
            {
                lastText = _currentPage.LastTextPart;
            }

            int nextTokenId = _currentPage?.LastTokenID + 1 ?? _offset;

            var page = await _pageLoader.GetPageAsync(nextTokenId, lastText);
            if (page == null || page.FirstTokenID < 0)
                return null;

            _nextPage = page;

            var bgBuilder = new PageRenderer(_images);
            _bookView.NextTexts.Clear();
            _bookView.NextLinks.Clear();
            await bgBuilder.RenderPageAsync(new RenderPageRequest()
            {
                Page = page,
                Panel = _bookView.GetNextPagePanel(),
                Texts = _bookView.NextTexts,
                Book = _bookModel,
                Links = _bookView.NextLinks,
                Bookmarks = _bookView.Bookmarks
            });            
            return page;
        }
    }
}
