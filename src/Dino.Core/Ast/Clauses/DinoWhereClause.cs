using Dino.Core.Ast.Visitors;

namespace Dino.Core.Ast.Clauses;

using Expressions;

public sealed class DinoWhereClause(
    DinoExpression condition,
    int line = 0,
    int column = 0)
    : DinoClause(line, column)
{
    public DinoExpression Condition { get; } = condition ?? throw new ArgumentNullException(nameof(condition));
    
    public override void Accept(IDinoQueryVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IDinoQueryVisitor<T> visitor) => visitor.Visit(this);
}