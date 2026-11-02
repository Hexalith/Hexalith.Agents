---
title: '5.4 Complete Conversations internal SDK catalog'
type: 'feature'
created: '2026-11-02'
status: 'done'
route: 'dispatch'
human_approval: 'authorized prerequisite engineering; owner replacement acceptance remains pending'
baseline_commit: '7c77a41e8bcdcb00fa1bcb070e19027ad0a98a25'
review_loop_iteration: 0
context:
  - '/home/administrator/projects/hexalith/conversations/AGENTS.md'
  - '/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/story-5-4-conversations-catalog-code-map-2026-11-02.md'
  - '/home/administrator/projects/hexalith/agents/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/agents/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
---

<frozen-after-approval reason="user authorized the remaining prerequisite implementation and will validate the concrete delivery">

## Intent

Complete the Conversations-owned compiled internal route catalogue and canonical-intent adapters so the real Platform source composition can progress beyond the missing EventStoreExtensions refusal. The user already authorized this engineering; do not ask for another general approval. Preserve the existing runtime authority and retry behavior, and prepare actual source/test evidence for later owner validation.

## Boundaries & Constraints

Use the existing EventStore catalogue/SDK and tool-selected source/Debug graph. Preserve unrelated edits. Enroll only actual receivers: 13 aggregate commands, seven SDK query handlers and both legacy/named projections. Preserve singular domain `conversation` and plural ordinary-query domain `conversations`, real wire aliases, delegated-query requirements, app `conversation` and resource `conversations`.

All new operation metadata is internal/workload-only: `CredentialKind=workload`, `PlatformAdmission=null`, existing `domain-service:process/query/project` constants and `Ingress=internal`. Existing owner checks remain authoritative; do not invent business permissions or publish new public REST/MCP operations. Preserve generated-field exclusions and every genuinely semantic occurrence/provenance/approval/delivery field. Actual runtime availability and independent installations are unchanged.

Do not introduce a module AppHost, raw persistence, a catalogue framework, invented authority/acceptance/profile hashes, live dependency consumption, deployment, credential grants or renewal, publication, commits or pushes. The separate OQ receiver is excluded. Contracts cannot reference Domain; do not clone wrapper DTOs or export their inner DTO as the actual wire. Unknown/missing selected owner types or unsupported SDK APIs must refuse.

## I/O & Edge-Case Matrix

| Case | Required outcome |
| --- | --- |
| Actual receivers | Complete exact 13-command/7-query/2-projection inventory, schemas, digests and finite receiver methods agree with SDK discovery |
| Wrapper schemas | Describe actual Domain wrapper payloads, including their outer fields; absent or foreign selected owner assembly refuses |
| Retry/conflict | Existing canonical semantics are reused; excluded generated event/correlation fields do not change logical intent; meaningful target/actor/content/provenance/approval/delivery changes do |
| Transport/business boundary | Internal catalogue grants no public/generated operation or business authority; delegated queries and current source/identity/approval checks remain intact |
| Source integration | Real catalogue generation and full Platform source composition use the mapped owner; missing/changed adapters or schemas refuse before resources |

</frozen-after-approval>

## Code Map

The required exact owner symbols, wire inventory and SDK patterns are in the implementation map loaded through `context:`. Reuse those paths and the actual domain fingerprints. The existing Platform reader compiles the mapped Contracts and EventStoreExtensions projects; preserve that source selection and fail-closed behavior.

## Tasks & Acceptance

- [x] Add complete Contracts declarations and real wrapper schemas with fail-closed owner resolution.
- [x] Add the compiled adapter project and canonical policies matching existing domain semantics; add it to `.slnx` and any necessary build/package inventory. Prepare packaging consistently if required; publish nothing and claim no released adapter image/package.
- [x] Enroll the catalogue and legacy projection through existing SDK registration.
- [x] Add/run meaningful owner tests for every matrix row, actual SDK receiver capture and serialized semantic retry/conflict cases; retain all failures and exact source/runtime provenance.
- [x] Exercise real Platform catalogue generation/full source composition with complete explicit configuration; retain negative boundaries and no Live/availability claims. Record exact remaining independent inputs separately.

Given the selected source graph, when SDK/catalogue discovery and Platform generation run, then every actual receiver and adapter agrees and full source composition progresses beyond the missing adapter. Given absent/foreign owner input or unavailable runtime authority, when the same path is invoked, then it fails closed without inventing authority or availability.

## Implementation Notes

The previous partial preparation stays preserved. Separate host investigation identified an unselected installed-image/native-loader measurement contract in `execution_profile.py`, in addition to the missing launched owning-process caller; this catalogue work does not bypass either gate.

## Spec Change Log

## Review Triage Log

All three fresh context-free review layers were launched before collection; every final report was collected before triage. The review diff is `/tmp/story-5-4-catalog-review-20261102/change.diff`, with owning roots, baselines and unrelated exclusions in `scope.json`. Every finding is recorded separately below. The blind hunter's finding-floor arithmetic is not a defect.

| ID | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH-1 | medium | patch | The new list/detail record constructor parameters have no defaults; the retained exported schema marks them required, while actual handlers accept omitted fields. Add defaults and verify actual schema optionality. |
| BH-2 | medium | patch | `ConversationDomainQueryHandlerBase` uses strict number handling and case-sensitive camel-case names, whereas the new exporter uses SDK Web options. Export the actual owner settings and assert representative request compatibility. |
| BH-3 | medium | patch | The SDK exporter deliberately returns unconstrained nodes for custom converters. The existing schema transform hook can express the owner's actual scalar converter wire contracts; use it rather than publishing `true` for identifiers and schema versions. |
| BH-4 | medium | patch | Delegated metadata receivers require an empty object. The generated nullable root still accepts null after only setting `additionalProperties=false`; close the root type too. |
| BH-5 | medium | patch | Reader receipt inventory binds Contracts/adapters/SDK Contracts but omits the newly used Domain assembly. Include actual loaded reader-output dependency assemblies and assert the Domain hash. This remains source evidence, not installed loader measurement. |
| BH-6 | medium | patch | Both real-reader negative requests select the Tenants owner. Add Conversations-owner missing/foreign adapter cases to cover its selected Domain/schema-loading refusal. |
| BH-7 | false | reject | The final directory README, added after the review diff snapshot, contains the exact producer preparation, combined mapping, `ConversationsCatalogSourceVerification=true`, explicit inputs and focused execution commands. Thus the claimed absence of any documented invocation no longer occurs. Normal-CI coverage is handled separately by VG-1. |
| BH-8 | medium | patch | The posting preservation assertion computes its expected value using the newly extracted helper also used by production. A fixed vector verified against the unchanged pre-extraction source will detect protocol drift. |
| BH-9 | low | patch | `_release_asset_digests` still reports eight expected assets although the executable inventory now has five package/symbol pairs. Derive the diagnostic count from its existing expected set. |
| BH-10 | low | reject | The duplicate reports and source snapshots are deliberate retained attempt/provenance artifacts, with completed results distinguished from earlier snapshots. Their small repository cost establishes no incorrect outcome; consolidating them would require changing retained evidence references and provides no useful boundary correction for this delivery. Preserve the original attempts. |
| EC-1 | medium | patch | Same demonstrated schema gap as BH-3: custom identifier/version converters export unconstrained shapes. Use the existing owner schema transform hook. |
| EC-2 | medium | patch | Same optional-constructor schema mismatch as BH-1; omitted list/detail fields supported by receivers must remain supported by the catalogue. |
| EC-3 | medium | patch | Null nested command/metadata values reach direct semantic dereferences. The gateway treats their NullReferenceException as retryable unavailable rather than malformed input; add explicit argument guards for the demonstrated cases. |
| VG-1 | medium | patch | Pre-verified: the normal `tests/catalog-builder/verify_catalog_builder.py` lane supplies valid adapters only; deleting the new inventory validation would still pass it. Extend its existing compiled fixtures with invalid selection and no-output assertions. |
| VG-2 | medium | patch | Pre-verified: current composition helpers leave bootstrap disabled and check only the first production source line; the new Platform test executes the gateway Program only. Execute the actual Conversations Program with controlled bootstrap and assert its installed observation. |
| PP-1 | medium | patch | Parent inspection found that the first normal-CI correction indexed `owners[1]`, while `--owners` supports a single-owner fixture. Use an existing foreign assembly fallback when no second owner exists; one-owner and multi-owner selection checks pass. |
| PP-2 | medium | patch | The serialized fixture reproduced NullReferenceException for null post MessageId/AuthorPartyId before semantic projection. Add exactly those two argument guards. The ordinary null actor case already throws ArgumentNullException in its constructor and requires no new guard; retain the repro and regression. |

BH-1/EC-2 and BH-3/EC-1 share corrections. The other surviving findings have distinct verified outcomes. All corrections are private guards, schema export settings, receipt inventory or focused verification; no new public operation, authority, acceptance or availability is authorized.

All surviving corrections are applied and verified. The final parent runs pass 1,586 Server, 648 Contracts and 189 Domain cases; release tooling passes 28 tests plus three subtests. All 38 Conversations catalogue/enrollment cases pass, including actual owner and gateway Program observations, selected schema-owner/adapter refusals and the expanded dependency receipt. Final-rerun source and complete runtime inventories match before/after: 38 source files and 626/227/214/862 files across the Server/Domain/Contracts/Platform outputs. Pre-implementation binary bytes remain unavailable and are not inferred.

The broader eight-case default Platform regression selection adds five passes and three restore failures: the current default graph resolves Tenants dependencies requiring EventStore 4.0.0 and McpCli 2.0.0 against centrally pinned 3.133.0 and 1.1.0. These NU1109 failures precede reader compilation/admission. The raw parent XML and retained restore logs identify the affected methods. This is recorded separately; no downgrade suppression, silent source-selection change or full-story completion is claimed. The published Release/NuGet lane was not run locally.

Final evidence is `../conversations/docs/implementation/ext-conv-ai-1-internal-catalog-2026-11-02/parent-final-20261102T195034Z-c2fdcdb8c402476d82c8402242adaf8f/`. The parent is authorized by the user's existing push instruction to record and push these reviewed task changes. Unrelated Story 16.1 work and dependency working-tree differences remain excluded. This spec completes the bounded catalogue work only; Story 5.4 and all installed/accepted/Live delivery gates remain incomplete.

## Verification

Use fresh Debug/source outputs from the producer's full build-preparation environment. Build individual affected test projects, execute built xUnit assemblies with exact `-class`/`-method` selections, then verify actual Platform generation/composition. Retain command intervals, failed attempts and before/after source/runtime hashes. No unavailable dependency transport or Live/OQ verifier may run.
