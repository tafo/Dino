namespace Dino.Core.Ast.Expressions;

using Dino.Core.Ast.Visitors;

public sealed class DinoLiteralExpression(object? value, Type valueType, int line = 0, int column = 0)
    : DinoExpression(line, column)
{
    public object? Value { get; } = value;
    public Type ValueType { get; } = valueType;

    public override void Accept(IDinoQueryVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IDinoQueryVisitor<T> visitor) => visitor.Visit(this);
}