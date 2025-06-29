using Dino.Core.Tokens;

namespace Dino.Core.Exceptions;

public class DinoParserException : Exception
{
    public int Position { get; }
    public int Line { get; }
    public int Column { get; }
    public string Query { get; }
    public DinoToken? Token { get; }

    public DinoParserException(string message, int position, int line, int column, string query, DinoToken? token = null) 
        : base(message)
    {
        Position = position;
        Line = line;
        Column = column;
        Query = query;
        Token = token;
    }

    public DinoParserException(string message, DinoToken token, string query) 
        : base(message)
    {
        Position = token.Position;
        Line = token.Line;
        Column = token.Column;
        Query = query;
        Token = token;
    }

    public override string ToString()
    {
        var tokenInfo = Token != null ? $" (Token: {Token.Category}:{Token.Value})" : "";
        return $"{Message} at line {Line}, column {Column}{tokenInfo}";
    }
}