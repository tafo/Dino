using Dino.Core.Ast.Clauses;
using Dino.Core.Ast.Expressions;
using Dino.Core.Ast.Queries;

namespace Dino.Core.Ast.Visitors;

public interface IDinoQueryVisitor<out T>
{
    T Visit(DinoSelectQuery node);
    T Visit(DinoFromClause node);
    T Visit(DinoWhereClause node);
    T Visit(DinoJoinClause node);
    T Visit(DinoGroupByClause node);
    T Visit(DinoOrderByClause node);
    T Visit(DinoBinaryExpression node);
    T Visit(DinoUnaryExpression node);
    T Visit(DinoIdentifierExpression node);
    T Visit(DinoLiteralExpression node);
    T Visit(DinoFunctionCallExpression node);
    T Visit(DinoParameterExpression node);
    T Visit(DinoMemberAccessExpression node);
    T Visit(DinoInExpression node);
    T Visit(DinoBetweenExpression node);
    T Visit(DinoCaseExpression node);
    T Visit(DinoExistsExpression node);
    T Visit(DinoSubqueryExpression node);
}