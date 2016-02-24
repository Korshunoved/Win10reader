using System.IO;

namespace BookParser.Tokens
{
    public class TagCloseToken : TokenBase
    {
        public TagCloseToken(int id, int parentID)
            : base(id)
        {
            ParentID = parentID;
        }

        public int ParentID { get; private set; }

        public override void Save(BinaryWriter writer)
        {
            writer.Write(ParentID);
        }

        public static TagCloseToken Load(BinaryReader reader, int id)
        {
            int parentID = reader.ReadInt32();
            return new TagCloseToken(id, parentID);
        }
    }
}