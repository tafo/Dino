using Dino.Core.Ast.Visitors;

namespace Dino.Core.Ast.Expressions;

using Queries;

public sealed class DinoSubqueryExpression(
    DinoSelectQuery query,
    int line = 0,
    int column = 0)
    : DinoExpression(line, column)
{
    public DinoSelectQuery Query { get; } = query ?? throw new ArgumentNullException(nameof(query));
    
    public override void Accept(IDinoQueryVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IDinoQueryVisitor<T> visitor) => visitor.Visit(this);
}