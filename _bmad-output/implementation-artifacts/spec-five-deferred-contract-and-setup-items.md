---
title: 'Complete five deferred contract and setup items'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five open ledger items leave setup correlation evidence stale, interaction/provider enum readers intolerant of future values, provider write identities insufficiently validated, configuration sections outside the required accordion, and the enum guard incomplete.

**Approach:** Complete DW-4, DW-13, DW-14, DW-18 and DW-23. Verify command-specific lifecycle confirmation, extend existing Unknown-zero converters while preserving existing ordinals and known-value behavior, validate provider write identities before dispatch, group configuration sections using FluentAccordion, and enforce coverage across public contract enums. Run focused regressions, independently review, close the five ledger items with evidence, commit and push main as requested.

</frozen-after-approval>

## Implementation Notes

- Baseline: clean main at `2f3df581d5e484e8643ddcb108649f4626441cad`. Freeform five-item batch; user supplies the scope override and explicitly authorizes pushing main. No sprint story key applies.
- Route facts: bounded reversible edits using established converters, validation, components and tests; no new API, ordinal migration, deployment or stored-data mutation. Most file changes are mechanical converter attributes. No unresolved intent gaps. No module-owned AppHost exists.
- DW-4 already uses command-derived target versions and exact canonical receipt identities; lifecycle commands increment configuration versions. Establish fresh focused evidence and add a regression for the original lifecycle/correlation defect where needed; do not claim production topology qualification.
- DW-13: migrate remaining Unknown-zero enums in AgentInteraction and ProviderCatalog to their existing self-typed UnknownFallbackEnumConverter. Preserve all declared ordinals, case-insensitive known names and supported numeric values. Leave flags with None-zero and Success-zero status enums on their existing behavior.
- DW-23: migrate six remaining Agent Unknown-zero enums and replace namespace exclusions in AgentOperationContractsTests with complete public enum discovery and explicit non-Unknown exceptions. Include nested Operations payload evidence, future-name/number fallback, known-value round trips, and serializer options with the existing fallback factory. Do not renumber AgentSetupWriteStatus or AgentInspectionStatus.
- DW-14: EventStoreProviderCatalogOperations WriteAsync/WriteTenantAsync retain receipts/effects but accept arbitrary caller identities. Reuse setup's canonical ULID rule, preserve authorization-first handling and refuse invalid generated IDs. Cover platform and tenant writes, invalid receipt identities, no dispatch/status reads on invalid requests, valid acceptance/replay and outcome behavior. Public enum/API shapes stay fixed.
- DW-18: replace ten sibling titled configuration sections with one FluentAccordion and one FluentAccordionItem per section. Keep heading, write live region and activation actions outside; expand editing and other sections initially to preserve visibility. Reuse existing localized headings and controls, with a focused component check for structure and action visibility.
- Validate Contracts, Server provider/setup/lifecycle, and UI configuration regressions in Debug/source mode with fresh builds and direct xUnit assembly filters. Run whole affected projects if practical; preserve exact counts and distinguish any broad blockers. All changed C# files retain one type per file and required documentation.
- Follow the workflow's context-free blind review, triage every finding, repair concrete defects, validate Conventional Commit with commitlint, fetch/check divergence, and push main. Preserve submodule contents and pointers.
- Implemented 52 enum migrations (44 interaction, two provider, six Agent); split the two co-located outcome enums into their own named files. The public compatibility inventory now covers 70 Unknown-zero enums, explicitly deferring only the three closed directory protocol types and naming five non-Unknown-zero exceptions. Compared all 70 declarations verbatim against the baseline to confirm no names or ordinals changed.
- Provider writes now reject malformed, lowercase, overflowing, GUID-shaped and whitespace IDs on all six command families before dispatch/status reads; canonical exact retries and mismatched receipts are exercised. Authorization runs before identity generation/validation. The focused server run passes 194 cases before the final two generated-ID cases were added.
- Fluent UI 5.0.1 inspection establishes Header (v4's Heading is an unmatched attribute), Expanded and HeadingLevel. The ten accordion items use actual supported Header parameters, begin expanded, and preserve level-two headings; activation actions and write live region remain outside. All 66 configuration tests pass.
- Initial individual Debug builds pass with zero warnings/errors. A plain solution build lacks a globally propagated source-reference mode and fails in Conversations converter compilation (SYSLIB1220/CS0115). The maintained Story 5.2 verifier explicitly supplies UseHexalithProjectReferences=true to restore and build, so use that established source lane for the full gate without suppressing diagnostics.
- The first maintained broad run built warning-free and passed all focused gates, but eight domain cases exposed missing dictionary-key support in the existing fallback converter. Added known-key round trips while preserving fail-closed rejection of unknown/undefined dictionary keys to prevent multiple facts collapsing onto Unknown. Added coverage across all 70 enums. Future scalar values still degrade to Unknown.
- Review corrections preserve the numeric writer for AgentInteractionContextInspectionStatus through an internal numeric adapter; all other migrated enums retain their by-name encodings. Empty supplied identities are rejected, null metadata still generates canonical IDs, and tenant writes validate supplied metadata before dependency reads. The coordination serializer now installs the fallback factory. Accordion expansion is retained in component state; a collapse/edit regression guards against reopening on parent renders. Original ordinals are captured in an embedded 70-enum snapshot from the canonical baseline.

## Review Triage Log

- R1 — high / patch: the context-inspection enum previously had a numeric default writer. Preserve its numeric output with an internal tolerant converter and keep its existing ordinals; prospective readers accept both representations.
- R2 — medium / patch: empty supplied IDs bypassed canonical validation through the inherited absent-value fallback. Generate only for null; include empty IDs in all six write-family cases.
- R3 — medium / patch: tenant enablement consulted platform state before checking supplied identities. Validate immediately after authorization; a throwing-store regression requires zero store/stream/dispatch access.
- R4 — medium / patch: the coordinator's options-level string converter overrides type attributes. Register the fallback factory first and exercise create/update source events with a future configuration name plus unchanged/wrong-terms controls.
- R5 — medium / patch: constant Expanded parameters can overwrite a user's collapse on a parent render. Bind each item to component state and cover collapse followed by draft editing.
- R6 — low / patch: IsPublic omits externally visible nested types. Use IsVisible in public enum and Operations-root discovery; future nested types cannot evade the inventory.
- R7 — medium / patch: current-value round trips do not detect ordinal reassignment. Embed baseline member/ordinal pairs and assert all 70 enums against that independent snapshot, including the newly migrated enums.
- R8 — medium / patch: the lifecycle regression seeded Draft after disable. Apply actual AgentCreated/AgentDisabled events through AgentSetupProjectionHandler and assert serialized store end-state plus the public disabled view.
- R9 — medium / patch (focused recheck): the numeric adapter and existing fallback did not preserve explicitly configured enum naming policies. Delegate known name encoding/decoding and dictionary keys to the configured string converter while retaining strict numeric and comma-list rejection. All 70 enums are compared against the original string converter under SnakeCaseLower and an independent dictionary-key policy; the 1,970-case Contracts suite passes.

- Final verification: `pwsh -NoProfile -File eng/verify-story-5.2.ps1` passes its warning-free source build, every focused/composition gate and all five full projects: Contracts 1,970; Client 6; Domain 1,017; Server 1,491; UI 1,171 (5,655 total), with zero errors/failures/skips/not-run. No build or audit gate was weakened. The canonical baseline and all submodule pointers are preserved.
- Independent blind review and two focused serializer rechecks are complete; all nine findings are patched, with no remaining concrete defects and no new deferrals. Exactly DW-4, DW-13, DW-14, DW-18 and DW-23 are closed. Fetch reports zero main/origin divergence; commit and push are authorized.
