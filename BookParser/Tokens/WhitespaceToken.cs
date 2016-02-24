
using System.IO;

namespace BookParser.Tokens
{
    public class WhitespaceToken : TokenBase
    {
        public WhitespaceToken(int id)
            : base(id)
        {
        }

        public override void Save(BinaryWriter writer)
        {
        }
    }
}