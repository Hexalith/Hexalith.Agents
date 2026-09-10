# Brownfield Drift Review — ARCHITECTURE-SPINE.md (v9, ad hoc)

**Lens:** Does the spine (and its tracking artifacts — `sprint-status.yaml`, `external-dependency-register.md`,
`launch-readiness-register.md`) ratify, rather than contradict, the actual current state of the codebase?
Independent re-derivation only; no prior-round finding trusted without a fresh check.

**Reviewer:** independent brownfield-drift pass against the round-5 Update ("round-4 Update pass" text,
committed as `c4121b0`) plus a further round of uncommitted edits layered on top of it.

**Trigger:** the task framed this as verifying a narrow, single-purpose Update — a `sprint-status.yaml` Story
5.3 annotation closing round-5's Medium M1, plus a `global.json` SDK bump. The actual working tree contains
that, but also a great deal more (see Finding 1).

---

## Finding 1 (scope) — Working tree is not limited to the spine + global.json + sprint-status.yaml

**Severity: Low (process/hygiene observation, not a spine defect).**

`git status` / `git diff --stat` show 15 modified tracked files and ~23 untracked files, not 3:

- As expected: `ARCHITECTURE-SPINE.md` (62 lines), `.memlog.md` (architecture, 34 lines), `global.json` (1
  line), `sprint-status.yaml` (1 line) — all fully accounted for in the architecture `.memlog.md`'s own
  round-5-validation entries (see Finding 2/3 below).
- Also present, **not** described by the task's framing: `prd.md` (+435/-), `addendum.md`, four PRD review
  files (`review-adversarial-general.md`, `review-consistency.md`, `review-implementation-drift.md`,
  `review-rubric.md`), `prds/.../.memlog.md`, plus ~15 untracked PRD "validate-3"/"polish"/"reconcile" files.
  These belong to a **separate, self-contained PRD "third update" run**, fully documented in
  `prds/prd-agents-2026-06-23/.memlog.md` (its own reviewer gate, its own `update-report-2026-09-09-3.md`,
  its own decision log) — not silent, not unaccounted-for, just accounted for in a different memlog than the
  one this task named.
- Also present: `external-dependency-register.md` (11 lines) and two `spec-*.md` implementation-artifact
  files, all explained by
  `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`'s
  "Post-Completion Product Resolution" addendum (2026-09-10: Product ruled Branch B on the Story 5.3 /
  `EXT-PROVIDER-1` historical-consumption question — see Finding 2) and its already-checked-off Code Map.

**No `src/` or `test/` file changed** (absent from `git status`), and no file changed that isn't traceable to
one of these two documented, self-describing update passes. Nothing here is undocumented drift. **Fix (optional,
non-blocking):** tighten the task-framing/handoff next time to say "the tree also carries an independent,
already-documented PRD update pass" so a reviewer doesn't have to re-derive that boundary from scratch.

---

## Finding 2 — Sprint-status Story 5.3 annotation: both factual claims independently substantiated

**Severity: informational (verification of a closed Medium, not a new finding).**

`sprint-status.yaml:89` now reads:

> `5-3-govern-provider-models-and-pricing-through-live-operations: backlog  # Noted 2026-09-09 (round-5 spine
> validation, brownfield M1): a completed, fully-tested prior implementation (2569/2569 tests passing) exists
> underneath this backlog status; not promoted to done/in-progress here because AD-2's platform/tenant
> ProviderCatalog split (NC-5.3-PLATFORM-CATALOG-SCOPE) is still unreconciled in code.`

Checked both clauses independently:

1. **"2569/2569 tests passing"** — confirmed in
   `_bmad-output/implementation-artifacts/spec-5-3-govern-provider-models-and-pricing-through-live-operations.md`
   (`status: done`), Dev Agent Record § "Latest Release Test Evidence" (run 2026-09-08T15:32:15Z): a five-row
   per-project table (`Hexalith.Agents.Client.Tests` 6/6, `Hexalith.Agents.Contracts.Tests` 327/327,
   `Hexalith.Agents.Server.Tests` 458/458, `Hexalith.Agents.Tests` 739/739, `Hexalith.Agents.UI.Tests`
   1039/1039) summing to **2569/2569, Result: PASS**. `git diff a191d24..HEAD -- src/ test/` is empty, so
   this evidence still describes the code on disk today.
2. **"AD-2's platform/tenant ProviderCatalog split ... still unreconciled in code"** — confirmed:
   `src/Hexalith.Agents/ProviderCatalog/ProviderCatalogAggregate.cs:19` still self-documents as "the
   **tenant-scoped** governed provider/model catalog"; no `TenantProviderEnablement` type or folder exists
   anywhere under `src/`. `external-dependency-register.md`'s `NC-5.3-PLATFORM-CATALOG-SCOPE` record (status
   `Open`, owner Agents Runtime Maintainer) states the identical finding and gives closure evidence
   conditions. Matches.

**Bonus finding, already resolved on disk, not a defect:** the annotation's premise ("Story 5.3 is `backlog`
because of an *unresolved Product approval condition* on whether it consumed `EXT-PROVIDER-1`") was itself
just settled on 2026-09-10, one day after the annotation was written — Product ruled **Branch B** (no
`EXT-PROVIDER-1` seam was executed; substituted test ports and the fail-closed deferred provider do not
count as live external consumption), recorded in both `spec-synchronize-second-...md`'s Post-Completion
Resolution and `external-dependency-register.md`'s "Story 5.3 / EXT-PROVIDER-1 Historical Consumption —
Resolved Branch B" section. This does **not** change why Story 5.3 stays `backlog` — that's still
`NC-5.3-PLATFORM-CATALOG-SCOPE`, per the register's own text ("does not commit `EXT-PROVIDER-1`... and cannot
be cited as current architecture or release conformance") — so the `sprint-status.yaml` annotation's
reasoning remains accurate, just one day out of date on a fact that turned out not to matter to its
conclusion. Not a defect; flagging only for completeness.

---

## Finding 3 — global.json SDK bump matches the spine's own Medium-tier fix, and closes ARCH-A-4 as claimed

**Severity: informational (verification).**

`global.json`: `"version": "10.0.301"` → `"version": "10.0.400"`. The architecture `.memlog.md`'s own
round-5-validation entries state this explicitly: *"ARCH-A-4's SDK escalation is resolved, not just
tracked: global.json bumped from 10.0.301 (capped 10.0.303, CVE-2026-69522-exposed) to 10.0.400, matching
every sibling repo's pin."* This directly answers the prior validation report's own open item ("the
'immediate' SDK remediation ARCH-A-4 calls for has not actually landed in global.json"). Confirmed: the file
change and the memlog narrative agree, and no other `global.json` field changed (`rollForward:
latestPatch` unchanged).

---

## Finding 4 — Previously-closed brownfield items: spot-checked, none regressed

**Severity: none (confirmation only).**

Because `git diff a191d24..HEAD -- src/ test/` is still empty (re-verified this round), every code-based v8
finding is guaranteed unchanged by construction — but per the task's instruction, two were independently
re-derived rather than trusted:

- **Story 5.2 `ConfigurationVersion` gap (H2 in v8):** `AgentAggregate.cs:267/298` still emit
  `new AgentActivated(agentId)` / `new AgentDisabled(agentId)` with no `ConfigurationVersion` argument or
  bump; `grep -rn "ConfigurationVersion" test/` finds no `AgentLifecycleConfigurationVersionTests` file
  anywhere. `sprint-status.yaml:88`'s `in-progress` annotation citing exactly this gap (DW-4) is still
  accurate — not regressed to a false `done`.
- **AD-16 AppHost-free claim (L1 in v8):** re-ran `ForbiddenHostingOwnershipTests` fresh —
  **5/5 tests pass**, including the token-scan test forbidding `Aspire.Hosting`, `DistributedApplication.
  CreateBuilder`, `Aspire.AppHost.Sdk`, etc. `find src -iname "*apphost*"` and `grep -rl Aspire src/*/*.csproj`
  both return nothing. Claim holds.
- **LR-AUDIT-PROTECTION-DELETION register symmetry (M2 in v8):** `launch-readiness-register.md:108` still
  reads "...evidence that a `LegalHold` **or a `DeletionRequest`** either propagates to (or has a confirmed
  accepted-risk waiver for)..." — the file is not even in this round's diff (byte-identical since the round-4
  Update pass that fixed it), so the fix is stable by construction, confirmed by direct read.

No other H1/H3/H5/H7 items from v8 were re-derived line-by-line this round (redundant given the empty
`src`/`test` diff and the task's explicit "don't re-litigate" instruction), but nothing in the spine text,
`sprint-status.yaml`, or the dependency register changed in a way that would newly contradict any of them.

---

## Finding 5 — Structural Seed section: unchanged gap, same shape as already-tracked H1/H4, not new drift

**Severity: none — already-tracked, re-verified unchanged (folds into v8's H1/H4).**

`find src test -maxdepth 4 -type d` (excluding `bin`/`obj`) shows `src/Hexalith.Agents/` contains only three
subfolders — `Agent/`, `AgentInteraction/`, `ProviderCatalog/` — against the spine's `## Structural Seed`
text tree (line ~536), which lists fourteen: the three that exist plus `TenantProviderEnablement/`,
`BudgetLedger/`, `TenantGovernancePolicy/`, `ContentSafetyPolicy/`, `LaunchReadinessGate/`,
`ConversationAgentState/`, `AuditInspection/`, `SecurityEventLog/`, `LegalHold/`, `AuditExport/`,
`ProtectedDeletion/`. Likewise `test/Hexalith.Agents.IntegrationTests/` does not exist (the spine's own text
already annotates that line "created by Story 5.6," so that one is self-qualified as aspirational).
`src/Hexalith.Agents.Server/Aggregates/` contains only a `README.md`; `Application/Tools/` contains only
`.gitkeep`.

This is exactly v8's H1 (`ProviderCatalog`/`TenantProviderEnablement` split) and H4 (Structural Seed gaps —
`Aggregates/`, `Application/Tools/` empty, `IntegrationTests` absent) restated, not a new observation — both
already carry named backlog stories (5.3, 5.6) and a passing conformance test
(`StructuralSeedConformanceTests`) that deliberately checks only the project/root-file/extension-folder level,
not these aspirational sub-namespaces — so there is no test lying about current state. Re-confirmed unchanged;
no spine amendment needed.

---

## Verdict

**FAILS TO RATIFY, FIXABLE — but only because the pre-existing, already-tracked implementation debt (H1–H7
carried from v6/v8) remains open, exactly as it has for three prior rounds.** This round finds:

- **No new drift.** No source or test file changed since `a191d24` (verified fresh). No spine claim newly
  contradicts the code. No tracking artifact newly contradicts the spine or the code.
- **The one Medium (M1) the prior round found was correctly closed**, and both factual clauses of the new
  annotation independently check out against real evidence (a persisted, dated test-evidence table; a live,
  open non-conformance record).
- **The SDK bump (M1-adjacent Medium from the round-5 validation report) is correctly applied and correctly
  narrated** in the architecture `.memlog.md`.
- **Every spot-checked previously-closed item is still closed**, confirmed by direct re-derivation (a test
  run, two greps, one file read), not by trusting the prior report's word.
- **The working tree is scoped more broadly than the task described**, but every extra file traces to one of
  two fully self-documented, already-completed update passes (this architecture Update, and a separate PRD
  "third update" run) — a hygiene/handoff observation, not a spine defect.

## Summary Table

| # | Severity | Item | Classification | One-liner |
|---|---|---|---|---|
| 1 | Low | Working-tree scope | process/hygiene, not new drift | Tree also carries a fully self-documented, separate PRD "third update" pass the task didn't mention; nothing silently unaccounted for |
| 2 | — | `sprint-status.yaml` Story 5.3 annotation | verified correct | Both the 2569/2569 test-evidence claim and the `NC-5.3-PLATFORM-CATALOG-SCOPE` claim independently substantiated; the annotation's premise (open Product approval) was resolved a day later but its conclusion is unaffected |
| 3 | — | `global.json` SDK bump | verified correct | 10.0.301 → 10.0.400 matches `.memlog.md`'s own ARCH-A-4-closure narrative; no other field changed |
| 4 | — | Story 5.2 / AD-16 / register spot checks | confirmed unregressed | `ConfigurationVersion` gap still present (in-progress correctly held); `ForbiddenHostingOwnershipTests` 5/5 pass; LR-AUDIT-PROTECTION-DELETION symmetry text unchanged |
| 5 | — | Structural Seed vs. actual `src`/`test` tree | already-tracked (= v8 H1/H4), unchanged | 11 of 14 listed `Hexalith.Agents/` sub-namespaces don't exist yet; conformance test correctly doesn't assert them; no spine amendment needed |

**No Critical, High, or new Medium findings this round.** One Low (process observation only). Recommend: no
spine amendment required; optionally note in the next handoff that the working tree carries two independent,
already-documented update passes rather than one.
