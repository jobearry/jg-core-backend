# Core tests

Tests for `src/Core` live alongside the host-contract tests in
`tests/Portfolio/Portfolio.Api.Tests` (JWT service, auth endpoints, CORS,
Swagger, and the exception handler), because that project owns the
`WebApplicationFactory` harness that boots the one host. Pure unit tests can
move here when a dedicated host/Core test project is added.
