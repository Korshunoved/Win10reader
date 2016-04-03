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
using System.IO.IsolatedStorage;
using System.Xml.Linq;

namespace BookParser.Models
{  
  
    public class BookModel : BaseTable
    {
      
        public string BookID { get; set; }

       
        public string Type { get; set; }

      
        public string Title { get; set; }

     
        public string Author { get; set; }

        [Obsolete]
        public int AuthorHash { get; set; }

        
        public int CurrentTokenID { get; set; }

       
        public int TokenCount { get; set; }

       
        public bool Hidden { get; set; }

       
        public bool Trial { get; set; }

       
        public long LastUsage { get; set; }

       
        public int WordCount { get; set; }

       
        public bool Deleted { get; set; }

       
        public long? CreatedDate { get; set; }

       
        public string UniqueID { get; set; }

       
        public string Url { get; set; }

      
        public bool IsFavourite { get; set; }

     
        public string CatalogItemId { get; set; }

      
        public string Language { get; set; }

     
        public string Description { get; set; }

        public void LoadInfo(string path)
        {
            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var inFile = storage.OpenFile(path, FileMode.Open, FileAccess.Read))
                {
                    var document = XDocument.Load(inFile);                    
                    foreach (var el in document.Root.Elements())
                    {
                        switch (el.Name.ToString())
                        {
                            case "tokenCount":
                                TokenCount = int.Parse(el.Value);
                                break;
                            case "wordCount":
                                WordCount = int.Parse(el.Value);
                                break;
                        }
                    }
                }
            }
        }

        public List<BookmarkModel> LoadBookmarks(string path)
        {
            try
            {
                using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    var document = new XDocument();
                    using (var inFile = storage.OpenFile(path, FileMode.Open, FileAccess.Read))
                    {
                        document = XDocument.Load(inFile);
                        inFile.Dispose();
                    }
                    var list = new List<BookmarkModel>();
                    foreach (var xElement in document.Root.Elements())
                    {
                        var bookmark = new BookmarkModel();
                        foreach (var attr in xElement.Attributes())
                        {
                            switch (attr.Name.ToString())
                            {
                                case "id":
                                    bookmark.BookmarkID = attr.Value;
                                    break;
                                case "tokenId":
                                    bookmark.TokenID = int.Parse(attr.Value);
                                    break;
                                case "percent":
                                    bookmark.Percent = attr.Value;
                                    break;
                                case "currentPage":
                                    bookmark.CurrentPage = int.Parse(attr.Value);
                                    break;
                                case "pages":
                                    bookmark.Pages = int.Parse(attr.Value);
                                    break;
                                case "chapter":
                                    bookmark.Chapter = attr.Value;
                                    break;
                                case "text":
                                    bookmark.Text = attr.Value;
                                    break;
                            }
                        }
                        list.Add(bookmark);
                    }
                    return list;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void SaveBookmark(string path, BookmarkModel bookmark)
        {
            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var exist = storage.FileExists(path);
                var document = new XDocument();
                var isAdded = false;
                if (exist)
                {
                    using (var inFile = storage.OpenFile(path, FileMode.Open, FileAccess.Read))
                    {
                        document = XDocument.Load(inFile);
                        var root = document.Root;
                        root.Add(PrepareBookmark(bookmark));
                        isAdded = true;
                        inFile.Dispose();
                    }
                }
                using (var outFile = storage.OpenFile(path, FileMode.Create, FileAccess.Write))
                {
                    var root = document.Root ?? new XElement("bookmarks");
                    if (document.Root == null) document.Add(root);
                    if (!isAdded)
                        root.Add(PrepareBookmark(bookmark));
                    document.Save(outFile);
                }
            }
        }

        public XElement PrepareBookmark(BookmarkModel bookmark)
        {
            var bookmarkElem = new XElement("bookmark");
            bookmarkElem.Add(new XAttribute("id", bookmark.BookmarkID));
            bookmarkElem.Add(new XAttribute("tokenId", bookmark.TokenID));
            bookmarkElem.Add(new XAttribute("percent", bookmark.Percent));
            bookmarkElem.Add(new XAttribute("currentPage", bookmark.CurrentPage));
            bookmarkElem.Add(new XAttribute("pages", bookmark.Pages));
            bookmarkElem.Add(new XAttribute("chapter", bookmark.Chapter));
            bookmarkElem.Add(new XAttribute("text", bookmark.Text ?? string.Empty));
            return bookmarkElem;
        }
    }
}