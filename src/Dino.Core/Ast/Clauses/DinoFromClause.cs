using Dino.Core.Ast.Visitors;

namespace Dino.Core.Ast.Clauses;

public sealed class DinoFromClause(
    DinoTableSource tableSource,
    IEnumerable<DinoJoinClause>? joins = null,
    int line = 0,
    int column = 0)
    : DinoClause(line, column)
{
    public DinoTableSource TableSource { get; } = tableSource ?? throw new ArgumentNullException(nameof(tableSource));
    public IReadOnlyList<DinoJoinClause> Joins { get; } = joins?.ToList().AsReadOnly() ?? new List<DinoJoinClause>().AsReadOnly();
    
    public override void Accept(IDinoQueryVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IDinoQueryVisitor<T> visitor) => visitor.Visit(this);
}

public sealed class DinoTableSource(string tableName, string? alias = null)
{
    public string TableName { get; } = tableName ?? throw new ArgumentNullException(nameof(tableName));
    public string? Alias { get; } = alias;
}