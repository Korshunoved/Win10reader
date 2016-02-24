
using System.IO;
using System.Xml.Linq;
using BookParser.Styling;

namespace BookParser.Tokens
{
    public class TagOpenToken : TokenBase
    {
        public TagOpenToken(int id, XElement element, TextVisualProperties properties, int parentID)
            : base(id)
        {
            Name = element.Name.LocalName;
            TextProperties = properties;
            ParentID = parentID;
        }

        private TagOpenToken(int id, string name, TextVisualProperties properties, int parentId)
            : base(id)
        {
            Name = name;
            TextProperties = properties;
            ParentID = parentId;
        }

        public string Name { get; private set; }
        public int ParentID { get; private set; }
        public TextVisualProperties TextProperties { get; private set; }

        public override void Save(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(ParentID);
            TextProperties.Save(writer);
        }

        public static TokenBase Load(BinaryReader reader, int id)
        {
            var name = reader.ReadString();
            int parentID = reader.ReadInt32();
            TextVisualProperties properties = TextVisualProperties.Load(reader);

            return new TagOpenToken(id, name, properties, parentID);
        }
    }
}