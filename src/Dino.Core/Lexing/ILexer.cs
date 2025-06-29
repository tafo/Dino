namespace Dino.Core.Lexing;

using Tokens;

public interface ILexer
{
    DinoToken NextToken();
    DinoToken PeekToken();
    void Reset();
    IEnumerable<DinoToken> Tokenize();
}