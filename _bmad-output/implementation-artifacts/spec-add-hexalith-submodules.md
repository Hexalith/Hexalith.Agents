---
title: 'Add Hexalith Submodules'
type: 'chore'
created: '2026-06-23'
status: 'done'
route: 'one-shot'
---

# Add Hexalith Submodules

## Intent

**Problem:** The agents repository did not record the required Hexalith dependency repositories as top-level submodules.

**Approach:** Add the requested Hexalith repositories as root-level git submodules and leave any submodules declared inside those repositories uninitialized.

## Suggested Review Order

**Submodule Manifest**

- Start with the root manifest that declares each top-level dependency.
  [`.gitmodules:1`](../../.gitmodules#L1)

- Confirm every requested repository is present with its GitHub URL.
  [`.gitmodules:4`](../../.gitmodules#L4)

- Check later entries for the same root-level path and URL pattern.
  [`.gitmodules:13`](../../.gitmodules#L13)

**Gitlinks**

- Validate the recorded submodule commits with `git submodule status`.
  [`Hexalith.AI.Tools:1`](../../Hexalith.AI.Tools#L1)
