using Dino.Core.Ast.Visitors;

namespace Dino.Core.Ast.Clauses;

using Expressions;

public sealed class DinoOrderByClause : DinoClause
{
    public IReadOnlyList<DinoOrderByItem> Items { get; }
    
    public DinoOrderByClause(
        IEnumerable<DinoOrderByItem> items,
        int line = 0, 
        int column = 0) 
        : base(line, column)
    {
        Items = items?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(items));
        if (Items.Count == 0)
            throw new ArgumentException("At least one order by item is required", nameof(items));
    }
    
    public override void Accept(IDinoQueryVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IDinoQueryVisitor<T> visitor) => visitor.Visit(this);
}

public sealed class DinoOrderByItem(
    DinoExpression expression,
    DinoOrderDirection direction = DinoOrderDirection.Ascending)
{
    public DinoExpression Expression { get; } = expression ?? throw new ArgumentNullException(nameof(expression));
    public DinoOrderDirection Direction { get; } = direction;
}