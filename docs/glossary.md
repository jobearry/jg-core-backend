# Glossary

## DDD terms as used in this repository

| Term | Definition | Here |
|---|---|---|
| **Bounded context** | A boundary inside which a model and its language are consistent. | One: **Portfolio**. |
| **Ubiquitous language** | The vocabulary shared by code and domain experts. | `Experience`, `Project`, `TechStack`, `Recommendation`, `Role`, `Period`. |
| **Entity** | A concept defined by identity, not attributes. It has a lifecycle. | `Experience`, `Project`, `TechStack`, `TechStackCategory`, `Recommendation`. |
| **Value object** | A concept defined entirely by its values, immutable, with value equality. | `Role`, `Company`, and `Period` are validating records. |
| **Aggregate** | A cluster of entities and value objects treated as one consistency boundary. | `Project` is its own aggregate; `Experience.Projects` is a read-only projection. See ADR-0003. |
| **Aggregate root** | The only entity through which an aggregate is accessed; the only thing a repository returns. | `Project` is the root of its aggregate. `Experience` is a read-only aggregate-shaped projection. |
| **Invariant** | A rule that must always hold. Enforced in constructors and behaviour methods. | Domain entities and value objects validate their required values and relationships. |
| **Repository (port)** | An interface in the domain describing persistence needs. | `IRepository<T>`, `IPortfolio*Repository`. |
| **Port / adapter** | Interface inside, implementation outside. | Ports live with the owning slice or in `Shared/Common`; EF adapters live in the slice, vendor adapters in the Notion/GitHub slices. |
| **Use case** | One application-level operation. | The `Handle(...)` methods on feature handlers. |
| **DTO** | A wire contract, decoupled from the domain model. | Endpoints use DTOs in `Features/<Slice>`; `AuthDto` is the login request contract. |
| **Anemic domain model** | Entities that are data bags with no behaviour; logic lives in services. | Not the intended model here: domain entities validate state and expose controlled behavior. |
| **Anti-corruption layer** | Translation layer that keeps a foreign model out of the domain. | `NotionPageContentReader` in the Notion slice translates the vendor adapter into that slice's `IPageContentReader` port (ADR-0005). |
| **Domain event** | A fact that happened in the domain, published after commit. | None yet (no writes). |
| **Composition root** | The single place where the object graph is built. | `src/JGBackend/Program.cs`, with feature wiring in `Features/DependencyInjection.cs`. |

## Project-specific terms

| Term | Meaning |
|---|---|
| **Experience** | A role held at a company for a period; its projects are exposed as a read-only projection. |
| **Project** | Work delivered, optionally attached to an experience; it is an aggregate root. |
| **TechStack / Skill** | A technology. `/skill` and `tech_stack` refer to the same concept. |
| **TechStackCategory** | Grouping of technologies (frontend, backend, …). |
| **Recommendation** | A testimonial with person, position, text, and date. |
| **Overview / DashboardItem** | Aggregated counts for the CV landing page. A read model, not a domain concept. |
| **NotionPage** | The vendor's page shape, held in the Notion slice; the handler sees `PageContent` (ADR-0005). |
| **Service token** | The JWT issued by `/auth/login`: a single shared key, one role claim, no user identity. |
| **`Result<T>`** | Application-layer return type carrying success/failure plus a value or typed error. Errors carry an `ErrorKind` and machine-readable code where applicable. |
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
| `Project.TechStacks` | A list of `TechStackRef` **value objects**, not entity instances (ADR-0003). |
| `PortfolioService` | Not a base service; it orchestrates overview and page content, and does not inherit `PortfolioServiceBase`. |
