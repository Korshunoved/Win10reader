
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Xml.Linq;
using BookParser.Common;
using BookParser.Data;
using BookParser.IO;
using BookParser.Models;
using BookParser.Parsers;
using BookParser.Tokens;

namespace BookRender.Tools
{
    public static class TokensTool
    {
        const string CatalogPath = "MyBooks/";

        public static List<int> GetTokens(string id)
        {
            var tokensCache = new Cache<string, List<int>>(delegate
            {
                FileWrapper file = null;
                try
                {
                    file =
                        FileStorage.Instance.GetFile(
                            Path.Combine(CatalogPath + id + ModelConstants.BookFileDataRefPath));
                }
                catch (Exception)
                {

                    file =
                        FileStorage.Instance.GetFile(
                            Path.Combine(CatalogPath + id + ".trial" + ModelConstants.BookFileDataRefPath));
                }
                using (file)
                {
                    using (file.Lock())
                    {
                        file.Seek(0, SeekOrigin.Begin);
                        var capacity = file.Reader.ReadInt32();
                        var list = new List<int>(capacity);
                        for (var index = 0; index < capacity; ++index)
                        {
                            list.Add(file.Reader.ReadInt32());
                        }
                        return list;
                    }
                }
            });

            return tokensCache.Get(id);
        }

        public static void SaveTokens(BookModel book, IBookSummaryParser parser)
        {
            var tokens = parser.GetTokenParser().GetTokens().ToList();
            var positions = SaveTokensWithPosition(book.GetTokensPath(), tokens);

            SaveTokenPosition(book.GetTokensRefPath(), positions);

            book.TokenCount = positions.Count;
            book.WordCount = tokens.Count(t => t is TextToken);
            book.CurrentTokenID = Math.Min(tokens.Count - 1, book.CurrentTokenID);

            parser.BuildChapters();

            SaveAnchors(book.BookID, book.GetAnchorsPath(), parser.Anchors, tokens);

            SaveChapters(book.BookID, book.GetChaptersPath(), parser.Chapters, tokens);
           
            SaveInfo(book.GetFolderPath(), book.TokenCount, book.WordCount);
        }

        private static void SaveInfo(string path, int tokenCount, int wordCount)
        {
            var filepath = path + "/bookinfo";
            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var document = new XDocument();
                using (var outFile = storage.CreateFile(filepath))
                {
                    var root = new XElement("info");
                    document.Add(root);
                    root.Add(new XElement("tokenCount", tokenCount));
                    root.Add(new XElement("wordCount", wordCount));
                    document.Save(outFile);
                }
            }
        }

        private static void SaveAnchors(string bookId, string path, Dictionary<string, int> anchors, IList<TokenBase> tokens)
        {
            var anchModels = anchors.Select(anchor => CreateAnchor(bookId, anchor, tokens));
            ToolsRepository.SaveAnchors(anchModels, path);
        }

        private static void SaveChapters(string bookId, string path, IEnumerable<BookChapter> chapters, IList<TokenBase> tokens)
        {
            var chapModels = chapters.Select(chapter => CreateChapter(bookId, chapter, tokens));
            ToolsRepository.SaveChapters(chapModels, path);
        }

        private static ChapterModel CreateChapter(string bookId, BookChapter chapter, IList<TokenBase> tokens)
        {
            return new ChapterModel
                {
                    BookID = bookId,
                    Level = chapter.Level,
                    Title = chapter.Title.Length > 1024 ? chapter.Title.Substring(0, 1024) : chapter.Title,
                    TokenID = GetUIToken(chapter.TokenID, tokens),
                    MinTokenID = GetMinToken(chapter.TokenID, tokens)
                };
        }

        private static AnchorModel CreateAnchor(string bookId, KeyValuePair<string, int> anchor, IList<TokenBase> tokens)
        {
            var key = anchor.Key;
            if (key.Length > 1024)
            {
                key = key.Substring(0, 1024);
            }

            return new AnchorModel
                {
                    BookID = bookId,
                    NameHash = key.GetHashCode(),
                    Name = key,
                    TokenID = GetUIToken(anchor.Value, tokens)
                };
        }

        private static int GetUIToken(int tokenId, IList<TokenBase> tokens)
        {
            for (var i = tokenId; i < tokens.Count; i++)
            {
                if (tokens[i] is TextToken || tokens[i] is PictureToken)
                {
                    return i;
                }
            }

            return tokenId;
        }

        private static int GetMinToken(int tokenId, IList<TokenBase> tokens)
        {
            for (var i = tokenId; i >= 0; i--)
            {
                if (tokens[i] is TextToken || tokens[i] is PictureToken)
                {
                    return i;
                }
            }

            return 0;
        }

        private static List<int> SaveTokensWithPosition(string tokensPath, IEnumerable<TokenBase> tokens)
        {
            var list = new List<int>();

            using (var file = FileStorage.Instance.GetFile(tokensPath))
            {
                using (file.Lock())
                {
                    foreach (var baseToken in tokens)
                    {
                        list.Add(file.Position);
                        TokenSerializer.Save(file.Writer, baseToken);
                    }
                }
            }

            return list;
        }

        private static void SaveTokenPosition(string path, List<int> positions)
        {
            using (var file = FileStorage.Instance.GetFile(path))
            {
                using (file.Lock())
                {
                    var writer = file.Writer;
                    writer.Write(positions.Count);
                    if (positions.Count <= 0)
                    {
                        return;
                    }

                    var buffer = new byte[positions.Count * 4];
                    Buffer.BlockCopy(positions.ToArray(), 0, buffer, 0, buffer.Length);
                    writer.Write(buffer);
                }
            }
        }
    }
}