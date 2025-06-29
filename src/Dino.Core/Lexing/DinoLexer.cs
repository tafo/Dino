namespace Dino.Core.Lexing;

using System.Text;
using Tokens;
using Exceptions;

public sealed class DinoLexer(string input) : IDinoLexer
{
    private int _position;
    private int _line = 1;
    private int _column = 1;
    private int _tokenStartPosition;
    private int _tokenStartLine;
    private int _tokenStartColumn;

    private static readonly Dictionary<string, DinoTokenCategory> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SELECT"] = DinoTokenCategory.Select,
        ["FROM"] = DinoTokenCategory.From,
        ["WHERE"] = DinoTokenCategory.Where,
        ["WITH"] = DinoTokenCategory.With,
        ["JOIN"] = DinoTokenCategory.Join,
        ["LEFT"] = DinoTokenCategory.Left,
        ["RIGHT"] = DinoTokenCategory.Right,
        ["INNER"] = DinoTokenCategory.Inner,
        ["OUTER"] = DinoTokenCategory.Outer,
        ["FULL"] = DinoTokenCategory.Full,
        ["CROSS"] = DinoTokenCategory.Cross,
        ["ON"] = DinoTokenCategory.On,
        ["GROUP"] = DinoTokenCategory.Group,
        ["BY"] = DinoTokenCategory.By,
        ["HAVING"] = DinoTokenCategory.Having,
        ["ORDER"] = DinoTokenCategory.Order,
        ["ASC"] = DinoTokenCategory.Asc,
        ["DESC"] = DinoTokenCategory.Desc,
        ["LIMIT"] = DinoTokenCategory.Limit,
        ["OFFSET"] = DinoTokenCategory.Offset,
        ["DISTINCT"] = DinoTokenCategory.Distinct,
        ["AS"] = DinoTokenCategory.As,
        ["UNION"] = DinoTokenCategory.Union,
        ["ALL"] = DinoTokenCategory.All,
        ["INTERSECT"] = DinoTokenCategory.Intersect,
        ["EXCEPT"] = DinoTokenCategory.Except,
        ["CASE"] = DinoTokenCategory.Case,
        ["WHEN"] = DinoTokenCategory.When,
        ["THEN"] = DinoTokenCategory.Then,
        ["ELSE"] = DinoTokenCategory.Else,
        ["END"] = DinoTokenCategory.End,
        ["EXISTS"] = DinoTokenCategory.Exists,
        ["ANY"] = DinoTokenCategory.Any,
        ["SOME"] = DinoTokenCategory.Some,
        ["CAST"] = DinoTokenCategory.Cast,
        ["CONVERT"] = DinoTokenCategory.Convert,
        ["TOP"] = DinoTokenCategory.Top,
        ["INTO"] = DinoTokenCategory.Into,
        ["OVER"] = DinoTokenCategory.Over,
        ["PARTITION"] = DinoTokenCategory.Partition,
        ["ROW"] = DinoTokenCategory.Row,
        ["ROWS"] = DinoTokenCategory.Rows,
        ["RANGE"] = DinoTokenCategory.Range,
        ["PRECEDING"] = DinoTokenCategory.Preceding,
        ["FOLLOWING"] = DinoTokenCategory.Following,
        ["CURRENT"] = DinoTokenCategory.Current,
        ["UNBOUNDED"] = DinoTokenCategory.Unbounded,
        ["AND"] = DinoTokenCategory.And,
        ["OR"] = DinoTokenCategory.Or,
        ["NOT"] = DinoTokenCategory.Not,
        ["IN"] = DinoTokenCategory.In,
        ["LIKE"] = DinoTokenCategory.Like,
        ["BETWEEN"] = DinoTokenCategory.Between,
        ["IS"] = DinoTokenCategory.Is,
        ["NULL"] = DinoTokenCategory.Null,
        ["TRUE"] = DinoTokenCategory.BooleanLiteral,
        ["FALSE"] = DinoTokenCategory.BooleanLiteral,
        ["COUNT"] = DinoTokenCategory.Count,
        ["SUM"] = DinoTokenCategory.Sum,
        ["AVG"] = DinoTokenCategory.Avg,
        ["MIN"] = DinoTokenCategory.Min,
        ["MAX"] = DinoTokenCategory.Max,
        ["STDDEV"] = DinoTokenCategory.StdDev,
        ["VARIANCE"] = DinoTokenCategory.Variance,
        ["FIRST"] = DinoTokenCategory.First,
        ["LAST"] = DinoTokenCategory.Last,
        ["STRING_AGG"] = DinoTokenCategory.StringAgg,
        ["ROW_NUMBER"] = DinoTokenCategory.RowNumber,
        ["RANK"] = DinoTokenCategory.Rank,
        ["DENSE_RANK"] = DinoTokenCategory.DenseRank,
        ["PERCENT_RANK"] = DinoTokenCategory.PercentRank,
        ["CUME_DIST"] = DinoTokenCategory.CumeDist,
        ["NTILE"] = DinoTokenCategory.Ntile,
        ["LAG"] = DinoTokenCategory.Lag,
        ["LEAD"] = DinoTokenCategory.Lead,
        ["FIRST_VALUE"] = DinoTokenCategory.FirstValue,
        ["LAST_VALUE"] = DinoTokenCategory.LastValue,
        ["INCLUDE"] = DinoTokenCategory.Include,
        ["THENINCLUDE"] = DinoTokenCategory.ThenInclude,
        ["ASNOTRACKING"] = DinoTokenCategory.AsNoTracking,
        ["ASTRACKING"] = DinoTokenCategory.AsTracking,
        ["ASSPLITQUERY"] = DinoTokenCategory.AsSplitQuery,
        ["ASNOTRACKINGWITHIDENTITYRESOLUTION"] = DinoTokenCategory.AsNoTrackingWithIdentityResolution
    };

    public DinoToken NextToken()
    {
        SkipWhitespace();

        if (IsAtEnd())
            return CreateToken(DinoTokenCategory.End, string.Empty);

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

    public DinoToken PeekToken()
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

    public IEnumerable<DinoToken> Tokenize()
    {
        Reset();
        var tokens = new List<DinoToken>();
        DinoToken dinoToken;

        do
        {
            dinoToken = NextToken();
            tokens.Add(dinoToken);
        } while (dinoToken.Category != DinoTokenCategory.End);

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

    private DinoToken ReadIdentifierOrKeyword()
    {
        var value = new StringBuilder();

        while (!IsAtEnd() && (char.IsLetterOrDigit(CurrentChar()) || CurrentChar() == '_'))
        {
            value.Append(CurrentChar());
            Advance();
        }

        var text = value.ToString();

        return Keywords.TryGetValue(text, out var category)
            ? CreateToken(category, category == DinoTokenCategory.BooleanLiteral ? text.ToUpper() : text)
            : CreateToken(DinoTokenCategory.Identifier, text);
    }

    private DinoToken ReadNumber()
    {
        var value = new StringBuilder();

        while (!IsAtEnd() && char.IsDigit(CurrentChar()))
        {
            value.Append(CurrentChar());
            Advance();
        }

        if (IsAtEnd() || CurrentChar() != '.' || Peek() == '.' || !char.IsDigit(Peek()))
        {
            return CreateToken(DinoTokenCategory.NumberLiteral, value.ToString());
        }

        value.Append(CurrentChar());
        Advance();

        while (!IsAtEnd() && char.IsDigit(CurrentChar()))
        {
            value.Append(CurrentChar());
            Advance();
        }

        return CreateToken(DinoTokenCategory.NumberLiteral, value.ToString());
    }

    private DinoToken ReadString()
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
            throw new DinoLexerException(
                $"Unterminated string literal starting at position {_tokenStartPosition}",
                _position, _line, _column, input);
        }

        Advance();
        return CreateToken(DinoTokenCategory.StringLiteral, value.ToString());
    }

    private DinoToken ReadParameter()
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
            throw new DinoLexerException(
                "Invalid parameter name",
                _position, _line, _column, input);
        }

        return CreateToken(DinoTokenCategory.Identifier, value.ToString());
    }

    private DinoToken ReadOperatorOrSymbol()
    {
        var ch = CurrentChar();
        Advance();

        switch (ch)
        {
            case '=':
                return CreateToken(DinoTokenCategory.Equal, "=");

            case '>':
                if (CurrentChar() == '=')
                {
                    Advance();
                    return CreateToken(DinoTokenCategory.GreaterThanOrEqual, ">=");
                }

                return CreateToken(DinoTokenCategory.GreaterThan, ">");

            case '<':
                if (CurrentChar() == '=')
                {
                    Advance();
                    return CreateToken(DinoTokenCategory.LessThanOrEqual, "<=");
                }

                if (CurrentChar() == '>')
                {
                    Advance();
                    return CreateToken(DinoTokenCategory.NotEqual, "<>");
                }

                return CreateToken(DinoTokenCategory.LessThan, "<");

            case '!':
                if (CurrentChar() == '=')
                {
                    Advance();
                    return CreateToken(DinoTokenCategory.NotEqual, "!=");
                }

                throw new DinoLexerException("Unexpected character '!'", _position, _line, _column, input);

            case '(':
                return CreateToken(DinoTokenCategory.OpenParen, "(");

            case ')':
                return CreateToken(DinoTokenCategory.CloseParen, ")");

            case '[':
                return CreateToken(DinoTokenCategory.OpenBracket, "[");

            case ']':
                return CreateToken(DinoTokenCategory.CloseBracket, "]");

            case ',':
                return CreateToken(DinoTokenCategory.Comma, ",");

            case '.':
                return CreateToken(DinoTokenCategory.Dot, ".");

            case '*':
                return CreateToken(DinoTokenCategory.Star, "*");

            case '+':
                return CreateToken(DinoTokenCategory.Plus, "+");

            case '-':
                return CreateToken(DinoTokenCategory.Minus, "-");

            case '/':
                return CreateToken(DinoTokenCategory.Divide, "/");

            case '%':
                return CreateToken(DinoTokenCategory.Modulo, "%");

            case '|':
                if (CurrentChar() != '|')
                {
                    throw new DinoLexerException("Unexpected character '|'", _position, _line, _column, input);
                }

                Advance();
                return CreateToken(DinoTokenCategory.Concat, "||");

            default:
                throw new DinoLexerException($"Unexpected character '{ch}'", _position - 1, _line, _column - 1, input);
        }
    }

    private DinoToken CreateToken(DinoTokenCategory category, string value)
    {
        return new DinoToken(category, value, _tokenStartPosition, _tokenStartLine, _tokenStartColumn);
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