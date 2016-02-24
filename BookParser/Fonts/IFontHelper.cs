

using Windows.Foundation;

namespace BookParser.Fonts
{
    public interface IFontHelper
    {
        Size GetSize(char c, double fontSize, bool bold = false, bool italic = false);
    }
}