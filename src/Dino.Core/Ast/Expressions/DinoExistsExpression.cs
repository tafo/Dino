using Dino.Core.Ast.Queries;
using Dino.Core.Ast.Visitors;

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
    
    public override void Accept(IDinoQueryVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IDinoQueryVisitor<T> visitor) => visitor.Visit(this);
}