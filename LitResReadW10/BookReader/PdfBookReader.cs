using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Sensors;
#if PDF_ENABLED
using pdftron;
using pdftron.PDF;
using pdftron.PDF.Controls.Extensions;

namespace LitRes.BookReader
{
    public class PdfBookReader: IBookReader
    {
        private int _currentPage = 1;
        private readonly PDFDoc _book;
        private PDFDraw m_draw = null;

        public int PagesCount { get { return _book.GetPageCount(); } }
        public int CurrentPageIndex { get { return _currentPage; } }
        public object CurrentPage { get { return _book.GetPage(_currentPage); } }

        public PdfBookReader(PDFDoc book)
        {
            PDFNet.Initialize();
            pdftron.PDFNet.SetViewerCache(100 * 1024 * 1024, true);
            _book = book;
            m_draw = new PDFDraw();
        }

        public void MoveToPage(int index)
        {
            _currentPage = index;
            if (_currentPage < 1)
                _currentPage = 1;
            else if (_currentPage > _book.GetPageCount()) 
                _currentPage = _book.GetPageCount();            
        }

        public async Task<WriteableBitmap> RenderCurrentPage()
        {
            return await m_draw.GetBitmapAsync((pdftron.PDF.Page) CurrentPage);
        }

        public async Task<WriteableBitmap> RenderCurrentPage(int width, int height)
        {
            return await RenderCurrentPage();
        }

        public string GetPageTitle(int page)
        {
            string result = null;
            if(_book.GetPageLabel(page).IsValid()) result = _book.GetPageLabel(page).GetLabelTitle(page);
            return result;
        }
    }
}

#endif