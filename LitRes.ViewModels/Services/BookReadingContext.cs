using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Athenaeum;
using Athenaeum.Formatter;


namespace LitRes.Services
{
	public class BookReadingContext
	{
		public BookReader Reader { get; set; }
        public DisplayOrientations Orientation { get; set; }
		public TableOfContent PlainToC { get; set; }
		public TableOfContent TreeToC { get; set; }
	}
}