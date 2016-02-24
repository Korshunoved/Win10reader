using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using BookParser.Styling;
using BookParser.TextStructure;

namespace BookParser.Tokens
{
    public class TextTokenBlock : TokenBlockBase
    {
        public TextTokenBlock()
        {
            Inlines = new List<TextElementBase>();
        }

        public List<TextElementBase> Inlines { get; set; }

        public string TextAlign { get; set; }

        public double MarginLeft { get; set; }

        public double MarginRight { get; set; }

        public double TextIndent { get; set; }

        public void AddText(string text, TextVisualProperties properties, double fontSize, Size size, string part = null, int tokenID = -1)
        {
            part = part ?? string.Empty;
            Inlines.Add(new TextElement
                        {
                            Text = text,
                            Width = size.Width,
                            Height = size.Height,
                            Bold = properties.Bold,
                            Italic = properties.Italic,
                            Size = fontSize,
                            SupOption = properties.SupOption,
                            SubOption = properties.SubOption,
                            Part = part,
                            LinkID = properties.LinkID,
                            TokenID = tokenID
                        });
        }

        public void UpdateHeight(double height)
        {
            Height = Math.Max(Height, height);
        }

        public void EndParagraph()
        {
            Inlines.Add(new EOPElement());
        }

        public void EndLine()
        {
            Inlines.Add(new EOLElement());
        }

        public override string GetLastPart()
        {
            TextElement textElement = Inlines.OfType<TextElement>().LastOrDefault();
            if (textElement == null)
                return string.Empty;
            return textElement.Part;
        }
    }
}