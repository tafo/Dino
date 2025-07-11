using Dino.Core.Ast.Visitors;

namespace Dino.Core.Ast.Expressions;

public sealed class DinoInExpression(
    DinoExpression expression,
    IEnumerable<DinoExpression> values,
    bool isNegated = false,
    int line = 0,
    int column = 0)
    : DinoExpression(line, column)
{
    public DinoExpression Expression { get; } = expression ?? throw new ArgumentNullException(nameof(expression));

    public IReadOnlyList<DinoExpression> Values { get; } =
        values.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(values));
    public bool IsNegated { get; } = isNegated;
    
    public override void Accept(IDinoQueryVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IDinoQueryVisitor<T> visitor) => visitor.Visit(this);
}