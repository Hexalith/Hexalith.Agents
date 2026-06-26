# Epic 4 Documentation Audit — 2026-06-25

Companion to `epic-4-retro-2026-06-25.md`. After the retrospective, candidate docs that *might* need
updates from Epic 4 implementation learnings were enumerated, then each was **verified against the actual
implementation code**. Only verified discrepancies were updated; proposals where code already matched the
docs were discarded. Focus areas: architecture decisions that changed during implementation, API docs that
diverged from specs, READMEs with outdated instructions, and configuration documentation.

## Candidate list and verdicts

| # | Doc | Claim under test | Verified against | Verdict |
| --- | --- | --- | --- | --- |
| 1 | `src/Hexalith.Agents.Server/Aggregates/README.md` | "This folder is the named home for the Agents domain aggregate roots, each added by the story that needs it" (Agent, ProviderCatalog, AgentInteraction). | Folder contains only `README.md`; all three aggregate roots live in the **domain** project: `src/Hexalith.Agents/{Agent,AgentInteraction,ProviderCatalog}/*Aggregate.cs`. The stories that needed them (1.2–1.7, Epic 2) are all `done`. | **UPDATED** — genuine, verified divergence. The pure aggregates were placed in the domain assembly per AD-1/AD-3 (the idiomatic EventStore domain-module layout), and this Server-side folder stayed empty. README rewritten to state where the roots actually live and what the Server project hosts instead. |
| 2 | `src/Hexalith.Agents.Aspire/README.md` | "intentionally empty shell… a functioning AppHost/topology is explicitly out of scope… hosting extensions land with the operational-topology stories (AD-16)." | Project contains only `.csproj` + `README.md`. Operational topology remains deferred through Epic 4 (Story 4.5: AppHost topology deferred; `.gitkeep`-only Workflows/Projections). | **DISCARDED** — code matches doc. Still empty; still deferred. |
| 3 | `src/Hexalith.Agents.ServiceDefaults/README.md` | "Intentionally empty… conventions and package dependencies are added by the operational-topology story (AD-16)." | Project contains only `.csproj` + `README.md`. Operational topology still deferred. | **DISCARDED** — code matches doc. |
| 4 | `ARCHITECTURE-SPINE.md` · AD-4 (Interaction Snapshot) | Snapshot floor must list `content-safety policy version` + `ContextPolicyReference`. | AD-4 already lists "content-safety policy version" and carries the 2026-06-24 reconciliation note for `ContentSafetyPolicyVersion`/`ContextPolicyReference`. Matches the shipped `AgentInteractionSnapshot`. | **DISCARDED** — already reconciled (Epic 2 Action Item #4, `done`). Code matches doc. |
| 5 | `ARCHITECTURE-SPINE.md` · Stack table | `Microsoft.Agents.AI 1.10.0 verified current`; Dapr/MCP versions; `Provider SDK — Deferred`. | Story 4.5 confirmed **zero** provider/runtime SDK packages are pinned in `Directory.Packages.props` (live owner deferred). The table already marks Provider SDK "Deferred" and AD-18/AD-19 `[ADOPTED]`-but-deferred; the versions are timestamped "verified current" as of the 2026-06-23 authoring. | **DISCARDED** — not a doc-vs-code divergence. The code does not contradict the doc; it simply hasn't bound the deferred SDKs (which the doc already says are deferred/adapter-local). The 1.10.0→1.11.0 NuGet drift noted in Story 4.1 is external, not a code divergence; editing version numbers in a dated planning artifact would be noise. |
| 6 | `ARCHITECTURE-SPINE.md` · Structural Seed | Layout includes `src/Hexalith.Agents.Server/Aggregates/`, `tests/Hexalith.Agents.IntegrationTests/`, `Hexalith.Agents.AppHost`, `Hexalith.Agents.Testing`. | `Hexalith.Agents.IntegrationTests` does not exist yet (Story 4.5: deliberately not created — in-process xUnit conformance is the V1 mode); the `Server/Aggregates/` folder is unused (see #1). | **DISCARDED for the spine** — the "Structural Seed" is an explicitly-labeled target/seed layout, and the conformance report already documents `IntegrationTests` as deferred-with-seam. The seed is aspirational by definition, so it is not "wrong." (The downstream consequence for `Server/Aggregates/` is handled at the README level in #1.) |

## Outcome

- **1 doc updated:** `src/Hexalith.Agents.Server/Aggregates/README.md` (verified divergence between the README's
  "aggregates added here" instruction and the actual domain-project placement of the aggregate roots).
- **5 candidate updates discarded:** code already matched the docs, or the doc was an explicitly-labeled
  target/seed or a timestamped planning record that the implementation does not contradict.

The strongest *forward-looking* doc signal is not a discrepancy but a gap recorded in the retro and
`4-5-governance-conformance-report.md`: the live-binding and integration-test surfaces are deferred, so several
seed/stack entries (live runtime owner, `IntegrationTests`, provider SDK versions) will become genuinely
verifiable — and may then need doc reconciliation — only when those bindings land. That work is captured as
Epic 4 Action Items #2 and #4 rather than as a doc edit today.
</content>
