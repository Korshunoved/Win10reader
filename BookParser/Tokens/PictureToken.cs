using System.IO;

namespace BookParser.Tokens
{
    public class PictureToken : TokenBase
    {
        public PictureToken(int id, string imageId)
            : base(id)
        {
            ImageID = imageId;
        }

        public string ImageID { get; private set; }

        public override void Save(BinaryWriter writer)
        {
            writer.Write(ImageID);
        }

        public static PictureToken Load(BinaryReader reader, int id)
        {
            var imageId = reader.ReadString();
            return new PictureToken(id, imageId);
        }
    }
}