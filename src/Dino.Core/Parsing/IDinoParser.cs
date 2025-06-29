namespace Dino.Core.Parsing;

using Dino.Core.Ast.Queries;

public interface IDinoParser
{
    DinoSelectQuery Parse(string query);
    DinoSelectQuery Parse(string query, IDictionary<string, object?>? parameters);
}