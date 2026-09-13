# Verified-Current Reviewer Gate — 2026-09-12 v5

**Lens:** configured BMad Architecture verified-current reviewer  
**Target:** `ARCHITECTURE-SPINE.md`  
**Review date:** 2026-09-12  
**Verdict:** **CHANGES REQUIRED** — the spine's technology and repository claims are current, and its architecture is aligned with the bound PRD and authoritative registers, but the replacement epics that the spine names as its delivery map still authorize two materially superseded designs. Two cited reviewer sources are also absent from the reviewed tree.

**Finding count:** 0 critical · 2 high · 1 medium · 0 low

## Reviewed Snapshot And Method

The frozen review snapshot was identified by:

- spine SHA-256 `665611db80663133540e979332d54659bd00c96142d82b1bbdceebbc17667715`;
- PRD SHA-256 `92585e96abf285e00d123bbadceaffd87004258448db5decd7b0e6cb7e5bc4a2`;
- external-dependency register SHA-256 `2c7438b9919d078023103046dedbebfbd7d2cc7e2d52e6b5a69d2a0488ef78fc`;
- launch-readiness register SHA-256 `25b9164c084159d46f9d66aa387da4dd5f450bc5fa7eae3bfae3064d86adde50`; and
- replacement epics SHA-256 `1fe89cad7d9e0609b5bbe32e313aec303cffd962ce4c6b3d480a444e8fdbe250`.

The gate reread the complete current spine, bound PRD, both registers, replacement epics where the spine maps architecture debt to stories, root manifests, solution/projects, exact parent gitlink objects, dirty submodule working-tree heads, and current public/source surface. Named technology and version statements were checked against exact repository authority and current official primary sources. The architecture linter returned `ok: true` with zero findings. No restore, build, or test was run because this reviewer was authorized to write only this review; no architecture, PRD, register, source, manifest, or submodule was modified.

## Critical Findings

None.

## High Findings

### VC5-H1 — The executable epics still prescribe the superseded aggregate, readiness, and cost-control architecture

**Evidence.** The replacement epics' Additional Requirements name an incomplete V1 aggregate inventory at line 139, omitting the spine's `RateLimitLedger`, `OpenInteractionLedger`, `TrustedEnvelopeReplay`, `SafetyVerdictEpoch`, `SafetyVerdictIndex`, and `ProtectionFence`. Line 146 fixes an obsolete six-field `ProviderReadinessResult`, omitting the spine/register's `CurrentDataHandlingVersion`, `InForceDataHandlingVersion`, optional `GraceExpiresAt`, and discriminated `Freshness`. Line 159 still mandates `OperationGateMatrixVersion = 3`, while AD-17 and the launch-readiness register require version 4. Line 160 retains the obsolete projection inventory. Most materially, Story 6.4 line 1993 says `BudgetLedger` atomically owns rate admission, open-interaction bounds, and monetary reservations; AD-2 and AD-21 deliberately split those lifetimes and failure domains across `RateLimitLedger`, `OpenInteractionLedger`, and `BudgetLedger`, with mutually exclusive interaction-owned commit/abort decisions.

**Failure mode and impact.** A delivery agent can satisfy Story 6.4 and the epics' cross-cutting rules while violating the architecture's rate-window, caller-concurrency, monetary-period, replay, safety-rescan, and readiness contracts. Reintroducing one mixed-lifetime ledger recreates the concurrency and crash-recovery hazard that the current spine identifies as implementation debt. Using matrix v3 or the truncated readiness result also makes API/BFF/UI/workflow/readiness consumers disagree with the authoritative register.

**Required correction.** Reconcile the existing Additional Requirements and Story 6.4 in place, preserving story IDs: copy the complete AD-2 durable-owner inventory, the version-4 operation-gate contract, the current projection vocabulary, and the complete public readiness schema; make Story 6.4 allocate rate admission, original-caller open-interaction leasing, and monetary reserve/settlement to their distinct aggregates and bind its evidence to AD-21's decision protocol. Do not mark any target behavior implemented merely by correcting the planning contract.

### VC5-H2 — Stories 8.2 and 8.3 can become executable without the mandatory protected export store and shared export/deletion fence decision

**Evidence.** The authoritative external-dependency register defines `EXT-EXPORT-STORE-1` as the immutable encrypted artifact store and requires its accepted evidence to name the approved `OD-EXPORT-LIFECYCLE-1` decision version. The launch-readiness register keeps that decision `Open` and binds export preparation, deletion completion, physical purge receipts, backup/restore treatment, and the AD-22 `ProtectionFence` to it. The spine's open-decisions table explicitly says Stories 8.2/8.3 cannot become `ready-for-dev` before that decision. In contrast, Story 8.2 lines 2747–2751 and its evidence manifest depend only on `EXT-PROTECTION-1` and `EXT-SECRETS-1`. Story 8.3 lines 2811–2815 does the same and affirmatively says export is independent and not a deletion prerequisite; its completion inventory does not require the immutable artifact store/index, its backup/restore copies, or physical purge receipts. The epics' dependency-authority list at line 167 omits both `EXT-EXPORT-STORE-1` and `EXT-CONV-RETRACTION-1` even though PRD §8 and the external register enumerate twelve records.

**Failure mode and impact.** Stories 8.2/8.3 could be declared ready and implemented while export bytes have no committed owner, while an export races deletion, or while deletion reports success despite a surviving artifact/backup/index copy. This is an all-copies deletion and irreversible-governance integrity breach, not ordinary implementation debt.

**Required correction.** Amend the existing Story 8.2/8.3 dependencies, acceptance criteria, evidence manifests, negative evidence, and result blockers to include `EXT-EXPORT-STORE-1`, the approved `OD-EXPORT-LIFECYCLE-1` version, `ProtectionFence` preparation/arm ordering, export-index high-water checks, and physical purge/backup/restore receipts. Replace the assertion that export is independent with AD-22's precise rule: a prior export request is not required, but every deletion must fence against and account for all overlapping committed/in-flight exports. Restore the complete twelve-record dependency inventory without creating a consumer for optional retraction unless Product selects OQ-23's branch.

## Medium Findings

### VC5-M1 — The spine cites two reviewer reports that do not exist in the reviewed snapshot

The spine frontmatter lists `reviews/review-2026-09-12-security-data-integrity-v3.md` and `reviews/review-2026-09-12-brownfield-drift-v3.md`, but neither path exists in the frozen tree. This does not change an architecture decision, but it makes the asserted source/evidence chain non-reproducible. Generate the named reviews before retaining the citations, or remove/replace the citations with the actual completed artifacts.

## Low Findings

None.

## Technology And Version Verification

| Spine claim | Primary or repository evidence | Result |
| --- | --- | --- |
| .NET SDK `10.0.401` with `latestPatch`; `net10.0`; C# 14; `.slnx`; Central Package Management | Root `global.json`, `Directory.Build.props`, `Hexalith.Agents.slnx`, and `Directory.Packages.props` match exactly; `dotnet --version` returns `10.0.401`. The official [.NET 10.0.12 release](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md) lists SDK `10.0.401`/`10.0.112` and September servicing. | **Confirmed.** The spine makes no unsupported build-success claim. |
| `CVE-2026-69522` scope and fixed floor | The official [.NET advisory](https://github.com/dotnet/announcements/issues/439) identifies the Windows-specific, CVSS 8.8 `Microsoft.DiaSymReader.Native` issue and fixed component floor. No direct repository reference to that component exists. | **Confirmed.** The root SDK update is not overstated as protection for a future direct component reference. |
| Parent-authoritative Hexalith versions | The root index records Builds `a32cb4227493`, Conversations `73bcee6f0447`, EventStore `ce9e779a3ec2`, FrontComposer `053b2008307d`, Parties `fa42398552fb`, and Tenants `2fac18396ff1`; each matches the spine. | **Confirmed.** |
| Dirty submodules are not authority | Working-tree HEADs are Builds `fa6472788c14`, Conversations `b819a7c43a70`, EventStore `a568af4ec963`, and FrontComposer `1b3608c9b039`; Parties and Tenants match their gitlinks. | **Confirmed.** The spine uses parent gitlinks and distinguishes the dirty Builds catalog. |
| Current Dapr exposure and versions | At exact EventStore gitlink `ce9e779a`, Client directly references `Dapr.Client`, DomainService directly references `Dapr.AspNetCore`, and Agents consumes that graph and calls `AddDaprClient()`. Exact parent Builds pins the Dapr family to `1.18.5`; dirty Builds pins `1.18.7`. No Agents project references `Dapr.Workflow`. Official indexes list `1.18.7` as the latest stable before 1.19 previews for [Client](https://api.nuget.org/v3-flatcontainer/dapr.client/index.json), [ASP.NET](https://api.nuget.org/v3-flatcontainer/dapr.aspnetcore/index.json), and [Workflow](https://api.nuget.org/v3-flatcontainer/dapr.workflow/index.json); the official [1.18.7 servicing commit](https://github.com/dapr/dotnet-sdk/commit/deb05064a4eb4530b9f479ed6e0c6e16ebcc2440) is titled “Fixing transient security vulnerability.” | **Confirmed.** Current Client/ASP.NET exposure, future Workflow adoption, and the non-authoritative dirty remediation are separated accurately. |
| Dapr Workflow instance-ID behavior | Official [Workflow features and concepts](https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-features-concepts/) document nonterminal same-ID blocking and post-terminal/purge reuse behavior. | **Confirmed.** AD-18's stricter project policy is compatible with platform semantics. |
| Microsoft Agent Framework and Dapr Agents maturity | Microsoft's official [Agent Framework 1.0 announcement](https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0/) calls v1.0 production-ready. Official [Dapr Agents documentation](https://docs.dapr.io/developing-ai/dapr-agents/) identifies v1.0 GA and a Python surface. | **Confirmed.** SDK deferral is a dependency choice, not an ecosystem-maturity claim. |
| Hosting and Provider/Agent Framework selection | `EXT-HOST-1` and `EXT-PROVIDER-1` remain `Uncommitted`; no module AppHost/Aspire/ServiceDefaults or Provider/Agent Framework reference exists. | **Confirmed unselected.** The spine does not invent those owner decisions. |
| MediatR `14.2.0`, FluentValidation `12.1.1`, OpenTelemetry `1.18.0` | Exact parent Builds pins those versions. Current official NuGet indexes confirm the latest stable versions: [MediatR](https://api.nuget.org/v3-flatcontainer/mediatr/index.json), [FluentValidation](https://api.nuget.org/v3-flatcontainer/fluentvalidation/index.json), and [OpenTelemetry](https://api.nuget.org/v3-flatcontainer/opentelemetry/index.json). Exact EventStore graph inspection places MediatR outside the Client/DomainService/ServiceDefaults graph Agents consumes; OpenTelemetry arrives through ServiceDefaults. | **Confirmed.** |
| Fluent UI Blazor `5.0.0-rc.5-26219.1` | Root and exact parent catalog pin it; the official [NuGet index](https://api.nuget.org/v3-flatcontainer/microsoft.fluentui.aspnetcore.components/index.json) lists the same RC as the newest v5 prerelease. | **Confirmed.** `ARCH-A-8` correctly keeps the production-risk decision open. |
| Test stack | Root pins Test SDK `18.6.0`, xUnit `3.2.2`, runner `3.1.5`, Shouldly `4.3.0`, NSubstitute `5.3.0`, and bUnit `2.9.0`; exact parent pins `18.9.0`, xUnit/runner `4.0.0`, Shouldly `4.3.0`, NSubstitute `6.2.0`, and bUnit `2.9.0`. Current official indexes list Test SDK `18.10.0`, xUnit/runner `4.0.0`, NSubstitute `6.2.0`, and bUnit `2.10.3`. Official [xUnit 4.0 notes](https://xunit.net/releases/v3/4.0.0) and [MTP guide](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform) confirm the MTP-v2/default and .NET 10 runner-selection behavior. Root lacks `test.runner`; all exact parent siblings except Conversations include it. | **Confirmed and accurately deferred** as build/delivery alignment, not shipped capability. |
| Dapr Conversation API | Official [Dapr Conversation API documentation](https://docs.dapr.io/reference/api/conversation_api/) still labels the API alpha. | **Confirmed.** Its exclusion from V1 is based on current evidence. |
| RFC-based export primitives | [RFC 8785](https://www.rfc-editor.org/rfc/rfc8785), [RFC 7515](https://www.rfc-editor.org/rfc/rfc7515), and [RFC 7518](https://www.rfc-editor.org/rfc/rfc7518) remain the selected standards. | **Confirmed as architecture selections.** Provider/lifecycle choices remain open separately. |

## Bound PRD, Register, And Current-Reality Reconciliation

- **PRD/register alignment:** PRD §8 names the same twelve `EXT-*` records as the external register. All twelve remain `Uncommitted`, so the spine correctly treats checkout/package evidence as non-commitment. The launch register's version-4 operation matrix, typed target scope, complete readiness fields, eighteen-GateId `RQ-1` profile, and versioned open-decision records agree with AD-17.
- **Unresolved decisions:** `OD-HOLD-DELETION-PRECEDENCE-1`, `OD-EXPORT-LIFECYCLE-1`, `OD-DAPR-SECURITY-1`, and `OD-SPRINT-5.1-5.2-1` remain explicit, owner-assigned, and fail-closed. The spine fixes only product-independent safety invariants; it does not invent irreversible hold/deletion behavior, export lifetime/provider, a Dapr exception, or corrected delivery history.
- **Current implementation:** source still exposes the legacy operations route and Party link/replace vocabulary, omits `AuthenticatedHumanActorId` and trusted-envelope HMAC/nonce/replay, has no Dapr Workflow or IntegrationTests project, and lacks the distinct rate/open-interaction ledgers, safety-rescan ownership, interaction outbox, protected export store, and shared fence. `eng/verify-story-5.1.ps1` is absent while sprint status and story evidence remain contradictory. The spine's debt table accurately labels these as unimplemented convergence work.
- **Cross-document defect:** the spine and its two authoritative registers are mutually aligned, but the replacement epics are not. VC5-H1 and VC5-H2 therefore concern stale build authorization, not a false claim that the code already implements the target architecture.

## Authoritative Prior-Finding Closure Audit

All Critical and High findings in `VALIDATION-REPORT-2026-09-12.md` are closed in the current spine itself: AD-13 separates transport fingerprint inputs from safety evidence (C-1); AD-22 plus versioned open decisions close all mechanically decidable hold/export/deletion safety boundaries without inventing unresolved policy (C-2); AD-17/register v4 close the circular gate and target-scope ambiguity (C-3); and H-1 through H-12 are represented as binding architecture, explicit implementation debt, external dependencies, or owner decisions. The new v5 High findings arise because those closures were not propagated into the executable epics that the spine maps to delivery.

## Gate Conclusion

The current spine is verified-current for named technology, exact parent repository authority, its distinction between target architecture and present implementation debt, and its direct PRD/register reliance. The complete gate nevertheless fails with **0 Critical, 2 High, 1 Medium, and 0 Low findings** because the replacement epics still authorize an obsolete mixed-lifetime cost-control design and incomplete export/deletion dependencies, and because two cited reviewer sources are absent. Reconcile the existing epics and source list, then rerun the complete gate.
