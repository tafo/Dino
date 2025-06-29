namespace Dino.Core.Lexing;

using Tokens;

public interface IDinoLexer
{
    DinoToken NextToken();
    DinoToken PeekToken();
    void Reset();
    IEnumerable<DinoToken> Tokenize();
}