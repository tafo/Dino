namespace Dino.Core.Ast;

using Visitors;

public abstract class DinoQueryNode(int line = 0, int column = 0)
{
    public int Line { get; init; } = line;
    public int Column { get; init; } = column;

    public abstract void Accept(IDinoQueryVisitor visitor);
    public abstract T Accept<T>(IDinoQueryVisitor<T> visitor);
}