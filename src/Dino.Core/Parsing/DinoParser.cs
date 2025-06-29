namespace Dino.Core.Parsing;

using System.Collections.Generic;
using Ast;
using Ast.Clauses;
using Ast.Expressions;
using Ast.Queries;
using Exceptions;
using Lexing;
using Tokens;

public sealed class DinoParser : IDinoParser
{
    private IDinoLexer _dinoLexer = null!;
    private DinoToken _currentToken = null!;
    private string _query = string.Empty;
    private IDictionary<string, object?>? _parameters;

    public DinoSelectQuery Parse(string query)
    {
        return Parse(query, null);
    }

    public DinoSelectQuery Parse(string query, IDictionary<string, object?>? parameters)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or empty", nameof(query));

        _query = query;
        _parameters = parameters;
        _dinoLexer = new DinoLexer(query);
        _currentToken = _dinoLexer.NextToken();

        var selectQuery = ParseSelectQuery();

        if (_currentToken.Category != DinoTokenCategory.End)
            throw CreateParserException($"Unexpected token '{_currentToken.Value}' after query end");

        return selectQuery;
    }

    private DinoSelectQuery ParseSelectQuery()
    {
        Consume(DinoTokenCategory.Select, "Expected SELECT keyword");

        var isDistinct = false;
        if (_currentToken.Category == DinoTokenCategory.Distinct)
        {
            isDistinct = true;
            NextToken();
        }

        var selectItems = ParseSelectItems();
        
        DinoFromClause? fromClause = null;
        if (_currentToken.Category == DinoTokenCategory.From)
        {
            fromClause = ParseFromClause();
        }

        DinoWhereClause? whereClause = null;
        if (_currentToken.Category == DinoTokenCategory.Where)
        {
            whereClause = ParseWhereClause();
        }

        DinoGroupByClause? groupByClause = null;
        if (_currentToken.Category == DinoTokenCategory.Group)
        {
            groupByClause = ParseGroupByClause();
        }

        DinoOrderByClause? orderByClause = null;
        if (_currentToken.Category == DinoTokenCategory.Order)
        {
            orderByClause = ParseOrderByClause();
        }

        int? limit = null;
        if (_currentToken.Category == DinoTokenCategory.Limit)
        {
            NextToken();
            limit = ParseIntegerLiteral("Expected integer after LIMIT");
        }

        int? offset = null;
        if (_currentToken.Category == DinoTokenCategory.Offset)
        {
            NextToken();
            offset = ParseIntegerLiteral("Expected integer after OFFSET");
        }

        return new DinoSelectQuery(
            selectItems,
            isDistinct,
            fromClause,
            whereClause,
            groupByClause,
            orderByClause,
            limit,
            offset,
            _currentToken.Line,
            _currentToken.Column);
    }

    private List<DinoSelectItem> ParseSelectItems()
    {
        var items = new List<DinoSelectItem>();

        if (_currentToken.Category == DinoTokenCategory.Star)
        {
            items.Add(new DinoSelectItem(
                new DinoIdentifierExpression("*", _currentToken.Line, _currentToken.Column)));
            NextToken();
            return items;
        }

        do
        {
            var expression = ParseExpression();
            string? alias = null;

            if (_currentToken.Category == DinoTokenCategory.As)
            {
                NextToken();
                alias = ConsumeIdentifier("Expected alias after AS");
            }
            else if (_currentToken.Category == DinoTokenCategory.Identifier)
            {
                // Implicit alias
                alias = _currentToken.Value;
                NextToken();
            }

            items.Add(new DinoSelectItem(expression, alias));

            if (_currentToken.Category == DinoTokenCategory.Comma)
            {
                NextToken();
            }
            else
            {
                break;
            }
        } while (true);

        return items;
    }

    private DinoFromClause ParseFromClause()
    {
        Consume(DinoTokenCategory.From, "Expected FROM keyword");

        var tableSource = ParseTableSource();
        var joins = new List<DinoJoinClause>();

        while (IsJoinKeyword(_currentToken.Category))
        {
            joins.Add(ParseJoinClause());
        }

        return new DinoFromClause(tableSource, joins, _currentToken.Line, _currentToken.Column);
    }

    private DinoTableSource ParseTableSource()
    {
        var tableName = ConsumeIdentifier("Expected table name");
        string? alias = null;

        if (_currentToken.Category == DinoTokenCategory.As)
        {
            NextToken();
            alias = ConsumeIdentifier("Expected alias after AS");
        }
        else if (_currentToken.Category == DinoTokenCategory.Identifier)
        {
            alias = _currentToken.Value;
            NextToken();
        }

        return new DinoTableSource(tableName, alias);
    }

    private DinoJoinClause ParseJoinClause()
    {
        var joinType = ParseJoinType();
        var tableSource = ParseTableSource();
        DinoExpression? onCondition = null;

        if (joinType != DinoJoinType.Cross && _currentToken.Category == DinoTokenCategory.On)
        {
            NextToken();
            onCondition = ParseExpression();
        }

        return new DinoJoinClause(joinType, tableSource, onCondition, _currentToken.Line, _currentToken.Column);
    }

    private DinoJoinType ParseJoinType()
    {
        DinoJoinType joinType = DinoJoinType.Inner;

        switch (_currentToken.Category)
        {
            case DinoTokenCategory.Inner:
                NextToken();
                Consume(DinoTokenCategory.Join, "Expected JOIN after INNER");
                break;
            case DinoTokenCategory.Left:
                NextToken();
                if (_currentToken.Category == DinoTokenCategory.Outer)
                    NextToken();
                Consume(DinoTokenCategory.Join, "Expected JOIN after LEFT");
                joinType = DinoJoinType.Left;
                break;
            case DinoTokenCategory.Right:
                NextToken();
                if (_currentToken.Category == DinoTokenCategory.Outer)
                    NextToken();
                Consume(DinoTokenCategory.Join, "Expected JOIN after RIGHT");
                joinType = DinoJoinType.Right;
                break;
            case DinoTokenCategory.Full:
                NextToken();
                if (_currentToken.Category == DinoTokenCategory.Outer)
                    NextToken();
                Consume(DinoTokenCategory.Join, "Expected JOIN after FULL");
                joinType = DinoJoinType.Full;
                break;
            case DinoTokenCategory.Cross:
                NextToken();
                Consume(DinoTokenCategory.Join, "Expected JOIN after CROSS");
                joinType = DinoJoinType.Cross;
                break;
            case DinoTokenCategory.Join:
                NextToken();
                break;
            default:
                throw CreateParserException("Expected JOIN keyword");
        }

        return joinType;
    }

    private DinoWhereClause ParseWhereClause()
    {
        Consume(DinoTokenCategory.Where, "Expected WHERE keyword");
        var condition = ParseExpression();
        return new DinoWhereClause(condition, _currentToken.Line, _currentToken.Column);
    }

    private DinoGroupByClause ParseGroupByClause()
    {
        Consume(DinoTokenCategory.Group, "Expected GROUP keyword");
        Consume(DinoTokenCategory.By, "Expected BY after GROUP");

        var groupingExpressions = new List<DinoExpression>();
        
        do
        {
            groupingExpressions.Add(ParseExpression());
            
            if (_currentToken.Category == DinoTokenCategory.Comma)
            {
                NextToken();
            }
            else
            {
                break;
            }
        } while (true);

        DinoExpression? havingCondition = null;
        if (_currentToken.Category == DinoTokenCategory.Having)
        {
            NextToken();
            havingCondition = ParseExpression();
        }

        return new DinoGroupByClause(groupingExpressions, havingCondition, _currentToken.Line, _currentToken.Column);
    }

    private DinoOrderByClause ParseOrderByClause()
    {
        Consume(DinoTokenCategory.Order, "Expected ORDER keyword");
        Consume(DinoTokenCategory.By, "Expected BY after ORDER");

        var items = new List<DinoOrderByItem>();
        
        do
        {
            var expression = ParseExpression();
            var direction = DinoOrderDirection.Ascending;

            if (_currentToken.Category == DinoTokenCategory.Asc)
            {
                NextToken();
            }
            else if (_currentToken.Category == DinoTokenCategory.Desc)
            {
                direction = DinoOrderDirection.Descending;
                NextToken();
            }

            items.Add(new DinoOrderByItem(expression, direction));
            
            if (_currentToken.Category == DinoTokenCategory.Comma)
            {
                NextToken();
            }
            else
            {
                break;
            }
        } while (true);

        return new DinoOrderByClause(items, _currentToken.Line, _currentToken.Column);
    }

    private DinoExpression ParseExpression()
    {
        return ParseOrExpression();
    }

    private DinoExpression ParseOrExpression()
    {
        var left = ParseAndExpression();

        while (_currentToken.Category == DinoTokenCategory.Or)
        {
            var op = _currentToken;
            NextToken();
            var right = ParseAndExpression();
            left = new DinoBinaryExpression(left, DinoBinaryOperator.Or, right, op.Line, op.Column);
        }

        return left;
    }

    private DinoExpression ParseAndExpression()
    {
        var left = ParseNotExpression();

        while (_currentToken.Category == DinoTokenCategory.And)
        {
            var op = _currentToken;
            NextToken();
            var right = ParseNotExpression();
            left = new DinoBinaryExpression(left, DinoBinaryOperator.And, right, op.Line, op.Column);
        }

        return left;
    }

    private DinoExpression ParseNotExpression()
    {
        if (_currentToken.Category == DinoTokenCategory.Not)
        {
            var op = _currentToken;
            NextToken();
            var operand = ParseNotExpression();
            return new DinoUnaryExpression(DinoUnaryOperator.Not, operand, op.Line, op.Column);
        }

        return ParseComparisonExpression();
    }

    private DinoExpression ParseComparisonExpression()
    {
        var left = ParseAdditiveExpression();

        switch (_currentToken.Category)
        {
            case DinoTokenCategory.Equal:
            case DinoTokenCategory.NotEqual:
            case DinoTokenCategory.LessThan:
            case DinoTokenCategory.LessThanOrEqual:
            case DinoTokenCategory.GreaterThan:
            case DinoTokenCategory.GreaterThanOrEqual:
                var op = MapComparisonOperator(_currentToken.Category);
                var opToken = _currentToken;
                NextToken();
                var right = ParseAdditiveExpression();
                return new DinoBinaryExpression(left, op, right, opToken.Line, opToken.Column);

            case DinoTokenCategory.Like:
                return ParseLikeExpression(left);

            case DinoTokenCategory.In:
                return ParseInExpression(left);

            case DinoTokenCategory.Between:
                return ParseBetweenExpression(left);

            case DinoTokenCategory.Is:
                return ParseIsExpression(left);

            default:
                return left;
        }
    }

    private DinoExpression ParseLikeExpression(DinoExpression left)
    {
        var op = _currentToken;
        NextToken();
        var pattern = ParseAdditiveExpression();
        return new DinoBinaryExpression(left, DinoBinaryOperator.Like, pattern, op.Line, op.Column);
    }

    private DinoExpression ParseInExpression(DinoExpression expression)
    {
        var inToken = _currentToken;
        NextToken();
        
        Consume(DinoTokenCategory.OpenParen, "Expected '(' after IN");
        
        var values = new List<DinoExpression>();
        
        // Check for subquery
        if (_currentToken.Category == DinoTokenCategory.Select)
        {
            var subquery = ParseSelectQuery();
            values.Add(new DinoSubqueryExpression(subquery, inToken.Line, inToken.Column));
        }
        else
        {
            do
            {
                values.Add(ParseExpression());
                
                if (_currentToken.Category == DinoTokenCategory.Comma)
                {
                    NextToken();
                }
                else
                {
                    break;
                }
            } while (true);
        }
        
        Consume(DinoTokenCategory.CloseParen, "Expected ')' after IN values");
        
        return new DinoInExpression(expression, values, false, inToken.Line, inToken.Column);
    }

    private DinoExpression ParseBetweenExpression(DinoExpression expression)
    {
        var betweenToken = _currentToken;
        NextToken();
        
        var lower = ParseAdditiveExpression();
        Consume(DinoTokenCategory.And, "Expected AND in BETWEEN expression");
        var upper = ParseAdditiveExpression();
        
        return new DinoBetweenExpression(expression, lower, upper, false, betweenToken.Line, betweenToken.Column);
    }

    private DinoExpression ParseIsExpression(DinoExpression left)
    {
        var isToken = _currentToken;
        NextToken();
        
        bool isNegated = false;
        if (_currentToken.Category == DinoTokenCategory.Not)
        {
            isNegated = true;
            NextToken();
        }
        
        Consume(DinoTokenCategory.Null, "Expected NULL after IS");
        
        var op = isNegated ? DinoUnaryOperator.IsNotNull : DinoUnaryOperator.IsNull;
        return new DinoUnaryExpression(op, left, isToken.Line, isToken.Column);
    }

    private DinoExpression ParseAdditiveExpression()
    {
        var left = ParseMultiplicativeExpression();

        while (_currentToken.Category == DinoTokenCategory.Plus || 
               _currentToken.Category == DinoTokenCategory.Minus ||
               _currentToken.Category == DinoTokenCategory.Concat)
        {
            var op = _currentToken.Category == DinoTokenCategory.Plus ? DinoBinaryOperator.Add :
                     _currentToken.Category == DinoTokenCategory.Minus ? DinoBinaryOperator.Subtract :
                     DinoBinaryOperator.Concat;
            var opToken = _currentToken;
            NextToken();
            var right = ParseMultiplicativeExpression();
            left = new DinoBinaryExpression(left, op, right, opToken.Line, opToken.Column);
        }

        return left;
    }

    private DinoExpression ParseMultiplicativeExpression()
    {
        var left = ParseUnaryExpression();

        while (_currentToken.Category == DinoTokenCategory.Star || 
               _currentToken.Category == DinoTokenCategory.Divide ||
               _currentToken.Category == DinoTokenCategory.Modulo)
        {
            var op = _currentToken.Category == DinoTokenCategory.Star ? DinoBinaryOperator.Multiply :
                     _currentToken.Category == DinoTokenCategory.Divide ? DinoBinaryOperator.Divide :
                     DinoBinaryOperator.Modulo;
            var opToken = _currentToken;
            NextToken();
            var right = ParseUnaryExpression();
            left = new DinoBinaryExpression(left, op, right, opToken.Line, opToken.Column);
        }

        return left;
    }

    private DinoExpression ParseUnaryExpression()
    {
        if (_currentToken.Category == DinoTokenCategory.Minus)
        {
            var op = _currentToken;
            NextToken();
            var operand = ParseUnaryExpression();
            return new DinoUnaryExpression(DinoUnaryOperator.Minus, operand, op.Line, op.Column);
        }

        if (_currentToken.Category == DinoTokenCategory.Plus)
        {
            var op = _currentToken;
            NextToken();
            var operand = ParseUnaryExpression();
            return new DinoUnaryExpression(DinoUnaryOperator.Plus, operand, op.Line, op.Column);
        }

        return ParsePostfixExpression();
    }

    private DinoExpression ParsePostfixExpression()
    {
        var expression = ParsePrimaryExpression();

        while (true)
        {
            if (_currentToken.Category == DinoTokenCategory.Dot)
            {
                NextToken();
                var memberName = ConsumeIdentifier("Expected member name after '.'");
                expression = new DinoMemberAccessExpression(expression, memberName, _currentToken.Line, _currentToken.Column);
            }
            else if (_currentToken.Category == DinoTokenCategory.OpenParen && expression is DinoIdentifierExpression identifier)
            {
                // Function call
                expression = ParseFunctionCall(identifier.Name);
            }
            else
            {
                break;
            }
        }

        return expression;
    }

    private DinoExpression ParsePrimaryExpression()
    {
        switch (_currentToken.Category)
        {
            case DinoTokenCategory.NumberLiteral:
                return ParseNumberLiteral();

            case DinoTokenCategory.StringLiteral:
                return ParseStringLiteral();

            case DinoTokenCategory.BooleanLiteral:
                return ParseBooleanLiteral();

            case DinoTokenCategory.Null:
                var nullToken = _currentToken;
                NextToken();
                return new DinoLiteralExpression(null, typeof(object), nullToken.Line, nullToken.Column);

            case DinoTokenCategory.Identifier:
                if (_currentToken.Value.StartsWith("@"))
                    return ParseParameter();
                return ParseIdentifier();

            case DinoTokenCategory.OpenParen:
                return ParseParenthesizedExpression();

            case DinoTokenCategory.Case:
                return ParseCaseExpression();

            case DinoTokenCategory.Exists:
                return ParseExistsExpression();

            case DinoTokenCategory.Count:
            case DinoTokenCategory.Sum:
            case DinoTokenCategory.Avg:
            case DinoTokenCategory.Min:
            case DinoTokenCategory.Max:
            case DinoTokenCategory.StdDev:
            case DinoTokenCategory.Variance:
            case DinoTokenCategory.First:
            case DinoTokenCategory.Last:
            case DinoTokenCategory.StringAgg:
            case DinoTokenCategory.RowNumber:
            case DinoTokenCategory.Rank:
            case DinoTokenCategory.DenseRank:
            case DinoTokenCategory.PercentRank:
            case DinoTokenCategory.CumeDist:
            case DinoTokenCategory.Ntile:
            case DinoTokenCategory.Lag:
            case DinoTokenCategory.Lead:
            case DinoTokenCategory.FirstValue:
            case DinoTokenCategory.LastValue:
                return ParseFunctionCall(_currentToken.Value);

            default:
                throw CreateParserException($"Unexpected token '{_currentToken.Value}'");
        }
    }

    private DinoExpression ParseNumberLiteral()
    {
        var token = _currentToken;
        var value = token.Value;
        NextToken();

        if (value.Contains('.'))
        {
            if (decimal.TryParse(value, out var decimalValue))
                return new DinoLiteralExpression(decimalValue, typeof(decimal), token.Line, token.Column);
        }
        else
        {
            // Always parse integers as decimal to avoid type conversion issues
            if (decimal.TryParse(value, out var decimalValue))
                return new DinoLiteralExpression(decimalValue, typeof(decimal), token.Line, token.Column);
        }

        throw CreateParserException($"Invalid number literal '{value}'", token);
    }

    private DinoExpression ParseStringLiteral()
    {
        var token = _currentToken;
        var value = token.Value;
        NextToken();
        return new DinoLiteralExpression(value, typeof(string), token.Line, token.Column);
    }

    private DinoExpression ParseBooleanLiteral()
    {
        var token = _currentToken;
        var value = token.Value.ToUpper() == "TRUE";
        NextToken();
        return new DinoLiteralExpression(value, typeof(bool), token.Line, token.Column);
    }

    private DinoExpression ParseParameter()
    {
        var token = _currentToken;
        var paramName = token.Value;
        NextToken();

        // If parameters are provided, replace with actual value
        if (_parameters != null && _parameters.TryGetValue(paramName.Substring(1), out var value))
        {
            var type = value?.GetType() ?? typeof(object);
            return new DinoLiteralExpression(value, type, token.Line, token.Column);
        }

        return new DinoParameterExpression(paramName, token.Line, token.Column);
    }

    private DinoExpression ParseIdentifier()
    {
        var token = _currentToken;
        var name = token.Value;
        NextToken();
        return new DinoIdentifierExpression(name, token.Line, token.Column);
    }

    private DinoExpression ParseParenthesizedExpression()
    {
        Consume(DinoTokenCategory.OpenParen, "Expected '('");
        
        // Check for subquery
        if (_currentToken.Category == DinoTokenCategory.Select)
        {
            var subquery = ParseSelectQuery();
            Consume(DinoTokenCategory.CloseParen, "Expected ')' after subquery");
            return new DinoSubqueryExpression(subquery, _currentToken.Line, _currentToken.Column);
        }
        
        var expression = ParseExpression();
        Consume(DinoTokenCategory.CloseParen, "Expected ')' after expression");
        return expression;
    }

    private DinoExpression ParseFunctionCall(string functionName)
    {
        var funcToken = _currentToken;
        
        if (_currentToken.Category != DinoTokenCategory.OpenParen)
        {
            NextToken();
        }
        
        Consume(DinoTokenCategory.OpenParen, $"Expected '(' after function {functionName}");
        
        var arguments = new List<DinoExpression>();
        
        if (_currentToken.Category != DinoTokenCategory.CloseParen)
        {
            do
            {
                if (_currentToken.Category == DinoTokenCategory.Star && arguments.Count == 0)
                {
                    arguments.Add(new DinoIdentifierExpression("*", _currentToken.Line, _currentToken.Column));
                    NextToken();
                }
                else
                {
                    arguments.Add(ParseExpression());
                }
                
                if (_currentToken.Category == DinoTokenCategory.Comma)
                {
                    NextToken();
                }
                else
                {
                    break;
                }
            } while (true);
        }
        
        Consume(DinoTokenCategory.CloseParen, $"Expected ')' after function arguments");
        
        return new DinoFunctionCallExpression(functionName, arguments, null, funcToken.Line, funcToken.Column);
    }

    private DinoExpression ParseCaseExpression()
    {
        var caseToken = _currentToken;
        NextToken();
        
        DinoExpression? expression = null;
        
        // Check for simple CASE (CASE expression WHEN ...)
        if (_currentToken.Category != DinoTokenCategory.When)
        {
            expression = ParseExpression();
        }
        
        var whenClauses = new List<DinoWhenClause>();
        
        while (_currentToken.Category == DinoTokenCategory.When)
        {
            NextToken();
            var condition = ParseExpression();
            Consume(DinoTokenCategory.Then, "Expected THEN after WHEN condition");
            var result = ParseExpression();
            whenClauses.Add(new DinoWhenClause(condition, result));
        }
        
        DinoExpression? elseExpression = null;
        if (_currentToken.Category == DinoTokenCategory.Else)
        {
            NextToken();
            elseExpression = ParseExpression();
        }
        
        Consume(DinoTokenCategory.End, "Expected END for CASE expression");
        
        return new DinoCaseExpression(whenClauses, expression, elseExpression, caseToken.Line, caseToken.Column);
    }

    private DinoExpression ParseExistsExpression()
    {
        var existsToken = _currentToken;
        NextToken();
        
        Consume(DinoTokenCategory.OpenParen, "Expected '(' after EXISTS");
        var subquery = ParseSelectQuery();
        Consume(DinoTokenCategory.CloseParen, "Expected ')' after EXISTS subquery");
        
        return new DinoExistsExpression(subquery, false, existsToken.Line, existsToken.Column);
    }

    private int ParseIntegerLiteral(string errorMessage)
    {
        if (_currentToken.Category != DinoTokenCategory.NumberLiteral)
            throw CreateParserException(errorMessage);

        if (!int.TryParse(_currentToken.Value, out var value))
            throw CreateParserException($"Invalid integer value '{_currentToken.Value}'");

        NextToken();
        return value;
    }

    private void NextToken()
    {
        _currentToken = _dinoLexer.NextToken();
    }

    private void Consume(DinoTokenCategory expected, string errorMessage)
    {
        if (_currentToken.Category != expected)
            throw CreateParserException($"{errorMessage}. Expected {expected} but found {_currentToken.Category}");
        NextToken();
    }

    private string ConsumeIdentifier(string errorMessage)
    {
        if (_currentToken.Category != DinoTokenCategory.Identifier)
            throw CreateParserException(errorMessage);
        
        var value = _currentToken.Value;
        NextToken();
        return value;
    }

    private bool IsJoinKeyword(DinoTokenCategory category)
    {
        return category == DinoTokenCategory.Join ||
               category == DinoTokenCategory.Inner ||
               category == DinoTokenCategory.Left ||
               category == DinoTokenCategory.Right ||
               category == DinoTokenCategory.Full ||
               category == DinoTokenCategory.Cross;
    }

    private DinoBinaryOperator MapComparisonOperator(DinoTokenCategory category)
    {
        return category switch
        {
            DinoTokenCategory.Equal => DinoBinaryOperator.Equal,
            DinoTokenCategory.NotEqual => DinoBinaryOperator.NotEqual,
            DinoTokenCategory.LessThan => DinoBinaryOperator.LessThan,
            DinoTokenCategory.LessThanOrEqual => DinoBinaryOperator.LessThanOrEqual,
            DinoTokenCategory.GreaterThan => DinoBinaryOperator.GreaterThan,
            DinoTokenCategory.GreaterThanOrEqual => DinoBinaryOperator.GreaterThanOrEqual,
            _ => throw CreateParserException($"Invalid comparison operator: {category}")
        };
    }

    private DinoParserException CreateParserException(string message)
    {
        return new DinoParserException(message, _currentToken, _query);
    }

    private DinoParserException CreateParserException(string message, DinoToken token)
    {
        return new DinoParserException(message, token, _query);
    }
}