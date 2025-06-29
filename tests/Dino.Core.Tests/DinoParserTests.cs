using Dino.Core.Ast;
using Dino.Core.Ast.Expressions;
using Dino.Core.Parsing;
using FluentAssertions;

namespace Dino.Core.Tests;

public class DinoParserTests
{
    private readonly IDinoParser _parser = new DinoParser();

    [Fact]
    public void Parse_SimpleSelectAll_ParsesCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM users";

        // Act
        var result = _parser.Parse(query);

        // Assert
        result.Should().NotBeNull();
        result.SelectItems.Should().HaveCount(1);
        result.SelectItems[0].Expression.Should().BeOfType<DinoIdentifierExpression>();
        ((DinoIdentifierExpression)result.SelectItems[0].Expression).Name.Should().Be("*");
        
        result.FromClause.Should().NotBeNull();
        result.FromClause!.TableSource.TableName.Should().Be("users");
        result.FromClause.TableSource.Alias.Should().BeNull();
    }

    [Fact]
    public void Parse_SelectWithColumns_ParsesCorrectly()
    {
        // Arrange
        var query = "SELECT id, name, email FROM users";

        // Act
        var result = _parser.Parse(query);

        // Assert
        result.SelectItems.Should().HaveCount(3);
        result.SelectItems[0].Expression.Should().BeOfType<DinoIdentifierExpression>();
        ((DinoIdentifierExpression)result.SelectItems[0].Expression).Name.Should().Be("id");
        
        result.SelectItems[1].Expression.Should().BeOfType<DinoIdentifierExpression>();
        ((DinoIdentifierExpression)result.SelectItems[1].Expression).Name.Should().Be("name");
        
        result.SelectItems[2].Expression.Should().BeOfType<DinoIdentifierExpression>();
        ((DinoIdentifierExpression)result.SelectItems[2].Expression).Name.Should().Be("email");
    }

    [Fact]
    public void Parse_SelectWithAlias_ParsesCorrectly()
    {
        // Arrange
        var query = "SELECT name AS userName, age AS userAge FROM users";

        // Act
        var result = _parser.Parse(query);

        // Assert
        result.SelectItems.Should().HaveCount(2);
        result.SelectItems[0].Alias.Should().Be("userName");
        result.SelectItems[1].Alias.Should().Be("userAge");
    }

    [Fact]
    public void Parse_WhereWithComparison_ParsesCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE age > 18";

        // Act
        var result = _parser.Parse(query);

        // Assert
        result.WhereClause.Should().NotBeNull();
        var whereExpr = result.WhereClause!.Condition.Should().BeOfType<DinoBinaryExpression>().Subject;
        
        whereExpr.Operator.Should().Be(DinoBinaryOperator.GreaterThan);
        whereExpr.Left.Should().BeOfType<DinoIdentifierExpression>();
        ((DinoIdentifierExpression)whereExpr.Left).Name.Should().Be("age");
        
        whereExpr.Right.Should().BeOfType<DinoLiteralExpression>();
        var literal = (DinoLiteralExpression)whereExpr.Right;
        literal.Value.Should().Be(18);
        literal.ValueType.Should().Be(typeof(decimal));
    }

    [Fact]
    public void Parse_WhereWithAndCondition_ParsesCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE age > 18 AND status = 'active'";

        // Act
        var result = _parser.Parse(query);

        // Assert
        result.WhereClause.Should().NotBeNull();
        var andExpr = result.WhereClause!.Condition.Should().BeOfType<DinoBinaryExpression>().Subject;
        andExpr.Operator.Should().Be(DinoBinaryOperator.And);
        
        // Left side: age > 18
        var leftExpr = andExpr.Left.Should().BeOfType<DinoBinaryExpression>().Subject;
        leftExpr.Operator.Should().Be(DinoBinaryOperator.GreaterThan);
        
        // Right side: status = 'active'
        var rightExpr = andExpr.Right.Should().BeOfType<DinoBinaryExpression>().Subject;
        rightExpr.Operator.Should().Be(DinoBinaryOperator.Equal);
        ((DinoLiteralExpression)rightExpr.Right).Value.Should().Be("active");
    }

    [Fact]
    public void Parse_WhereWithInClause_ParsesCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE status IN ('active', 'pending', 'approved')";

        // Act
        var result = _parser.Parse(query);

        // Assert
        var inExpr = result.WhereClause!.Condition.Should().BeOfType<DinoInExpression>().Subject;
        inExpr.Expression.Should().BeOfType<DinoIdentifierExpression>();
        ((DinoIdentifierExpression)inExpr.Expression).Name.Should().Be("status");
        
        inExpr.Values.Should().HaveCount(3);
        inExpr.Values.All(v => v is DinoLiteralExpression).Should().BeTrue();
        inExpr.IsNegated.Should().BeFalse();
    }

    [Fact]
    public void Parse_WhereWithBetween_ParsesCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM products WHERE price BETWEEN 100 AND 500";

        // Act
        var result = _parser.Parse(query);

        // Assert
        var betweenExpr = result.WhereClause!.Condition.Should().BeOfType<DinoBetweenExpression>().Subject;
        ((DinoIdentifierExpression)betweenExpr.Expression).Name.Should().Be("price");
        ((DinoLiteralExpression)betweenExpr.LowerBound).Value.Should().Be(100);
        ((DinoLiteralExpression)betweenExpr.UpperBound).Value.Should().Be(500);
        betweenExpr.IsNegated.Should().BeFalse();
    }

    [Fact]
    public void Parse_WhereWithLike_ParsesCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE name LIKE 'John%'";

        // Act
        var result = _parser.Parse(query);

        // Assert
        var likeExpr = result.WhereClause!.Condition.Should().BeOfType<DinoBinaryExpression>().Subject;
        likeExpr.Operator.Should().Be(DinoBinaryOperator.Like);
        ((DinoLiteralExpression)likeExpr.Right).Value.Should().Be("John%");
    }

    [Fact]
    public void Parse_OrderBy_ParsesCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM users ORDER BY name ASC, age DESC";

        // Act
        var result = _parser.Parse(query);

        // Assert
        result.OrderByClause.Should().NotBeNull();
        result.OrderByClause!.Items.Should().HaveCount(2);
        
        result.OrderByClause.Items[0].Expression.Should().BeOfType<DinoIdentifierExpression>();
        ((DinoIdentifierExpression)result.OrderByClause.Items[0].Expression).Name.Should().Be("name");
        result.OrderByClause.Items[0].Direction.Should().Be(DinoOrderDirection.Ascending);
        
        result.OrderByClause.Items[1].Expression.Should().BeOfType<DinoIdentifierExpression>();
        ((DinoIdentifierExpression)result.OrderByClause.Items[1].Expression).Name.Should().Be("age");
        result.OrderByClause.Items[1].Direction.Should().Be(DinoOrderDirection.Descending);
    }

    [Fact]
    public void Parse_GroupByWithHaving_ParsesCorrectly()
    {
        // Arrange
        var query = "SELECT category, COUNT(*) FROM products GROUP BY category HAVING COUNT(*) > 5";

        // Act
        var result = _parser.Parse(query);

        // Assert
        result.GroupByClause.Should().NotBeNull();
        result.GroupByClause!.GroupingExpressions.Should().HaveCount(1);
        
        var groupExpr = result.GroupByClause.GroupingExpressions[0].Should().BeOfType<DinoIdentifierExpression>().Subject;
        groupExpr.Name.Should().Be("category");
        
        result.GroupByClause.HavingCondition.Should().NotBeNull();
        var havingExpr = result.GroupByClause.HavingCondition.Should().BeOfType<DinoBinaryExpression>().Subject;
        havingExpr.Operator.Should().Be(DinoBinaryOperator.GreaterThan);
    }

    [Fact]
    public void Parse_LimitOffset_ParsesCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM users LIMIT 10 OFFSET 20";

        // Act
        var result = _parser.Parse(query);

        // Assert
        result.Limit.Should().Be(10);
        result.Offset.Should().Be(20);
    }

    [Fact]
    public void Parse_WithParameters_ReplacesCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE age > @minAge AND status = @status";
        var parameters = new Dictionary<string, object?>
        {
            ["minAge"] = 18,
            ["status"] = "active"
        };

        // Act
        var result = _parser.Parse(query, parameters);

        // Assert
        var andExpr = result.WhereClause!.Condition.Should().BeOfType<DinoBinaryExpression>().Subject;
        
        // Check that parameters were replaced with literals
        var leftExpr = andExpr.Left.Should().BeOfType<DinoBinaryExpression>().Subject;
        var ageValue = leftExpr.Right.Should().BeOfType<DinoLiteralExpression>().Subject;
        ageValue.Value.Should().Be(18);
        
        var rightExpr = andExpr.Right.Should().BeOfType<DinoBinaryExpression>().Subject;
        var statusValue = rightExpr.Right.Should().BeOfType<DinoLiteralExpression>().Subject;
        statusValue.Value.Should().Be("active");
    }

    [Fact]
    public void Parse_InvalidSyntax_ThrowsException()
    {
        // Arrange
        var query = "SELECT * FORM users"; // Typo: FORM instead of FROM

        // Act & Assert
        var act = () => _parser.Parse(query);
        act.Should().Throw<Exceptions.DinoParserException>()
            .WithMessage("*Unexpected token 'FORM' after query end*");
    }

    [Fact]
    public void Parse_EmptyQuery_ThrowsException()
    {
        // Arrange
        var query = "";

        // Act & Assert
        var act = () => _parser.Parse(query);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Query cannot be null or empty*");
    }
}