---
name: review-architecture
description: Review a change to the portfolio API against clean-architecture and DDD rules — dependency direction, domain purity, aggregate boundaries, DTO boundary, error semantics, tests, and docs. Use when reviewing a PR or before marking work complete.
---

# Architecture review

Read the diff, then work down this list. Report findings by severity, with
`file:line` evidence. Do not restate what the code does.

## 1. Dependency direction (blocker)

```bash
git diff -- '*.csproj'
```

- [ ] No infrastructure project references `Application`.
- [ ] Nothing references the Presentation project.
- [ ] Domain references nothing (no EF, ASP.NET, AutoMapper, HTTP).

## 2. Domain purity (blocker)

- [ ] No `Persistence.Entities` type in Domain, Application, or endpoints.
- [ ] No `partial`, `= null!`, `CreatedAt`/`UpdatedAt` in Domain.
- [ ] No public setters on domain entities.
- [ ] Value objects are `record`s with validating constructors.
- [ ] New behaviour is on the entity, not only in a service.

## 3. Aggregate boundaries

- [ ] One repository per aggregate root.
- [ ] Entities inside an aggregate are not reached through their own repository
      for writes.
- [ ] References to other aggregates are by id, not object reference.
- [ ] Shared reference data (`TechStack`) is not owned by a consumer.

## 4. Application layer

- [ ] Services orchestrate; they do not contain persistence queries.
- [ ] `Result<T>` carries a typed error, not a string.
- [ ] No `catch (HttpRequestException)` around database calls.
- [ ] No rethrow that discards the inner exception.
- [ ] `ILogger` used with structured parameters on failure paths.
- [ ] `CancellationToken` in the signature and passed down.

## 5. API layer

- [ ] Endpoint returns a DTO, not a `Domain.*` type.
- [ ] `IsSuccess` checked / result mapped — never `Results.Ok(result.Value)`
      unconditionally.
- [ ] Correct status code (`404`, `400`, `500`, `503`) — never `502`.
- [ ] Routes under the `/api/v1` group, not hand-spelled.
- [ ] `.WithName` / `.Produces` / `.ProducesProblem` present.
- [ ] Request validation exists for any body/parameter binding.

## 6. Persistence

- [ ] Reads use `.AsNoTracking()`; sibling includes use `.AsSplitQuery()`.
- [ ] Schema change is a **new** migration; no applied migration edited.
- [ ] Mapping is explicit for value objects; no reliance on `ToString()`.
- [ ] No unused reverse mapping left broken.

## 7. Tests

- [ ] New behaviour has a test that failed first.
- [ ] Invariants, mapping, and endpoint contracts are covered.
- [ ] `dotnet test` passes; `dotnet build` has zero warnings.

## 8. Security and docs

- [ ] No secret, token, or connection string added.
- [ ] CORS / auth changes were approved.
- [ ] `docs/api-reference.md`, `docs/database.md`, `docs/known-issues.md`
      updated where affected.
- [ ] ADR added if a structural decision changed.

## Output format

```
BLOCKER  src/.../File.cs:42  Persistence references Application.
         Fix: remove the ProjectReference (plan 1.1).

MAJOR    src/.../Endpoints/X.cs:13  Failure returns 200 with null.
         Fix: check IsSuccess and map the error kind.

MINOR    src/.../Services/Y.cs:22  Fallback is unreachable; repository never returns null.
NIT      unused using.
```

Findings without a concrete fix are opinions, not review comments.
