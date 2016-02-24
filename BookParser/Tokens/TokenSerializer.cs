using System;
using System.Collections.Generic;
using System.IO;

namespace BookParser.Tokens
{
    public static class TokenSerializer
    {
        private static readonly Dictionary<Type, TokenType> Types = new Dictionary<Type, TokenType>
            {
                {typeof (TagOpenToken), TokenType.TagOpenToken},
                {typeof (TagCloseToken), TokenType.TagCloseToken},
                {typeof (TextToken), TokenType.TextToken},
                {typeof (WhitespaceToken), TokenType.WhitespaceToken},
                {typeof (NewPageToken), TokenType.NewPageToken},
                {typeof (PictureToken), TokenType.PictureToken}
            };

        public static TokenBase Load(BinaryReader reader, int id)
        {
            var type = (TokenType) reader.ReadByte();
            switch (type)
            {
                case TokenType.TagOpenToken:
                    return TagOpenToken.Load(reader, id);
                case TokenType.TagCloseToken:
                    return TagCloseToken.Load(reader, id);
                case TokenType.TextToken:
                    return TextToken.Load(reader, id);
                case TokenType.WhitespaceToken:
                    return new WhitespaceToken(id);
                case TokenType.NewPageToken:
                    return new NewPageToken(id);
                case TokenType.PictureToken:
                    return PictureToken.Load(reader, id);
                default:
                    return null;
            }
        }

        public static void Save(BinaryWriter writer, TokenBase token)
        {
            TokenType type;
            if(!Types.TryGetValue(token.GetType(), out type))
                throw new NotSupportedException();

            writer.Write((byte)type);
            token.Save(writer);
        }
    }
}