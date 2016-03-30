﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using BookParser;
using BookParser.IO;
using BookParser.Models;
using BookParser.Parsers;
using BookParser.Tokens;

namespace BookRender.Tools
{
    public class BookTool
    {
        public string GetText(BookModel book, int tokenOffset, int wordsCount, out int lastTokenId)
        {
            lastTokenId = -1;
            var result = new List<string>();
            
            using (var tokenIterator = new BookTokenIterator(book.GetTokensPath(), TokensTool.GetTokens(book.BookID)))
            {
                int words = 0;
                tokenIterator.MoveTo(tokenOffset);
                while (tokenIterator.MoveNext() && words < wordsCount)
                {
                    if (tokenIterator.Current is NewPageToken && result.Count > 0)
                        break;

                    var textToken = tokenIterator.Current as TextToken;
                    if (textToken == null)
                        continue;
                    lastTokenId = textToken.ID;
                    result.Add(textToken.Text);
                    words++;
                }
            }
            return string.Join(" ", result);
        }

        public Stream GetOriginalBook(BookModel book)
        {
            var destinationStream = new MemoryStream();
            using (var file = FileStorage.Instance.GetFile(book.GetBookPath()))
            {
                using (file.Lock())
                {
                    file.Reader.BaseStream.CopyTo(destinationStream);
                }
            }
            destinationStream.Seek(0, SeekOrigin.Begin);
            return destinationStream;
        }

        public string GetChapterByToken(int token)
        {
            var chapters = AppSettings.Default.Chapters.Where(n => n.MinTokenID <= token).ToList();
            var chapter = string.Empty;
            for (var i = chapters.Count - 1; i >= 0; i--)
            {
                chapter = chapters[i].Title.TrimEnd().Replace("\r\n", " - ").Trim();
                if (!string.IsNullOrEmpty(chapter))
                {
                    break;
                }
            }
            return chapter;
        }

        public string GetLastParagraphByToken(BookModel book, int tokenOffset, int wordsCount, out int lastTokenId)
        {
            lastTokenId = -1;
            var result = new List<string>();

            using (var tokenIterator = new BookTokenIterator(book.GetTokensPath(), TokensTool.GetTokens(book.BookID)))
            {
                int words = 0;
                tokenIterator.MoveTo(tokenOffset);
                while (tokenIterator.MoveNext() && words < wordsCount)
                {
                    if (tokenIterator.Current is NewPageToken && result.Count > 0)
                        break;

                    var textToken = tokenIterator.Current as TextToken;
                    var tagToken = tokenIterator.Current as TagOpenToken;
                    if (tagToken != null && tagToken.Name.Contains("p"))
                       result.Clear();
                    if (textToken == null)
                        continue;
                    lastTokenId = textToken.ID;
                    result.Add(textToken.Text);
                    words++;
                }
            }
            return string.Join(" ", result);
        }
    }
}