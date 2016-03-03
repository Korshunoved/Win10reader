using System.IO;

namespace BookParser.Tokens
{
    public abstract class TokenBase
    {
        protected TokenBase(int id)
        {
            ID = id;
        }

        public int ID { get; private set; }

        public abstract void Save(BinaryWriter writer);
    }
}
