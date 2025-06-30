# Contributing to Dino

First off, thank you for considering contributing to Dino! It's people like you that make Dino such a great tool.

## Code of Conduct

By participating in this project, you are expected to maintain a respectful and collaborative environment. Please be kind and considerate in all interactions.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check existing issues as you might find out that you don't need to create one. When you are creating a bug report, please include as many details as possible:

- **Use a clear and descriptive title**
- **Describe the exact steps which reproduce the problem**
- **Provide specific examples to demonstrate the steps**
- **Describe the behavior you observed after following the steps**
- **Explain which behavior you expected to see instead and why**
- **Include code samples and stack traces if applicable**

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, please include:

- **Use a clear and descriptive title**
- **Provide a step-by-step description of the suggested enhancement**
- **Provide specific examples to demonstrate the steps**
- **Describe the current behavior and explain which behavior you expected to see instead**
- **Explain why this enhancement would be useful**

### Pull Requests

1. Fork the repo and create your branch from `main`
2. Branch naming convention: `feature/your-feature-name`
3. If you've added code that should be tested, add tests
4. If you've changed APIs, update the documentation
5. Ensure the test suite passes
6. Make sure your code follows the existing code style
7. Issue that pull request!

## Development Setup

1. **Prerequisites**
   - .NET 9.0 SDK or later
   - Your favorite IDE (Visual Studio, VS Code, Rider)

2. **Clone the repository**
   ```bash
   git clone https://github.com/tafo/dino.git
   cd dino
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run tests**
   ```bash
   dotnet test
   ```

## Project Structure

```
Dino/
├── src/
│   ├── Dino.Core/          # Core DSL functionality (Lexer, Parser, AST)
│   └── Dino.EFCore/        # Entity Framework Core integration
├── tests/
│   ├── Dino.Core.Tests/    # Unit tests for core functionality
│   └── Dino.EFCore.Tests/  # Integration tests with EF Core
└── samples/                 # Example projects
```

## Coding Guidelines

### General Guidelines

- Write clean, readable, and maintainable code
- Follow C# naming conventions
- Keep methods small and focused
- Write meaningful commit messages
- Add XML documentation comments for public APIs
- Avoid unnecessary comments - code should be self-documenting

### C# Style Guide

- Use `var` when the type is obvious
- Use object initializers when possible
- Prefer LINQ over loops when it improves readability
- Use async/await for asynchronous operations
- Use nullable reference types

### Testing

- Write unit tests for all new functionality
- Use descriptive test names that explain what is being tested
- Follow the Arrange-Act-Assert pattern
- Use FluentAssertions for test assertions

### Example Test

```csharp
[Fact]
public void Parse_SimpleSelectQuery_ReturnsCorrectAst()
{
    // Arrange
    var parser = new DinoParser();
    var query = "SELECT * FROM users WHERE age > 18";
    
    // Act
    var result = parser.Parse(query);
    
    // Assert
    result.Should().NotBeNull();
    result.SelectItems.Should().HaveCount(1);
    result.WhereClause.Should().NotBeNull();
}
```

## Adding New Features

1. **Lexer Tokens**: If adding new SQL keywords, update `DinoTokenCategory` enum
2. **Parser**: Add parsing logic in `DinoParser` for new syntax
3. **AST Nodes**: Create new node types inheriting from `DinoQueryNode`
4. **Visitor**: Update `IDinoQueryVisitor` and implement in `DinoExpressionVisitor`
5. **Tests**: Add comprehensive tests for the new feature

## Commit Messages

- Use the present tense ("Add feature" not "Added feature")
- Use the imperative mood ("Move cursor to..." not "Moves cursor to...")
- Limit the first line to 72 characters or less
- Reference issues and pull requests liberally after the first line

### Examples

```
Add support for CASE WHEN expressions

- Implement CaseExpression AST node
- Add parsing logic for CASE/WHEN/THEN/ELSE/END
- Update expression visitor to handle CASE expressions
- Add comprehensive unit tests

Fixes #123
```

## Release Process

1. Update version numbers (follow Semantic Versioning)
2. Update CHANGELOG.md
3. Create a release tag
4. Build and publish NuGet packages

## Questions?

Feel free to open an issue with your question.

---

Copyright (c) 2024 Tayfun Yirdem

Thank you for contributing! 🦕