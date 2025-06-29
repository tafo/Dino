using Dino.Core.Ast.Queries;

namespace Dino.Core.Ast.Expressions;

public sealed class DinoExistsExpression(
    DinoSelectQuery subquery,
    bool isNegated = false,
    int line = 0,
    int column = 0)
    : DinoExpression(line, column)
{
    public DinoSelectQuery Subquery { get; } = subquery ?? throw new ArgumentNullException(nameof(subquery));
    public bool IsNegated { get; } = isNegated;
}