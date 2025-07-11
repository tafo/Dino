namespace Dino.Core.Exceptions;

public class DinoLexerException : Exception
{
    public int Position { get; }
    public int Line { get; }
    public int Column { get; }
    public string Input { get; }

    public DinoLexerException(string message, int position, int line, int column, string input) 
        : base(message)
    {
        Position = position;
        Line = line;
        Column = column;
        Input = input;
    }

    public DinoLexerException(string message, int position, int line, int column, string input, Exception innerException) 
        : base(message, innerException)
    {
        Position = position;
        Line = line;
        Column = column;
        Input = input;
    }

    public override string ToString()
    {
        return $"{Message} at line {Line}, column {Column} (position {Position})";
    }
}