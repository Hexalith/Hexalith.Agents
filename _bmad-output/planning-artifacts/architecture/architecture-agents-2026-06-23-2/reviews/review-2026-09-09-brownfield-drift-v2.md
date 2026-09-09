# Reviewer Gate — Brownfield Drift Review v2 (2026-09-09)

**Reviewer role:** independent verification of whether ARCHITECTURE-SPINE.md (updated 2026-09-09) still
accurately describes/ratifies `src/Hexalith.Agents*` as it exists on disk today, and whether the drift
documented in `sprint-change-proposal-2026-09-09.md` has narrowed, is unchanged, or has grown.

**Method:** read the full spine (755 lines) and the full sprint change proposal, then read the actual
`ProviderCatalog` aggregate/state/projection code, the `Agent` aggregate's lifecycle events, the identity
helper classes under `Hexalith.Agents.Server/Application/AgentInteractions`, and the on-disk folder/test
layout. No files were modified.

## Overall Verdict

The spine's text is internally self-consistent and the sprint change proposal accurately describes the
drift as of 2026-09-09, but **none of the drift it flagged has been remediated in code** — the shipped
`ProviderCatalogAggregate` is still tenant-scoped with a transitional boolean admin gate, `TenantProviderEnablement`
does not exist anywhere in `src/` or `test/`, the AD-29 identity canonicalizer is still five separate helper
classes rather than one `AgentsIdentity` canonicalizer, `AD-4`'s `ConfigurationVersion` still is not incremented
by `AgentActivated`/`AgentDisabled`, and the structural seed mismatches (`Server/Aggregates`, `Application/Tools`,
missing `Hexalith.Agents.IntegrationTests`) are all still present exactly as the proposal described. This is
"drift as documented, not yet fixed" rather than "new, undocumented drift" — with one exception (Finding 5,
medium) where the spine's own AD-30 text now names the exact transitional extension key the code uses, which
narrows the gap between prose and code in one specific spot while leaving the substantive security gap
(no HMAC tagging) wide open.

## Findings

### Finding 1 (Critical) — AD-2 platform-catalog claim contradicts shipped code; zero narrowing since the proposal

**Spine claim (AD-2, line 108):** `ProviderCatalog` is scoped to "reserved EventStore tenant `system`, one
stream per `ProviderId` + `ModelId`" and `TenantProviderEnablement` (`TenantId`) is a separate aggregate
that owns "which platform entries the tenant may see and select."

**Code reality:**
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents/ProviderCatalog/ProviderCatalogAggregate.cs`
  line 17 doc comment: *"Pure, replay-safe aggregate for the **tenant-scoped** governed provider/model catalog
  (AD-2, AD-3, AD-9, AD-10)."* Its authorization gate (line 22-27, 297-299) is a raw string check on
  `envelope.Extensions["actor:agentsProviderAdmin"] == "true"` — no tenant/system-scope split, no HMAC.
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents.Server/Projections/ProviderCatalogProjectionFold.cs`
  lines 77, 102, 148 thread `request.TenantId` through `ToState`/`ToReadModel`, i.e. the `provider-catalog`
  read model is still tenant-keyed, not platform-scoped.
- `grep -rn "TenantProviderEnablement"` across `src/` and `test/` returns **zero hits**. The aggregate, its
  commands/events, and its projection do not exist in the codebase at all.
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents/ProviderCatalog/ProviderCatalogAggregate.cs`
  file mtime is 2026-09-08 16:05, i.e. it was last touched the day *before* the spine amendment and the
  approved sprint-change proposal, and has not been touched since.

**Conclusion:** this is exactly the drift `sprint-change-proposal-2026-09-09.md` §1 described ("Story 5.3
shipped a tenant-scoped `ProviderCatalogAggregate`... The new architecture instead requires a platform-scoped
catalog... plus... `TenantProviderEnablement`"). It has not narrowed at all — Story 5.3 is still `backlog`
per the proposal's own sprint-status regeneration plan, and no code change has landed since. The spine, as
currently written, does **not** describe the code; it describes the target state the (approved, unstarted)
reopened Story 5.3 is supposed to build.

### Finding 2 (High) — AD-4 `ConfigurationVersion` claim contradicts `AgentActivated`/`AgentDisabled`; unchanged (DW-4 still open)

**Spine claim (AD-4, line 120):** *"`ConfigurationVersion` increments on every accepted `Agent` configuration
or lifecycle event."*

**Code reality:** `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents/Agent/AgentAggregate.cs`
line 267: `DomainResult.Success([new AgentActivated(agentId)])`, and line 298:
`DomainResult.Success([new AgentDisabled(agentId)])` — neither event carries a `ConfigurationVersion`
argument (contrast with e.g. line 346 `AgentPartyIdentityLinked(agentId, command.PartyId, state.ConfigurationVersion + 1)`,
which does bump it). `AgentState.Apply(AgentActivated e)` / `Apply(AgentDisabled e)`
(`AgentState.cs` lines 156, 167) do not touch `ConfigurationVersion` either.

This is precisely the sprint-change proposal's "DW-4" item ("Current `AgentActivated` and `AgentDisabled`
events do not carry or increment it"), assigned to Story 5.2 as an AC patch. No such patch has landed —
the code is byte-for-byte the same gap. The spine's AD-4 rule is therefore currently false as a description
of the shipped `Agent` aggregate.

### Finding 3 (High) — AD-29 "one shared `AgentsIdentity` canonicalizer" contradicts five separate helper classes; unchanged

**Spine claim (AD-29, line 270):** *"One shared `AgentsIdentity` canonicalizer... derives every deterministic
id"* (interaction id, attempt id, reservation/admission/queue ids, proposal version id, message id, safety
decision id, etc.).

**Code reality:** `grep -rn "class.*Identity"` over `src/` finds no `AgentsIdentity` type at all. Instead:
`AgentProposalIdentity.cs`, `AgentResponsePostingIdentity.cs`, `AgentProposalEditIdentity.cs`,
`AgentProposalRegenerationIdentity.cs`, and `AgentInteractionIdentity.cs` — five independent internal
static classes under `Hexalith.Agents.Server/Application/AgentInteractions/` — plus a separate
`AgentCommandIdentityFactory` under `Ports/`. None of the file names or (spot-checked) contents reference
a shared canonicalizer, HMAC digest keying, or the `U+001F`-separator/length-prefix grammar AD-29 specifies.

This matches the proposal's trigger evidence row verbatim ("Current generation, proposal, edit, and
regeneration paths use several incompatible derivation helpers") and is unresolved.

### Finding 4 (High) — Structural seed mismatches are still present exactly as flagged; not narrowed

Spine "Structural Seed" section (lines ~495-528) declares:
- `Hexalith.Agents/` should contain a `TenantProviderEnablement/` folder alongside `ProviderCatalog/` — **absent**
  (`ls src/Hexalith.Agents` shows only `Agent/`, `AgentInteraction/`, `ProviderCatalog/`; none of the other
  eleven listed AD-2 aggregate folders exist either, e.g. `BudgetLedger/`, `TenantGovernancePolicy/`,
  `ContentSafetyPolicy/`, `LaunchReadinessGate/`, `ConversationAgentState/`, `AuditInspection/`,
  `SecurityEventLog/`, `LegalHold/`, `AuditExport/`, `ProtectedDeletion/` are all missing — expected, since
  their owning stories are backlog, but worth noting the seed lists them as already-structural).
- `Hexalith.Agents.Server/` seed lists only `Api/`, `Application/{Agents,AgentInteractions,Queries,Workflows,Activities}`,
  `Composition/`, `Ports/`, `Projections/` — it does **not** list `Aggregates/` or `Application/Tools/`.
  Both exist on disk today:
  `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents.Server/Aggregates/` (contains only a
  `README.md`) and
  `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents.Server/Application/Tools/` (contains only
  `.gitkeep`).
- `test/` seed lists `Hexalith.Agents.IntegrationTests/` as a required project ("created by Story 5.6").
  `find test -maxdepth 1 -type d` shows only `Hexalith.Agents.Tests`, `Hexalith.Agents.Contracts.Tests`,
  `Hexalith.Agents.Server.Tests`, `Hexalith.Agents.Client.Tests`, `Hexalith.Agents.UI.Tests` — no
  `Hexalith.Agents.IntegrationTests` project, and it is absent from `Hexalith.Agents.slnx`.

All three items are exactly the ones the proposal's "Structural Seed" trigger-evidence row named
("`Hexalith.Agents.IntegrationTests` is required; `Server/Aggregates` and `Application/Tools` are absent")
— unchanged, and correctly assigned to (not-yet-started) Story 5.6.

### Finding 5 (Medium) — AD-30's extension-key text has moved closer to the code's literal naming, but the substantive gap (no HMAC) is unchanged

**Observation:** AD-30 (line 276) now states the `Platform` principal is "carried as the reserved
`actor:agentsProviderAdmin` extension" — this is the literal constant name
(`ProviderAdminExtensionKey = "actor:agentsProviderAdmin"`) already used by
`ProviderCatalogAggregate.cs`. That specific string match did **not** exist as a point of contradiction
before, so on this narrow lexical point the spine and code agree.

However, AD-30 requires: ingress-issued HMAC-tagged extensions verified by the command pipeline before
the aggregate ("Every reserved extension carries an HMAC tag issued to the Agents Server principal through
`EXT-SECRETS-1` and verified by the Server command pipeline before the aggregate, which also rejects an
untagged extension"). The code's actual check (`IsProviderAdmin`, lines 297-299) is a plain string equality
test with no HMAC, no tag verification, and the aggregate's own doc comment (lines 22-27) calls the whole
scheme "transitional" pending "the full Agents authorization story." This is the proposal's DW-2 item
("Story 5.4 must replace transitional trusted booleans and close DW-2") and remains fully open — flag this
as a documented, not new, gap, but note the naming convergence so a future reviewer does not mistake it for
resolution of the underlying security requirement.

### Finding 6 (Low) — No new (previously-uncalled-out) drift found beyond what the proposal already lists

I looked specifically for drift the sprint-change proposal's own trigger table does *not* mention, in the
areas the task asked about (ProviderCatalog, aggregate identity, deterministic-id canonicalizer, structural
seed). I did not find any additional contradiction beyond Findings 1-5, all of which trace directly to rows
already in the proposal's trigger-evidence table (AD-2, AD-29, AD-4, Structural Seed, AD-30/DW-2). The spine
itself is honest about this: AD-17's Live-Seam Matrix language and the "Deferred Beyond V1" section do not
claim Story 5.3 or the identity/structural items are done, and `ARCH-A-5` in the Architecture Assumptions
table is explicitly marked "**RETIRED 2026-09-09**" with the correct retirement rationale, consistent with
the proposal's execution record. So the spine document's own bookkeeping is currently accurate about *what
it requires*; the inaccuracy is entirely in the implicit reader assumption that "final" / "status: final"
(line 8 front matter) means the code underneath already conforms. It does not, and the spine contains no
explicit "brownfield gap" callout table cross-referencing AD-2/AD-4/AD-29/structural-seed to their
non-conformant current implementation — a reader who does not also read
`sprint-change-proposal-2026-09-09.md` would have no way to know from the spine alone that Story 5.3 is
non-conformant today.

## Summary Table

| # | AD / Section | Spine claim | Code reality | Status vs. 2026-09-09 proposal |
|---|---|---|---|---|
| 1 | AD-2 | `ProviderCatalog` in tenant `system`; separate `TenantProviderEnablement` | Tenant-scoped `ProviderCatalogAggregate`; `TenantProviderEnablement` does not exist | Unchanged (critical, still open) |
| 2 | AD-4 | `ConfigurationVersion` increments on every accepted lifecycle event | `AgentActivated`/`AgentDisabled` carry no version | Unchanged (DW-4, still open) |
| 3 | AD-29 | One shared `AgentsIdentity` canonicalizer | 5+ separate identity helper classes, no shared canonicalizer | Unchanged (still open) |
| 4 | Structural Seed | No `Server/Aggregates`, no `Application/Tools`; `Hexalith.Agents.IntegrationTests` exists | Both stray folders present; integration test project absent | Unchanged (Story 5.6 not started) |
| 5 | AD-30 | HMAC-tagged trusted extension issued by ingress | Plain string-equality transitional gate, doc-flagged as such | Lexical naming converged; substantive gap (DW-2) unchanged |
| 6 | — | — | — | No new undocumented drift found |

## Files Consulted

- `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md`
- `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-09.md`
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents/ProviderCatalog/ProviderCatalogAggregate.cs`
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents/ProviderCatalog/ProviderCatalogState.cs`
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents.Server/Projections/ProviderCatalogProjectionFold.cs`
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents/Agent/AgentAggregate.cs`
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents/Agent/AgentState.cs`
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents.Server/Application/AgentInteractions/*.cs`
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents.Server/Aggregates/README.md`
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents.Server/Application/Tools/.gitkeep`
- Directory listings of `src/` and `test/` (top-level, non-`bin`/`obj`)
- `/home/administrator/projects/hexalith/agents/Hexalith.Agents.slnx`
