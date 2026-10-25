---
title: 'Complete five deferred terminal setup-status items'
type: 'bugfix'
created: '2026-10-25'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
context: []
baseline_commit: 15fc5834d38fa1d769e3be5a77e989bf4c8dc132
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Setup commands use a deferred status reader, leaving exact domain rejections unverifiable. The UI treats rejected/blocked writes as retryable outages. The standard EventStore issuer excludes its existing workload status operation.

**Approach:** Resolve DW-7, DW-12, DW-24, DW-25 and DW-26 through authenticated terminal setup status. Bind reads to the trusted command's tenant, domain and receipt; release fresh rejected attempts using existing localized messages; prevent known-unbound surfaces from starting attempts. Verify, review and push changes on main in each owning repository.

## Boundaries & Constraints

**Always:** Preserve authorization, exact identities, host overrides, enum ordinals, cancellation, uncertainty and original accepted-write evidence. DW-25 is the carried pointer owned by DW-12. Record evidence scope honestly.

**Never:** Bypass tenant authorization, fall back to the public user-status route, fabricate rejection from transport failure, mint local credentials, add a NotBound status or clear ambiguous attempts. Production qualification and DW-6/DW-19/DW-21 remain separate. Preserve external commitments and owner evidence.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Exact rejection | Matching trusted tenant/domain/message/correlation and canonical rejection | Rejected; release fresh attempt | Existing localized message |
| Uncertainty | Missing credentials/status, mismatch, infrastructure failure or unknown status | UnableToVerify | Preserve exact retry identity |
| Isolation | Foreign scope or undeclared operation/domain | No unauthorized evidence | Refuse |
| Unbound | No authoritative setup; deferred gateway | No new attempt | Existing unavailable surface |
| Accepted | Original receipt retained; inconsistent retry | Preserve target/projection evidence | No ordinary fresh-rejection reset |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Agents.Server/Ports/IAgentCommandStatusReader.cs`: message-only seam; add a compatible trusted-intent overload.
- Server `Application/Agents/` administration operation classes already validate receipts; `Composition/AgentSetupServiceCollectionExtensions.cs` currently always installs the deferred reader.
- New internal server `Ports/EventStoreAgentCommandStatusReader.cs` and `Ports/CommandStatusWorkloadAssertionSource.cs`: reuse SDK HTTP transport with a delegate binding assertions to one exact domain.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DomainServiceGatewayWorkloadAssertionSource.cs`: excludes CommandStatus. The existing gateway workload controller already requires tenant/domain binding.
- `src/Hexalith.Agents.UI/Services/Gateways/AgentsClientSetupGateway.cs` and `Components/Pages/AgentConfiguration.razor`: terminal mapping/attempt handling; Rejected enum and English/French messages already exist.

## Tasks & Acceptance

**Execution:**
- [x] Owner issuer file above and `references/Hexalith.EventStore/tests/Hexalith.EventStore.DomainService.Tests/EventStoreGatewayWorkloadTransportTests.cs` — support status grants only for declared domains; test actual issuer claims, absent credentials and refused scopes/operations.
- [x] Server seam, adapters, both administration operation classes and composition above — pass trusted intent, verify complete status identity, bind configured transport and preserve host overrides/deferred fallback.
- [x] `test/Hexalith.Agents.Server.Tests/AgentSetupCompositionTests.cs`, administration operation tests and new `EventStoreAgentCommandStatusReaderTests.cs` — prove composition, exact rejection, mismatches, cancellation and credential/transport failure.
- [x] UI files above, `test/Hexalith.Agents.UI.Tests/AgentsClientSetupGatewayTests.cs` and `AgentConfigurationTests.cs` — handle fresh Rejected/Blocked terminally, guard known-unbound starts, preserve accepted/uncertain attempts.
- [x] `CONTRIBUTING.md`, `_bmad-output/implementation-artifacts/deferred-work.md` and a dated evidence report — document prerequisites and append exactly five supported closure records, preserving original entries.

**Acceptance Criteria:**
- Given matching authenticated rejection, when setup status is read, then return Rejected and allow a new UI attempt with a new identity.
- Given missing, foreign or infrastructure-only evidence, when status is read, then claim neither domain rejection nor completion.
- Given unavailable initial setup, when rendered or receiving a stale submit event, then start no attempt; previously accepted attempts retain their evidence.
- Given either dependency mode, when validated, then use supported public APIs and state any issuer-version prerequisite explicitly.

## Implementation Notes

- Root baseline: `15fc5834d38fa1d769e3be5a77e989bf4c8dc132`, clean main. No root AppHost. No implementation or deployment performed.
- Two read-only investigations informed this plan. Implementation package validation confirmed that published Client 3.127.0 exposes workload status and three-argument assertion delegation, but not current source's exact-domain overload. No newer published Client was available. Exact-domain delegation therefore uses a compile-time source capability; package mode refuses credentials before HTTP until compatible published Client/DomainService and the matching capability are available, while existing host-reader overrides remain supported. No new public gateway API is necessary.
- EventStore baseline: `d41a3c55a684491d02f61c5ea98e0f74332b5d4d`, clean, locally behind origin/main by two commits. Fetch/reconcile before editing. Push owner content before root gitlink; initialize no nested submodules.
- Cross-repository credential changes and a compatible server seam addition require dispatch and the specification checkpoint. The user requested this five-entry batch and main pushes.
- Existing Builds gitlink/workflow assertion drift may block broad checks; preserve the gate and report exact failures separately.

## Verification

Run affected owner issuer/client/controller projects individually with native nonzero xUnit selections, then focused root server/UI suites and `eng/verify-story-5.2.ps1`. Use Debug/source locally and Release/packages for package validation. Exercise authenticated isolation and credential refusals; record exact commands, full revisions and outcomes. Review independently and triage findings. Check whitespace, localization and five closures; validate Conventional Commit messages with pinned commitlint, fetch divergence and push main without force.

- Parent audit: inspected staged production/test/documentation changes and owner issuer diff; verified all seven compressed/uncompressed native result hashes, all-pass/nonzero guards and every matrix reference against native XML. All five tasks and four acceptance criteria are satisfied within the recorded source/package and local-evidence boundaries.


## Review Triage Log

- BH-1 — medium / patch: the new default scoped rejection overload delegates to legacy message-only evidence. Default it to unknown; legacy host registration remains usable but must explicitly implement scoped evidence to establish rejection.
- BH-2 — medium / patch: a configured singleton resolves host assertion sources from the root provider. Register the configured reader as scoped and cover a scoped source with scope validation enabled.
- BH-3 — medium / patch: provider lookup catches a non-cancellation exception after a custom reader cancels the caller token. Recheck cancellation in that catch; preserve an actual cancellation result.
- BH-4 — low / patch documentation: default ReadAsync deliberately returns unknown because legacy boolean readers cannot provide the new authenticated status DTO. There was no legacy provider-status implementation on this interface to preserve; document the capability requirement and test conservative behavior rather than reintroduce the unsafe public-route fallback.
- BH-5 — medium / defer: the closure addendum explicitly closes local implementation and records the package limitation, but the original deployment prerequisite remains. Compatible published SDK availability predates this change. Record a concrete publication/pin/capability follow-up; no claim of complete package-mode terminal binding or live qualification is made.
- BH-6 — medium / patch: fresh rejected activation can expose a stale configuration version on its next attempt. Refresh the read-only setup after fresh rejection without overwriting editable drafts; retain the terminal outcome if that refresh fails.
- BH-7 — low / rejected: fixed public uncertainty is intentional; additional diagnostic taxonomy and logging would add machinery beyond a direct correction. Scoped resolution is fixed separately and prerequisites are documented. No incorrect outcome or disclosure follows from the absence of logging.
- BH-8 — low / patch: the evidence honestly separates issuer, SDK and controller tests, but joining actual issuer output with receiving authentication strengthens this changed grant boundary. Add a local signed-status HTTP integration case and negative bindings using existing test infrastructure.
- BH-9 — low / patch: composed transport currently exercises only agent. Extend domain coverage to platform catalog and tenant enablement; retain receipt-correlation assertions.
- BH-10 — low / patch: add Submitted to refused initial truth states and assert the injected ULID factory receives no allocations on refused stale events.
- EC-1 — medium / patch, grouped with BH-2: scoped credential-source resolution fails through the root-bound singleton; the same lifetime correction addresses both findings.
- VG-1 — medium / patch: verified test gap for provider status without expected correlation and blank response correlation. Add null/empty/whitespace negative cases through the real reader; each must remain UnableToVerify.

- Final parent verification: maintained gate passes 5,821 full owning tests after review patches and upstream reconciliation; owner authentication/controller 81 and final package UI 108 pass, all warning-free. EventStore owner commit 4e91e552be41bfba984d67d68335e6b1709917b5 is pushed on main. Native summaries, artifact hashes and exact changed-source fingerprints are retained in tests/terminal-setup/post-review.json; compatible published SDK enablement remains explicitly deferred.
