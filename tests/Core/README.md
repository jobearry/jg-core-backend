# Core tests

Tests for `src/Core`.

The auth, CORS, Swagger, and exception-handler tests still live in
`tests/Portfolio/Portfolio.Api.Tests` because Portfolio owns the current host.
Move them here when the single-host merge (phase 3) makes `src/JGBackend` the
only host and `src/Core` the only auth implementation.
