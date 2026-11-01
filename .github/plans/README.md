# Plans

Numbered plans for work that has not been implemented. Completed implementation
details belong in `.github/docs/`; lasting design decisions belong in ADRs.

Active plans:

- [002 — Optional authenticated GitHub visibility](002-github-visibility-auth.md)

## Conventions

- File name: `NNN-kebab-case-title.md`, zero-padded, monotonically increasing.
- Every plan starts with **Status**, **Scope**, **Goal**, and a **Decisions
  needed** table if any choice is still open.
- Every item has **What / Why / How to verify / Risk / Effort**. "Why" is not
  optional — a plan item nobody can justify is a preference, not a task.
- Checkboxes are the source of truth for progress. Tick them in the same PR that
  implements them, and add the PR number next to the item.
- When a plan is finished, move its resulting system description into the
  relevant docs, record lasting decisions in ADRs, and remove the completed
  plan. Do not use plans as implementation history.
- If a plan is superseded, mark `Status: Superseded by NNN` instead of editing
  the old items.

## Writing a new plan

1. State the problem in one paragraph. If you cannot, it is not ready.
2. Separate **objective defects** (bugs, rule violations) from **design
   choices** (judgement calls). Do not mix them in one list.
3. For each item, write how the reader can verify the claim themselves —
   a command, a query, or an observation. This is what makes a plan reviewable.
4. List the decisions that block implementation, with options and a
   recommendation.
5. Add an appendix with a cheapest-first verification order.
