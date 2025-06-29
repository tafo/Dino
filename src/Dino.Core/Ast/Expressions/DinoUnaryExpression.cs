using Dino.Core.Ast.Visitors;

namespace Dino.Core.Ast.Expressions;

public sealed class DinoUnaryExpression(
    DinoUnaryOperator @operator,
    DinoExpression operand,
    int line = 0,
    int column = 0)
    : DinoExpression(line, column)
{
    public DinoUnaryOperator Operator { get; } = @operator;
    public DinoExpression Operand { get; } = operand ?? throw new ArgumentNullException(nameof(operand));
    
    public override void Accept(IDinoQueryVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IDinoQueryVisitor<T> visitor) => visitor.Visit(this);
}