using System.Collections.Generic;
using System.IO;
using BookParser.Data;

namespace BookParser.Parsers
{
    public interface IBookSummaryParser
    {
        Dictionary<string, int> Anchors { get; }

        List<BookChapter> Chapters { get; }

        void BuildChapters();

        void SaveImages(Stream stream);

        bool SaveCover(string bookID);

        BookSummary GetBookPreview();

        ITokenParser GetTokenParser();
    }
}
