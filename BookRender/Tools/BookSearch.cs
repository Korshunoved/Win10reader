using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookParser.Models;
using BookParser.Parsers;
using BookParser.Tokens;

namespace BookRender.Tools
{
    public class BookSearchResult
    {
        public List<TextToken> PreviousContext { get; set; }
        public List<TextToken> SearchResult { get; set; }
        public List<TextToken> NextContext { get; set; }
    }

    public class BookSearch : IDisposable
    {
        private BookTokenIterator _bookTokenIterator;
        private List<string> _query;
        private BookModel _book;

        public BookSearch(BookModel book)
        {
            _book = book;
            _bookTokenIterator = new BookTokenIterator(book.GetTokensPath(), TokensTool.GetTokens(book.BookID));
        }

        public void Init()
        {
            if (_bookTokenIterator == null || _book == null) return;
            var tokenId = 0;
            try
            {
                tokenId = _bookTokenIterator.Current.ID;
            }
            catch (Exception)
            {
                // ignored
            }
            _bookTokenIterator.Dispose();
            _bookTokenIterator = new BookTokenIterator(_book.GetTokensPath(), TokensTool.GetTokens(_book.BookID));
            _bookTokenIterator.MoveTo(tokenId);
            _bookTokenIterator.MoveNext();
        }

        public Task<List<BookSearchResult>> Search(BookModel book, string query, int count)
        {
            if (string.IsNullOrEmpty(query) || book == null)
                return Task<List<BookSearchResult>>.Factory.StartNew(() => new List<BookSearchResult>());

            _bookTokenIterator?.Dispose();

            _book = book;
            _bookTokenIterator = new BookTokenIterator(_book.GetTokensPath(), TokensTool.GetTokens(_book.BookID));

            _query = PrepareQuery(query);

            return Task<List<BookSearchResult>>
                .Factory.StartNew(() => Load(_bookTokenIterator, _query, count));
        }

        public Task<List<BookSearchResult>> SearchNext(int count)
        {
            return Task<List<BookSearchResult>>
                .Factory.StartNew(() => Load(_bookTokenIterator, _query, count));
        }

        private List<BookSearchResult> Load(BookTokenIterator bookTokenIterator, List<string> query, int count)
        {
            var result = new List<BookSearchResult>();

            try
            {
                if (query.Count == 1)
                {
                    result = SearchOneWord(bookTokenIterator, query[0], count);
                }

                if (query.Count > 1)
                {
                    result = SearchGroupWords(bookTokenIterator, query, count);
                }
            }
            catch (Exception tokenExp)
            {
                throw new Exception("Book tokenizer exception has occured", tokenExp);
            }

            return result;
        }

        private List<BookSearchResult> SearchOneWord(BookTokenIterator bookTokenIterator, string query, int count)
        {
            var result = new List<BookSearchResult>();
            while (bookTokenIterator.MoveNext())
            {
                var textToken = bookTokenIterator.Current as TextToken;

                if (textToken?.Text == null || (textToken.Text).IndexOf(query, StringComparison.Ordinal) < 0) continue;
                var previousContext = GetSearchBeforeContext(bookTokenIterator, textToken.ID);
                var afterContext = GetSearchAfterContext(bookTokenIterator, textToken.ID);

                result.Add(new BookSearchResult
                {
                    PreviousContext = previousContext,
                    SearchResult = new List<TextToken> { textToken },
                    NextContext = afterContext
                });

                if (result.Count >= count)
                    break;
            }
            return result;
        }

        private List<BookSearchResult> SearchGroupWords(BookTokenIterator bookTokenIterator, List<string> query, int count)
        {
            var result = new List<BookSearchResult>();

            var firstWordQuery = query[0];
            var lastWordQuery = query.Last();
            TextToken firstWordToken;
            while ((firstWordToken = FindFirstWord(bookTokenIterator, firstWordQuery)) != null)
            {
                var resultSequence = new List<TextToken> {firstWordToken};

                var findNextSequence = false;
                for (int i = 1; i < query.Count - 1; i++)
                {
                    TextToken intermediateToken;
                    if (CheckIntermediateWord(bookTokenIterator, query[i], out intermediateToken))
                    {
                        resultSequence.Add(intermediateToken);
                    }
                    else
                    {
                        findNextSequence = true;
                        break;
                    }
                }

                if (findNextSequence)
                    continue;

                TextToken lastToken;
                if (!CheckLastWord(bookTokenIterator, lastWordQuery, out lastToken)) continue;
                resultSequence.Add(lastToken);

                var previousContext = GetSearchBeforeContext(bookTokenIterator, firstWordToken.ID);
                var afterContext = GetSearchAfterContext(bookTokenIterator, lastToken.ID);

                result.Add(new BookSearchResult
                {
                    PreviousContext = previousContext,
                    SearchResult = resultSequence,
                    NextContext = afterContext
                });

                if (result.Count >= count)
                    break;
            }
            return result;
        }

        private TextToken FindFirstWord(BookTokenIterator bookTokenIterator, string query)
        {
            while (bookTokenIterator.MoveNext())
            {
                var textToken = bookTokenIterator.Current as TextToken;
                if (textToken == null)
                    continue;

                if (textToken.Text.EndsWith(query))
                {
                    return textToken;
                }
            }
            return null;
        }

        private bool CheckIntermediateWord(BookTokenIterator bookTokenIterator, string query, out TextToken result)
        {
            result = null;
            while (bookTokenIterator.MoveNext())
            {
                var textToken = bookTokenIterator.Current as TextToken;
                if (textToken == null)
                    continue;

                if (textToken.Text.Equals(query))
                {
                    result = textToken;
                    return true;
                }
                return false;
            }
            return false;
        }

        private bool CheckLastWord(BookTokenIterator bookTokenIterator, string query, out TextToken result)
        {
            result = null;
            while (bookTokenIterator.MoveNext())
            {
                var textToken = bookTokenIterator.Current as TextToken;
                if (textToken == null)
                    continue;

                if (textToken.Text.StartsWith(query))
                {
                    result = textToken;
                    return true;
                }
                return false;
            }
            return false;
        }

        private List<TextToken> GetSearchBeforeContext(BookTokenIterator bookTokenIterator, int startTokenId, int count = 8)
        {
            var result = new List<TextToken>();
            var tokenId = startTokenId;

            while (--tokenId >= 0 && result.Count < count)
            {
                bookTokenIterator.MoveTo(tokenId);
                bookTokenIterator.MoveNext();

                if (bookTokenIterator.Current is NewPageToken)
                    break;

                var textToken = bookTokenIterator.Current as TextToken;
                if (textToken == null)
                    continue;

                result.Insert(0, textToken);
            }

            bookTokenIterator.MoveTo(startTokenId);
            bookTokenIterator.MoveNext();
            return result;
        }

        private List<TextToken> GetSearchAfterContext(BookTokenIterator bookTokenIterator, int endTokenId, int count = 8)
        {
            var result = new List<TextToken>();
            var tokenId = endTokenId;

            while (++tokenId < bookTokenIterator.Count && result.Count < count)
            {
                bookTokenIterator.MoveTo(tokenId);
                bookTokenIterator.MoveNext();

                if (bookTokenIterator.Current is NewPageToken)
                    break;

                var textToken = bookTokenIterator.Current as TextToken;
                if (textToken == null)
                    continue;

                result.Add(textToken);
            }

            bookTokenIterator.MoveTo(endTokenId);
            bookTokenIterator.MoveNext();
            return result;
        }

        public void Dispose()
        {
            _bookTokenIterator?.Dispose();
        }


        public static List<string> PrepareQuery(string query)
        {
            return query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}
