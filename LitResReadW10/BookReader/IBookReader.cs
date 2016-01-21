using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace LitRes.BookReader
{    
    public interface IBookReader
    {
        int PagesCount { get;}
        int CurrentPageIndex { get; }
        object CurrentPage { get; }
        void MoveToPage(int index);
        Task<WriteableBitmap> 
            RenderCurrentPage();
        Task<WriteableBitmap> RenderCurrentPage(int width, int height);
    }
}
