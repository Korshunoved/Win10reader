
namespace BookParser.Tokens
{
    public abstract class TokenBlockBase
    {
        public double Height { get; set; }

        public int FirstTokenID { get; set; }

        public int LastTokenID { get; set; }

        public virtual string GetLastPart()
        {
            return string.Empty;
        }
    }
}