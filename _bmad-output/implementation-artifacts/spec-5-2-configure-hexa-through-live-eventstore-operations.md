---
title: '5.2 Lifecycle Configuration Version Remediation'
type: 'feature'
created: '2026-09-13'
status: 'done'
route: 'oneshot'
review_loop_iteration: 1
context:
  - '_bmad-output/implementation-artifacts/epic-5-context.md'
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-state-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The historical Story 5.2 delivered live administration and setup projection, but lifecycle events do not advance `ConfigurationVersion`, so read-your-writes polling can confirm pre-transition lifecycle truth.

**Approach:** Evolve lifecycle events additively and propagate their versions through replay, projection, UI polling, and later interaction snapshots.

**Decision (2026-09-13):** Deliver only the lifecycle-version remediation identified by sprint tracking. Defer immutable Party provisioning/link retirement and command-specific projection correlation; keep canonical Story 5.2 `in-progress` after this slice.

</frozen-after-approval>

## Implementation Notes

- Preserved the one-argument lifecycle-event constructors and added an init-only `ConfigurationVersion`; zero is the legacy sentinel and never causes a synthetic replay increment.
- Updated aggregate emission, state replay, setup projection evidence, UI N+1 catch-up, and interaction-snapshot passthrough evidence. Added the named `AgentLifecycleConfigurationVersionTests` focused suite.
- Aligned the root bUnit pin with the referenced FrontComposer test stack at 2.10.3 so source-mode UI verification remains buildable.
- Verification passed: warning-as-error solution build, 100 directly affected tests, and the complete `eng/verify-story-5.2.ps1` gate including all five owning regression projects.
- Treated this scoped remediation as non-epic work for sprint synchronization: the canonical 5.2 tracker remains `in-progress`, and the full Party/correlation gaps stay deferred.

## Review Triage Log

Blind Hunter floor: `(41,324-byte diff + 6,716 untracked bytes) / 1,000 = 48.04 kB`; `N = min(floor(sqrt(48.04) + 1), 10) = 7`. Thirteen findings were triaged:

1. **Deferred:** submitted-but-rejected/no-op commands need a terminal-outcome projection contract; captured with the command/outcome/projection correlation item in `deferred-work.md`.
2. **Deferred:** safe re-submission after polling timeout needs the same outcome-aware correlation/rebase contract; captured in that item.
3. **Deferred:** a concurrent command can still satisfy an N+1 threshold; exact command correlation remains explicitly out of this slice.
4. **Rejected:** reducers consistently trust persisted event payloads, while aggregate emission proves exact `current + 1` and projection delivery separately enforces stream sequence continuity. Adding lifecycle-only reducer sequencing would be inconsistent with all other configuration events.
5. **Patched:** negative lifecycle versions now throw before lifecycle or version mutation, with focused coverage; zero alone remains the legacy sentinel.
6. **Patched:** UI fixtures now keep inner/outer configuration versions aligned and lifecycle tests return a real version-4 transitioned projection before confirmation.
7. **Rejected:** the frozen intent claims only bumped-version passthrough into an existing interaction snapshot; it does not claim the deliberately deferred reader binding is live.
8. **Patched:** projection coverage now includes activation and proves the payload configuration version is used when it differs from stream sequence.
9. **Rejected for historical rewrite; current state patched:** the append-only DW-4 record remains historical, while the current sprint comment records the completed lifecycle half and the latest deferred entry isolates the remaining outcome/correlation work.
10. **Patched:** the sprint tracker retains `in-progress` while accurately naming the completed lifecycle slice and remaining canonical scope.
11. **Patched:** the bUnit 2.10.3 root pin is synchronized into the Epic 5 stack baseline and Architecture Spine, and stale Story 5.6 debt wording is removed.
12. **Patched:** disable documentation now distinguishes preserved configuration content/history from the advancing configuration version.
13. **Rejected:** tracked repository C# files currently use LF consistently; normalizing only touched files to CRLF would create isolated mechanical churn rather than improve this bounded change.
