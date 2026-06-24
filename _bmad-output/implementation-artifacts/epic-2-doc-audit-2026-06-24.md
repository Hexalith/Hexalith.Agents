# Epic 2 — Documentation Audit (Implementation-Learning Reconciliation)

- **Date:** 2026-06-24
- **Scope:** Post-Epic-2 verification of project documentation against the actual `Hexalith.Agents` implementation.
- **Method:** For each candidate, read the current doc claim, compare against the shipped code, and either (a) update a **verified** discrepancy or (b) **discard** when code already matches the doc / the gap is deferred future work / fixing it would be net-new authoring rather than a correction.
- **Companion:** `epic-2-retro-2026-06-24.md`

> Doc landscape note: the repo `docs/` folder is **empty** and the agents-root `README.md` is a one-line stub. The substantive "documentation" for this module is the BMAD planning set — the **architecture spine** (architecture decisions), **epics.md** + **prd.md** (specs/API), plus inline `Program.cs` configuration comments. The audit targets those.

---

## Summary

| # | Candidate doc | Type | Verdict |
| --- | --- | --- | --- |
| A | `ARCHITECTURE-SPINE.md` — **AD-4 Interaction Snapshot** | Architecture decision | ✅ **Updated** (verified discrepancy) |
| B | `ARCHITECTURE-SPINE.md` — **Structural Seed** (test projects) | Architecture detail | ⛔ Discarded (deferred scaffold, not a changed decision) |
| C | `ARCHITECTURE-SPINE.md` — **Stack** table | Config/versions | ⛔ Discarded (code matches docs) |
| D | `ARCHITECTURE-SPINE.md` — **AD-6/AD-7 + Deferred** (AddParticipant seam) | Architecture decision | ⛔ Discarded (doc already anticipates reality) |
| E | `epics.md` / `prd.md` — acceptance criteria / API specs | API/spec | ⛔ Discarded (implementation matches specs) |
| F | agents-root `README.md` | README | ⛔ Discarded (stub; no instruction contradicts code) |
| G | `Hexalith.Agents` module README | README | ⛔ Discarded (does not exist → authoring, not a fix) |
| H | Configuration documentation (`Parties` / `Conversations` sections) | Config docs | ⛔ Discarded (none exists → authoring; recommendation noted) |

**Net result: 1 verified update applied, 7 candidates verified and discarded** — exactly the discard-where-code-matches outcome the audit is meant to produce.

---

## A. AD-4 Interaction Snapshot — ✅ UPDATED

**Doc claim (before):** AD-4's Rule enumerated the snapshot as: configuration version, instructions version, response mode, approver policy version, `ProviderId`, `ModelId`, provider capability version, caller `PartyId`, source `ConversationId`, context-build policy.

**Code reality:** `src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionSnapshot.cs` ships those fields **plus `ContentSafetyPolicyVersion`**, and carries the context-build policy as `ContextPolicyReference` (V1 default `"full-conversation-v1"`). The source XML doc explicitly states `ContentSafetyPolicyVersion` is *"an additive extension beyond AD-4's enumerated floor … snapshotted here to anticipate Story 2.4's safety check."*

**Verdict:** Genuine, code-acknowledged doc-vs-code discrepancy; the architecture decision grew additively during implementation.

**Change applied:** Added `content-safety policy version` to the AD-4 Rule enumeration, added a dated *Epic 2 reconciliation* note recording the additive `ContentSafetyPolicyVersion` + `ContextPolicyReference`, and bumped the spine's `updated:` frontmatter to `2026-06-24`.

---

## B. Structural Seed — test projects — ⛔ DISCARDED

**Doc claim:** Structural Seed lists test projects `Hexalith.Agents.Contracts.Tests`, `Hexalith.Agents.Server.Tests`, `Hexalith.Agents.Client.Tests`, `Hexalith.Agents.UI.Tests`, `Hexalith.Agents.IntegrationTests`.

**Code reality (`Hexalith.Agents.slnx` + `tests/`):** `Hexalith.Agents.Contracts.Tests`, `Hexalith.Agents.Server.Tests`, `Hexalith.Agents.Tests` (domain, 527 tests), `Hexalith.Agents.UI.Tests`. `Client.Tests` and `IntegrationTests` do **not** exist.

**Verdict — discard.** This block is explicitly labelled a *"Structural Seed"* (a scaffold, not a current inventory). `Client.Tests` is legitimately absent because `Hexalith.Agents.Client` is still a marker-only project (live client deferred to Epic 4 / story 4.1); `IntegrationTests` is deferred with the live end-to-end bindings (Epic 4). These are **deferred future work, not changed decisions**. The only true omission is the present-but-unlisted domain test project `Hexalith.Agents.Tests`; amending a historical seed scaffold to add one line would misrepresent the seed as an inventory for marginal value. No edit.

---

## C. Stack table — ⛔ DISCARDED

**Verified against code:** Target framework `net10.0` ✓ (`Directory.Build.props`); .NET SDK `10.0.301` ✓ within the stated `10.0.300`–`10.0.301` range (`global.json`); xUnit v3 `3.2.2` ✓, Shouldly `4.3.0` ✓, Fluent UI Blazor `5.0.0-rc.3-26138.1` ✓ (`Directory.Packages.props`).

**Verdict — discard.** Every package the code actually consumes matches the Stack table. The not-yet-listed packages (provider SDK, Agent Framework, Dapr, etc.) are forward-looking by design — `Directory.Packages.props` states future packages are added by the stories that need them. **Code matches docs.**

---

## D. AD-6 / AD-7 + Deferred table — Conversations `AddParticipant` seam — ⛔ DISCARDED

**Doc claim:** AD-6/AD-7 require Agent membership via a Conversations-owned `AddParticipant` seam, and the **Deferred** table already lists *"Public Conversations `AddParticipant` client/API seam if absent"* as a prerequisite to expose if not public when implementation starts.

**Code reality:** Story 2.5 verified the live Conversations module (commit `46df0cd`) exposes no public `AddParticipantAsync`; `ConversationClientResponsePoster` *verifies* membership via `GetConversationAsync` and *establishes* via a fail-closed `SeamUnavailable` path.

**Verdict — discard.** The doc already anticipates exactly this and prescribes the fail-closed behaviour that was implemented. **No discrepancy** — the architecture predicted reality. (Confirmation is captured in the retro's Next-Epic risks rather than as a spine edit.)

---

## E. epics.md / prd.md — acceptance criteria & API specs — ⛔ DISCARDED

**Verified:** Every Epic 2 story's Senior Developer Review confirmed all four acceptance criteria implemented and tested; the public contract surface (commands/events/enums/queries) realises the ACs faithfully. The two internal "noted divergences" (capability-version reconciliation in 2.3; `AddParticipant` seam in 2.5) are implementation-internal and already covered by the architecture's deferred items.

**Verdict — discard.** There is no separate API-reference document enumerating concrete type names that could be stale, and the behavioural specs match what shipped. **Implementation matches specs.** (The capability-version reconciliation is tracked as a retro action item, not a spec-doc fix.)

---

## F. agents-root `README.md` — ⛔ DISCARDED

**Doc claim:** `# Hexalith.Agents` / `THe Hexalith Agents module`.

**Verdict — discard.** A stub with no setup/usage instructions and no claim that contradicts the code; the audit fixes *outdated instructions*, and there are none. (Cosmetic note: the body has a "THe" → "The" typo — left untouched to keep this audit scoped to verified code-vs-doc discrepancies rather than cosmetic authoring.)

---

## G. `Hexalith.Agents` module README — ⛔ DISCARDED

No module-level README exists. Authoring one is net-new documentation, not the correction of a verified discrepancy, so it is out of scope for this audit. Recorded as a backlog recommendation only.

---

## H. Configuration documentation — ⛔ DISCARDED (recommendation noted)

**Code reality:** Epic 1–2 introduced two opt-in configuration sections that gate live adapters — `Parties` (`Parties:BaseUrl`, `Parties:Tenant`) and `Conversations` (`Conversations:BaseUrl`). They are documented **only** inline in `src/Hexalith.Agents.Server/Program.cs` comments; no `appsettings.json` and no standalone config doc exist.

**Verdict — discard for now.** There is no configuration document to be *wrong* — so there is no discrepancy to fix; producing one is authoring. **Recommendation (for when live bindings land in Epic 4):** add a short configuration reference for the `Parties` and `Conversations` sections (and the future provider-adapter and content-safety sections), since the fail-closed defaults mean a missing/incorrect section silently disables live behaviour — exactly the kind of thing config docs should warn about.

---

*Outcome: the architecture spine now matches the shipped snapshot contract (AD-4); all other candidates were verified to already match the implementation or to represent deferred/uncreated docs, and were discarded with the rationale above.*
