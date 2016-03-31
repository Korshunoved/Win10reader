using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using BookParser;
using BookParser.Data;
using BookParser.Models;

namespace BookRender.Tools
{
    public static class ToolsRepository
    {
        internal static void SaveAnchors(IEnumerable<AnchorModel> anchors)
        {

        }

        internal static void SaveChapters(IEnumerable<ChapterModel> chapters, string path)
        {
            var chapterModels = chapters as IList<ChapterModel> ?? chapters.ToList();
            AppSettings.Default.Chapters = chapterModels;
            var document = new XDocument();
            var root = new XElement("chapters");
            document.Add(root);
            var xmlChapters = chapterModels.ToList();
            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var outFile = storage.CreateFile(path))
                {
                    foreach (var chapterModel in xmlChapters)
                    {
                        var chapterNode = new XElement("chapter");
                        chapterNode.Add(new XAttribute("ItemID", chapterModel.ItemID));
                        chapterNode.Add(new XAttribute("Level", chapterModel.Level));
                        chapterNode.Add(new XAttribute("MinTokenID", chapterModel.MinTokenID));
                        chapterNode.Add(new XAttribute("Title", chapterModel.Title));
                        chapterNode.Add(new XAttribute("TokenID", chapterModel.TokenID));
                        root.Add(chapterNode);
                    }
                    document.Save(outFile);
                }
            }
        }

        public static IEnumerable<ChapterModel> GetChapters(string bookId, string path)
        {                                    
            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var inFile = storage.OpenFile(path, FileMode.Open, FileAccess.Read))
                {
                    var document = XDocument.Load(inFile);
                    var chapters = new List<ChapterModel>();
                    foreach (var el in document.Root.Elements())
                    {
                        var chapter = new ChapterModel {BookID = bookId};
                        foreach (var xAttribute in el.Attributes())
                        {
                            switch (xAttribute.Name.ToString())
                            {
                                case "ItemID":
                                    chapter.ItemID = int.Parse(xAttribute.Value);
                                    break;
                                case "Level":
                                    chapter.Level = int.Parse(xAttribute.Value);
                                    break;
                                case "MinTokenID":
                                    chapter.MinTokenID = int.Parse(xAttribute.Value);
                                    break;
                                case "Title":
                                    chapter.Title = xAttribute.Value;
                                    break;
                                case "TokenID":
                                    chapter.TokenID = int.Parse(xAttribute.Value);
                                    break;
                            }
                        }
                        chapters.Add(chapter);
                    }
                    return chapters;
                }
            }               
        }

        internal static int GetAnchorsTokenId(string linkId, string bookId)
        {
            return 0;
        }

        internal static IEnumerable<BookImage> GetImages(string bookId)
        {
            try
            {
                using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    var imagesFilePath = Path.Combine(ModelConstants.BooksFolder + bookId + ModelConstants.BookImagesFileName);
                    var trialbookImagesFilePath =
                        Path.Combine(ModelConstants.BooksFolder + bookId + ".trial" + ModelConstants.BookImagesFileName);
                    IsolatedStorageFileStream imagesFileStream;
                    try
                    {
                        imagesFileStream = storage.OpenFile(imagesFilePath, FileMode.Open, FileAccess.Read);
                    }
                    catch (Exception)
                    {
                        imagesFileStream = storage.OpenFile(trialbookImagesFilePath, FileMode.Open, FileAccess.Read);                      
                    }
                    using (imagesFileStream)
                    {
                        var imagesXml = XDocument.Load(imagesFileStream).Root;

                        if (imagesXml == null)
                        {
                            throw new Exception("Can't load images. Something wrong with document");
                        }
                        return imagesXml.Elements("image").Select(t => new BookImage(t)).ToList();
                    }
                }
            }
            catch (Exception)
            {
                return new List<BookImage>();
            }
        }
    }
}