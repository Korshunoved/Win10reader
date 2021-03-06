﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;
using BookParser.Common.ExtensionMethods;
using BookParser.Data;
using BookParser.Extensions;
using BookParser.IO;
using BookParser.Styling;

namespace BookParser.Parsers.Fb2
{
    public class Fb2SummaryParser : BookSummaryParserBase
    {
        private readonly XNamespace _ns;
        private readonly XElement _root;

        public Fb2SummaryParser(Stream source)
        {
            try
            {
                source.Position = 0;
                try
                {
                    var zip = ZipContainer.Unzip(source);
                    source = zip.Files.First().Stream;
                }
                catch (Exception)
                {
                    // ignored
                }
                source.Position = 0;
                XDocument xmlDocument = source.GetXmlDocument();
                _root = xmlDocument.Root;
            }
            catch (Exception)
            {
                throw new Exception("Can't load book.");
            }
          
            if (_root == null)
            {
                throw new Exception("Can't load book.");
            }
            XAttribute attribute = _root.Attribute("xmlns");
            if (attribute == null)
            {
                throw new Exception("Can't load book.");
            }
            _ns = attribute.Value;
            Root = _root;
        }

        public override ITokenParser GetTokenParser()
        {
            Chapters.Clear();
            Anchors.Clear();
            return new Fb2TokenParser(_ns, _root, GetCss(), Chapters, Anchors);
        }

        public override BookSummary GetBookPreview()
        {
            XElement info = _root
                .Elements(_ns + "description")
                .Elements(_ns + "title-info")
                .FirstOrDefault();
            var preview = new BookSummary();
            if (info == null)
                return preview;

            preview.Title = PrepareBookTitle(info);
            preview.AuthorName = PrepareBookAuthor(info);
            preview.Description = PrepareAnnotation(info);
            preview.Language = PrepareLanguage(info);
            preview.UniqueId = PrepareUniqueID();
            return preview;
        }


        public override bool SaveCover(string bookID)
        {
            string cover = GetCoverImageID();
            if (string.IsNullOrEmpty(cover))
                return false;

            XElement xelement = _root.Elements(_ns + "binary")
                                     .Select(b => new { b = b, attr = b.Attribute("id") })
                                     .Where(b => b.attr != null && b.attr.Value == cover)
                                     .Select(t => t.b)
                                     .FirstOrDefault();

            if (xelement == null)
                return false;

            MemoryStream stream;
            try
            {
                stream = new MemoryStream(Convert.FromBase64String(
                    xelement.Value
                            .Replace(" ", string.Empty)
                            .Replace("\n", string.Empty)));
            }
            catch (Exception)
            {
                return false;
            }
            return SaveCoverImages(bookID, stream);
        }

        public override void SaveImages(Stream output)
        {
            var document = new XDocument();
            var images = new XElement("images");
            document.Add(images);
            List<BookImage> xmlImages;
            try
            {
                xmlImages = GetXmlImages().ToList();
            }
            catch (Exception)
            {
                return;
            }            
            foreach (BookImage bookImage in xmlImages)
            {
                try
                {
                    Stream streamSource = bookImage.CreateStream();                    
                    try
                    {
                        streamSource.GetImageSize();
                    }
                    catch (Exception)
                    {
                        
                    }
                    var imageSize = BitmapImageExtension.ImageSize;
                    if (imageSize.Height > 0)
                    {
                        bookImage.Width = (int)imageSize.Width; 
                        bookImage.Height = (int)imageSize.Height;
                    }
                    else
                    {
                        bookImage.Width = 200; 
                        bookImage.Height = 200; 
                    }
                    
                    images.Add(bookImage.Save());
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            document.Save(output);
        }

        private string PrepareAnnotation(XContainer info)
        {
            XElement element = info.Element(_ns + "annotation");
            if (element == null)
                return string.Empty;

            var sb = new StringBuilder();
            var textWriter = new StringWriter(sb);
            element.Save(textWriter, SaveOptions.DisableFormatting);
            return Regex.Replace(Regex.Replace(sb.ToString().Replace("\n", string.Empty), "</p>", "\n"), "<[^>]*>", "");

        }

        private string PrepareBookAuthor(XContainer info)
        {
            XElement author = info.Elements(_ns + "author").FirstOrDefault();
            string fullName = string.Empty;
            if (author == null)
                return string.Empty;

            AddFullName(ref fullName, author, "first-name");
            AddFullName(ref fullName, author, "middle-name");
            AddFullName(ref fullName, author, "last-name");

            return fullName.Trim();
        }

        private string PrepareBookTitle(XContainer info)
        {
            XElement element = info.Elements(_ns + "book-title").FirstOrDefault();
            if (element == null)
                return string.Empty;

            return element.Value;
        }

        private string PrepareLanguage(XElement info)
        {
            XElement element = info.Elements(_ns + "lang").FirstOrDefault();

            return element?.Value;
        }


        private string PrepareUniqueID()
        {
            XElement isbn = _root
                .Elements(_ns + "description")
                .Elements(_ns + "publish-info")
                .Elements(_ns + "isbn")
                .FirstOrDefault();
            if (isbn == null)
                return string.Empty;

            string str = isbn.Value.SafeSubstring(1000);
            return ("isbn://" + str);
        }

        private void AddFullName(ref string fullName, XElement author, string name)
        {
            XElement element = author.Elements(_ns + name).FirstOrDefault();
            if (element != null)
            {
                fullName = fullName.Trim() + " " + element.Value.Trim();
            }
        }

        private string GetCoverImageID()
        {
            XAttribute attribute = _root
                .Elements(_ns + "description")
                .Descendants(_ns + "coverpage")
                .Elements(_ns + "image")
                .Attributes()
                .FirstOrDefault(t => t.Name.LocalName == "href");

            return attribute?.Value.TrimStart('#');
        }

        private IEnumerable<BookImage> GetXmlImages()
        {
            return (from binary in _root.Elements(_ns + "binary")
                    let id = binary.Attribute("id")
                    where id != null
                    let data = binary.Value.Replace(" ", string.Empty).Replace("\n", string.Empty)
                    select new BookImage { ID = id.Value, Data = data });
        }

        private CSS GetCss()
        {
            CSS sheet = new CSS();
            sheet.Analyze(DefaultFb2Css);
            return sheet;
        }

        private const string DefaultFb2Css =
@"sup{vertical-align:super;}
sub{vertical-align:sub;}
p{font-size:1em;text-indent:32px;margin:0px;}
image{margin:8px;}
sup,sub,a,b,i,u,strong,em,emphasis,style,strikethrough,span {display:inline;margin:0px;}
b,strong,epigraphAuthor {font-weight: bold;}
i,em,emphasis,cite,epigraph,epigraphAuthor,style {font-style: italic;}
v,cite {margin-left:25px;margin-bottom:0px;}
title,epigraph,stanza {margin-bottom:12px;}
epigraph,text-author {text-align:right;}
title {text-align:center;}";

    }
}