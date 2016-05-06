
using System.IO;
using System.Xml.Linq;
using BookParser.Styling;

namespace BookParser.Tokens
{
    public class TagOpenToken : TokenBase
    {
        public TagOpenToken(int id, XElement element, TextVisualProperties properties, int parentID, string pointer)
            : base(id)
        {
            Name = element.Name.LocalName;
            TextProperties = properties;
            ParentID = parentID;
            Pointer = pointer;
        }

        private TagOpenToken(int id, string name, TextVisualProperties properties, int parentId, string pointer)
            : base(id)
        {
            Name = name;
            TextProperties = properties;
            ParentID = parentId;
            Pointer = pointer;
        }

        public string Name { get; private set; }
        public int ParentID { get; private set; }
        public TextVisualProperties TextProperties { get; }
        public string Pointer { get; }

        public override void Save(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(ParentID);
            TextProperties.Save(writer);
            writer.Write(Pointer);
        }

        public static TokenBase Load(BinaryReader reader, int id)
        {
            var name = reader.ReadString();
            int parentID = reader.ReadInt32();
            TextVisualProperties properties = TextVisualProperties.Load(reader);
            var pointer = reader.ReadString();
            return new TagOpenToken(id, name, properties, parentID, pointer);
        }        
    }
}