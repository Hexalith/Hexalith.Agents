---
title: '5.4 Prepare owner deliveries for validation'
type: 'feature'
created: '2026-11-02'
status: 'done'
route: 'dispatch'
human_approval: 'accepted for preparation; commitment and Live acceptance pending'
baseline_commit: 'b9e106155bb056d2a6a3677542a1b9cd53c30392'
review_loop_iteration: 0
context:
  - '/home/administrator/projects/hexalith/agents/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/external-dependency-register.md'
---

<frozen-after-approval reason="user authorized preparation now and will validate the concrete result">

## Intent

Prepare the missing Story 5.4 owner deliveries for human validation. Close the verified ordinary Conversations source-composition gap, record the actual nominated business approver, and replace stale protection/source descriptions with current evidence. Produce concrete next implementation and installation actions while preserving the complete qualification contracts.

The user nominated `jpiquot` as the person who should issue business approval evidence. An authenticated Teams directory lookup resolves Jerome Piquot, `jpiquot@itaneo.com`, directory object `b054ec7b-1145-4e87-a50d-112b82c154f4`. This establishes a nomination and directory identity only, not an enrolled OIDC subject, Party/Human binding, signed approval or independent service.

## Boundaries & Constraints

Preserve unrelated concurrent edits in every repository. Work in the owning sibling Platform checkout. Use source/Debug builds and individually executed focused tests. Ordinary Conversations composition must retain its real declaration identities and explicit configuration. The OQ receiver remains a separately installed, bounded component.

Never fabricate installed identities, acceptance, dates, profile hashes or Live success. Do not reuse expired operational profiles, create approval evidence for the nominated human, renew credentials, deploy placeholders, run unavailable consuming seams, reserve a cohort, change register availability, weaken the production static-ELF launch boundary, stage, commit or push. Existing installation authorization remains valid but cannot supply absent production providers or independent evidence.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected behavior |
| --- | --- | --- |
| Ordinary source enrollment | Real Conversations declaration, owner mapping and complete explicit configuration | Resolve only `conversations/domain` to its actual server; app `conversation`, resource `conversations` |
| Missing/foreign source or configuration | Absent owner mapping/project, physically foreign project or missing declared settings | Refuse before resource composition |
| Package and receiver boundaries | No accepted published Conversations image; ordinary declaration | Package image remains unavailable; no conformance receiver becomes an ordinary workload |
| Nominated approver | User supplies `jpiquot`; directory lookup matches | Record nomination separately; installed issuer/trust/current-role evidence remains unestablished |
| Protection and Host evidence | Loop-10 source, no accepted installed profile; static-ELF-only release protocol | Describe current tested scope and remaining implementation/installation requirements; no replacement or Live claim |

</frozen-after-approval>

## Code Map

- `../platform/src/Hexalith.Platform.Composition/PlatformServerBindings.cs`: add the actual owner project to `ReferenceProjects`; do not add an unverified image or change defaults.
- `../platform/tests/Hexalith.Platform.Composition.Tests/PartiesAdoptionTests.cs`, `DomainEnrollmentBindingTests.cs`, `PlatformOwnerFixture.cs`: reuse real owner/declaration/source containment and explicit-configuration tests.
- `../conversations/module/hexalith.module-manifest.v2.json` and its selected declaration: authoritative ordinary server identity, dependencies, readiness and configuration.
- `../platform/docs/implementation/story-5-4-coupled-delivery-2026-10-25/owner-decisions.json`: append a narrowly named business-approver nomination under `mvpOperationalInputs.independentDecisionAuthority`; retain null installed-evidence fields and all unrelated bytes/values.
- `../platform/docs/implementation/ext-protection-1-loop10/deferred-rows-af01b493fa91/README.md`: current 96 Platform/370 EventStore focused results; explicitly missing fresh native and production unissued-authority coverage.
- `../platform/src/Hexalith.Platform.Composition/AcceptedLaunchArtifactLease.cs`, `AgentsHostComposition.cs`: static ELF closure and missing owning-process caller; a .NET startup hook cannot satisfy that production protocol.

## Tasks & Acceptance

- [x] `../platform/src/Hexalith.Platform.Composition/PlatformServerBindings.cs`: enroll the real Conversations domain project.
- [x] `../platform/tests/Hexalith.Platform.Composition.Tests/ConversationsEnrollmentTests.cs`: exercise real declaration/source/config success and failure boundaries; run the focused suite and retain logs/XML.
- [x] Platform owner form: record the user nomination and directory-lookup provenance without granting roles or changing evidence/availability fields.
- [x] `../platform/docs/implementation/story-5-4-owner-validation-2026-11-02/`: prepare a current validation document and machine-readable delivery status with exact source/evidence hashes, commands, remaining code gaps, installed prerequisites, and installation order. Keep incomplete targets/dates/hashes explicitly absent; do not submit an incomplete replacement.

Given the real mapped source and configuration, when ordinary Conversations enrollment resolves, then it selects the owning project with the exact declaration identities. Given missing production inputs, when the validation package is prepared, then it states the exact remaining work and leaves all runtime, acceptance and availability gates unchanged.

## Implementation Notes

User authorization: "do those actions and I will validate"; subsequent nomination: "jpiquot". No intermediate implementation approval is required. Full external dependency acceptance and the single Live cohort remain separate later steps.

The scoped Platform implementation passes its fresh Debug/source build with zero warnings/errors, 37 focused cases and 30 related regression cases. The original failed full-composition attempt is retained: the Conversations module lacks its owning compiled catalog declarations/adapter. Enrollment resolution succeeds; full composition remains restrictive. The broad regression selection initially failed legacy root discovery under an external runtime path, then passed with the identical 852-file closure beneath ignored Platform output. Both results are retained.

Read-only installed Keycloak discovery and exact nominated UPN/email lookup establish issuer `https://auth.tache.ai/realms/tache` and subject `5eec1154-ae14-4041-8c08-3c51df8e5550` as a nomination candidate only. The account is enabled/email-verified and Microsoft-federated; observed realm roles do not establish business approval authority. The full lookup receipt retains a later optional master-realm 403. No role, realm, credential, register, sprint or availability change occurred.

The validation package is at `../platform/docs/implementation/story-5-4-owner-validation-2026-11-02/README.md`. It states the unimplemented catalog inventory/adapter, launched owning-process integration, fresh native/production-authority coverage and actual independent installation prerequisites. Source prep is partial; no complete replacement, receiver installation, native run or Live/OQ attempt is claimed. The pinned disposable-native-test image was acquired locally; its presence is not protection qualification. The post-edit Aspire observation refused pre-existing nested dependency state before resources and preserved the unrelated running EventStore application.

## Spec Change Log

## Review Triage Log

Review 1 collected all three independent layers before triage. The verification-gap reviewer returned no findings. Every blind-hunter and edge-case finding is recorded below; the finding-floor arithmetic is not a defect. All changes route to direct source-test or evidence corrections, with no acceptance/runtime-scope change.

| ID | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH-1 | medium | patch | The new missing-catalog test uses the actual owner checkout, so completing the documented adapter would break it. Use a copied owner fixture with that adapter deliberately absent. |
| BH-2 | low | patch | Existing `PlatformLocalProfile.Bind` intentionally generates a local secret for an explicitly supplied empty `secret` input; this mapping introduces no generator or production authority. Clarify that inherited local behavior so the preparation's explicit-configuration claim cannot be mistaken for enrolled credentials. No shared-binder change is justified. |
| BH-3 | medium | patch | All substantive package checks use `assert`; Python optimization removes them while the script still prints Passed. Replace assertions with explicit failing checks. |
| BH-4 | medium | patch | The verifier derives its current repository path but indexes historical absolute baseline keys. Identical relocated sibling checkouts therefore raise KeyError. Compare stable repository-relative identities while retaining the original observation. |
| BH-5 | medium | patch | Relocation equality output is accepted from booleans; only a before manifest is retained. Preserve and compare the original, relocated-before and relocated-after inventories rather than relabeling a recorded assertion as an independent recheck. |
| BH-6 | low | patch | The package verifier hashes listed artifacts but does not check that their set equals the retained package. Reject extra/unlisted members, respecting the inventory's explicit self-exclusion. |
| BH-7 | low | patch | XML checks accept the first assembly and aggregate counts without checking not-run/per-case results or intended classes. Verify the complete retained single-assembly selections before printing verified counts. |
| BH-8 | medium | patch | The retained identity receipt's aggregate HTTP 403 omits the failing stage. Parent execution established that it followed successful tache reads, but the artifact alone cannot reproduce that attribution. Retain it and collect an explicitly staged exact-subject tache observation; preserve its nomination-only authority boundary. |
| BH-9 | low | patch | The native recipe stores projectReferences=true and root fields without the corresponding executable property arguments. Include the explicit source-reference switch and actual root properties. No native success is inferred. |
| BH-10 | medium | patch | Captured baseline hashes cover the binding/owner form and Agents files, while the broader source list is recorded after building from mutable shared roots. The new test correction requires a fresh build anyway: capture/recheck its actual relevant source/build input closure before and after the final build/test run, retaining previous attempts. |
| EC-1 | medium | patch | Same root cause as BH-4: current absolute path lookup cannot match a moved checkout's historical baseline key. Preserve observation metadata and use stable relative identities in the verifier. |

BH-4 and EC-1 share one correction; every other finding has a distinct recorded outcome. Corrections preserve the source mapping, real declaration identities, 37-case scope, related regression coverage, nomination-only form change and full unavailable-dependency gates.

All direct corrections are applied. The absent-adapter test now uses an isolated owner fixture, and the README identifies inherited local empty-secret behavior. The package verifier uses explicit checks under normal and optimized Python, stable relative baseline identities, exact artifact membership, complete test selections/results and actual retained manifest comparisons. The native recipe has explicit source/root arguments. The staged exact-subject identity receipt preserves successful per-operation outcomes and a hash of its executed capture source while retaining the old aggregate-403 receipt unchanged.

The final fresh Debug/source build and 37 focused cases pass without warning/analyzer/source-selection suppression. Its 4,054 raw input records (4,053 distinct source/build files across 35 projects) match before and after; four 852-file runtime manifests match. The 30 earlier related regression cases remain retained with their exact historical graph/equality limitation stated explicitly. Twenty-two copied-bundle positive/tamper checks pass in normal and optimized modes. Parent normal and `python3 -O` package rechecks both pass; whitespace checks pass. The corrected scoped diff is `/tmp/story-5-4-owner-delivery-reviewed-final.patch`.

Preparation remains in review for the user's validation; full owner delivery and Story 5.4 are not complete. No complete replacement acceptance, receiver installation, native/Live/OQ attempt, authority grant or availability change is claimed. The installed Live manifest remains absent and the attempt journal contains zero files. Agents register/sprint hashes equal the pushed baseline. Task changes remain unstaged/uncommitted, preserving the preparation constraints and unrelated staged Platform work.

## Verification

Build the affected Platform Composition test project in Debug with the repository's existing source mapping, then execute the built test assembly directly with the `ConversationsEnrollmentTests` class selection. Retain any broader environmental failure separately. Check JSON, local links, exact nomination-only owner-form delta and unchanged Agents register/sprint status.

## Subsequent continuation

This bounded preparation phase is implemented, reviewed and verified. Its earlier unstaged/in-review statements describe the capture at that time; parallel Platform commits subsequently recorded the preparation. The user has already requested pushing all task changes and authorized continuing the implementation before validation, so the parent will record and push the reviewed task work in its owning repositories. No additional general permission is required.

The missing catalogue was subsequently implemented and reviewed under [the separate catalogue spec](spec-5-4-complete-conversations-internal-catalog.md). Its evidence supersedes that source blocker only. The earlier sealed preparation package remains historical evidence; it is not relabelled as a current complete delivery. Full Story 5.4, independent commitment/installation, replacement acceptance and Live qualification remain incomplete, with availability and register/sprint status unchanged.
