namespace Dino.Core.Ast.Visitors;

using Expressions;
using Queries;
using Clauses;

public interface IDinoQueryVisitor
{
    void Visit(DinoSelectQuery node);
    void Visit(DinoFromClause node);
    void Visit(DinoWhereClause node);
    void Visit(DinoJoinClause node);
    void Visit(DinoGroupByClause node);
    void Visit(DinoOrderByClause node);
    void Visit(DinoBinaryExpression node);
    void Visit(DinoUnaryExpression node);
    void Visit(DinoIdentifierExpression node);
    void Visit(DinoLiteralExpression node);
    void Visit(DinoFunctionCallExpression node);
    void Visit(DinoParameterExpression node);
    void Visit(DinoMemberAccessExpression node);
    void Visit(DinoInExpression node);
    void Visit(DinoBetweenExpression node);
    void Visit(DinoCaseExpression node);
    void Visit(DinoExistsExpression node);
    void Visit(DinoSubqueryExpression node);
}