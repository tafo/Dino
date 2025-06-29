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
}