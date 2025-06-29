namespace Dino.Core.Ast.Expressions;

public sealed class DinoBinaryExpression(
    DinoExpression left,
    DinoBinaryOperator @operator,
    DinoExpression right,
    int line = 0,
    int column = 0)
    : DinoExpression(line, column)
{
    public DinoExpression Left { get; } = left ?? throw new ArgumentNullException(nameof(left));
    public DinoBinaryOperator Operator { get; } = @operator;
    public DinoExpression Right { get; } = right ?? throw new ArgumentNullException(nameof(right));
}