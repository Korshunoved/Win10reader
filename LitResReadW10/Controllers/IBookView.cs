using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using BookParser.Models;
using BookRender.RenderData;

namespace LitResReadW10.Controllers
{
    public interface IBookView
    {
        IList<TextRenderData> NextTexts { get; }
        IList<TextRenderData> CurrentTexts { get; }
        IList<TextRenderData> PreviousTexts { get; }

        IList<LinkRenderData> NextLinks { get; }
        IList<LinkRenderData> CurrentLinks { get; }
        IList<LinkRenderData> PreviousLinks { get; }

        IList<BookmarkModel> Bookmarks { get; set; }

        Panel GetCurrentPagePanel();
        Panel GetNextPagePanel();
        Panel GetPrevPagePanel();
        void SwapPrevWithCurrent();
        void SwapNextWithCurrent();
        Size GetSize();

        void SetSelection(TextRenderData wa, TextRenderData wb);
    }
}
