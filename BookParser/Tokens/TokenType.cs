
namespace BookParser.Tokens
{
    public enum TokenType : byte
    {
        TagOpenToken = 0,
        TagCloseToken = 1,
        TextToken = 2,
        WhitespaceToken = 3,
        NewPageToken = 4,        
        PictureToken = 5,
    }
}
