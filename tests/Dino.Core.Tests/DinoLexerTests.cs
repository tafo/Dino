using Dino.Core.Lexing;
using Dino.Core.Tokens;
using FluentAssertions;

namespace Dino.Core.Tests;

public class DinoLexerTests
{
    [Fact]
    public void NextToken_SimpleSelect_ReturnsCorrectTokens()
    {
        // Arrange
        var lexer = new DinoLexer("SELECT * FROM users");

        // Act & Assert
        var token1 = lexer.NextToken();
        token1.Category.Should().Be(DinoTokenCategory.Select);
        token1.Value.Should().Be("SELECT");

        var token2 = lexer.NextToken();
        token2.Category.Should().Be(DinoTokenCategory.Star);
        token2.Value.Should().Be("*");

        var token3 = lexer.NextToken();
        token3.Category.Should().Be(DinoTokenCategory.From);
        token3.Value.Should().Be("FROM");

        var token4 = lexer.NextToken();
        token4.Category.Should().Be(DinoTokenCategory.Identifier);
        token4.Value.Should().Be("users");

        var token5 = lexer.NextToken();
        token5.Category.Should().Be(DinoTokenCategory.End);
    }

    [Fact]
    public void NextToken_StringLiteral_HandlesCorrectly()
    {
        // Arrange
        var lexer = new DinoLexer("'Hello World'");

        // Act
        var token = lexer.NextToken();

        // Assert
        token.Category.Should().Be(DinoTokenCategory.StringLiteral);
        token.Value.Should().Be("Hello World");
    }

    [Fact]
    public void NextToken_NumberLiterals_HandlesIntegers()
    {
        // Arrange
        var lexer = new DinoLexer("42");

        // Act
        var token = lexer.NextToken();

        // Assert
        token.Category.Should().Be(DinoTokenCategory.NumberLiteral);
        token.Value.Should().Be("42");
    }

    [Fact]
    public void NextToken_NumberLiterals_HandlesDecimals()
    {
        // Arrange
        var lexer = new DinoLexer("42.56");

        // Act
        var token = lexer.NextToken();

        // Assert
        token.Category.Should().Be(DinoTokenCategory.NumberLiteral);
        token.Value.Should().Be("42.56");
    }

    [Fact]
    public void NextToken_BooleanLiterals_HandlesTrue()
    {
        // Arrange
        var lexer = new DinoLexer("TRUE");

        // Act
        var token = lexer.NextToken();

        // Assert
        token.Category.Should().Be(DinoTokenCategory.BooleanLiteral);
        token.Value.Should().Be("TRUE");
    }

    [Fact]
    public void NextToken_Operators_HandlesComparison()
    {
        // Arrange
        var lexer = new DinoLexer(">= <= <> !=");

        // Act & Assert
        lexer.NextToken().Category.Should().Be(DinoTokenCategory.GreaterThanOrEqual);
        lexer.NextToken().Category.Should().Be(DinoTokenCategory.LessThanOrEqual);
        lexer.NextToken().Category.Should().Be(DinoTokenCategory.NotEqual);
        lexer.NextToken().Category.Should().Be(DinoTokenCategory.NotEqual);
    }

    [Fact]
    public void NextToken_Parameters_HandlesCorrectly()
    {
        // Arrange
        var lexer = new DinoLexer("@userId");

        // Act
        var token = lexer.NextToken();

        // Assert
        token.Category.Should().Be(DinoTokenCategory.Identifier);
        token.Value.Should().Be("@userId");
    }

    [Fact]
    public void NextToken_ComplexQuery_TokenizesCorrectly()
    {
        // Arrange
        var query = "SELECT name, age FROM users WHERE age > 18 AND status = 'active'";
        var lexer = new DinoLexer(query);

        // Act
        var tokens = new List<DinoToken>();
        DinoToken token;
        do
        {
            token = lexer.NextToken();
            tokens.Add(token);
        } while (token.Category != DinoTokenCategory.End);

        // Assert
        tokens.Should().HaveCount(15); // Including END token

        // Verify key tokens
        tokens[0].Category.Should().Be(DinoTokenCategory.Select);
        tokens[4].Category.Should().Be(DinoTokenCategory.From);
        tokens[6].Category.Should().Be(DinoTokenCategory.Where);
        tokens[8].Category.Should().Be(DinoTokenCategory.GreaterThan);
        tokens[9].Category.Should().Be(DinoTokenCategory.NumberLiteral);
        tokens[9].Value.Should().Be("18");
        tokens[10].Category.Should().Be(DinoTokenCategory.And);
        tokens[12].Category.Should().Be(DinoTokenCategory.Equal);
        tokens[13].Category.Should().Be(DinoTokenCategory.StringLiteral);
        tokens[13].Value.Should().Be("active");
        tokens[14].Category.Should().Be(DinoTokenCategory.End);
    }

    [Fact]
    public void NextToken_InvalidCharacter_ThrowsException()
    {
        // Arrange
        var lexer = new DinoLexer("SELECT $ FROM users");
        lexer.NextToken(); // SELECT

        // Act & Assert
        var act = () => lexer.NextToken();
        act.Should().Throw<Exceptions.DinoLexerException>()
            .WithMessage("Unexpected character '$'*");
    }

    [Fact]
    public void NextToken_UnterminatedString_ThrowsException()
    {
        // Arrange
        var lexer = new DinoLexer("'unterminated string");

        // Act & Assert
        var act = () => lexer.NextToken();
        act.Should().Throw<Exceptions.DinoLexerException>()
            .WithMessage("Unterminated string literal*");
    }

    [Fact]
    public void NextToken_MultiLineQuery_TracksLineAndColumn()
    {
        // Arrange
        var query = @"SELECT *
FROM users
WHERE age > 18";
        var lexer = new DinoLexer(query);

        // Act
        var selectToken = lexer.NextToken();
        lexer.NextToken(); // *
        var fromToken = lexer.NextToken();
        lexer.NextToken(); // users
        var whereToken = lexer.NextToken();

        // Assert
        selectToken.Line.Should().Be(1);
        selectToken.Column.Should().Be(1);

        fromToken.Line.Should().Be(2);
        fromToken.Column.Should().Be(1);

        whereToken.Line.Should().Be(3);
        whereToken.Column.Should().Be(1);
    }
}