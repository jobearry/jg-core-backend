---
name: tdd-feature
description: Implement a feature or fix test-first in this .NET solution — write the failing test, make it pass, refactor, then verify build and tests. Use when adding behaviour to domain entities or value objects, changing the AutoMapper profile, or adding a branch to an application service.
---

# Test-driven change

Test projects already exist: `tests/JDB_Portfolio_API.Domain.Tests` and
`tests/JDB_Portfolio_API.Application.Tests`. Add to the one that matches the
layer you are changing. Creating a *new* test project still needs approval
(AGENTS.md §10).

## 0. First time only — create the test project

Requires approval (AGENTS.md §10):

```bash
dotnet new xunit -o tests/JDB_Portfolio_API.Domain.Tests -f net10.0
dotnet sln src/JDB_Portfolio_API.slnx add tests/JDB_Portfolio_API.Domain.Tests
dotnet add tests/JDB_Portfolio_API.Domain.Tests reference \
  src/JDB_Portfolio_API.Domain/JDB_Portfolio_API.Domain.csproj
```

Then add `dotnet test` to `.github/workflows/main_jdb-portfolio-api.yml`.

## 1. Red — state the behaviour as a test

Write the smallest test that describes the desired behaviour. It must fail for
the **right reason** — run it and read the failure message.

```csharp
[Fact]
public void Constructor_WithEmptyName_Throws()
{
    // Arrange / Act / Assert
    Assert.Throws<ArgumentException>(() => new Project(1, ""));
}
```

If it passes immediately, either the behaviour already exists (stop — you are
done) or the test does not actually assert anything.

## 2. Green — minimum implementation

Change production code only enough to pass. No speculative parameters, no
"while I'm here" refactors.

## 3. Refactor

With tests green, improve names and structure. Re-run after each step.

## 4. Verify

```bash
dotnet build src/JDB_Portfolio_API.slnx   # zero warnings
dotnet test                                # all green
```

## Choosing the test layer

| Behaviour | Layer | Location |
|---|---|---|
| Invariant, value equality, `Period` rules | Domain | `tests/...Domain.Tests` |
| Service branching, `Result` kind | Application | `tests/...Application.Tests` with fake repositories |
| Mapping (both directions) | Persistence | `tests/...Application.Tests` or `Persistence.Tests` |
| Status code, response shape | API | `tests/...Api.Tests` with `WebApplicationFactory` |

## Rules

- One behaviour per test; name it `Method_Scenario_Expectation`.
- Assert outcomes, not interaction counts.
- No shared mutable fixtures; build data explicitly per test.
- For mapping, add a **runtime** map test — `AssertConfigurationIsValid()` misses
  `string → int`.
- A bug fix ships with the failing test that reproduces it.

## Stop conditions

- The test requires a database → use Testcontainers, not the in-memory provider.
- The test requires `DateTime.UtcNow` to be controlled → inject `TimeProvider`
  instead of mocking a static.
- Adding a package to make the test possible → ask first.
