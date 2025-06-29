namespace Dino.Core.Ast.Queries;

using Clauses;
using Expressions;

public sealed class DinoSelectQuery : DinoQueryNode
{
    public IReadOnlyList<DinoSelectItem> SelectItems { get; }
    public bool IsDistinct { get; }
    public DinoFromClause? FromClause { get; }
    public DinoWhereClause? WhereClause { get; }
    public DinoGroupByClause? GroupByClause { get; }
    public DinoOrderByClause? OrderByClause { get; }
    public int? Limit { get; }
    public int? Offset { get; }
    
    public DinoSelectQuery(
        IEnumerable<DinoSelectItem> selectItems,
        bool isDistinct = false,
        DinoFromClause? fromClause = null,
        DinoWhereClause? whereClause = null,
        DinoGroupByClause? groupByClause = null,
        DinoOrderByClause? orderByClause = null,
        int? limit = null,
        int? offset = null,
        int line = 0,
        int column = 0) 
        : base(line, column)
    {
        SelectItems = selectItems?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(selectItems));
        if (SelectItems.Count == 0)
            throw new ArgumentException("At least one select item is required", nameof(selectItems));
        IsDistinct = isDistinct;
        FromClause = fromClause;
        WhereClause = whereClause;
        GroupByClause = groupByClause;
        OrderByClause = orderByClause;
        Limit = limit;
        Offset = offset;
    }
}

public sealed class DinoSelectItem(DinoExpression expression, string? alias = null)
{
    public DinoExpression Expression { get; } = expression ?? throw new ArgumentNullException(nameof(expression));
    public string? Alias { get; } = alias;
}