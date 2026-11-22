---
title: 'Five deferred lease, inspection and verification items'
type: 'bugfix'
created: '2026-11-22'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '5dc8d946a0d97297b4d7be4ec36477ff7bc79670'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The deferred review still lacks evidence for two public automatic-interaction status mappings and the legacy interaction identity collision. A committed lease can lose its replay outcome after Closing, an inspection missing its wire status can become Success, and the audit ledger describes a superseded Release policy.

**Approach:** Finish both outstanding Story 6.1 review patches, require explicit status on the three inspection results while preserving their enum ordinals, establish the legacy collision in the local producer and aggregate tests, and reconcile the audit record against current Release execution. Record the installed legacy inventory as an outstanding Goal B input.

</frozen-after-approval>

## Implementation Notes

- The directory returns an event-free no-op for an exact committed lease retry after Closing. A cancelled reservation still rejects. Registration and acquisition retain their first Closing guard.
- Inspection JSON requires an explicit status on all three results. The serializer writes Success even under default-value omission. Status ordinals stay pinned by the existing contract test.
- The identity check compares the two actual producers’ dispatch targets and exercises the derivations with equal ASCII and Unicode tuples. The existing aggregate check covers the typed LegacyStream rejection. Installed stream inventory remains an open Goal B obligation.
- The audit reconciliation cites the current shared severity policy and its Release conformance test. The implementation changes no audit configuration.

## Review Triage Log

- Medium, patched. The legacy producer now seeds the same replay fixture used by automatic intake. The test asserts RecoveryRequired, the dispatched stream address, and the unconsumed permit and absence of a lease or scheduled workflow after replay.
- Medium, patched. A current-revision fresh acquisition and an exact permit retry both assert ConversationClosing. The two commit orderings exercise the cancelled reservation and the existing committed lease.
- False. A populated named-enum Success result is already exercised through the production serializer by VerifiedSetupQueryCallerTests and AgentSetupQueryHandlerTests. The new contract tests cover missing and explicit numeric statuses.
- Low, patched. The verification section distinguishes Release policy evaluation from the outcome of the actual audited restore.

## Verification

- `dotnet restore Hexalith.Agents.slnx -p:Configuration=Release -p:UseHexalithProjectReferences=false -m:1` completed with auditing enabled. The Release solution build reported zero warnings and errors. The EventStore package-floor verifier accepted the 13 selected package references at 5.4.0.
- Release package-mode `BuildContractConformanceTests.OrdinaryReleaseShouldEvaluateTheHighAndCriticalAuditPolicy` passed. The Release MSBuild property evaluation reported `NuGetAudit=true`, `NuGetAuditMode=all`, and `TreatWarningsAsErrors=true`. The warnings-as-errors exemptions were `NU1901`, `NU1902` and `CS0618`. These values establish the policy. The audited restore completed independently of this property evaluation.
- Debug used the prepared source mapping from released Builds CLI 6.6.1. The seeded legacy-stream fixture passed through both production orchestrators and replayed the durable test events. It returned RecoveryRequired after the permit append, with no acknowledgement, lease, or scheduling.
