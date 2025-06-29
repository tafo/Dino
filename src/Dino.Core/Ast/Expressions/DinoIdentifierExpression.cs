namespace Dino.Core.Ast.Expressions;

public sealed class DinoIdentifierExpression(string name, int line = 0, int column = 0) : DinoExpression(line, column)
{
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
}