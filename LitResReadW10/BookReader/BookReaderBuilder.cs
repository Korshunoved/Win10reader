using LitRes.Services;
#if PDF_ENABLED
using pdftron.PDF;
#endif
namespace LitRes.BookReader
{
    public class BookReaderBuilder
    {
        public static IBookReader BuildBookReader(BookReadingContext book)
        {
            return new Fb2BookReader(book);
        }
#if PDF_ENABLED
        public static IBookReader BuildBookReader(PDFDoc book)
        {
            return new PdfBookReader(book);
        }
#endif
    }
}
