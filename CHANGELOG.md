# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- GROUP BY clause support with HAVING conditions
- Aggregate functions (COUNT, SUM, AVG, MIN, MAX)
- CASE WHEN expressions
- Subquery support
- More comprehensive error messages

### Changed
- Improved performance for complex queries
- Better type inference for numeric operations

## [1.0.0] - 2025-01-01

### Added
- Initial release of Dino
- SQL-like DSL for Entity Framework Core
- Basic query support (SELECT, FROM, WHERE, ORDER BY)
- JOIN support with automatic Include() mapping
- Parameter binding to prevent SQL injection
- Support for common operators (=, !=, >, <, >=, <=)
- IN and BETWEEN operators
- LIKE operator with pattern matching
- IS NULL / IS NOT NULL support
- LIMIT and OFFSET for pagination
- DISTINCT queries
- Dynamic table queries via DbContext extension
- Comprehensive test coverage
- Full async/await support

### Security
- Safe parameter binding to prevent SQL injection
- Validation against EF Core model to prevent invalid queries

[Unreleased]: https://github.com/tafo/dino/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/tafo/dino/releases/tag/v1.0.0