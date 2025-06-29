namespace Dino.Core.Ast.Expressions;

public sealed class DinoFunctionCallExpression(
    string functionName,
    IEnumerable<DinoExpression> arguments,
    DinoExpression? o = null,
    int line = 0,
    int column = 0)
    : DinoExpression(line, column)
{
    public string FunctionName { get; } = functionName ?? throw new ArgumentNullException(nameof(functionName));

    public IReadOnlyList<DinoExpression> Arguments { get; } =
        arguments.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(arguments));
    public DinoExpression? Object { get; } = o;
}