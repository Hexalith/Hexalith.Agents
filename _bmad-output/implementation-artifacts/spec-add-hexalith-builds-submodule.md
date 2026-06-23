---
title: 'Add Hexalith Builds Submodule'
type: 'chore'
created: '2026-06-23'
status: 'done'
route: 'one-shot'
---

# Add Hexalith Builds Submodule

## Intent

**Problem:** The agents repository did not record `Hexalith.Builds` as a top-level submodule.

**Approach:** Add `Hexalith.Builds` as a root-level git submodule and verify it does not initialize nested submodules.

## Suggested Review Order

- Confirm the new root submodule entry uses the expected path and GitHub URL.
  [`.gitmodules:25`](../../.gitmodules#L25)

- Validate the recorded gitlink with `git submodule status`.
  [`Hexalith.Builds:1`](../../Hexalith.Builds#L1)
