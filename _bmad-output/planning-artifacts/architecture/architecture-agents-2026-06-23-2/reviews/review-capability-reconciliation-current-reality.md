# AD-4/AD-10 Capability-Version Reconciliation — Current-Reality Review

**Review date:** 2026-08-01  
**Lens:** brownfield code and Story 2.3 reality  
**Verdict:** **CONDITIONAL PASS — retain the amended context-time floor rule (`live CapabilityVersion >= snapshot ProviderCapabilityVersion`). Do not ratify the current no-comparison behavior as the architecture invariant.** The decision is supported by the version model and closes a real stale-projection hole. The spine correctly declares the principal implementation gap, but its claim that current readiness is re-evaluated exposes one additional Story 2.3 code/test gap that should travel with the implementation follow-up.

## Decision confirmation

The snapshot version is correctly defined as a **minimum trusted catalog revision**, not an equality pin:

- `CapabilityVersion` is per provider/model entry, replay-derived, and monotonic for the entry's lifetime: create sets `1`; each genuine `ProviderModelEntryMetadataUpdated` increments it; exact-duplicate updates emit no event; enable/disable do not decrement or reset it (`ProviderModelEntryState.cs:45-51`, `ProviderCatalogState.cs:36-100`, `ProviderCatalogAggregate.cs:126-176`). There is no delete/recreate path for an existing identity.
- Therefore, for the same snapshotted `ProviderId`/`ModelId`, an authoritative live value below the snapshot is not a legitimate later state. It signals stale/incomplete dependency state (or an invalid reader) and must fail closed.
- Exact equality would be too strict. The counter advances for every genuine safe-metadata update, including changes that need not invalidate an in-flight interaction. Accepting a higher revision while consuming the higher revision's current readiness and limits avoids false blocking and still prevents stale limits from being trusted.
- Provider/model identity remains frozen. The rule permits only a newer revision of the same snapshotted entry; it cannot retarget the interaction.

This makes the 2026-08-01 memlog decision and amended `AD-4`/`AD-10` coherent (`.memlog.md:63-64`; `ARCHITECTURE-SPINE.md:90-95,127-132`).

## Findings

### High — the Story 2.3 path is non-conformant exactly as the spine says

`AgentInteractionContextRequest.ProviderCapabilityVersion` is documented as snapshot-recorded but is never read by `AgentInteractionContextOrchestrator`. `ReadModelBudgetAsync` accepts any successful, text-capable entry and copies the entry's current version into the measurement (`AgentInteractionContextRequest.cs:24,29-42`; `AgentInteractionContextOrchestrator.cs:196-213`). The pure policy rejects only a non-positive measurement version; it has no snapshot floor input (`AgentInteractionContextPolicy.cs:65-76`).

Consequences:

- `live < snapshot` can incorrectly reach `ContextReady` when its limits fit.
- `live == snapshot` and `live > snapshot` already use the correct current limits and record the live version.
- No focused lower/equal/higher tests exist; the context test helpers hard-code both values to `1` (`AgentInteractionContextOrchestratorTests.cs:414-455`).

The spine's explicit implementation-gap note is accurate. The implementation follow-up should compare in the server orchestration, map missing/lower to an unavailable model budget so the existing policy emits `ContextBlocked(ModelBudgetUnavailable)`, and add lower/equal/higher tests including evidence assertions.

### High — the implementation-gap note understates the live-readiness mismatch

Amended AD-4/AD-10 says the current entry's readiness and safe limits govern context build. The context orchestrator checks `SupportsTextGeneration`, but it does **not** check `Status == Enabled` or `IsSelectableForNewActiveUse` (`AgentInteractionContextOrchestrator.cs:196-213`). The test named `Disabled_or_non_text_capable_entry_returns_context_blocked` supplies an **enabled/selectable** entry with only `SupportsTextGeneration: false`; it never exercises a disabled entry (`AgentInteractionContextOrchestratorTests.cs:150-161,414-428`).

This matters because enable/disable deliberately does not bump `CapabilityVersion`: the new floor comparison cannot substitute for a current-readiness check. A provider disabled after the invocation gate but before context build would still supply a trusted budget today. The same focused follow-up should cover disabled/non-selectable readiness separately from version ordering, or the spine should narrow its readiness claim. Keeping the readiness claim and fixing the code is the safer, internally consistent choice.

### Medium — Story 2.3's prior rationale overstates the Story 2.2 gate

The Story 2.3 review says the capability floor is revalidated by the Story 2.2 gate before `Authorized` (`2-3-build-conversation-context-with-safe-bounds.md:426`). That is not true in the brownfield code. `AgentInteractionGateRequest` carries provider/model identity but no `ProviderCapabilityVersion`, and the gate checks only whether the current entry is selectable (`AgentInteractionGateRequest.cs:16-42`; `AgentInteractionGateOrchestrator.cs:218-238,308-316`). It cannot detect `catalog CapabilityVersion < snapshot ProviderCapabilityVersion`.

The context-time floor is therefore not redundant; it closes a check that no earlier gate performs. The older Story 2.3 Cross-Module prose also says “no longer matches,” implying equality (`2-3-build-conversation-context-with-safe-bounds.md:245`); the amended spine's explicit `>=` rule should supersede that wording.

## Evidence semantics

The amended rule matches the existing evidence model:

- The immutable interaction snapshot preserves the request-time `ProviderCapabilityVersion` as provenance.
- The context measurement's `ProviderCapabilityVersion` means “the catalog revision backing this budget,” and `AgentInteractionContextPolicy` copies it into both ready and measured-block evidence (`AgentInteractionContextPolicy.cs:102-113`). It should therefore remain the **live accepted revision**, not be overwritten with the snapshot revision.
- Missing/lower revision should produce `ContextBlocked(ModelBudgetUnavailable)`. The existing unavailable-budget convention zeroes untrusted budget fields/version; this is audit-safe but intentionally coarse. Equal/higher accepted reads should record the actual live revision together with the limits used.
- Context blocked is a durable success outcome/evidence event, not a structural rejection, so the failure remains replayable and auditable without exposing catalog internals.

No contract change is required for the reconciliation. The server already has both values at the comparison point; only orchestration, documentation, and focused tests need adjustment.

## Technology/version research relevance

**No web research is relevant to this decision.** The reconciliation introduces no library, framework, provider SDK, protocol, or externally versioned behavior. Its authorities are the repository's event/replay semantics, request/evidence contracts, Story 2.3 artifacts, and current orchestrator/test code. Existing stack-version verification elsewhere in the spine neither supports nor weakens this local domain decision.

## Handoff

Keep AD-4/AD-10 as amended. Treat Story 2.3 as needing one focused brownfield correction package:

1. reject missing or `live CapabilityVersion < snapshot ProviderCapabilityVersion` as `ModelBudgetUnavailable`;
2. accept equal/higher and record the live accepted version and live limits in context evidence;
3. reject currently disabled/non-selectable/non-text-capable entries independently of version; and
4. test lower/equal/higher, disabled readiness, outcome reason, dispatched measurement, and durable evidence.

## Recheck 2026-08-01

**Verdict: PASS — no remaining critical or high spine finding.** The amended AD-4, AD-10, AD-13, implementation-gap note, and sprint follow-up now represent the brownfield reality accurately. The code remains intentionally non-conformant pending the open implementation action; the spine no longer understates or misclassifies those gaps.

- **AD-4 remains accurate:** request-time provider/model identity and snapshot version stay provenance, while current safe constraints may block an in-flight interaction without retargeting it.
- **AD-10 now captures both earlier high findings:** it requires the version floor and separately requires enabled/configured/text-generation readiness and valid step-specific limits, correctly noting that enable/disable does not bump `CapabilityVersion`. This matches the current context defect: `ReadModelBudgetAsync` checks only successful read + text capability and ignores the snapshot floor, enabled state, and configured state.
- **Cross-step evidence is now explicit:** snapshot `ProviderCapabilityVersion` is request provenance; each runtime evidence field records the effective live version consumed. This accurately identifies generation/regeneration's current misuse: both read current limits but pass and record the snapshot version.
- **AD-13 faithfully captures the retry hole:** current generation and regeneration derive deterministic attempt identities but do not persist or compare effective version, limits, timeout, or input fingerprint before reusing those identities. The amended rule prevents a retry from invoking changed provider inputs under the same attempt id.
- **The implementation-gap note is complete at the relevant altitude:** it expressly names context floor/readiness, generation/regeneration effective-version provenance, cross-step reconciliation, and retry binding. The open sprint action at `sprint-status.yaml:125-127` mirrors those requirements and includes the focused lower/equal/higher, readiness, cross-step evidence, and changed-input retry tests. The original decision actions remain correctly marked done because the architecture choice is closed while implementation is tracked separately.

No further web research is relevant; this recheck concerns only repository-defined revision, evidence, and idempotency semantics.

## Final recheck 2026-08-01

**Verdict: PASS — no remaining critical or high brownfield-reality finding.** Every newly tightened AD-10/AD-11/AD-13 rule is now distinguishable from current behavior and is covered by an explicit non-conformance statement plus an open implementation follow-up.

- AD-10's durable capability high-water mark, trust-bearing freshness requirement, independent readiness checks, and distinct `EffectiveProviderCapabilityVersion` contract are explicitly identified as absent from current context/generation/regeneration paths.
- AD-11's immediate pre-invocation repeat of authorized context read, token measurement, capability validation, and full budget calculation is included in the implementation-gap note as missing pre-invocation revalidation; current generation/regeneration only re-read content and selected catalog limits.
- AD-13's durable prepared-attempt checkpoint, canonical request fingerprint, restricted transient retry scope, and fail-closed changed-input rule are target behavior. The gap note marks retry binding non-conformant, and the open sprint action makes the missing checkpoint/fingerprint and retry constraints explicit.
- The sprint action remains `open` and correctly says this work must land before live provider binding, while the architecture-decision actions remain `done`. No text presents the tightened invariants as already shipped.

The implementation gaps remain real, but they are now faithfully and sufficiently labeled in the spine and tracking artifact.
