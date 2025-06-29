namespace Dino.Core.Lexing;

using Tokens;

public interface ILexer
{
    Token NextToken();
    Token PeekToken();
    void Reset();
    IEnumerable<Token> Tokenize();
}