---
name: add-read-endpoint
description: Add a new read-only endpoint to the portfolio API end to end — DTO, service method, repository query, endpoint mapping, Swagger metadata, test, and docs. Use when asked to expose new portfolio data such as a detail view or a filtered collection.
---

# Add a read endpoint

Follow the target architecture, not the current shortcut of returning domain
entities.

## Preconditions

- The data already exists in the schema. If not, use `add-migration` first.
- You know which aggregate the data belongs to (see ADR-0003).

## Steps

### 1. DTO

Create `src/JDB_Portfolio_API.Application/DTO/<Name>Dto.cs`:

```csharp
namespace JDB_Portfolio_API.Application.DTO;

public sealed record ProjectDto(
    int Id,
    string ProjectName,
    string? Description,
    string? Contribution,
    IReadOnlyList<TechStackRefDto> TechStacks);
```

Rules: no domain types, no `CreatedAt`/`UpdatedAt`, no foreign keys the client
does not need.

### 2. Data access

Two legitimate options — pick one, do not mix them:

**A. Project straight to the DTO (preferred for reads).** Declare a read-model
interface in `Application/Abstractions` and implement it in `Persistence`,
projecting with `Select(...)`. No domain entity is constructed, and the mapping
profile is bypassed:

```csharp
public interface IProjectReadModel
{
    Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(bool includeTools, CancellationToken ct);
}
```

**B. Reuse the aggregate port.** Add a method to the **specific** repository
interface in `Domain/Interfaces/` (never to `IRepository<T>`) returning domain
objects, then map domain → DTO in the service.

Read-query rules either way: `.AsNoTracking()`, `.AsSplitQuery()` for sibling
includes, `CancellationToken` passed to `ToListAsync`.

Never return a `Persistence.Entities.*` type across the boundary.

### 3. Service

Add a method on the owning application service:

```csharp
public async Task<Result<IReadOnlyList<ProjectDto>>> GetProjectsAsync(
    bool includeTools, CancellationToken ct = default)
```

- Catch nothing that the database cannot throw.
- Return `Result<T>.Success(...)`; return a typed failure for expected cases.
- Log failures with context.

### 4. Endpoint

In the matching `Endpoints/Portfolio*Endpoints.cs`:

```csharp
app.MapGet("/project", async (
        PortfolioProjectService service,
        bool includeTools = false,
        CancellationToken ct = default) =>
    {
        var result = await service.GetProjectsAsync(includeTools, ct);
        return result.ToHttpResult();
    })
    .WithName("GetProjects")
    .WithTags("Portfolio")
    .Produces<IReadOnlyList<ProjectDto>>()
    .ProducesProblem(StatusCodes.Status500InternalServerError);
```

Route under the `/api/v1` group; do not hardcode the prefix.

### 5. Test

- One success test: correct status and shape.
- One failure test: repository throws → `500` `ProblemDetails`.

### 6. Docs

Update `docs/api-reference.md` (path, params, sample response, error codes).

## Verification

```bash
dotnet build src/JDB_Portfolio_API.slnx
dotnet test
curl -s "http://localhost:5254/api/v1/<route>" | jq
```

## Anti-patterns to avoid

- Returning `result.Value` without checking `IsSuccess`.
- `statusCode: 502`.
- Adding the method to `IRepository<T>` "for reuse" — generic repositories
  attract unrelated queries.
- Putting `[FromServices]` on parameters.
- Serializing a domain entity because "the DTO is identical".
