---
name: tdd-feature
description: Implement a feature or fix test-first in this .NET solution — write the failing test, make it pass, refactor, then verify build and tests. Use when adding behaviour to domain entities or value objects, changing a hand-written mapper, or adding a branch to a feature handler.
---

# Test-driven change

Test projects already exist under `tests/Portfolio/`. Add the test to the
matching `Features/<Slice>/` or `Shared/` folder and link it from the matching
csproj. Creating a *new* test project still needs approval (AGENTS.md §10).

## 0. First time only — create the test project

Requires approval (AGENTS.md §10):

```bash
dotnet new xunit -o tests/Portfolio/Portfolio.Domain.Tests -f net10.0
dotnet sln JGBackend.sln add tests/Portfolio/Portfolio.Domain.Tests
dotnet add tests/Portfolio/Portfolio.Domain.Tests reference \
  src/Portfolio/Portfolio.csproj
```

Then add the project to `JGBackend.sln`; `.github/workflows/ci.yaml` runs
`dotnet test` on the whole solution, so no workflow edit is needed.

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
dotnet build JGBackend.sln   # zero warnings
dotnet test                                # all green
```

## Choosing the test layer

| Behaviour | Layer | Location |
|---|---|---|
| Invariant, value equality, `Period` rules | Domain | `tests/...Domain.Tests` |
| Service branching, `Result` kind | Application | `tests/...Application.Tests` with fake repositories |
| Mapping (both directions) | Persistence | `tests/Portfolio/Features/<Slice>` or `Shared/Persistence` |
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
