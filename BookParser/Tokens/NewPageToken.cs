using System.IO;

namespace BookParser.Tokens
{
    public class NewPageToken : TokenBase
    {
        public NewPageToken(int id)
            : base(id)
        {
        }

        public override void Save(BinaryWriter writer)
        {
        }
    }
}