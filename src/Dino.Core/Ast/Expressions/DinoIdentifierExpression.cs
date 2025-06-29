using Dino.Core.Ast.Visitors;

namespace Dino.Core.Ast.Expressions;

public sealed class DinoIdentifierExpression(string name, int line = 0, int column = 0) : DinoExpression(line, column)
{
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
    
    public override void Accept(IDinoQueryVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IDinoQueryVisitor<T> visitor) => visitor.Visit(this);
}