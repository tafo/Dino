namespace Dino.Core.Ast.Clauses;

public abstract class DinoClause(int line = 0, int column = 0) : DinoQueryNode(line, column);