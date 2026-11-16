# Repository guidance for agents

The canonical agent instructions for this repository live in
[`.github/copilot-instructions.md`](.github/copilot-instructions.md) plus the
path-scoped rules under [`.github/instructions/`](.github/instructions/).
The entry file covers the tools we use, the practices we implement, the
project layout, build/run commands, and points to the per-area files; the
scoped files govern each `Generic/*` project, `docker/`, and the workflows.

This pointer exists so tools that discover `AGENTS.md` at the repo root (e.g.
OpenCode, Codex-style agents) find the same guidance GitHub Copilot uses.
