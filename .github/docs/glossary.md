# Glossary

## DDD terms as used in this repository

| Term | Definition | Here |
|---|---|---|
| **Bounded context** | A boundary inside which a model and its language are consistent. | One: **Portfolio**. |
| **Ubiquitous language** | The vocabulary shared by code and domain experts. | `Experience`, `Project`, `TechStack`, `Recommendation`, `Role`, `Period`. |
| **Entity** | A concept defined by identity, not attributes. It has a lifecycle. | `Experience`, `Project`, `TechStack`, `TechStackCategory`, `Recommendation`. |
| **Value object** | A concept defined entirely by its values, immutable, with value equality. | `Role`, `Company`, `Period` (intended; currently classes). |
| **Aggregate** | A cluster of entities and value objects treated as one consistency boundary. | Intended: `Experience` + its `Project`s. Currently ambiguous — see ADR-0003. |
| **Aggregate root** | The only entity through which an aggregate is accessed; the only thing a repository returns. | `Experience`; `Project` also has a repository, which contradicts it. |
| **Invariant** | A rule that must always hold. Enforced in constructors and behaviour methods. | `Role` non-empty, `Period.FinishedAt >= StartedAt`; most others are missing. |
| **Repository (port)** | An interface in the domain describing persistence needs. | `IRepository<T>`, `IPortfolio*Repository`. |
| **Port / adapter** | Interface inside, implementation outside. | Ports in `Domain/Interfaces`, adapters in `Persistence` and `ExternalService`. |
| **Use case** | One application-level operation. | The `GetAll*Async` methods on the services. |
| **DTO** | A wire contract, decoupled from the domain model. | Only `AuthDto` today; the rest is planned. |
| **Anemic domain model** | Entities that are data bags with no behaviour; logic lives in services. | Current state of `Project`, `TechStack`, `TechStackCategory`. |
| **Anti-corruption layer** | Translation layer that keeps a foreign model out of the domain. | Intended role of `ExternalService/Notion`. |
| **Domain event** | A fact that happened in the domain, published after commit. | None yet (no writes). |
| **Composition root** | The single place where the object graph is built. | `Program.cs` + `ConfigurationSettings.cs`. |

## Project-specific terms

| Term | Meaning |
|---|---|
| **Experience** | A role held at a company for a period; the main aggregate candidate. |
| **Project** | Work delivered, optionally attached to an experience. |
| **TechStack / Skill** | A technology. `/skill` and `tech_stack` refer to the same concept. |
| **TechStackCategory** | Grouping of technologies (frontend, backend, …). |
| **Recommendation** | A testimonial with person, position, text, and date. |
| **Overview / DashboardItem** | Aggregated counts for the CV landing page. A read model, not a domain concept. |
| **NotionPage** | A card parsed from a Notion markdown page. An integration concept. |
| **Service token** | The JWT issued by `/auth/login`: a single shared key, one role claim, no user identity. |
| **`Result<T>`** | Application-layer return type carrying success/failure plus a value or an error string. |
| **`ProblemDetails`** | RFC 7807 error response shape returned by ASP.NET. |
| **Entity suffix** | `*Entity` types live in `Persistence` and represent tables, never domain concepts. |
| **Snapshot** | `PortfolioContextModelSnapshot.cs` — EF's record of the current model. Committed with every migration. |
| **Baseline migration** | `ProdBaseline` — marks the schema state at production deploy. |
| **User-secrets** | .NET mechanism for local configuration outside the repo. |

## Naming traps

| Looks like | Actually is |
|---|---|
| `Experience.Duration` | A `Period` value object, not a `TimeSpan`. `Period.Duration` is the `TimeSpan`. |
| `TechStack.Category` (string) | A denormalised category name; the table stores `tech_stack_category_id`. |
| `Project.TechStacks` | A list of **entity** instances, not references. |
| `PortfolioService` | Not a base service; it orchestrates overview and Notion, and does not inherit `BaseService`. |
| `INotionService` in `Domain` | A vendor port that does not belong in the domain. |
| `ExperienceId` | An unused `Guid` record that contradicts `Experience.Id` (`int`). |
