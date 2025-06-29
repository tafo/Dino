namespace Dino.Core.Ast.Expressions;

public sealed class DinoBetweenExpression(
    DinoExpression expression,
    DinoExpression lowerBound,
    DinoExpression upperBound,
    bool isNegated = false,
    int line = 0,
    int column = 0)
    : DinoExpression(line, column)
{
    public DinoExpression Expression { get; } = expression ?? throw new ArgumentNullException(nameof(expression));
    public DinoExpression LowerBound { get; } = lowerBound ?? throw new ArgumentNullException(nameof(lowerBound));
    public DinoExpression UpperBound { get; } = upperBound ?? throw new ArgumentNullException(nameof(upperBound));
    public bool IsNegated { get; } = isNegated;
}