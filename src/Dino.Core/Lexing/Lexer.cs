namespace Dino.Core.Lexing;

using System.Text;
using Tokens;
using Exceptions;

public sealed class Lexer(string input) : ILexer
{
    private int _position;
    private int _line = 1;
    private int _column = 1;
    private int _tokenStartPosition;
    private int _tokenStartLine;
    private int _tokenStartColumn;

    private static readonly Dictionary<string, TokenCategory> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SELECT"] = TokenCategory.Select,
        ["FROM"] = TokenCategory.From,
        ["WHERE"] = TokenCategory.Where,
        ["WITH"] = TokenCategory.With,
        ["JOIN"] = TokenCategory.Join,
        ["LEFT"] = TokenCategory.Left,
        ["RIGHT"] = TokenCategory.Right,
        ["INNER"] = TokenCategory.Inner,
        ["OUTER"] = TokenCategory.Outer,
        ["FULL"] = TokenCategory.Full,
        ["CROSS"] = TokenCategory.Cross,
        ["ON"] = TokenCategory.On,
        ["GROUP"] = TokenCategory.Group,
        ["BY"] = TokenCategory.By,
        ["HAVING"] = TokenCategory.Having,
        ["ORDER"] = TokenCategory.Order,
        ["ASC"] = TokenCategory.Asc,
        ["DESC"] = TokenCategory.Desc,
        ["LIMIT"] = TokenCategory.Limit,
        ["OFFSET"] = TokenCategory.Offset,
        ["DISTINCT"] = TokenCategory.Distinct,
        ["AS"] = TokenCategory.As,
        ["UNION"] = TokenCategory.Union,
        ["ALL"] = TokenCategory.All,
        ["INTERSECT"] = TokenCategory.Intersect,
        ["EXCEPT"] = TokenCategory.Except,
        ["CASE"] = TokenCategory.Case,
        ["WHEN"] = TokenCategory.When,
        ["THEN"] = TokenCategory.Then,
        ["ELSE"] = TokenCategory.Else,
        ["END"] = TokenCategory.End,
        ["EXISTS"] = TokenCategory.Exists,
        ["ANY"] = TokenCategory.Any,
        ["SOME"] = TokenCategory.Some,
        ["CAST"] = TokenCategory.Cast,
        ["CONVERT"] = TokenCategory.Convert,
        ["TOP"] = TokenCategory.Top,
        ["INTO"] = TokenCategory.Into,
        ["OVER"] = TokenCategory.Over,
        ["PARTITION"] = TokenCategory.Partition,
        ["ROW"] = TokenCategory.Row,
        ["ROWS"] = TokenCategory.Rows,
        ["RANGE"] = TokenCategory.Range,
        ["PRECEDING"] = TokenCategory.Preceding,
        ["FOLLOWING"] = TokenCategory.Following,
        ["CURRENT"] = TokenCategory.Current,
        ["UNBOUNDED"] = TokenCategory.Unbounded,
        ["AND"] = TokenCategory.And,
        ["OR"] = TokenCategory.Or,
        ["NOT"] = TokenCategory.Not,
        ["IN"] = TokenCategory.In,
        ["LIKE"] = TokenCategory.Like,
        ["BETWEEN"] = TokenCategory.Between,
        ["IS"] = TokenCategory.Is,
        ["NULL"] = TokenCategory.Null,
        ["TRUE"] = TokenCategory.BooleanLiteral,
        ["FALSE"] = TokenCategory.BooleanLiteral,
        ["COUNT"] = TokenCategory.Count,
        ["SUM"] = TokenCategory.Sum,
        ["AVG"] = TokenCategory.Avg,
        ["MIN"] = TokenCategory.Min,
        ["MAX"] = TokenCategory.Max,
        ["STDDEV"] = TokenCategory.StdDev,
        ["VARIANCE"] = TokenCategory.Variance,
        ["FIRST"] = TokenCategory.First,
        ["LAST"] = TokenCategory.Last,
        ["STRING_AGG"] = TokenCategory.StringAgg,
        ["ROW_NUMBER"] = TokenCategory.RowNumber,
        ["RANK"] = TokenCategory.Rank,
        ["DENSE_RANK"] = TokenCategory.DenseRank,
        ["PERCENT_RANK"] = TokenCategory.PercentRank,
        ["CUME_DIST"] = TokenCategory.CumeDist,
        ["NTILE"] = TokenCategory.Ntile,
        ["LAG"] = TokenCategory.Lag,
        ["LEAD"] = TokenCategory.Lead,
        ["FIRST_VALUE"] = TokenCategory.FirstValue,
        ["LAST_VALUE"] = TokenCategory.LastValue,
        ["INCLUDE"] = TokenCategory.Include,
        ["THENINCLUDE"] = TokenCategory.ThenInclude,
        ["ASNOTRACKING"] = TokenCategory.AsNoTracking,
        ["ASTRACKING"] = TokenCategory.AsTracking,
        ["ASSPLITQUERY"] = TokenCategory.AsSplitQuery,
        ["ASNOTRACKINGWITHIDENTITYRESOLUTION"] = TokenCategory.AsNoTrackingWithIdentityResolution
    };

    public Token NextToken()
    {
        SkipWhitespace();

        if (IsAtEnd())
            return CreateToken(TokenCategory.End, string.Empty);

        MarkTokenStart();

        var ch = CurrentChar();

        if (char.IsLetter(ch) || ch == '_')
            return ReadIdentifierOrKeyword();

        if (char.IsDigit(ch))
            return ReadNumber();

        return ch switch
        {
            '\'' or '"' => ReadString(),
            '@' => ReadParameter(),
            _ => ReadOperatorOrSymbol()
        };
    }

    public Token PeekToken()
    {
        var savedPosition = _position;
        var savedLine = _line;
        var savedColumn = _column;

        var token = NextToken();

        _position = savedPosition;
        _line = savedLine;
        _column = savedColumn;

        return token;
    }

    public void Reset()
    {
        _position = 0;
        _line = 1;
        _column = 1;
    }

    public IEnumerable<Token> Tokenize()
    {
        Reset();
        var tokens = new List<Token>();
        Token token;

        do
        {
            token = NextToken();
            tokens.Add(token);
        } while (token.Category != TokenCategory.End);

        return tokens;
    }

    private void SkipWhitespace()
    {
        while (!IsAtEnd() && char.IsWhiteSpace(CurrentChar()))
        {
            if (CurrentChar() == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }

            _position++;
        }
    }

    private Token ReadIdentifierOrKeyword()
    {
        var value = new StringBuilder();

        while (!IsAtEnd() && (char.IsLetterOrDigit(CurrentChar()) || CurrentChar() == '_'))
        {
            value.Append(CurrentChar());
            Advance();
        }

        var text = value.ToString();

        return Keywords.TryGetValue(text, out var category)
            ? CreateToken(category, category == TokenCategory.BooleanLiteral ? text.ToUpper() : text)
            : CreateToken(TokenCategory.Identifier, text);
    }

    private Token ReadNumber()
    {
        var value = new StringBuilder();

        while (!IsAtEnd() && char.IsDigit(CurrentChar()))
        {
            value.Append(CurrentChar());
            Advance();
        }

        if (IsAtEnd() || CurrentChar() != '.' || Peek() == '.' || !char.IsDigit(Peek()))
        {
            return CreateToken(TokenCategory.NumberLiteral, value.ToString());
        }

        value.Append(CurrentChar());
        Advance();

        while (!IsAtEnd() && char.IsDigit(CurrentChar()))
        {
            value.Append(CurrentChar());
            Advance();
        }

        return CreateToken(TokenCategory.NumberLiteral, value.ToString());
    }

    private Token ReadString()
    {
        var quote = CurrentChar();
        Advance();
        var value = new StringBuilder();

        while (!IsAtEnd() && CurrentChar() != quote)
        {
            if (CurrentChar() == '\\' && Peek() == quote)
            {
                Advance();
                value.Append(quote);
                Advance();
            }
            else
            {
                value.Append(CurrentChar());
                Advance();
            }
        }

        if (IsAtEnd())
        {
            throw new LexerException(
                $"Unterminated string literal starting at position {_tokenStartPosition}",
                _position, _line, _column, input);
        }

        Advance();
        return CreateToken(TokenCategory.StringLiteral, value.ToString());
    }

    private Token ReadParameter()
    {
        Advance();
        var value = new StringBuilder("@");

        if (!IsAtEnd() && (char.IsLetter(CurrentChar()) || CurrentChar() == '_'))
        {
            while (!IsAtEnd() && (char.IsLetterOrDigit(CurrentChar()) || CurrentChar() == '_'))
            {
                value.Append(CurrentChar());
                Advance();
            }
        }
        else
        {
            throw new LexerException(
                "Invalid parameter name",
                _position, _line, _column, input);
        }

        return CreateToken(TokenCategory.Identifier, value.ToString());
    }

    private Token ReadOperatorOrSymbol()
    {
        var ch = CurrentChar();
        Advance();

        switch (ch)
        {
            case '=':
                return CreateToken(TokenCategory.Equal, "=");

            case '>':
                if (CurrentChar() == '=')
                {
                    Advance();
                    return CreateToken(TokenCategory.GreaterThanOrEqual, ">=");
                }

                return CreateToken(TokenCategory.GreaterThan, ">");

            case '<':
                if (CurrentChar() == '=')
                {
                    Advance();
                    return CreateToken(TokenCategory.LessThanOrEqual, "<=");
                }

                if (CurrentChar() == '>')
                {
                    Advance();
                    return CreateToken(TokenCategory.NotEqual, "<>");
                }

                return CreateToken(TokenCategory.LessThan, "<");

            case '!':
                if (CurrentChar() == '=')
                {
                    Advance();
                    return CreateToken(TokenCategory.NotEqual, "!=");
                }

                throw new LexerException("Unexpected character '!'", _position, _line, _column, input);

            case '(':
                return CreateToken(TokenCategory.OpenParen, "(");

            case ')':
                return CreateToken(TokenCategory.CloseParen, ")");

            case '[':
                return CreateToken(TokenCategory.OpenBracket, "[");

            case ']':
                return CreateToken(TokenCategory.CloseBracket, "]");

            case ',':
                return CreateToken(TokenCategory.Comma, ",");

            case '.':
                return CreateToken(TokenCategory.Dot, ".");

            case '*':
                return CreateToken(TokenCategory.Star, "*");

            case '+':
                return CreateToken(TokenCategory.Plus, "+");

            case '-':
                return CreateToken(TokenCategory.Minus, "-");

            case '/':
                return CreateToken(TokenCategory.Divide, "/");

            case '%':
                return CreateToken(TokenCategory.Modulo, "%");

            case '|':
                if (CurrentChar() != '|')
                {
                    throw new LexerException("Unexpected character '|'", _position, _line, _column, input);
                }

                Advance();
                return CreateToken(TokenCategory.Concat, "||");

            default:
                throw new LexerException($"Unexpected character '{ch}'", _position - 1, _line, _column - 1, input);
        }
    }

    private Token CreateToken(TokenCategory category, string value)
    {
        return new Token(category, value, _tokenStartPosition, _tokenStartLine, _tokenStartColumn);
    }

    private void MarkTokenStart()
    {
        _tokenStartPosition = _position;
        _tokenStartLine = _line;
        _tokenStartColumn = _column;
    }

    private char CurrentChar() => input[_position];

    private char Peek() => _position + 1 < input.Length ? input[_position + 1] : '\0';

    private bool IsAtEnd() => _position >= input.Length;

    private void Advance()
    {
        if (IsAtEnd()) return;
        if (CurrentChar() == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        _position++;
    }
}