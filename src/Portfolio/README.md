# JG Portfolio API

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?logo=postgresql&logoColor=white)

An API for portfolio experience, projects, tech stacks, recommendations,
Notion page content, and public GitHub repository data.

Part of the `JGBackend` monorepo. This is a class library — the endpoints are
composed into the single `src/JGBackend` host, which owns auth, configuration,
rate limiting, and deployment. See [`docs/architecture.md`](../../docs/architecture.md).
