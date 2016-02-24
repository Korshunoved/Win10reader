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

using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using BookParser.Models;
using BookParser.TextStructure;
using BookRender.RenderData;

namespace BookRender.PageRender
{
    public class RenderPageRequest
    {
        public Panel Panel { get; set; }

        public BookModel Book { get; set; }

        public PageInfo Page { get; set; }

        public IList<LinkRenderData> Links { get; set; }

        public IList<TextRenderData> Texts { get; set; }

        public IList<BookmarkModel> Bookmarks { get; set; }
    }
}