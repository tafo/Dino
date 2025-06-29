using Dino.Core.Ast.Visitors;

namespace Dino.Core.Ast.Clauses;

using Expressions;

public sealed class DinoGroupByClause : DinoClause
{
    public IReadOnlyList<DinoExpression> GroupingExpressions { get; }
    public DinoExpression? HavingCondition { get; }
    
    public DinoGroupByClause(
        IEnumerable<DinoExpression> groupingExpressions,
        DinoExpression? havingCondition = null,
        int line = 0, 
        int column = 0) 
        : base(line, column)
    {
        GroupingExpressions = groupingExpressions?.ToList().AsReadOnly() ??
                              throw new ArgumentNullException(nameof(groupingExpressions));
        if (GroupingExpressions.Count == 0)
            throw new ArgumentException("At least one grouping expression is required", nameof(groupingExpressions));
        HavingCondition = havingCondition;
    }
    
    public override void Accept(IDinoQueryVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IDinoQueryVisitor<T> visitor) => visitor.Visit(this);
}