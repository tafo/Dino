namespace Dino.Core.Ast.Expressions;

public sealed class DinoLiteralExpression(object? value, Type valueType, int line = 0, int column = 0)
    : DinoExpression(line, column)
{
    public object? Value { get; } = value;
    public Type ValueType { get; } = valueType;
}