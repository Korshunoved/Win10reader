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
        internal static void SaveAnchors(IEnumerable<AnchorModel> anchors, string path)
        {
            var anchorModels = anchors as AnchorModel[] ?? anchors.ToArray();
            AppSettings.Default.Anchors = anchorModels;
            var document = new XDocument();
            var root = new XElement("anchors");
            document.Add(root);
            var xmlAnchors = anchorModels.ToList();
            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var outFile = storage.CreateFile(path))
                {
                    foreach (var anchorModel in xmlAnchors)
                    {
                        var node = new XElement("anchor");
                        node.Add(new XAttribute("AnchorID", anchorModel.AnchorID));
                        node.Add(new XAttribute("Name", anchorModel.Name));
                        node.Add(new XAttribute("TokenID", anchorModel.TokenID));
                        node.Add(new XAttribute("BookID", anchorModel.BookID));
                        node.Add(new XAttribute("NameHash", anchorModel.NameHash));
                        root.Add(node);
                    }
                    document.Save(outFile);
                }
            }
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

        public static IEnumerable<AnchorModel> GetAnchors(string bookId, string path)
        {
            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var inFile = storage.OpenFile(path, FileMode.Open, FileAccess.Read))
                {
                    var document = XDocument.Load(inFile);
                    var anchors = new List<AnchorModel>();
                    foreach (var el in document.Root.Elements())
                    {
                        var anchorModel = new AnchorModel { BookID = bookId };
                        foreach (var xAttribute in el.Attributes())
                        {
                            switch (xAttribute.Name.ToString())
                            {  
                                case "AnchorID":
                                    anchorModel.AnchorID = int.Parse(xAttribute.Value);
                                    break;
                                case "Name":
                                    anchorModel.Name = xAttribute.Value;
                                    break;
                                case "TokenID":
                                    anchorModel.TokenID = int.Parse(xAttribute.Value);
                                    break;
                                case "BookID":
                                    anchorModel.BookID = xAttribute.Value;
                                    break;
                                case "NameHash":
                                    anchorModel.NameHash = int.Parse(xAttribute.Value);
                                    break;
                            }
                        }
                        anchors.Add(anchorModel);
                    }
                    return anchors;
                }
            }
        }

        internal static int GetAnchorsTokenId(string linkId, string bookId)
        {
            //try
            //{
            //    using (var bookDataContext = BookDataContext.Connect())
            //    {
            //        var hash = linkId.GetHashCode();
            //        var statement = new Func<AnchorModel, bool>(t => t.BookID == bookId && t.NameHash == hash && t.Name == linkId);

            //        var anchorModel = bookDataContext.Anchors.FirstOrDefault(statement);
            //        return anchorModel != null ? anchorModel.TokenID : -1;
            //    }
            //}
            //catch (Exception)
            //{
            //    return -1;
            //}
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