using Dino.Core.Ast.Visitors;

namespace Dino.Core.Ast.Clauses;

using Expressions;

public sealed class DinoJoinClause : DinoClause
{
    public DinoJoinType JoinType { get; }
    public DinoTableSource TableSource { get; }
    public DinoExpression? OnCondition { get; }
    
    public DinoJoinClause(
        DinoJoinType joinType,
        DinoTableSource tableSource,
        DinoExpression? onCondition = null,
        int line = 0, 
        int column = 0) 
        : base(line, column)
    {
        JoinType = joinType;
        TableSource = tableSource ?? throw new ArgumentNullException(nameof(tableSource));
        
        if (joinType != DinoJoinType.Cross && onCondition == null)
            throw new ArgumentException("ON condition is required for non-CROSS joins", nameof(onCondition));
            
        OnCondition = onCondition;
    }
    
    public override void Accept(IDinoQueryVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IDinoQueryVisitor<T> visitor) => visitor.Visit(this);
}