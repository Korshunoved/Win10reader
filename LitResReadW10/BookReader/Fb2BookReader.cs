using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using Athenaeum.Formatter;
using LitRes.Services;

namespace LitRes.BookReader
{
    public  class Fb2BookReader: IBookReader
    {
        private readonly BookReadingContext _cacheItem;

        public int PagesCount { get { return _cacheItem.Reader.Formatter.Pages.Count; } }
        public int CurrentPageIndex { get { return _cacheItem.Reader.CurrentPage; } }
        public object CurrentPage { get { return _cacheItem.Reader.Formatter.Pages[_cacheItem.Reader.CurrentPage - 1]; } }

        public Fb2BookReader(BookReadingContext bookContext)
        {
            _cacheItem = bookContext;
        }

        public void MoveToPage(int index)
        {
            _cacheItem.Reader.MoveTo(index);        
        }

        public async Task<WriteableBitmap> RenderCurrentPage()
        {
            return (WriteableBitmap)_cacheItem.Reader.Paint();
        }

        public async Task<WriteableBitmap> RenderCurrentPage(int width, int height)
        {
            return await RenderCurrentPage();
        }

        public object GetLinkPointer(XPoint point, bool isHd)
        {
            return _cacheItem.Reader.GetLinkPointer(point, isHd);
        }
    }
}
