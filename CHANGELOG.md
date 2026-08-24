# Changelog

All notable changes to the CWM Clean Architecture Template will be documented in this file.

## [1.0.0] - 2026-03-25

### Initial Release

- Clean Architecture: Domain, Application, Infrastructure, Api
- .NET 10 / C# 14 with `.slnx` solution format
- Manual CQRS (ICommand, IQuery, handlers) — zero commercial dependencies
- Minimal APIs with TypedResults
- FluentValidation 12 + Result pattern + ProblemDetails (RFC 9457)
- EF Core 10 + PostgreSQL
- Microsoft HybridCache (L1 in-memory + L2 Redis)
- ASP.NET Identity + JWT authentication with refresh tokens
- Scalar API documentation (modern OpenAPI UI)
- Serilog 10 structured logging
- .NET Aspire 13 + OpenTelemetry (traces, metrics, logs)
- Global exception handler
- Database seeder (admin user + sample todos)
- Central Package Management
- Docker Compose for standalone usage
- Architecture tests (NetArchTest — 9 tests)
- Unit tests (xUnit v3 + NSubstitute + FluentAssertions — 8 tests)
