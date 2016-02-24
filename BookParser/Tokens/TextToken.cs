
using System.IO;

namespace BookParser.Tokens
{
    public class TextToken : TokenBase
    {
        public TextToken(int id, string text)
            : base(id)
        {
            Text = text;
            Part = string.Empty;
        }

        public string Part { get; set; }

        public string Text { get; private set; }

        public override void Save(BinaryWriter writer)
        {
            writer.Write(Text);
        }

        public static TokenBase Load(BinaryReader reader, int id)
        {
            string text = reader.ReadString();
            return new TextToken(id, text);
        }
    }
}