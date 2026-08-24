# CWM Clean Architecture Template

**v1.0.0** | .NET 10 | C# 14 | Aspire 13 | EF Core 10 | xUnit v3

A production-ready **Clean Architecture** starter template for **.NET 10** by [Mukesh Murugan](https://codewithmukesh.com).

Built with the latest packages, zero commercial dependencies, and everything you need to start shipping features from day one.

## SIDC Dotnet 10 Template
- Install and register this template into your NET CLI Environment by double-clicking the `install-template.bat` file.
- Validate installation by running `dotnet new list` in the CMD. Find the Template Name `SIDC .NET 10 Web API Template` with Short Name `sidcapi`.
- You can now directly create a project using this template by running `dotnet new sidcapi -n ZARI`.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Architecture** | Clean Architecture (Domain, Application, Infrastructure, Api) |
| **Runtime** | .NET 10 / C# 14 |
| **API** | Minimal APIs with TypedResults |
| **CQRS** | Manual handlers — zero dependencies, zero licensing risk |
| **Validation** | FluentValidation 12 + Result pattern |
| **Errors** | ProblemDetails (RFC 9457) + global exception handler |
| **Database** | EF Core 10 + PostgreSQL |
| **Caching** | Microsoft HybridCache (L1 in-memory + L2 Redis) |
| **Auth** | ASP.NET Identity + JWT with refresh tokens |
| **API Docs** | Scalar (modern OpenAPI UI) |
| **Logging** | Serilog 10 structured logging |
| **Observability** | .NET Aspire 13 + OpenTelemetry (traces, metrics, logs) |
| **Testing** | xUnit v3 + FluentAssertions + NSubstitute + NetArchTest |
| **Solution** | `.slnx` format + Central Package Management |

## Architecture

```
┌──────────────────────────────────────────────────┐
│                    Api Layer                      │
│         Endpoints, Program.cs, Scalar            │
└──────────────────┬───────────────────────────────┘
                   │ depends on
┌──────────────────▼───────────────────────────────┐
│              Infrastructure Layer                 │
│     EF Core, Identity, JWT, HybridCache          │
└──────────────────┬───────────────────────────────┘
                   │ depends on
┌──────────────────▼───────────────────────────────┐
│              Application Layer                    │
│      CQRS Handlers, Validators, DTOs             │
└──────────────────┬───────────────────────────────┘
                   │ depends on
┌──────────────────▼───────────────────────────────┐
│                Domain Layer                       │
│      Entities, Result, Repositories (interfaces) │
└──────────────────────────────────────────────────┘
```

**Dependency rule:** Each layer only depends on the layer below it. Domain has zero external dependencies. Architecture tests enforce this at build time.

## Project Structure

```
├── src/
│   ├── ZARI.Domain/           # Entities, value objects, repository interfaces
│   ├── ZARI.Application/       # CQRS commands/queries, handlers, validators
│   ├── ZARI.Infrastructure/    # EF Core, Identity, JWT, caching, repositories
│   ├── ZARI.Api/               # Minimal API endpoints, Scalar, middleware
│   ├── ZARI.AppHost/           # Aspire orchestration (PostgreSQL + Redis)
│   └── ZARI.ServiceDefaults/   # OpenTelemetry, health checks, resilience
├── tests/
│   ├── ZARI.Architecture.Tests/ # Dependency rule enforcement (9 tests)
│   └── ZARI.Application.UnitTests/ # Handler unit tests (8 tests)
├── Directory.Build.props                         # .NET 10, C# latest, nullable enabled
├── Directory.Packages.props                      # Central Package Management
├── .editorconfig                                 # Code style rules
├── docker-compose.yml                            # PostgreSQL + Redis (standalone, no Aspire)
└── README.md
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for Aspire containers)

### Run with Aspire (recommended)

```bash
cd src/ZARI.AppHost
dotnet run
```

This starts everything:
- **API** with auto-migration and seed data
- **MySQL** 

### Migration Commands (run inside src folder)
- dotnet ef migrations add InitialCreate --project Infrastructure/ZARI.Infrastructure.csproj --startup-project Api/ZARI.Api.csproj --output-dir Persistence/Migrations --context AppDbContext
- dotnet ef database update --project Infrastructure/ZARI.Infrastructure.csproj --startup-project Api/ZARI.Api.csproj --context AppDbContext

### Explore the API

Navigate to `https://localhost:7200/scalar/v1` for the interactive Scalar API docs.

**Default admin credentials** (seeded automatically):
- Email: `admin@cwm.dev`
- Password: `Admin@123`

### Run Tests

```bash
cd src
dotnet test ZARI.slnx
```

## Sample: Todos Feature

The template includes a complete **Todos** CRUD feature as a reference implementation:

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/todos` | GET | Yes | Get all todos (paginated) |
| `/api/todos/{id}` | GET | Yes | Get a todo by ID |
| `/api/todos` | POST | Yes | Create a new todo |
| `/api/todos/{id}` | PUT | Yes | Update a todo |
| `/api/todos/{id}/complete` | PATCH | Yes | Mark as completed |
| `/api/todos/{id}` | DELETE | Yes | Delete a todo |

### Adding a New Feature

Follow the Todos pattern:

1. **Domain** — Add your entity in `Domain/Entities/`
2. **Application** — Create a feature folder in `Application/Features/YourFeature/` with command/query records, handlers, and validators
3. **Infrastructure** — Add EF Core configuration in `Infrastructure/Persistence/Configurations/` and repository in `Infrastructure/Persistence/Repositories/`
4. **Api** — Add endpoints in `Api/Endpoints/` and register in `Program.cs`

## Key Design Decisions

| Decision | Why |
|----------|-----|
| **Manual CQRS** over MediatR | Zero licensing risk. MediatR is commercial since v13. You learn the pattern, not a library. |
| **Scalar** over Swagger UI | Modern, faster, better UX. Swagger UI is legacy. |
| **HybridCache** over IMemoryCache | Built-in stampede protection, L1+L2 cache, automatic serialization. |
| **Result pattern** over exceptions | Explicit error handling, no hidden control flow, better API contracts. |
| **Manual handler registration** over Scrutor | Zero dependencies for DI scanning. Assembly reflection is 40 lines of code. |
| **.slnx** over .sln | XML-based, merge-friendly, smaller, future of .NET solutions. |
# This template retained Swagger UI for more efficient API Implementation to client-side projects using Swagger Editor

## About

Built by [Mukesh Murugan](https://codewithmukesh.com) — .NET content creator helping developers build production-ready applications.

- [Newsletter](https://codewithmukesh.com/newsletter) — Weekly .NET insights, architecture deep-dives, and exclusive content
- [LinkedIn](https://linkedin.com/in/iaboromukesh) — 40K+ followers
- [YouTube](https://youtube.com/@codewithmukesh) — Video tutorials and walkthroughs
- [GitHub](https://github.com/iammukeshm) — Open source projects

## License

MIT License. Use it, modify it, ship it. Attribution appreciated but not required.
