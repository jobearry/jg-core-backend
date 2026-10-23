# Coding standards

Normative. C# / .NET 10, nullable reference types enabled in every project.

## 1. Language and style

- File-scoped namespaces (`namespace X;`), matching the existing files.
- One public type per file; file name equals type name.
- `var` only when the type is obvious from the right-hand side.
- Prefer expression-bodied members for one-liners.
- No regions. No commented-out code — delete it, git remembers.
- Keep `using` directives minimal; remove unused ones (they are a review signal).
- Do not reformat files unrelated to the change.

## 2. Naming

| Element | Convention | Example |
|---|---|---|
| Type, method, property | PascalCase | `PortfolioExperienceService` |
| Parameter, local | camelCase | `includeProjects` |
| Private field | `_camelCase` | `_repository` |
| Interface | `I` prefix | `IPortfolioProjectRepository` |
| Async method | `Async` suffix | `GetAllProjectsAsync` |
| Value object | noun, no `Value` suffix unless it wraps a scalar | `Role`, `Period` |
| DTO | `Dto` suffix | `ExperienceDto` |
| EF entity | `Entity` suffix | `ProjectEntity` |
| Endpoint file | `<Area>Endpoints` | `PortfolioProjectEndpoints` |

Do not use `Base` in a type name unless it is genuinely `abstract` and provides
shared behaviour (see plan 2.3).

## 3. Nullability

- `Nullable` is enabled. Treat every warning as a defect.
- **Never** use `= null!` in `Domain`. It is an EF idiom that lies about the
  model. If a value is required, require it in the constructor.
- Do not return `List<T?>` — nullable *elements* in a collection are meaningless
  here. Use `List<T>`.
- Prefer `required` / constructor parameters over `init` + nullable.

## 4. Entities and value objects

- Entities: private setters, validating constructor or static factory, behaviour
  methods that preserve invariants.
- Value objects: `sealed record`, validating constructor, `ToString()` only when
  it is genuinely the wire/display form.
- No public collection setters. Expose `IReadOnlyCollection<T>` or a private
  `List<T>` with controlled mutators.
- No audit columns (`CreatedAt`, `UpdatedAt`) in Domain.
- No `partial` in Domain.

## 5. Async and cancellation

- Every I/O method is `async` and returns `Task` / `Task<T>`.
- Every I/O method accepts `CancellationToken cancellationToken = default` and
  passes it to the next layer (`ToListAsync(ct)`, `GetAsync(..., ct)`).
- Never use `.Result`, `.Wait()`, or `GetAwaiter().GetResult()`.
- Never write `async` without an `await` (CS1998).
- Use `ConfigureAwait(false)` only in library/infrastructure code, not in
  ASP.NET request paths.

## 6. Errors

- Application methods return `Result<T>`; they do not throw for expected
  failures.
- `Result<T>` carries a **typed** error (kind + message + optional code), not a
  bare string. Kinds map to status codes in one place.
- Never catch `HttpRequestException` around a database call. EF/Npgsql throws
  `DbException`/`NpgsqlException`; an `HttpRequestException` there is a bug.
- Never rethrow `new XException(e.Message)` — it discards the inner exception and
  the stack. Use `throw;` or wrap with the inner exception.
- Unhandled exceptions are translated once, by a global handler, into
  `ProblemDetails`.

## 7. Logging

- Inject `ILogger<T>`; use structured parameters, not string concatenation:
  `_logger.LogError(ex, "Failed to load {Count} projects for {ExperienceId}", n, id)`.
- Log at `Error` for failures, `Warning` for recoverable anomalies, `Information`
  for lifecycle events. Never log at `Information` inside a loop.
- **Never** log secrets, tokens, connection strings, or full request bodies
  containing credentials.

## 8. Time and numbers

- Store and transport **UTC**. Column type should be `timestamptz`
  (`timestamp with time zone`); the current schema uses `timestamp without time
  zone`, which is a known issue.
- Use `DateTimeOffset` at boundaries where possible; `DateTime` only with
  `DateTimeKind.Utc`.
- `decimal` for money, never `double`.
- Use `TimeProvider` instead of `DateTime.UtcNow` in code you want to test.

## 9. Collections and LINQ

- Materialise once: do not enumerate an `IEnumerable` repeatedly.
- Do not `.ToList()` before an `await` you could have awaited directly.
- Use `Any()` over `Count() > 0`.
- Sorting belongs in the query (`OrderBy` in SQL) when the set can be large.

## 10. Forbidden

| Never | Why |
|---|---|
| `catch (Exception) { }` | swallows failures silently |
| `throw new Exception("...")` | no type, no handler can discriminate |
| `statusCode: 502` for a DB failure | wrong semantics; use 500/503 |
| `= null!` in Domain | hides a required value |
| Public setters on domain entities | allows invalid state |
| Secrets in `appsettings*.json` | leaks into git |
| `DateTime.Now` | local time, untestable |
| Editing an applied migration | diverges deployed schemas |
| `Thread.Sleep` / `.Result` | blocks the request thread |
