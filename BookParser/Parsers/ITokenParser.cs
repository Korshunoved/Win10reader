using System.Collections.Generic;
using BookParser.Tokens;

namespace BookParser.Parsers
{
    public interface ITokenParser
    {
        IEnumerable<TokenBase> GetTokens();
    }
}
