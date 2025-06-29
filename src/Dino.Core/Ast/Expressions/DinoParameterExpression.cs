using Dino.Core.Ast.Visitors;

namespace Dino.Core.Ast.Expressions;

public sealed class DinoParameterExpression : DinoExpression
{
    public string ParameterName { get; }
    
    public DinoParameterExpression(string parameterName, int line = 0, int column = 0) 
        : base(line, column)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
            throw new ArgumentException("Parameter name cannot be null or empty", nameof(parameterName));
            
        ParameterName = parameterName.StartsWith('@') ? parameterName : "@" + parameterName;
    }
    
    public override void Accept(IDinoQueryVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IDinoQueryVisitor<T> visitor) => visitor.Visit(this);
}