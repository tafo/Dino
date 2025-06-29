using Dino.Core.Ast.Visitors;

namespace Dino.Core.Ast.Expressions;

public sealed class DinoCaseExpression : DinoExpression
{
    public DinoExpression? Expression { get; }
    public IReadOnlyList<DinoWhenClause> WhenClauses { get; }
    public DinoExpression? ElseExpression { get; }
    
    public DinoCaseExpression(
        IEnumerable<DinoWhenClause> whenClauses,
        DinoExpression? expression = null,
        DinoExpression? elseExpression = null,
        int line = 0, 
        int column = 0) 
        : base(line, column)
    {
        WhenClauses = whenClauses.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(whenClauses));
        if (WhenClauses.Count == 0)
            throw new ArgumentException("At least one WHEN clause is required", nameof(whenClauses));
        Expression = expression;
        ElseExpression = elseExpression;
    }
    
    public override void Accept(IDinoQueryVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IDinoQueryVisitor<T> visitor) => visitor.Visit(this);
}

public sealed class DinoWhenClause(DinoExpression condition, DinoExpression result)
{
    public DinoExpression Condition { get; } = condition ?? throw new ArgumentNullException(nameof(condition));
    public DinoExpression Result { get; } = result ?? throw new ArgumentNullException(nameof(result));
}