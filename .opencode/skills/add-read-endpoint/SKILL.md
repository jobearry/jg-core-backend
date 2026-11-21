---
name: add-read-endpoint
description: Add a new read-only endpoint to the portfolio API end to end — DTO, handler, repository query, endpoint mapping, Swagger metadata, test, and docs. Use when asked to expose new portfolio data such as a detail view or a filtered collection.
---

# Add a read endpoint

Follow the vertical-slice shape, not the old layered project layout.

## Preconditions

- The data already exists in the schema. If not, use `add-migration` first.
- You know which slice owns the data (see ADR-0003 for aggregate boundaries).

## Steps

### 1. DTO

Create `src/Portfolio/Features/<Slice>/<Name>Dto.cs`:

```csharp
namespace Portfolio.Features.Project;

public sealed record ProjectDto(
    int Id,
    string ProjectName,
    string? Description,
    string? Contribution,
    IReadOnlyList<TechStackRefDto> TechStacks);
```

Rules: no domain or EF types, no `CreatedAt`/`UpdatedAt`, no foreign keys the
client does not need. Keep an existing DTO type name if it is already published
in Swagger.

### 2. Data access

Add the query to the owning slice's repository port and EF implementation (for
example `IProjectRepository` / `EfProjectRepository`). Prefer projecting straight
to a DTO when no domain behaviour is needed; otherwise return domain entities
and map them in the handler.

Read-query rules either way: `.AsNoTracking()`, `.AsSplitQuery()` for sibling
includes, `CancellationToken` passed to `ToListAsync`. Never return a
`Persistence.Entities.*` type upward.

### 3. Handler

Add or extend a handler in the slice:

```csharp
public async Task<Result<IReadOnlyList<ProjectDto>>> Handle(
    SomeQuery query,
    CancellationToken ct = default)
```

- Catch nothing the database cannot throw.
- Return `Result<T>.Success(...)`; return a typed failure for expected cases.
- Log failures with structured context.

### 4. Endpoint

In `src/Portfolio/Features/<Slice>/<Slice>Endpoints.cs`:

```csharp
app.MapGet("/project", async (
        GetProjectHandler handler,
        bool includeTools = false,
        CancellationToken ct = default) =>
    {
        var result = await handler.Handle(new ProjectQuery(includeTools), ct);
        return result.ToHttpResult();
    })
    .WithName("GetProjects")
    .WithTags("Portfolio")
    .Produces<IReadOnlyList<ProjectDto>>()
    .ProducesProblem(StatusCodes.Status500InternalServerError);
```

Route under the `/api/v1` group; do not hardcode the prefix. Register the
handler/repository in `Features/DependencyInjection.cs` if the slice is new.

### 5. Test

- One success test: correct status and shape.
- One failure test: repository throws → `503` ProblemDetails.

Tests live in `tests/Portfolio/Features/<Slice>/` and are linked from the
matching test csproj.

### 6. Docs

Update `docs/api-reference.md` (path, params, sample response, error codes).

## Verification

```bash
dotnet build JGBackend.sln
dotnet test JGBackend.sln
curl -s "http://localhost:5254/api/v1/<route>" | jq
```

## Anti-patterns to avoid

- Returning `result.Value` without checking `IsSuccess`.
- Choosing a status code in the endpoint. `ApiResult.cs` is the only mapper.
- `statusCode: 502` for a database failure: DB is `503`, upstream is `502`, bug is `500`.
- Adding a route prefix by hand — the pattern is relative to the `api/v1` group in `Program.cs`.
- Depending on another slice's internals.
- `[FromServices]` on parameters.
- Serializing a domain entity because "the DTO is identical".
