namespace Dino.Core.Ast;

public abstract class DinoQueryNode(int line = 0, int column = 0)
{
    public int Line { get; init; } = line;
    public int Column { get; init; } = column;
}