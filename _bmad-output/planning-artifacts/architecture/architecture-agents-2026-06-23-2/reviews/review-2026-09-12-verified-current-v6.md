# Verified-Current Reviewer Gate — 2026-09-12 v6

**Lens:** configured BMad Architecture verified-current reviewer  
**Target:** `ARCHITECTURE-SPINE.md`  
**Review date:** 2026-09-12  
**Verdict:** **CHANGES REQUIRED** — the spine's technology, exact parent-repository authority, authoritative PRD/register reconciliation, and current-versus-target classification are verified, and both v5 High findings were corrected. The active delivery map nevertheless still lacks an executable implementation/evidence path for the binding distributed safety-rescan design and omits an authoritative human-identity dependency from three governance/compliance consumers.

**Finding count:** 0 critical · 2 high · 2 medium · 0 low

## Reviewed Snapshot And Method

The frozen artifact snapshot was identified by:

- spine SHA-256 `e3fc24de16eb03449eaaf3a1b495dfbb97e3e2915f696ba32817bf8fb739ce6a`;
- authoritative validation report SHA-256 `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`;
- bound PRD SHA-256 `aeac1bd0bff7abe009e6388d90da67e22bf70b50b04d6805f042241f372d0924`;
- replacement epics SHA-256 `311b011c762c8482492701c10b0d844cf6747358b08799a702dd596550da796b`;
- external-dependency register SHA-256 `89096ef5d57c315dbe87b4bdfda5a76250b7295f890467068168d73c4c4fe6e5`; and
- launch-readiness register SHA-256 `f49a2450994bed0621ab2c78ecc425ec04f4282bd50c927922dac436f51de644`.

The gate reread the complete spine, authoritative validation report, bound PRD, active Epics 5–8 and their cross-cutting delivery rules, both registers, root manifests and solution, current source/public surface, exact parent gitlink objects, and dirty submodule working-tree state. Named versions and ecosystem-status claims were checked against repository objects and official primary sources; every URL cited in the spine frontmatter returned HTTP 200. The architecture linter returned `ok: true` with zero findings. No restore, build, or test was run because this reviewer was authorized to modify only this review.

## Critical Findings

None.

## High Findings

### VC6-H1 — The delivery map names the safety-rescan owners but gives them no executable rescan contract

**Evidence.** PRD FR-27 (`prd.md:708`) requires policy publication and per-tenant `DigestKey` rotation to invalidate cached verdicts, start a bounded background re-scan, block calls from stale verdicts, and return exactly `ContextReadUnavailable(RescanPending)` while a Conversation remains pending. AD-20 (`ARCHITECTURE-SPINE.md:318`) makes that requirement implementable through durable `SafetyVerdictEpoch`/`SafetyVerdictIndex`, a frozen Conversation-enumeration checkpoint, a monotonically fenced cross-replica coordinator lease, versioned positive worker/batch/lease/wait bounds, pending activation, crash recovery, and exact result vocabulary. The spine correctly classifies those absent capabilities as implementation debt owned by Stories 6.3 and 8.4 (`ARCHITECTURE-SPINE.md:942`), and the authoritative live-seam register assigns those stories `SafetyVerdictRescanRecoveryLiveTests` (`launch-readiness-register.md:256`) plus the `safety-verdict-status` projection (`:339`) and LR-SAFETY recovery/bounds evidence (`:368`).

The active epics mention `SafetyVerdictEpoch`, `SafetyVerdictIndex`, and `RescanPending` only in the aggregate inventory at line 139. Story 6.3's criteria and evidence (`epics.md:1936-1973`) cover two-stage decisions and no-weaker retry but contain no epoch, index, invalidation, pending activation, Conversation enumeration, bounded cross-replica worker, fenced lease, recovery, `RescanPending`, or required rescan-live test; its dependency and result rows also omit `EXT-SECRETS-1`. Story 8.4 (`epics.md:2887-2934`) publishes policies and high-water marks but likewise contains no activation/rescan protocol, declares “No new external commitment,” omits `EXT-SECRETS-1`, and names no rescan recovery evidence. This contradicts the external register's explicit `EXT-SECRETS-1` consumer mapping for both stories (`external-dependency-register.md:185-191`) and its rotation-triggered invalidation contract.

**Failure mode and impact.** A delivery agent can complete both named owner stories while activating a policy or rotated digest key before the tenant's durable invalidation epoch exists, reuse stale cached verdicts across replicas, or collapse an in-progress rescan into a generic context failure. The architecture would still describe the safe target, but the executable delivery/evidence authority would permit the exact stale-verdict and restart race the target is intended to prevent.

**Required correction.** Reconcile Stories 6.3 and 8.4 in place. Allocate the complete AD-20 protocol between them without adding a story: durable epoch/index and projection ownership; platform-cohort and tenant activation barriers; digest-key-rotation invalidation; frozen enumeration checkpoint; positive readiness-profile bounds; fenced coordinator/worker recovery; exact `RescanPending` public/metric result; and `SafetyVerdictRescanRecoveryLiveTests`. Add `EXT-SECRETS-1` to both dependency/evidence/result gates. This is delivery-map correction for unimplemented target architecture, not a claim that the current code already provides the capability.

### VC6-H2 — Stories 8.1, 8.2, and 8.8 can claim governance/compliance evidence without their authoritative human-identity seam

**Evidence.** `EXT-PARTIES-1` is the authoritative human/liveness and historical Party-to-`AuthenticatedHumanActorId` binding seam. Its exact consumer row names Stories 8.1, 8.2, and 8.8 in addition to 5.2, 5.4, and 6.6, and expressly forbids any consumer from citing either branch-compatible development or the human actor-binding seam as launch evidence while the record is `Uncommitted` (`external-dependency-register.md:105-119`). The active epics' global dependency rule truncates the development allowance to Stories 5.2, 5.4, and 6.6 (`epics.md:167`). Story 8.1's dependency/evidence/result rows (`epics.md:2703-2752`) require only protection and secrets while accepting Compliance Inspector submission plus distinct second-party release approval. Story 8.2 (`epics.md:2754-2821`) similarly requires second-party approval and key delivery to an authorized human but omits `EXT-PARTIES-1`. Story 8.8 (`epics.md:3111-3160`) calculates human subject sets, anti-collusion, and second-party review yet omits the record from requirements, dependencies, and its Level 4/5 result gate.

**Failure mode and impact.** The stories can be read as permitting Level 4/5 evidence from role/Party assertions that cannot be bound to the authenticated human or reproduced historically. That defeats separation-of-duty, anti-self-approval, and audit attribution on legal-hold release, export approval/key delivery, and protected audit inspection. A prior generic “trusted principals” story reference is insufficient where the dependency register deliberately identifies each evidence-producing consumer and keeps the seam Uncommitted.

**Required correction.** Add `EXT-PARTIES-1` to the dependency, requirements/evidence, negative-evidence, and current blocker rows of Stories 8.1, 8.2, and 8.8; require current liveness plus historical actor-binding/version evidence for each human approval/review transition. Expand the global development-allowance sentence to the exact authoritative six-story consumer set while preserving its fail-closed rule: Branch-B-compatible development is not launch evidence, and no branch or human actor-binding claim is qualifying until the record is Available with its compatibility command passing.

## Medium Findings

### VC6-M1 — Two named evidence artifacts still encode the superseded operation-matrix version

Story 5.5 correctly requires `OperationGateMatrixVersion 4` in its acceptance criteria (`epics.md:1509-1512`), but its evidence row still names `OperationGateMatrixV3ParityTests` (`:1537`). Story 5.7 likewise evaluates version 4 (`:1617-1620`) while naming `OperationGateMatrixV3ActivationTests` (`:1650`). Rename/rewrite those artifacts as version-4 tests and cover v4's target-aware bootstrap/repair, split kill-switch, replay-admission, governance variants, scope, and producer rules. Leaving the names unchanged creates avoidable ambiguity about which fixture is completion evidence, though the binding acceptance criteria themselves are correct.

### VC6-M2 — Two asserted reviewer sources are still absent from the frozen tree

The spine frontmatter cites `reviews/review-2026-09-12-security-data-integrity-v3.md` and `reviews/review-2026-09-12-brownfield-drift-v3.md`; neither file exists in the reviewed snapshot. All other local source/companion paths resolve after stripping their explicit parenthetical supersession annotations, and every cited external URL returned HTTP 200. Generate the two named reports before retaining the citations, or replace/remove the citations so the source chain is reproducible.

## Low Findings

None.

## Technology, Version, And Repository Verification

| Spine claim | Primary or repository evidence | Result |
| --- | --- | --- |
| .NET SDK `10.0.401` with `latestPatch`; `net10.0`; C# 14; `.slnx`; Central Package Management | Root `global.json`, `Directory.Build.props`, `Hexalith.Agents.slnx`, and `Directory.Packages.props` match; installed `dotnet --version` is `10.0.401`. The official [.NET 10.0.12 release](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md) lists SDK `10.0.401`/`10.0.112` and CVE-2026-69522 in the 2026-09-08 bundle. Every root-declared sibling with a `global.json` is pinned to `10.0.400` at its exact parent gitlink. | **Confirmed.** The spine makes no unsupported build-success claim. |
| Parent-authoritative Hexalith revisions | Root commit `46936c9e63ca` records Builds `a32cb4227493`, Conversations `73bcee6f0447`, EventStore `ce9e779a3ec2`, FrontComposer `053b2008307d`, Parties `fa42398552fb`, and Tenants `2fac18396ff1`; each matches the spine. | **Confirmed.** |
| Dirty submodules are separate from architecture authority | Current working-tree HEADs are Builds `cf52f74c983b`, Conversations `64b050831eea`, EventStore `a568af4ec963`, FrontComposer `1b3608c9b039`, and Memories `42dfa26b23de`; Parties and Tenants match their gitlinks. | **Confirmed.** The spine uses exact parent gitlinks and does not elevate dirty checkout state. |
| Current Dapr exposure and versions | Exact EventStore gitlink `ce9e779a` directly references `Dapr.Client` in Client and `Dapr.AspNetCore` in DomainService; Agents references those projects/packages and calls `AddDaprClient()`. Exact parent Builds `a32cb422` pins the Dapr .NET family to `1.18.5`; dirty Builds pins `1.18.7`. No Agents project references `Dapr.Workflow`. Official NuGet indexes list `1.18.7` as the newest stable before 1.19 previews for [Client](https://api.nuget.org/v3-flatcontainer/dapr.client/index.json), [ASP.NET](https://api.nuget.org/v3-flatcontainer/dapr.aspnetcore/index.json), and [Workflow](https://api.nuget.org/v3-flatcontainer/dapr.workflow/index.json); the official [servicing commit](https://github.com/dapr/dotnet-sdk/commit/deb05064a4eb4530b9f479ed6e0c6e16ebcc2440) carries the security-fix title cited by prior review. | **Confirmed.** Existing Client/ASP.NET exposure, future Workflow adoption, and non-authoritative dirty remediation are distinguished accurately. |
| Dapr Workflow and optional Agent Framework boundary | Official [Dapr Workflow concepts](https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-features-concepts/) are compatible with AD-18's stricter instance/recovery policy. Microsoft's official [Agent Framework 1.0 announcement](https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0/) and official [Dapr Agents documentation](https://docs.dapr.io/developing-ai/dapr-agents/) support the GA maturity statement. No Provider or Agent Framework SDK is referenced in Agents. | **Confirmed unselected.** Deferral is a commitment choice, not a maturity claim. |
| MediatR `14.2.0`, FluentValidation `12.1.1`, OpenTelemetry `1.18.0` | Exact parent Builds pins those versions; official indexes for [MediatR](https://api.nuget.org/v3-flatcontainer/mediatr/index.json), [FluentValidation](https://api.nuget.org/v3-flatcontainer/fluentvalidation/index.json), and [OpenTelemetry](https://api.nuget.org/v3-flatcontainer/opentelemetry/index.json) match the current stable versions. Exact EventStore graph inspection confirms MediatR is outside the Client/DomainService path while OpenTelemetry arrives through the referenced ServiceDefaults project. | **Confirmed.** |
| Fluent UI Blazor `5.0.0-rc.5-26219.1` | Root and exact parent catalog pin the same build; the official [NuGet index](https://api.nuget.org/v3-flatcontainer/microsoft.fluentui.aspnetcore.components/index.json) still lists it as the newest v5 prerelease. | **Confirmed.** `ARCH-A-8` correctly keeps the production-risk decision open. |
| Root-versus-catalog test stack | Root pins Test SDK `18.6.0`, xUnit `3.2.2`, runner `3.1.5`, Shouldly `4.3.0`, NSubstitute `5.3.0`, and bUnit `2.9.0`. Exact parent Builds pins Test SDK `18.9.0`, xUnit/runner `4.0.0`, Shouldly `4.3.0`, NSubstitute `6.2.0`, and bUnit `2.9.0`; dirty Builds has advanced Test SDK to `18.10.0` and bUnit to `2.10.3`. Official package indexes confirm the catalog/latest relationships. Root `global.json` still has no `test.runner`. | **Confirmed and accurately deferred** as implementation/build debt. |
| Hosting topology | `EXT-HOST-1` remains Uncommitted. The solution has no module-owned AppHost, Aspire, or ServiceDefaults project; the current Server uses the shared EventStore DomainService host. | **Confirmed.** |
| Dapr Conversation API and export standards | Official [Dapr Conversation API documentation](https://docs.dapr.io/reference/api/conversation_api/) still labels that API alpha. [RFC 8785](https://www.rfc-editor.org/rfc/rfc8785), [RFC 7515](https://www.rfc-editor.org/rfc/rfc7515), and [RFC 7518](https://www.rfc-editor.org/rfc/rfc7518) exist and remain the selected canonicalization/signature standards. | **Confirmed.** Provider/storage lifecycle decisions remain separately open. |

## v5 Correction And Prior-Finding Closure Audit

- **VC5-H1 is closed.** The epics now name the complete durable aggregate inventory (`epics.md:139`), the complete Provider readiness fields, the version-4 operation matrix (`:159`), and the distinct rate/open-interaction/budget owners and decision protocols in Story 6.4. The current source still lacks those target capabilities, and the spine correctly calls that implementation debt rather than shipped behavior.
- **VC5-H2 is closed.** The global inventory now lists all twelve external records (`epics.md:167`); Stories 8.2 and 8.3 now require `EXT-EXPORT-STORE-1`, the versioned open lifecycle decisions, the shared `ProtectionFence`, partial-output recovery, all-copy/accounting inventories, and physical purge/backup/restore receipts.
- **VC5-M1 remains as VC6-M2.** The two cited v3 reviewer reports remain absent.
- **Authoritative validation report.** C-1 through C-3 and H-1 through H-12 remain closed in the spine itself. AD-13 separates transport fingerprint from safety evidence; AD-22 fixes every product-independent deletion/export safety invariant while preserving the two Product/Governance decisions as open; AD-17 and matrix v4 remove circular readiness and scope ambiguity; and the remaining High items are present as enforceable target rules, dependency commitments, explicit owner decisions, or current implementation debt. VC6-H1/H2 are propagation failures in the executable delivery map, not regressions in those AD rules.

## Bound PRD, Registers, And Current-Reality Reconciliation

- The PRD, spine, and both registers agree on the twelve external records, eighteen minimum GateIds, matrix version 4, typed target/scope semantics, open decisions, safety-rescan behavior, and hold/export/deletion safety invariants.
- `OD-HOLD-DELETION-PRECEDENCE-1`, `OD-EXPORT-LIFECYCLE-1`, `OD-DAPR-SECURITY-1`, and `OD-SPRINT-5.1-5.2-1` remain explicit and fail closed; the spine does not invent their unresolved Product/Governance/Security or delivery-history answers.
- Current source still exposes the legacy `/api/agents/operations` routes and Party link/replace vocabulary; lacks `AuthenticatedHumanActorId`, the trusted-envelope MAC/replay protocol, Dapr Workflow, an IntegrationTests project, distinct rate/open ledgers, durable safety-rescan state, the interaction outbox, protected export store, and shared protection fence; and has no `eng/verify-story-5.1.ps1` despite the tracker/story contradiction. The spine's debt table accurately identifies these as unimplemented convergence work.
- The two High findings are cross-document delivery authorization defects: the target architecture remains internally coherent, but the story contracts it names as owners can currently complete without implementing or qualifying all of that target.

## Gate Conclusion

The frozen spine is verified-current for named technologies, exact root authority, repository reality, authoritative PRD/register requirements, v5 corrections, and its separation of target architecture from implementation debt. The complete configured gate still fails with **0 Critical, 2 High, 2 Medium, and 0 Low findings**. Reconcile the existing Stories 6.3/8.4 safety-rescan contract and the Stories 8.1/8.2/8.8 Parties dependency/evidence gates, correct the two stale v3 test names, restore or remove the two absent source citations, then rerun the complete reviewer gate.
