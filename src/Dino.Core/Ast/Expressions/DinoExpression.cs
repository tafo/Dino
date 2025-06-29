namespace Dino.Core.Ast.Expressions;

public abstract class DinoExpression(int line = 0, int column = 0) : DinoQueryNode(line, column);