# Terminal command rules

The decision owner supervises shell access to this repository. An agent may run
commands, but it must not run commands that change state the owner has not
explicitly agreed to.

## 1. The default

**Show the command, then run it.** Before a command that is not on the
"routine" list in §2, state exactly what it will do and what it will touch.
The owner can veto.

Prefer the narrowest command that does the job:

- Edit files with the editor tools, not `sed`/`cat >`/heredocs. A file edit
  shows up in the diff; a shell rewrite does not.
- Read with the read tool, not `cat`.
- Reserve the shell for what only a shell can do: `dotnet build`, `dotnet test`,
  `git`, `psql`, `docker`, process control.

## 2. Routine — run without asking

Read-only, local, and reversible. These do not need a preamble:

| Command | Why it is safe |
|---|---|
| `dotnet build`, `dotnet test`, `dotnet restore` | local, no side effects outside the repo |
| `git status`, `git diff`, `git log`, `git show`, `git blame` | read-only |
| `git add`, `git stash` | local and reversible |
| `grep`, `rg`, `find`, `ls`, `sed -n` (read-only) | read-only |
| `dotnet ef migrations list` | reads migration metadata only |
| `dotnet ef migrations add` | creates a new file; **does not** touch the database |
| `dotnet format --verify-no-changes` | reports only |

## 3. Ask first — state changes

Each of these needs the owner's explicit go-ahead. State the command, what it
changes, and what it cannot be undone by.

| Command | Why it needs approval |
|---|---|
| `dotnet ef database update` | **applies a migration to a database** |
| `psql` with `INSERT`/`UPDATE`/`DELETE`/`DROP`/`TRUNCATE`/`ALTER` | writes to a database |
| `git commit`, `git push`, `git merge`, `git rebase`, `git reset --hard` | changes history or the remote |
| `git clean`, `rm`, `git checkout --` | **deletes work** |
| `git branch -D`, `git worktree remove` | destroys a branch or worktree |
| `git push --force` | rewrites the remote |
| `docker compose down -v`, `docker system prune` | destroys volumes and images |
| `dotnet tool install --global` | installs outside the project |
| `curl`/`wget` that write files, or hit a non-localhost host | external effect |
| starting a long-running or background process | occupies a port and the machine |

**Schema changes are doubly gated.** AGENTS.md §10 already requires approval to
change the database schema. So: approval to *write the migration* is not
approval to *apply* it. Ask separately for each.

## 4. Secrets

- Never print a secret value. `dotnet user-secrets list` prints secrets in
  cleartext — read it, then redact the output before showing it.
- Never pass a secret on a command line; it lands in the shell history and in
  the process list. Use environment variables that are redacted in any output.
- Never `cat` a file whose contents are credentials.

## 5. Phases

When a task has phases, **stop and ask before starting the next one.** Report
what the finished phase did, what was verified, and what the next phase would
change. The owner decides whether to continue.

A phase boundary is also the right place to surface a question that would
otherwise be discovered mid-implementation — a missing decision, a
contradiction between two documents, or a choice that changes the shape of the
work.

## 6. Verification is not a phase

Running the build, the tests, or a read-only query to check the work is part
of the phase that produced it, not a new phase. Do it, then report the result.

But anything that **persists** — applying a migration, committing, pushing —
is a phase boundary of its own.
