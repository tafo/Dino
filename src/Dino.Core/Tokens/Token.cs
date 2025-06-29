namespace Dino.Core.Tokens;

public sealed class Token(TokenCategory category, string value, int position, int line = 1, int column = 1)
{
    public TokenCategory Category { get; init; } = category;
    public string Value { get; init; } = value;
    public int Position { get; init; } = position;
    public int Line { get; init; } = line;
    public int Column { get; init; } = column;

    public override string ToString() => $"{Category}:{Value} at {Line}:{Column}";

    public override bool Equals(object? obj) => obj is Token token &&
                                                Category == token.Category &&
                                                Value == token.Value &&
                                                Position == token.Position;

    public override int GetHashCode() => HashCode.Combine(Category, Value, Position);
}