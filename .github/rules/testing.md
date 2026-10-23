# Testing rules

## 1. Why test-first here, specifically

The repository currently has **zero tests**. That matters more than usual for two
reasons:

1. **Every refactor in plan 001 is unverifiable without them.** Converting
   `Role` from a class to a record, adding invariants to `Project`, changing the
   mapping profile — none of these can be proven safe by reading the diff.
2. **AI-generated code is plausible-looking.** The failure mode is not a syntax
   error; it is code that compiles, reads well, and is subtly wrong (the
   `FinishedAt` mapping is exactly that). A test is the only cheap oracle.

So: **TDD is the default for domain logic, mapping, and endpoint contracts.** It
is *not* required for trivial pass-through code — writing a test that asserts
`_repo.GetAllAsync()` was called adds noise, not safety.

| Change | Test-first? |
|---|---|
| Value object / entity invariant | **Yes** |
| AutoMapper profile (either direction) | **Yes** |
| Application service with branching | **Yes** |
| Endpoint status codes / response shape | **Yes** (one integration test) |
| Repository query behaviour | Yes, once there is a test DB |
| Pure delegation | No |
| Configuration / DI wiring | Smoke test only |

## 2. Test projects

```
tests/
  JDB_Portfolio_API.Domain.Tests/          # no dependencies beyond Domain + xUnit
  JDB_Portfolio_API.Application.Tests/     # fakes for repositories
  JDB_Portfolio_API.Api.Tests/             # WebApplicationFactory, one per endpoint
```

Adding xUnit requires approval (see AGENTS.md §10). Suggested baseline:
`xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`,
`Microsoft.AspNetCore.Mvc.Testing` (API project only). Do **not** add a mocking
library until a hand-written fake becomes painful.

## 3. The loop

1. **Red** — write the smallest failing test that states the behaviour.
   Run it. Confirm it fails *for the right reason*.
2. **Green** — write the minimum code to pass. No speculative generality.
3. **Refactor** — improve names and structure with the test green.
4. Commit. Repeat.

Example for plan 3.3 (`Project` invariants):

```csharp
[Fact]
public void Constructor_WithEmptyName_Throws()
{
    Assert.Throws<ArgumentException>(() => new Project(1, ""));
}
```

That test fails today (`new Project(1, "")` succeeds). It is the specification
for the change, not an afterthought.

## 4. Naming

```
MethodOrProperty_Scenario_ExpectedOutcome
```

- `Period_WithFinishedBeforeStarted_Throws`
- `GetAllProjectsAsync_WhenIncludeToolsIsFalse_DoesNotLoadTechStacks`
- `GetSkill_WhenRepositoryFails_ReturnsProblemDetails`

One behaviour per test. No `Test1`. No `Assert.True(true)`.

## 5. Structure

Arrange / Act / Assert, visually separated by blank lines. Prefer a small
`Builder` or factory over 20 lines of setup:

```csharp
var experience = AnExperience().WithRole("Developer").Build();
```

Test data must be **explicit** — no shared mutable fixtures, no ordering
dependencies.

## 6. Fakes over mocks

- Write a hand-rolled fake in `Application.Tests/Fakes/` implementing the
  repository port. It makes intent obvious and survives refactors.
- Assert on **outcomes** (returned value, thrown error), not on call counts,
  unless the call itself is the contract (e.g. "must not hit the DB twice").

## 7. Mapping tests are mandatory

The mapping profile is the highest-risk file in the solution (see plan 1.2, 1.3,
1.5). Every direction that is used must have a test:

```csharp
[Fact]
public void Map_ExperienceEntity_To_Experience_PreservesRoleAndCompany() { ... }

[Fact]
public void Map_TechStack_To_TechStackEntity_DoesNotThrow() { ... }
```

Also add a `MapperConfiguration` assertion test so unmapped destination members
fail the build:

```csharp
var config = new MapperConfiguration(c => c.AddProfile<PortfolioMappingProfile>());
config.AssertConfigurationIsValid();   // catches unmapped members, not bad conversions
```

Note: `AssertConfigurationIsValid()` does **not** catch `string → int`. Only a
runtime map does.

## 8. Integration tests

- Use `WebApplicationFactory<Program>`; requires `public partial class Program { }`
  in the API project.
- Replace the DbContext with a test database (Testcontainers PostgreSQL) — not
  the EF in-memory provider, which does not behave like PostgreSQL (no
  constraints, different SQL).
- One test per endpoint is enough to lock the contract: success shape, and the
  failure status code.

## 9. What not to test

- Framework behaviour (routing, JSON serialization).
- Private methods.
- Getters and setters with no logic.
- The exact text of an error message — assert the **kind**/status code instead.

## 10. Gates

- `dotnet test` must pass before merge.
- A bug fix without a regression test is not complete.
- Coverage percentage is not a target. "Every invariant has a test" is the
  target.
