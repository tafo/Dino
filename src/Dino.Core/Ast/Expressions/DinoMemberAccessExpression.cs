using Dino.Core.Ast.Visitors;

namespace Dino.Core.Ast.Expressions;

public sealed class DinoMemberAccessExpression(
    DinoExpression o,
    string memberName,
    int line = 0,
    int column = 0)
    : DinoExpression(line, column)
{
    public DinoExpression Object { get; } = o ?? throw new ArgumentNullException(nameof(o));
    public string MemberName { get; } = memberName ?? throw new ArgumentNullException(nameof(memberName));
    
    public override void Accept(IDinoQueryVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IDinoQueryVisitor<T> visitor) => visitor.Visit(this);
}