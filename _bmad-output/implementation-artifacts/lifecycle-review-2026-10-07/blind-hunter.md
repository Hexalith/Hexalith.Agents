Conduct a review of CONTENT.
Look for what's missing, not only what's wrong.
Compute your finding floor N from the diff file's size: N = min(floor(sqrt(kB) + 1), 10), where kB is the file's size in kilobytes. State the arithmetic in one line, then find at least N issues to fix or improve.
Output a Markdown list of findings only — no severity, priority, or ranking.
If the content is empty, stop and say so.
If you have zero findings, re-check and keep thinking; do not stop with an empty list.

CONTENT: the unified diff below. Read this content — it is the content under review.

<unified-diff>
diff --git a/agents/_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md b/agents/_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md
index fdeef33..9e17f83 100644
--- a/agents/_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md
+++ b/agents/_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md
@@ -115,3 +115,7 @@ The user stated “I approve” after reviewing the implemented policy guards, m
 ## Concrete retention/custody proposal — 2026-10-06
 
 The user asked to supply the remaining retention/custody inputs and then requested a deeper pragmatic comparison. [Decision packet](story-5-4-retention-custody-proposal-2026-10-06.md) now proposes 365 fixed days from binding-effective-at, supplies isolated valid JSON, compares existing custody/managed adapter/Vault/local options and defines exact expiry/destruction/restore qualification exercises. Existing SDK configuration validation passes. The binding-start clock does not provide a full year after every later action; that limitation is explicit. The new duration is a recommendation prepared after the prior approval, not silently approved or loaded into production. No production custody implementation was found; Azure vault discovery failed on missing cached authentication. No qualified target, live result or owner availability is fabricated.
+
+## Accepted 365-day lifetime application — 2026-10-07
+
+The user's “do recommended” instruction accepts the concrete duration and scope in the October 6 proposal. [Applied policy and cleanup](story-5-4-retention-application-2026-10-07.md) now supply 365 fixed days from binding-effective-at in Parties host configuration and a bounded Platform operation through existing custody. Both Debug builds passed; 191 focused/regression tests passed. Numeric duration approval is no longer pending. A qualified production custody/lifecycle/restore target, complete copy receipts, successor-after-expired-predecessor proof and full owner compatibility commitments remain pending. No dependency record was promoted. Independent review is pending because fresh agents cannot be spawned.
diff --git a/agents/_bmad-output/implementation-artifacts/story-5-4-retention-custody-proposal-2026-10-06.md b/agents/_bmad-output/implementation-artifacts/story-5-4-retention-custody-proposal-2026-10-06.md
index 9db3146..3cd284e 100644
--- a/agents/_bmad-output/implementation-artifacts/story-5-4-retention-custody-proposal-2026-10-06.md
+++ b/agents/_bmad-output/implementation-artifacts/story-5-4-retention-custody-proposal-2026-10-06.md
@@ -1,6 +1,6 @@
 # Concrete retention and custody proposal
 
-Prepared in response to the user's request to supply the missing retention/custody behavior and compare pragmatic solutions. **Status: concrete recommendation for review; no production configuration or qualification receipt is created.** The earlier approval covered the local implementation/design before a numeric duration was proposed.
+Prepared in response to the user's request to supply the missing retention/custody behavior and compare pragmatic solutions. **Original status on 2026-10-06: concrete recommendation for review. Accepted by the user’s “do recommended” instruction on 2026-10-07.** The earlier approval covered the local implementation/design before a numeric duration was proposed. The accepted period is now applied to the Parties host configuration; a production custody qualification receipt is still absent.
 
 ## Proposed retention
 
@@ -47,3 +47,9 @@ Qualify on an isolated owner-approved target using shortened synthetic intervals
 Prefer option 1. If no suitable custody exists, use option 2 in the current platform/cloud with the smallest adapter and already qualified independent lifecycle infrastructure. Use option 3 only if Vault is already operated. Do not build new self-managed custody or a generic policy system for this feature.
 
 Workspace investigation found interfaces and synthetic tests, but no production actor-history custody implementation. Azure account metadata was cached; actual vault enumeration failed because the account's MSAL token was unavailable. No deployment was selected, provisioned or exercised. The proposed policy parses and derives its deadline through the existing SDK contract; [configuration validation](tests/actor-retention-proposal-2026-10-06/configuration-validation.json) confirms 365 days and the expected exclusive deadline. The temporary PowerShell check initially could not load the Parties Web assembly because the ASP.NET runtime was absent from that host; it then used the SDK policy contract directly. This is not a new domain test or custody/restore qualification. Full Story 5.4 and the existing dependency register remain unchanged.
+
+## Application — 2026-10-07
+
+The user selected this concrete recommendation. `party-actor-retention-v1` now has an accepted duration of 365 fixed days from each binding’s effective instant, with the post-erasure scope and timing limitation above. [The implementation spec](spec-5-4-apply-365-day-lifecycle.md) records authorization and the bounded Platform cleanup operation. The original proposed JSON and its dated validation remain an archived pre-acceptance snapshot; runtime configuration is now [Parties appsettings](../../../parties/src/Hexalith.Parties/appsettings.json).
+
+No existing production actor-history custody implementation was found on reinspection. Azure vault enumeration was retried; deployment qualification remains contingent on actual authenticated resource discovery, independent lifecycle infrastructure, inventory and restore exercise results. No new self-managed vault, policy engine, database or deployed service is introduced.

diff --git a/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md b/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md
new file mode 100644
index 0000000..73879ba
--- /dev/null
+++ b/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md
@@ -0,0 +1,75 @@
+---
+title: '5.4 Apply approved actor-history lifetime with bounded cleanup'
+type: 'feature'
+created: '2026-10-07'
+status: 'in-review'
+route: 'dispatch'
+human_approval: 'accepted'
+approval_source: 'User: do recommended (365-day proposal dated 2026-10-06)'
+baseline_commit: '7653521e2b21825cba196a3365c53c003432c9b6'
+parties_baseline_commit: 'b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca'
+platform_baseline_commit: 'fd5db04d48423bc30f5a5e141a9019fede5d736f'
+review_loop_iteration: 0
+context:
+  - '/home/administrator/projects/hexalith/agents/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
+  - '/home/administrator/projects/hexalith/agents/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
+  - '/home/administrator/projects/hexalith/parties/AGENTS.md'
+  - '/home/administrator/projects/hexalith/parties/.editorconfig'
+  - '/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/story-5-4-retention-custody-proposal-2026-10-06.md'
+  - '/home/administrator/projects/hexalith/eventstore/src/Hexalith.EventStore.Contracts/Security/IIdentityHistoryCustody.cs'
+  - '/home/administrator/projects/hexalith/eventstore/src/Hexalith.EventStore.Contracts/Security/IdentityHistoryPolicy.cs'
+  - '/home/administrator/projects/hexalith/eventstore/src/Hexalith.EventStore.Contracts/Security/IdentityHistoryCustodyEvidence.cs'
+---
+
+<frozen-after-approval reason="User explicitly selected the concrete 365-day recommendation">
+
+## Intent
+
+Apply the selected `party-actor-retention-v1` policy: 365 fixed days from `binding-effective-at`, including bounded historical attribution after profile erasure. Provide a small, executable Platform cleanup operation through the existing EventStore custody interface. Production remains unavailable until independently operated custody and restore behavior are qualified. User acceptance establishes this engineering policy, not a fabricated infrastructure receipt or a statutory period.
+
+## Boundaries & Constraints
+
+Use the existing Parties options binding and EventStore policy/evidence records. Populate the Parties host configuration with the accepted ID, `365.00:00:00` and trigger; keep options library defaults unset. Keep the existing mandatory custody/source/authorization guards. No expiry extension on erasure, renewal, restore or retry. A long-lived binding requires an authorized new version; an action near the end of a binding has only the remainder of that binding's lifetime.
+
+Keep shared lifecycle orchestration in Platform.Custody; source events/snapshots stay in EventStore. The cleanup operation is a library call for one owner-inventoried retention unit, not a new service, database, scheduler, discovery API or generic policy engine. Reuse `IIdentityHistoryCustody.DestroyExpiredAsync` and `CanReadAsync`. Accept a missing provider as pending; do not install a development backend. Validate exact policy, purpose, finite derived expiry and lifecycle flags before contacting custody. Never invoke destruction before exclusive expiry. Provider timeout, exception, missing receipt, readable-after-destruction and unknown outcomes remain pending. Preserve caller cancellation and observe late task faults without retaining or printing payloads. A retry uses the exact same identity and evidence reference, not a new retention unit. The provider owns authentication, independent lifecycle durability, all-copy irreversible destruction and idempotent outcome lookup.
+
+Provider-confirmed cleanup is not independent production qualification. No provider/resource selection is invented: the workspace has no actor-history backend, and earlier Azure discovery had no usable token. Do not provision infrastructure, deploy, alter frozen original Story 5.4 or its dependency register, stage/commit/push, update submodules, add a general managed-cloud client, or claim predecessor-expiry/restore completeness. Preserve other work and prior dated evidence.
+
+## I/O & Edge-Case Matrix
+
+| Scenario | Expected behavior |
+| --- | --- |
+| Host configuration consumed | Existing options binding derives exactly 365 days; overrides can withdraw policy; library defaults remain unset |
+| Unexpired retention unit | No provider call; not expired |
+| Exactly at/after expiry | Destroy exact unit; confirm fresh custody denies reads before reporting provider-confirmed destruction |
+| Invalid policy/evidence/scope | Typed invalid result, no provider calls; never release actor/profile data |
+| Provider absent, false, throws or stalls | Pending; no claim of irreversibility or changed deadline |
+| Destruction reports true but unit remains readable | Pending |
+| Lost acknowledgement and repeated cleanup | Retry same immutable unit; exact evidence ID/policy/deadline passed again |
+| Caller cancels, provider ignores cancellation | Prompt cancellation; late completion observed; no fabricated result |
+| Another tenant or live successor | Operation targets only supplied exact expired unit; caller cannot sweep unrelated records |
+
+</frozen-after-approval>
+
+## Code Map
+
+- Parties `src/Hexalith.Parties/appsettings.json`, `Authorization/PartyIdentityOptions.cs`, `Extensions/PartiesServiceCollectionExtensions.cs` — existing policy configuration and guards. No new enable flag is required: absent custody denies binding admission/read.
+- Parties `_bmad-output/planning-artifacts/actor-history-retention-policy-v1.md` — record concrete approval and duration; keep independent qualification pending.
+- Platform `src/Hexalith.Platform.Custody/Hexalith.Platform.Custody.csproj` and `PlatformCustodyServiceCollectionExtensions.cs` — add source/package Contracts references consistent with existing build switch; register cleanup helper with optional provider, no provider implementation/default key material.
+- Platform new `IdentityHistoryCleanup.cs` and `IdentityHistoryCleanupOutcome.cs` — bounded single-unit operation with a fixed five-second bound per provider await, caller cancellation, safe late-fault observation and typed outcomes.
+- Platform `tests/Hexalith.Platform.Custody.Tests/` — behavior tests with synthetic SDK custody implementation and clock; one C# type per file.
+- Parties `tests/Hexalith.Parties.Tests/Gateway/PartyIdentityRetentionConfigurationTests.cs` — real host JSON/options binding and withdrawal tests; copy the host JSON into test output deliberately through the test project.
+
+## Tasks & Acceptance
+
+- [x] Apply approved host configuration and policy record. Given the actual host JSON and existing options binding, when consumed, then ID/trigger and exact 365-day expiry match; library defaults still deny.
+- [x] Implement bounded Platform cleanup and meaningful matrix tests. Given an expired valid retention unit and provider confirmation plus fresh denial, when cleaned, then return provider-confirmed destruction; every failure/unknown case remains pending and retries preserve exact input.
+- [x] Run normal Debug builds, all Platform custody tests, and focused Parties configuration/query/admission regression tests. Preserve exact source hashes/commands/results in a new dated evidence packet; do not overwrite earlier evidence. Record production/provider/restore and independent review limits separately.
+
+## Verification
+
+- Parties actual host-policy binding, withdrawal and fixed-day leap-year deadline: two tests passed. Existing query/admission suite: 78 tests passed.
+- Platform cleanup matrix: 43 tests passed; existing custody suite: 68 tests passed. Both source Debug builds: zero warnings/errors. No skips, failures, errors or not-run cases.
+- [Source/artifact hashes and exact commands](tests/actor-history-lifecycle-2026-10-07/evidence.json); [applied behavior and limits](story-5-4-retention-application-2026-10-07.md).
+- Required fresh implementation-agent spawn failed with `agent thread limit reached`; implemented directly after loading context.
+- Production custody/restore qualification and independent review are separate pending gates; full Story 5.4 remains incomplete.

diff --git a/agents/_bmad-output/implementation-artifacts/story-5-4-retention-application-2026-10-07.md b/agents/_bmad-output/implementation-artifacts/story-5-4-retention-application-2026-10-07.md
new file mode 100644
index 0000000..e89b963
--- /dev/null
+++ b/agents/_bmad-output/implementation-artifacts/story-5-4-retention-application-2026-10-07.md
@@ -0,0 +1,13 @@
+# Applied 365-day actor-history recommendation
+
+Authorization: the user said “do recommended” after receiving the concrete 365-day/custody comparison. This accepts the stated engineering policy and timing limitation. `party-actor-retention-v1`, purpose `party-actor-history-v1`, now uses 365 fixed days from binding-effective-at. Scope includes retained opaque attribution after profile erasure. Erasure/rebind/restore/retry cannot extend an existing deadline; an action late in a binding has only its remaining lifetime. This is not statutory-period or deployed-provider qualification.
+
+Applied the numeric policy to [the Parties host JSON](../../../parties/src/Hexalith.Parties/appsettings.json) and [the policy record](../../../parties/_bmad-output/planning-artifacts/actor-history-retention-policy-v1.md). Real JSON/options-binding tests verify exactly 31,536,000 seconds, including a leap-year boundary and policy withdrawal. Options library defaults remain unset.
+
+Added [Platform single-unit cleanup](../../../platform/src/Hexalith.Platform.Custody/IdentityHistoryCleanup.cs) through existing EventStore custody. It never calls custody before expiry, validates policy/purpose/deadline/lifecycle fields, bounds each provider await to five seconds, propagates caller cancellation and leaves unknown outcomes Pending. Completion requires provider-confirmed irreversible destruction and a fresh read denial. Retries preserve the exact original retention unit. No production provider, scheduler, new storage or new service is installed.
+
+Verification: both normal Debug source builds passed with zero warnings/errors. All **191 tests** passed: 111 Platform custody tests (43 new cleanup cases) and 80 Parties configuration/query/admission tests (two new actual host-configuration cases). No failures, errors, skips or not-run cases. [Evidence packet](tests/actor-history-lifecycle-2026-10-07/evidence.json) contains exact commands/source/artifact hashes and matrix coverage. Prior dated evidence is preserved as history; changed policy/proposal sources have pre-change snapshots in this packet.
+
+Production history remains disabled: no production actor-history custody implementation or independent lifecycle target was found. Azure vault enumeration again failed because no usable MSAL token was available; no deployment was discovered, provisioned, modified or qualified. Complete copy/destruction/restore evidence, predecessor-expiry/successor proof and owner compatibility acceptance remain missing. The older broad Parties replay test blocker and original Story 5.4 dependency register are unchanged.
+
+Implementation ran directly after the required fresh implementation-agent spawn failed with `agent thread limit reached`. Independent three-layer BMAD review remains pending; [the standalone review requests](lifecycle-review-2026-10-07/README.md) are ready for separate sessions. Local acceptance does not fabricate those findings or close the original story.

diff --git a/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/evidence.json b/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/evidence.json
new file mode 100644
index 0000000..abc93cb
--- /dev/null
+++ b/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/evidence.json
@@ -0,0 +1,588 @@
+{
+  "authorization": "User: do recommended; concrete 365-day policy from 2026-10-06 comparison accepted 2026-10-07",
+  "policy": {
+    "policyId": "party-actor-retention-v1",
+    "purpose": "party-actor-history-v1",
+    "retentionSeconds": 31536000,
+    "expiryTrigger": "binding-effective-at",
+    "scope": "opaque action attribution after Party profile erasure, bounded by original binding expiry"
+  },
+  "productionEnabled": false,
+  "custodyQualified": false,
+  "independentReview": "Pending: required fresh agents cannot spawn (agent thread limit reached)",
+  "observedHeads": {
+    "agents": "7653521e2b21825cba196a3365c53c003432c9b6",
+    "platform": "fd5db04d48423bc30f5a5e141a9019fede5d736f",
+    "parties": "b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca",
+    "eventstore": "27ac3c628db75ac6a410c88689546edd4de60ae4"
+  },
+  "commands": {
+    "platformBuild": "dotnet build tests/Hexalith.Platform.Custody.Tests/Hexalith.Platform.Custody.Tests.csproj -c Debug --artifacts-path /tmp/hexalith-platform54-lifecycle-artifacts -p:UseHexalithProjectReferences=true -p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/eventstore -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -m:1",
+    "platformTests": "dotnet /tmp/hexalith-platform54-lifecycle-artifacts/bin/Hexalith.Platform.Custody.Tests/debug/Hexalith.Platform.Custody.Tests.dll -result-xml /tmp/platform54-lifecycle-tests-20261007.xml",
+    "partiesBuild": "dotnet build tests/Hexalith.Parties.Tests/Hexalith.Parties.Tests.csproj -c Debug --artifacts-path /tmp/hexalith-agents54-retention-artifacts -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/eventstore -p:HexalithCommonsRoot=/home/administrator/projects/hexalith/parties/references/Hexalith.Commons -p:HexalithMemoriesRoot=/tmp/parties-no-memories-source -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -m:1",
+    "partiesTests": "dotnet /tmp/hexalith-agents54-retention-artifacts/bin/Hexalith.Parties.Tests/debug/Hexalith.Parties.Tests.dll -class '*PartyIdentityRetentionConfigurationTests' -class '*PartyIdentityQueryHandlerTests' -class '*PartyIdentityAdmissionTests' -result-xml /tmp/parties54-lifecycle-tests-20261007.xml"
+  },
+  "builds": {
+    "platform": {
+      "exitCode": 0,
+      "warnings": 0,
+      "errors": 0
+    },
+    "parties": {
+      "exitCode": 0,
+      "warnings": 0,
+      "errors": 0
+    }
+  },
+  "lanes": {
+    "platform": {
+      "summary": {
+        "total": "111",
+        "passed": "111",
+        "errors": "0",
+        "failed": "0",
+        "skipped": "0",
+        "not-run": "0",
+        "time": "10.228"
+      },
+      "tests": [
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.AtOrAfterExpiry_ConfirmsExactUnitAndFreshDenial(offsetTicks: 0)",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.AtOrAfterExpiry_ConfirmsExactUnitAndFreshDenial(offsetTicks: 1)",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.AtOrAfterExpiry_ConfirmsExactUnitAndFreshDenial(offsetTicks: 864000000000)",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.BeforeExpiry_DoesNotContactProvider(offsetTicks: -1)",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.BeforeExpiry_DoesNotContactProvider(offsetTicks: -864000000000)",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.CancelledAtProviderReturn_DoesNotReturnSuccess(finalCheck: False)",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.CancelledAtProviderReturn_DoesNotReturnSuccess(finalCheck: True)",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.PreCancelled_DoesNotContactProvider",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.FailedFreshDenial_RemainsPending(failure: \\\"readable\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.FailedFreshDenial_RemainsPending(failure: \\\"sync-throw\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.FailedFreshDenial_RemainsPending(failure: \\\"async-throw\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.FailedFreshDenial_RemainsPending(failure: \\\"provider-cancel\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.ExactExpiredUnit_PreservesForeignUnitAndLiveSuccessor",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.ForeignReceipt_IsRejectedByScopedProvider",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.StalledProvider_TimesOutAsPending(finalCheck: False)",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.StalledProvider_TimesOutAsPending(finalCheck: True)",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.LostAcknowledgement_RetryRecoversSameDestroyedUnit",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.DefaultRegistration_KeepsMissingCustodyPending",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.CancelledWhileProviderIgnoresToken_PreservesCancellation(finalCheck: False, lateFault: False)",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.CancelledWhileProviderIgnoresToken_PreservesCancellation(finalCheck: False, lateFault: True)",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.CancelledWhileProviderIgnoresToken_PreservesCancellation(finalCheck: True, lateFault: False)",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.CancelledWhileProviderIgnoresToken_PreservesCancellation(finalCheck: True, lateFault: True)",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.FailedDestruction_RemainsPending(failure: \\\"false\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.FailedDestruction_RemainsPending(failure: \\\"sync-throw\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.FailedDestruction_RemainsPending(failure: \\\"async-throw\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.FailedDestruction_RemainsPending(failure: \\\"provider-cancel\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"domain\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"restore\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"source\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"receipt\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"revision\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"expiry\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"purpose\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"copies\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"trigger\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"negative\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"zero\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"id\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"null-evidence\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"null-policy\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"null-identity\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"duration\\\")",
+        "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"overflow\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.IssueVerifyAndRedispatch_RetainLogicalIdentityButRefreshDelivery",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"schema\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"nonce\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"expires\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"issued\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"logical\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"digest-version\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"fingerprint\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"tuple\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"causation\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"correlation\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"resource\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"tenant\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"operation\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"signing-version\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"contract\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"activity\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"workflow-instance\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"workflow-kind\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"role\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"binding\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"party\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"actor\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"actor-tenant\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"kind\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"audience\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"issuer\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"profile\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"on-behalf\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ChangedAuthenticatedField_IsRejected(field: \\\"tag\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ProfileChangeDuringResolution_RejectsOldRevision",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.PartyFreeHumanShape_RequiresFreshAuthorityBasis(kind: Administrator)",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.PartyFreeHumanShape_RequiresFreshAuthorityBasis(kind: Platform)",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ClosedWorkflowShape_CannotCarryHumanAuthority(kind: \\\"Interaction\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ClosedWorkflowShape_CannotCarryHumanAuthority(kind: \\\"SystemTimer\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ClosedWorkflowShape_CannotCarryHumanAuthority(kind: \\\"GovernanceProtection\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ClosedWorkflowShape_CannotCarryHumanAuthority(kind: \\\"InteractionDirectoryMigration\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ClosedWorkflowShape_CannotCarryHumanAuthority(kind: \\\"ConversationDeletionPropagation\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.FreshProviderRejectsScopeVersionRevocationAndOutage(condition: \\\"tenant\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.FreshProviderRejectsScopeVersionRevocationAndOutage(condition: \\\"purpose\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.FreshProviderRejectsScopeVersionRevocationAndOutage(condition: \\\"version\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.FreshProviderRejectsScopeVersionRevocationAndOutage(condition: \\\"revoked\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.FreshProviderRejectsScopeVersionRevocationAndOutage(condition: \\\"outage\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.RotationOverlap_EndsForEnvelopeButNotRecordedDigest(observation: False)",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.RotationOverlap_EndsForEnvelopeButNotRecordedDigest(observation: True)",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.CallerComponentsMutationWhileProviderSuspended_DoesNotChangeAuthenticatedInput(digest: False)",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.CallerComponentsMutationWhileProviderSuspended_DoesNotChangeAuthenticatedInput(digest: True)",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ContentDigest_GoldenHmacAndPurposeIsolation",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.DefaultDependencyInjection_FailsClosedWithoutProductionPolicy",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.MissingOrInvalidProfile_NeverResolvesKeys(condition: \\\"missing\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.MissingOrInvalidProfile_NeverResolvesKeys(condition: \\\"expired\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.MissingOrInvalidProfile_NeverResolvesKeys(condition: \\\"zero-lifetime\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.MissingOrInvalidProfile_NeverResolvesKeys(condition: \\\"negative-skew\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.MissingOrInvalidProfile_NeverResolvesKeys(condition: \\\"negative-overlap\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.MissingOrInvalidProfile_NeverResolvesKeys(condition: \\\"zero-horizon\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.MissingOrInvalidProfile_NeverResolvesKeys(condition: \\\"short-retention\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.MissingOrInvalidProfile_NeverResolvesKeys(condition: \\\"overflow\\\")",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.CancellationBoundsNonCooperativeProviderAndDisposesLateKey(secondResolution: False, digest: False)",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.CancellationBoundsNonCooperativeProviderAndDisposesLateKey(secondResolution: True, digest: False)",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.CancellationBoundsNonCooperativeProviderAndDisposesLateKey(secondResolution: False, digest: True)",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.CancellationBoundsNonCooperativeProviderAndDisposesLateKey(secondResolution: True, digest: True)",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.IndependentlyExpectedConcreteContract_IsRequired",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.CanonicalGoldenBytes_PreserveUtf16LengthsAndAbsentVersusEmpty",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.SnapshotCopiesMaterialAndDisposalIsSafeToRepeat",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ValidityExpiresDuringFinalResolution_RejectsRelease(profileExpiry: False)",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.ValidityExpiresDuringFinalResolution_RejectsRelease(profileExpiry: True)",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.TimingBoundaries_AreExclusiveAndConfigured",
+        "Hexalith.Platform.Custody.Tests.CustodyPrerequisiteTests.PreCancelledAndSynchronousCancelledProvider_DoNotReleaseSuccess"
+      ],
+      "rawXmlSha256": "c0d100fe8793dfb166faa49fb7166448a4d80580f9f82050b67a0835ff0ff716"
+    },
+    "parties": {
+      "summary": {
+        "total": "80",
+        "passed": "80",
+        "errors": "0",
+        "failed": "0",
+        "skipped": "0",
+        "not-run": "0",
+        "time": "0.399"
+      },
+      "tests": [
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: False, inCustody: False, remove: False)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: True, inCustody: False, remove: False)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: False, inCustody: True, remove: False)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: True, inCustody: True, remove: False)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: False, inCustody: False, remove: True)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: True, inCustody: False, remove: True)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: False, inCustody: True, remove: True)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: True, inCustody: True, remove: True)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CurrentHuman_RequiresCompleteSourceAndCurrentExactActor",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.NonFiniteOrBeyondCustodyHistory_DeniesWithNoEvidence(missingEnd: True)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.NonFiniteOrBeyondCustodyHistory_DeniesWithNoEvidence(missingEnd: False)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \\\"unregistered\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \\\"missing-id\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \\\"blank-id\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \\\"missing-duration\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \\\"zero-duration\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \\\"missing-trigger\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \\\"unsupported-trigger\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.FinalAuthorityCancelsThenReturnsValidGrant_DoesNotReleaseIdentity(historical: False)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.FinalAuthorityCancelsThenReturnsValidGrant_DoesNotReleaseIdentity(historical: True)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ForeignAuthoritativeSourceAndFutureAction_DenyWithNoEvidence",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ProvisionedOrganization_KeepsBranchBClassificationAndNeverCarriesHumanBinding",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.SerializedRetainedSource_ReconstructsBothIntervalsWithoutProfileOrCurrentBinding",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ReaderCancelsThenReturnsValidSource_DoesNotReleaseIdentity(historical: False)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ReaderCancelsThenReturnsValidSource_DoesNotReleaseIdentity(historical: True)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesAtFinalAuthority_DenyBothReads(historical: False, remove: False)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesAtFinalAuthority_DenyBothReads(historical: True, remove: False)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesAtFinalAuthority_DenyBothReads(historical: False, remove: True)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesAtFinalAuthority_DenyBothReads(historical: True, remove: True)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CustodyExpiryCrossingAwait_DeniesHistoricalEvidence",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RevocationWithoutLivePredecessor_DeniesWithNoEvidence(missingPredecessor: True)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RevocationWithoutLivePredecessor_DeniesWithNoEvidence(missingPredecessor: False)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ErasedProfile_CurrentReadFailsWhileIndependentHistoryKeepsOriginalPosition",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MissingRetainedReaderOrExpiredCustody_CannotFallBackToAvailableFullProfile",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.WrongAuthorizedActor_DeniesBeforeSourceRead",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.HistoricalReplay_BeforeBoundaryReturnsOriginalAndAtBoundaryReturnsSuccessor",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UncreatedInactiveRestrictedUnknownAndErased_AreNeverResolved",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.HistoricalRead_DoesNotRequireCurrentActorActivityAndHonorsHalfOpenBoundary",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateExpiryCrossingCustodyAwait_DeniesWithoutRenewingObservation",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity(historical: False, canRead: False)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity(historical: True, canRead: False)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity(historical: False, canRead: True)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity(historical: True, canRead: True)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.InactiveOrRestrictedHuman_ReturnsIneligibleWithoutUsableBinding(inactive: True)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.InactiveOrRestrictedHuman_ReturnsIneligibleWithoutUsableBinding(inactive: False)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead(historical: False, inCustody: False)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead(historical: True, inCustody: False)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead(historical: False, inCustody: True)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead(historical: True, inCustody: True)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor(change: \\\"revoked\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor(change: \\\"different-actor\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor(change: \\\"different-revision\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor(change: \\\"different-source\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \\\"gap\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \\\"overlap\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \\\"profile-substitution\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \\\"wrong-purpose\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \\\"foreign-binding\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \\\"transit-expiry\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \\\"missing-authority\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \\\"future-observation\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \\\"short-name-substitution\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \\\"assembly-qualified-substitution\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \\\"namespace-alias-substitution\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \\\"policy-version\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \\\"duration\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \\\"recorded-expiry\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \\\"purpose\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \\\"source-expiry\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \\\"restore\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \\\"derived-copies\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CustodyFailureOrNetworkFailure_IsUnavailableAndCancellationPropagates",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ValidRevocationReplay_PreservesBeforeBoundaryAndReturnsGapAtBoundary",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PreCancelledQuery_DoesNotConsultAuthorityOrReaders(historical: False)",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PreCancelledQuery_DoesNotConsultAuthorityOrReaders(historical: True)",
+        "Hexalith.Parties.Tests.Domain.PartyIdentityAdmissionTests.MissingOrUnsupportedPolicy_DeniesBeforeAnyProtectedUnwrap(trigger: null)",
+        "Hexalith.Parties.Tests.Domain.PartyIdentityAdmissionTests.MissingOrUnsupportedPolicy_DeniesBeforeAnyProtectedUnwrap(trigger: \\\"unsupported\\\")",
+        "Hexalith.Parties.Tests.Domain.PartyIdentityAdmissionTests.MissingOrUnsupportedPolicy_DeniesBeforeAnyProtectedUnwrap(trigger: \\\"binding-closure\\\")",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityRetentionConfigurationTests.HostConfiguration_BindsApproved365FixedDays",
+        "Hexalith.Parties.Tests.Gateway.PartyIdentityRetentionConfigurationTests.HostConfiguration_PolicyWithdrawalDeniesBindings"
+      ],
+      "rawXmlSha256": "b704c7f2d30e58216527a6d4d97be47678ad37cca2fd94f32a5618ecac695a58"
+    }
+  },
+  "matrixCoverage": {
+    "host configuration consumed": [
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityRetentionConfigurationTests.HostConfiguration_BindsApproved365FixedDays",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityRetentionConfigurationTests.HostConfiguration_PolicyWithdrawalDeniesBindings"
+    ],
+    "unexpired retention unit": [
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.BeforeExpiry_DoesNotContactProvider(offsetTicks: -1)",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.BeforeExpiry_DoesNotContactProvider(offsetTicks: -864000000000)"
+    ],
+    "at/after exclusive expiry": [
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.AtOrAfterExpiry_ConfirmsExactUnitAndFreshDenial(offsetTicks: 0)",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.AtOrAfterExpiry_ConfirmsExactUnitAndFreshDenial(offsetTicks: 1)",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.AtOrAfterExpiry_ConfirmsExactUnitAndFreshDenial(offsetTicks: 864000000000)"
+    ],
+    "invalid policy/evidence/scope": [
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"domain\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"restore\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"source\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"receipt\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"revision\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"expiry\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"purpose\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"copies\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"trigger\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"negative\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"zero\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"id\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"null-evidence\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"null-policy\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"null-identity\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"duration\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.InvalidInput_DeniesBeforeProvider(field: \\\"overflow\\\")"
+    ],
+    "provider absent/false/throws/stalls": [
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.StalledProvider_TimesOutAsPending(finalCheck: False)",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.StalledProvider_TimesOutAsPending(finalCheck: True)",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.DefaultRegistration_KeepsMissingCustodyPending",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.FailedDestruction_RemainsPending(failure: \\\"false\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.FailedDestruction_RemainsPending(failure: \\\"sync-throw\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.FailedDestruction_RemainsPending(failure: \\\"async-throw\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.FailedDestruction_RemainsPending(failure: \\\"provider-cancel\\\")"
+    ],
+    "destruction true but no fresh denial": [
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.FailedFreshDenial_RemainsPending(failure: \\\"readable\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.FailedFreshDenial_RemainsPending(failure: \\\"sync-throw\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.FailedFreshDenial_RemainsPending(failure: \\\"async-throw\\\")",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.FailedFreshDenial_RemainsPending(failure: \\\"provider-cancel\\\")"
+    ],
+    "lost acknowledgement/retry": [
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.LostAcknowledgement_RetryRecoversSameDestroyedUnit"
+    ],
+    "caller cancellation/ignoring provider": [
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.CancelledAtProviderReturn_DoesNotReturnSuccess(finalCheck: False)",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.CancelledAtProviderReturn_DoesNotReturnSuccess(finalCheck: True)",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.PreCancelled_DoesNotContactProvider",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.CancelledWhileProviderIgnoresToken_PreservesCancellation(finalCheck: False, lateFault: False)",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.CancelledWhileProviderIgnoresToken_PreservesCancellation(finalCheck: False, lateFault: True)",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.CancelledWhileProviderIgnoresToken_PreservesCancellation(finalCheck: True, lateFault: False)",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.CancelledWhileProviderIgnoresToken_PreservesCancellation(finalCheck: True, lateFault: True)"
+    ],
+    "foreign tenant/live successor": [
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.ExactExpiredUnit_PreservesForeignUnitAndLiveSuccessor",
+      "Hexalith.Platform.Custody.Tests.IdentityHistoryCleanupTests.ForeignReceipt_IsRejectedByScopedProvider"
+    ]
+  },
+  "sources": [
+    {
+      "path": "/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md",
+      "sha256": "b5c936f56aeb2f18fd543c2cf275647b9a70a3f1575e0bb3e51605d0f2c0b64a"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/story-5-4-retention-application-2026-10-07.md",
+      "sha256": "2a3f3d284205ea6eee7a33e4ffc2b369a44e94fd646007eb0772d93ec134aaa4"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/story-5-4-retention-custody-proposal-2026-10-06.md",
+      "sha256": "a46f6b5d9e9574f1553b9e735f27d072e4b013a6fe07f9b2eb99d48348f8d6c7"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/eventstore/src/Hexalith.EventStore.Contracts/Identity/AggregateIdentity.cs",
+      "sha256": "eac15dd1466aa44cdb217794ca46321158325434a84508dcb9e46fc5ffb9906d"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/eventstore/src/Hexalith.EventStore.Contracts/Security/IIdentityHistoryCustody.cs",
+      "sha256": "7b5f9494d3c060ba2e9a157ee349d8936830bc7903294ebe3598cb957ab07284"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/eventstore/src/Hexalith.EventStore.Contracts/Security/IdentityHistoryCustodyEvidence.cs",
+      "sha256": "299291c4c38f8c6c089d36529517a0f3946bd375a41ea8594a0ed150375b2b70"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/eventstore/src/Hexalith.EventStore.Contracts/Security/IdentityHistoryPolicy.cs",
+      "sha256": "d32c3ba4c1724b29b918793e0f566f1d2905d6268af5ebccf62476e99721df27"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/_bmad-output/planning-artifacts/actor-history-retention-policy-v1.md",
+      "sha256": "8f6780d252154803343432e2126e596209e432284cfddf1c7f5b9eb596aa2fe1"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/Authorization/PartyIdentityOptions.cs",
+      "sha256": "a67302db5f47ee45fb972ce0a1ae28204f3e153f4ccc5aee6000b4524257e77c"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/Domain/PartyDomainProcessor.cs",
+      "sha256": "b0fc58385a83dfb6ec3d10849a99db39cf3589444fd96df16c061f5d8b131a53"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/Extensions/PartiesServiceCollectionExtensions.cs",
+      "sha256": "967c93a1d5acc22222884ebdcc31284de2129ed56c604ce34ee4cbabe4d35769"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs",
+      "sha256": "999280447dcd4969e19b62ace1d1f10328fff53d893c2c4dfac6fc93d6ebd928"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/Queries/RetainedHumanActorBinding.cs",
+      "sha256": "8f5bd2a7de7fa65a54a2134efa33dfc62a03d7c212449f5f9c1230dc911d7349"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/Queries/RetainedHumanActorHistoryFold.cs",
+      "sha256": "bfa7ff1402b3641ca91cbc31a2902ea5fa5f8fdf41eb4ad672f786a2256d205f"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/appsettings.json",
+      "sha256": "3974bb0107f9c6789f27a2c82ce84685febd2e9e1394b3875146b005a64ab063"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Domain/PartyIdentityAdmissionTests.cs",
+      "sha256": "0cd0c8b39f7ab7a1944fda955502de81f6f37ebadb8d59bbc87dbb21f2131d83"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs",
+      "sha256": "0bb08d7cebf4e02e8ad284cd8dc274a634d39e83a710bc37a761fd2caff723d7"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityRetentionConfigurationTests.cs",
+      "sha256": "15e024d3915c86570fdadcb3201d9ebefa890fc3a407ff8644250a2dec7acad7"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Hexalith.Parties.Tests.csproj",
+      "sha256": "7565d2aab001cd862bb5533c6ded29724aaddbcb5a2f6cd72cb76ea1d1761a86"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/docs/implementation/actor-history-lifecycle-2026-10-07.md",
+      "sha256": "78cf9ffe55eeea1eef02d861af2f6cac282f3c513207aa9064fe8dd4ad6bdd74"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/CustodyStatus.cs",
+      "sha256": "bdbba11231cfa282fc4069dfc0211fdf011e83f38a31e47a0e88b5a60be6fd0c"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/Hexalith.Platform.Custody.csproj",
+      "sha256": "fb859c5efd25057f468faf592cdba587635c18cdec3bc93e3c40f6e537b586d2"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/IPlatformHmacKeyProvider.cs",
+      "sha256": "929b244db6cd2b7297f88d4b7b0981dea59859d6cf2f394b49e3272e98950224"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/IPlatformSigningProfileProvider.cs",
+      "sha256": "11e9cc3cd32526c383d669b39a94a732037ed9853a25cb8cbc7d8392eb37b97d"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/IdentityHistoryCleanup.cs",
+      "sha256": "6000d2490c820b13e294f85a170e63030c32f7a1b107bf2fcc359dfb31afbef5"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/IdentityHistoryCleanupOutcome.cs",
+      "sha256": "c86707b2bbb70980e9b1896653e2ceef7f08979e17041dfa719c4de56945f831"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/PlatformCanonicalBytes.cs",
+      "sha256": "a4624fbc113d5cdc79fbf75e5875f2000a3821821e4eff8442c006fec186171a"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/PlatformCustodyServiceCollectionExtensions.cs",
+      "sha256": "564f7715fa04df908c1b49d0598a3eb9ca374c37338bd13d6c22d56cb0bf09f8"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/PlatformHmacKeyMetadata.cs",
+      "sha256": "6871b94c140f917a2847b6e4fa88aef5d85267cb070ec48bfadb780fd3fd95c9"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/PlatformHmacKeyResolution.cs",
+      "sha256": "51f44587dec6ff4e854efbfefb2d23ba206be102d26fb73f122842b7840a0a30"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/PlatformHmacKeySnapshot.cs",
+      "sha256": "0e8082c10791d7e5c318cb5fdf48bff7b5c2d8556cdc47587f91af6f4d69c26b"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/PlatformHmacKeyState.cs",
+      "sha256": "3ea583bddffecb3c583f733627f35d6bb39029cb7cf74caaeede572e649e81fe"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/PlatformHmacPurpose.cs",
+      "sha256": "f5ac7f0ed2dbef0f7a30a0ddd938006017bf521a41e10b8e8df519e039c8a31b"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/PlatformHmacResult.cs",
+      "sha256": "30e70b6dc05abeb2493c4e874c73949b4f191208569083b2e961d91ed8dd6aa5"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/PlatformHmacScope.cs",
+      "sha256": "67a9ad4a570dd7b8311615fb0fb120917ee16fadabf949ec81504c10ccad7aa8"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/PlatformHmacService.cs",
+      "sha256": "65050608d927fac05f13a26a0ec962498d4e422874bb2914898a08544f179721"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/PlatformKeyResolution.cs",
+      "sha256": "46c5db43be142f848700711d0ed9061607cdcbd73b39433b5cf8f363d0def8c0"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/PlatformSigningProfile.cs",
+      "sha256": "b81e1119fad9b213ce405f496d6be85240ad41b80b4644781fcd58af3fd0ca25"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/TrustedEnvelope.cs",
+      "sha256": "e056effa2d9745b5997ce314cdabe62afc76581429a5d7e2d4c33d627a4142cb"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/TrustedEnvelopeAuthenticator.cs",
+      "sha256": "c19e71232d64e58e9fda94b372fa99a010e8160c0ccea2b4d61c96b5f9bcaf7d"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/TrustedEnvelopeCodec.cs",
+      "sha256": "6c6010b914fb315cd66108cc6d711cdd92235f70b0fa1245ffc02abd3f236780"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/TrustedEnvelopeIdentity.cs",
+      "sha256": "24a07db789e949f143f8ef02134304580d874ba15de74f47face4e8f1a37ad46"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/TrustedEnvelopeResult.cs",
+      "sha256": "8869687bb94c3c9ecb424df72e6480eb2332c2a5a69241b93a1c2f2109a8049f"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/TrustedPrincipal.cs",
+      "sha256": "36dc110f0d9a4908b3146a2ab4f400e175b8f761f98727c067819de1d70b1b26"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/TrustedPrincipalKind.cs",
+      "sha256": "d24ae1b599acdc3a3e691822af71571672992556379d0ef257a6e81c46ddbe46"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/UnavailablePlatformHmacKeyProvider.cs",
+      "sha256": "cf1d4a65e130eb63f18cd8a635892055dd52ce6c7c5b5d72dd3cf3f5d9f362b6"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/UnavailablePlatformSigningProfileProvider.cs",
+      "sha256": "c1fd8b528ecc563a35e470a55e693824c385466854dd9ae35004e3ddbcf2a75e"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/tests/Hexalith.Platform.Custody.Tests/CustodyFixtureClock.cs",
+      "sha256": "f4e1373fa2560a60c16a6a5d78b31d06c67d5440bd9340c37ea2482bed25429e"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/tests/Hexalith.Platform.Custody.Tests/CustodyFixtureKeyProvider.cs",
+      "sha256": "8e12fd716ec36b3c89d881935baf9ce8b503bb556f14bde0b36217905d7bfe45"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/tests/Hexalith.Platform.Custody.Tests/CustodyFixtureProfileProvider.cs",
+      "sha256": "6561ffb22c4f852e8f0e1323d905efe589c4666ff71f21685c69a05412be83b4"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/tests/Hexalith.Platform.Custody.Tests/CustodyPrerequisiteTests.cs",
+      "sha256": "9c44e34e881a50592343a336f41ad2bed9777a5f90f985ddca807349cbc1bdca"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/tests/Hexalith.Platform.Custody.Tests/Hexalith.Platform.Custody.Tests.csproj",
+      "sha256": "64a9ab9e89c6f6d8d312773f6da4b8fc20b650e475899b366dbd2bdfc2006048"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/tests/Hexalith.Platform.Custody.Tests/IdentityHistoryCleanupFixture.cs",
+      "sha256": "10cd40f6c28ae5a6e317928cd1f17248dbbc3e86f5a5ce2ec67f2e15ddf62acc"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/platform/tests/Hexalith.Platform.Custody.Tests/IdentityHistoryCleanupTests.cs",
+      "sha256": "75382d0bd213ce5ade13eddb3203a11db2f7c1ed215e80e51bd246c791a7510f"
+    }
+  ],
+  "artifacts": [
+    {
+      "path": "/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/parties-build.log",
+      "sha256": "de6784a8691e43f7ccc6cbf12d95582a0bb6e073653f5dcd184a745efef492fa"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/parties-tests.log",
+      "sha256": "3e1182bc1f9e87cc4cee01734598ea0cbbc4c645f3e9149bf54af571701e85cf"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/parties-tests.xml.gz",
+      "sha256": "49884290fc56712d5a29e82e382eededddcb24bdbe95e01588c40817db5ca7bd"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/platform-build.log",
+      "sha256": "20f9604083bc45c209918e9b8bbe93f5af3bbd217c2f252d47ebd5dfbf9d610a"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/platform-tests.log",
+      "sha256": "106917cbacc79016b0d481d5203848e6fed942b3eb2791773d832197c7048713"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/platform-tests.xml.gz",
+      "sha256": "6bced44232d74d2edd422d3310928bf72b99e871275bc605961e2932b87fc670"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/pre-change-source-snapshots.json",
+      "sha256": "99430ec64cd6f18149945d75beb517ab0c175e3b2972646d161a713ab70a785f"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/sdk-contract-source.txt",
+      "sha256": "58f1d2852d2119346a827892347c24ee24455fa7e0f57d961910bc0cbc05e0ee"
+    }
+  ],
+  "resourceDiscovery": {
+    "command": "az keyvault list --query '[].{id:id,name:name,location:location,purgeProtection:properties.enablePurgeProtection,recoveryDays:properties.softDeleteRetentionInDays}' --output json",
+    "exitCode": 1,
+    "result": "Missing MSAL token; no production custody target discovered or qualified"
+  },
+  "limits": [
+    "No production provider or independent lifecycle/restore target",
+    "No inventory/scheduler or deployed cleanup worker",
+    "No live crypto/copy destruction or backup/restore qualification; fixture intentionally cannot encrypt/decrypt",
+    "Existing strict Parties fold does not prove successor after predecessor expiry",
+    "Full Story 5.4 and owner availability commitments remain incomplete",
+    "Prior broad Parties protected historical payload replay test blocker remains separate"
+  ]
+}

diff --git a/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/parties-tests.xml.gz b/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/parties-tests.xml.gz
new file mode 100644
index 0000000000000000000000000000000000000000..7c8c6d24c40fa0d817fb77d9722e883828be7d1a
GIT binary patch
literal 6088
zcmV;(7dPl1iwFP!00002|LvXYbK6Fe!2cDNK3sWgGuFHxzBpBi^Q^s#eU7vH;qJ=S
zGt(pUOaTl?%2EFM>me!2mMsgE2nX4v%O5BZ0D;C2{g~<Q`4ScjJd3j_E+&ig3eSL@
zW4D;L&Eq3=G@0hdN7lF;4Hy;>Gt){-=AD$xL{+kvC21XwCaFD}ua?+7KKka{pa1sa
z=`;5CAD@0NH9wlH7Iib^vuQJ3EIU}X-O*&Rgl@^YWqEugxwK4h#@!2U9&z<Z=!e>A
zBc1*$=Z`p@qSQlQ-MqVZ%2*jLcVAA=kbdB7{`g4nWA2XYF%#}KdmnxE<@T;GCfJ-$
zyS6#QW=VIdS)48>4}P=yFO!Eqefwe}_(T3s$jN`Fb~eMbB)PCTb}Y^43|~y<&;f0q
zO&(l*Pn(6FEMIj9dGVM3-N+DllF5+xa(04g44Fmk5<_l+%AlRXbTnz;3?CmIFVE)3
zukbgRO_#42oYJK&jAk7!=~KM`65O;vS}u;`v^oA^yXV7abvrH|zMxxQ9A}KH(;L2g
zm}j%2Nw;cPh9$l$(aqK>zQ;$G!rk>*E$Js%>F8eGwB75+N5B2jOqUNETu#o_L|1wK
z)noPm%4oW2I`=8QcR-c=leWW?`TUE&Z(%&IPvU5TUDtLCx;8=rrZdc&kGs*)v~)A8
zW_{?ql0awi^m*}mI-hS(xfQ`f6~g9^&Dqyyp#OC{=j*#|moQr&|J9dQ5Nsc1mG<PF
zt)=ZIH$0N>=}EkSi}luvA9H=w%ZvY7VR!KZG<k;Idbi1gX?`^E-Up{raKHsZ$jJ*H
zov=x&;!CtH79(YpB8O7EC{AD~+Czfqz4rbu8z_s2j?=NM$l#cEgugE)(u*x1_<+`7
zeoxLH6Pz<27o61kqk^OD?+K2M|LV(%%=(fv{dV%m85$s6lYz4!=?Y_t8G?owlD~XU
z?j^{F&)V5ET|9XO%_%OvS#{Iq^yCdpmoLA;=Hef8+IF>kjx&fjqp!aGZ9eTT9=sy`
z)ppYavqzKfV79<7Ce!B0iZ<qpw_7?qYtM1}CyBbl#cD>{<rzurN<PB4kIIL(Z{%~!
zOJ_(eE8+VpNT;+j=I!pwi}`&ecr-afk`g_%yNHtRuU&NsSs}Ve=e=sr@c2CwaXjzZ
z|HG=~E_LDYd~=z{%e98PLGf*`{`i*XpB8T~fv$=!!Eyrj_#GC7k2Z*?Sh13HHe|tq
zDV)iWtO4n(-W~~yFS^w|P~_Zx6h+$J3q{hdLTS+(MJ=qg;ecX)6cG$0be^Ro1Jm3A
zBRwe?x)g*;LMoj{s}?tK_C-<NfTHY&q885Z{#sEhZ`}dK9Z)3u4c<jZ00v-MWQqoO
zMEb;I(UJUypvI~dcOy~VK%(kNq6*UOpcEYsSGylb?4cAzLbA+`c?8L72*QBN%*dn7
z+Jyj0xj&1qSjoHz#Tzi>Ju!6TQJNhvbU_^y;tm)lRe)=m7zMP2>4?C*%!RoGAO$!b
zEc{8_#33o9xYPBg&?^(<fx`Y2MuQYAhsY?rrdVFmo=`wBm3d0G<ZO)klX!>&Qs^~#
z%Ka$}+-iHEus?+vlF1Mfv*0+zDJrqZbz;t$5RD~3E&e&2!vP@-LWo@;v|5=1g#97p
zvPj1vGgS<!LePdm5Qb@$UD75(R%t&FK3R1gefPua44RkE@z>RqczM#E&1bm8pVt?8
z(&X)4-~I+^d6L$XA2);?;^SnAo@5y*l<se2t+a-_LCCtZV8#ZQwAbJYJ_AGm79tl_
z!%@(YV`k{c*RxItBlhI&{nR$!(Ty+hY4<f=v{hVdP4&ZCiCxr}l+IaS)1TUJ&!=>r
zgb&W9i$#4`oBU}NcK?AEt3=?cKWxS-KMZVl5!CodQ%+n{V-7hK>B)tYNO1+3(GU|^
zRdC5fD;$_S64E|7l5#(5X^rEe|0s&JN-KH5tUqQkiV(O@%#@f2v#3}w%Ci!ZD-NY8
zmhT6%Uz*f5WqP{mFmGIst<1h|m#@B_HkojGUMI~j*?KhjpMU#x)h_>i)pU4D5<@z2
zyXg~(?FK8lF{3?*v(CF66r1y252M(63v7It86~d`8|UO~)n+^f%e1R@6G~*{Tx#;x
zWXewd(U@)jX4BgsyQfNXL5BV#a@H$n^Z~G20E;PTN$QG8Wysf05g15tEL$H@x)hVl
ze>7lm2F>fi@$y<~xdUEK8*#wv7QDPm1w4|%vMP`drV0x=Yl<<w0x|)aXFC@Cl~*LQ
zY1<5pnG08w__t#gxRvsN*)5o*XbNy{nGvSclq|m)#?4w<UUJmH;g81bckJ5F#8?ng
z>@c7a$h$qLvfCKTTE$S3WeN<rVmXtg%!RWoEy{qFLQORmRkrM=r>EErOqu51??9Qg
z-W({qg|dJ?iI5d5Sx{W*!LpzzxQtRq@@KUS8vgjWa<Q7v+ir>ZGm++m+QIV=b?pB@
z+AXB1P>3^^nDPdg4jx$u+%XfA0bhzkx2F+|@1_mRPF5tsj=p~aO~TpY#Vc%{<8swC
zi@(#k^Y<Mz%a`A@xcI59(<cZEygcQH^Po46V~86DXhqKfTE~^*{f(!WWNe2CGw&_7
z5ke^VwPfKec@Qx(=VPg9aX|3_N`|&%6{O4}=P^P!>?DC#T+9BWx84{(P&Vej0)sNr
zTkM5oCTqiVfyBHhg&aFHDQS>0#XT|Db~rt4F6Bv)!1>j*-ghgKD4wh<Ufxb6zdI&A
z6l|7MSbsmx2kr;Tbi1E;MmJ@^!~2{SDGX#8n1>LEGbx#unmB`^12+!n!=AD{y7qip
z>%TBR?M|VY{=N=6U;YhozWAEn*7WM@C$$uBT1cJMuSmc@;W>6spzG-8)-%vsf97WJ
z^bO$Yp5Rg3DQ$1?g3`thlvcD~d5vy1UYTpIA9*bl%~BNWt*?_;r14NkRxr52t2N1H
zKP3K)-INah9WxmWpsUH77y9LMBu|>;u_7^4=lREGiQPH5cH0CRdL&hTPrGfl!)-7M
zcZ1v4uPlt{qUfsk^pzL&-!L)8{zW!69)-SUfgwm#Udx&WOcq#xW&x96u|%Z5GQ}x-
z8YQm5S$%6i>c?#5eE%5ao^z>0mla<0hfx{phlxS%F0b)nr<HmiJCj`$N@Ye72-Xoj
zP7yFEM3hNo5n|z4jS9P4qK^Agr!4u#{i)O5b910>SLz(ttbEoi8A-;sIKeyz&p3Bp
zm!Q$5AqpQ(mhZ=MYahCrk<A$2M>5TO6@1-mWX}tujNC!dky_-RA*uJ%vKnD7ud&yL
zRdloo6B*P%J862cnRhNTD`WO00vNCVSjKj!DZD3QR!Zk~K+Kxp4v5`~7@$)jCC;MB
zY15holCms?AWIQGdZk9C#s(`*sLTWx`XlDJRq}w?t%#XY^Phzx)2K7k$@@AD7MRun
zKubd*f*6w)8;~$3xR5&#rlbi6!fqwZrVuHR2PTcLH80VyC_J+4T`631t3mCn=4GAr
zyr~9y(zWXu)wg9sbqMJ#R=1D%nj2^`eQ7dQ^*4!9dS%5x1uu7y%IGE(t`}0dn5{%$
zNkxK+C&z*=$RrSI35_i(?klW3Z#UzS+aaL;n4aUa4l^cPEZXjqB?#~AH}HIxmr6?G
z`vYeJY4?F5%R5MHgmVk#+K4V>jaJ*lB+6Ke_aXyl6$>E+o3s^TUn9EvV{SmFp9l*5
zlX<<gBrAIWZWrJ{Bx|H%5Gk(I)+H7}NCrl!B+!K7_B4v|Z3hd?+rjOZ+p+EHV0;JD
z+2S9sXzPzn#yK|i6x*_*^~}xFZhBh%lV|N>TA#pBGg)3bu4MnFn@SLG!;IJNQ&^*$
zHjvjw(sESfnWW5&s|9|N{J1E|+7@Fi7eKMXz6|YuZq(Q1gv@4q@@cpBTkx`??d20_
zns!NQZT2;!*Du<Wa}qkl8Gg5#&9={E5Ot?Su*US~PkJ*@?bSX7HM(h3DXzz-f}3nC
zSy|&LKIM@Gt|^3&B8qHn^l?wcS^wx-@{2cN*6*0tRo|DHhgjNfn<i1kJq%!qd)t3%
zKq>3QFe4m0Nn&(XR9}lfwJj(L1m;W~*N_RAM<tk$A))0(Lfli|)~i73InHNr@p2=r
zuiI6Vp}W{luv{8}lO~@muYLFPDs5br!6u-T3OjT=^2&G{hKWn}Dy`A!G4HPFG3SAE
zWtkFn4&Hjjq9<jRgO0^0F9hP=a?U%XUGt9(q?+FKoHmo3Y0EFo>!y8E6Kgg;%*&Gw
z*C{r%OFKKqJec>UtgxoP_olh!VX(r|dx&RrGoSog%wPo1LYU0Fsx|YR!iA8tti6te
zl^UE%YX8~}^3`1Nx}(b0D_)z9nooF6ry#lh&^B$i_yK0+DY?g&uk&EaPV>N3|AZqg
zg7d>GyZZ`jbh^%6v$)CGYyw(_1vD{e3=3KmMuw+oo+V1<_cc(m@qwRUU3Z}74dVOb
zoLg`B-Qmu+-J=?OLwh`pi<;r}>DcwY9FkkR!%&G<#u_!yP|1D;HoEwgU+*D;Xq^g*
zfkuj7i<b;aZ4u0^bV~c=q}c21!-p9)mv8%%wyAWzKb`ZmyEO;yi%Ei}s*S;O;<O4o
zacZp7VaQ2~`wDY}20Q5MC1O#e5|F7-NJT22Sq#xLRYZw0AjddjKsS@K{rJ3Jc&v>R
z{YzoJw%QqaVDnyVatodaEG!y_OdCbYv#!UIGGq}ka>wJJM|lpHI-so){TG?=+HnyM
zdh>p4YLz5NiHrjfn_4RtWXw#UMVTpnL@)m|dUHTD9p7cxnv|w;_JHQS&`jiU=HfI<
z6mHi{D#?7|o<S%|6&_vUd&&&G9K)zmx*Wy0>}<N0?T-!4rl-?5!>t$p%c9N!Y|B<2
zOt;4mOQ4rw2dC0o>xMDh_mtKMBX4u9$S3=3!28IosyWFrS{6YIMy6Uy#dESejg7n^
zeMawu-eJ@+D0NkwaX@b;^uQNoiU>>usp?XkoP|i}X<dwTR!Aw`-g?45Y1=t=uo=GH
zOn+X@d(LL-x|WThdAeE>C2yOC^jZ33!1y{?Y5I>x8o{L&Lv@V3vkXUQfsMc3($jLH
zPcv?P?Yk1<dYMLsLWvl3R1(9;VEp(3D8uMQ0?To}1BZbNci?b$4iooD<^YU7VC|Vl
zK?+6A42yC+k*96q_?6;tt|5V?LjTEVvLoCd^x+*iRDuji3YlfpbvfNaexZ?>MQ;Tr
zk+_I@?D}v(45ia{2eYh%6$cFOfT7EvLNNqibWpSQ1@nN4B_*Rrk+KbDf5qTeEtTF~
zM&?qO?<^$q^C~XrG^=GTjHr{a57PRGx6SxEJa1nQ-`}0c(%#$tAX0qe>|oX5cbDJz
zP;}SQaa2SX3Rot6tslt*VVx;tNgIrSE|+~x3+|W1JS_!lvH*kn^^h$g39mqoUw;~n
zmIJj>{%5CAP&k4~O9s{^rb`6oxdBFQcR~dvSRuzvBWcKWbEjc}G}`uGK$&)T(jI`k
z3y{W2E=urqrBF}m(Np}avu4_YEj;iNaO6O4Zf{^5-A+-o<ys%C^<8j8U$96Qh{RNf
zbLbN!ieVHw1-U4bT^KB(>}mJyIqiNWwoPxutHh{nF-0x4h>%`CNNv}x=5@cfT%N*w
zc<0e-rFL*03Esg9K<_QY5oVTs;6euPvQ@|sIo9SJo^+%~W<A8>A#?O*)KIqP*v)PQ
z^PW1=IW2Ywt$ocvJ3x77C=Cz6W@{L_>PbqI85k!RC-qsJDu^0K4dr~Z3tPN%l3@w;
zTDl$7W{{He1EF^&v>1X?D2lv63Z?-l)&ZZHG}gJSBriUXn$S13I*!d(-Mn24>|h3I
z{0<IgFlwMhbN@Ba6xzAyIK|zzRx1F7;W4SilFoo8>4;v98qKn4?#<#RYhYkF>4n`z
zZ+hqD0nR(aX|r;IXJFDhT|;uqKssbzkh#w}mMq7uH%X4oV!C9wt~VK&%^-HDHndy_
zCk|}hna!wO$^|?lxmU$miw+$)=4&=zC|0->N6qHtnqF`@h%zXi#G93T2RxN`2i19Z
zJiW8VJC~U8XzK!*ysA#gxCI?`6jrEl2SsJQE(dF`D(?p7Q%fIq;L`?u@H_9$Cl-m0
zN6reC%yi(A0SL{MQKBeatQO<;I~Sxhm#kjk=Zo7dcgmU_APpuQ;`X~kO0hYat>~D_
z-Y~7|BGxw7j%1-Y6##+|qlT2w!?QTMV85>D;>r~BXM|O7-hcfk+FfdXu=sa}6<kij
z=E9=lo@o;V^RiIvR28kNqw~U!8rFJgi#gC8(zU_jbCN2ZS38iZgFb{#_n1X8u?Psr
zoT|Mtk|VV$xn;gc!>zYTa@<>gy)#&*SGTOHx|QU1;<z>yT_1HdK)*hionyBco<+}h
zXlZMuHOddwa&LFpjj)Tdz1A(LB%N&Lg@s7kQ9B^|GzVr<QJK6_A$S-SeV=1(cLK?A
z)4!bCDw0w-aJK_@!UrE*$V|Gb^*{yYjZaJ_Z+Hq4%f9l__rzU?WTZ2>jGtljl6cZ?
zhsZ{0ZukMe9q<b_f)hrwT$HP2vXMc>uZXC#SK4GbF8n^nY$I``h1~%=XRSG4w*z*F
zkb<Z_y{m<>4l;`<9kWV#asea2zA}dPgx&UbK8w!te23&UtA&&Yle-IiIxFdINz92&
zRdyg(j|k+BC!;yn7S+g1ZY>5ISm$Z6!}7?qyU_!+CH-f`E|O{^i&`5;fnqu+(tbV*
zvbRDAS5v6>mF}~z2m`z6IXQZ%ozu|+(`SO-!O_#f4wORi&q^PeTZc9oVh)k%n${E0
z7gkCJit%DHa=#7C>JkvE4xe^E;qpz}z25%)^2fz54P18MKWXx&f^)WxWuJBJ98T$H
z7K7!6TO+LLUubP@4bO(`0(D<Gjjro_;MXgzbI4hcqNA9C$dffCB2)ZWlJzMn!^NIP
zR@MvlUM+#MZKnQ=NW$*?vSa^c_h1s~w&Be8Fu!W3K8$*Eb?K`9(0QTtFkNr=BcjnU
zr>^z7rO?6yImRsInp!J!WTDjcf}%p>kp~<0wOnY2+cy*Q`rB(g+}>;7R*TpdpL1z<
zTN{7xFozMG9cm@Vo#iybDt>W2oknVHGS_59qi0$f$s$zNxFFjaoKVJ%0<+;dMv#5v
zJ0uu7uDMVL!fqk#IJI;esjBF&zP$RpMX4S<d1ou>?8yy6{H8sFY4g4ePx3m+uWi=5
zOdd@0qe*ck$KrVLsrESHCP<`!#3*ZX3dOlxoKZyrSFxlVG<W2%XApJlCKpX`ty3d}
zaWR;{5W&>YTC>PSW=c?KZ3~?tldUi0)nHzu*R%xJs#$-Y*Sv)4FpyAQPv`UPE|<zs
z)K>W)L6x>|P+hi5nAOAIoz~$1JbJ?dte0G^Zm-jY0S8G|K%vdImG9mz!pjA_o_4<w
zY4ds8Ew6_UYrA^VT#)Cs#LWsDZ#tMiSa#FXQ|um1n$>J}qa6^+-Eix!*IP=_fa9+B
zJPzrtAbh{K^mgfduw2*fVjAv6-CX|hrylNyGuPL9xlrt6>tf_F*M+&HB?p%$&jh^*
zMuBzy)_nBO!`L>2UtOJG=u%uKG?!fU2hVcVvlRCL-VsE^9Ft{S*~m0T$9y8ao4D5C
zgeW3~F@d)&#$jpJE>^>iXA((*+pq(8y(`B4b-~-4SMpKUjzZ1)q4xc1%Jg)b-tdl(
zl8TAJMGX>dDs_I+N*z_g5Mp*JN=r}H$)sx=H|;G3ED!`aFbU|evh`7d78tCKH3iPp
zwPZaDLTTn*khH0eN!;$E$dCId+}^NuwUzJrDDvpNCfQN0>p*!ZlIiRM^WJkNgHm)m
zu1IyK$*@ld^x+TfV)@?YmvwY=aWW^W&M_-%zMKAr`5U;nc__}_aI3txPa^vFW4D@f
z=kGD&@Cj+WvpCf*V)?AW!>1nRhwtkL%TP%hWLPhSX6sUDoZRx5STZ5!gcj4?GM0BE
zY@_(<$a(JI4a~M0aAOn}LoEkywXw>J{+xNi?^D)(N6u~&&Uj%{`a|Pze3@c?^^tz^
OtN#ZihVF;R&Hw=3-1KPx

literal 0
HcmV?d00001


diff --git a/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/platform-tests.xml.gz b/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/platform-tests.xml.gz
new file mode 100644
index 0000000000000000000000000000000000000000..4b98136af0efb2122c8c678c9307b8b80e2f1c20
GIT binary patch
literal 7270
zcmV-s9GT-EiwFP!00002|LvXYZyU*x!2cCN9}W(Zw)@>(dvO4-y-tkm!?2y?!`;E?
zN43N=HN(u1+ExDgt0w7}M6Ea$$7J?sz_LXSDNXU$_3G-XKZd@?i#VNNf6}LOya?<P
zyMEF(k5AR<$s|8M_0k$`L}Ds=!8BT6(R<IV<W5K_90+%MlG=;eVvgP8(`Vnk{_C5c
zUb4Tx`03A5^V5?>Ump(nV$w|dc^BqwcY4y#LpNvLygWXYTv{eL<L(VN-*EYjhKE8Z
zskr|==ihL8i&9^De#_+s9x5#WlzM;pd~$*G11@HdPX&L(-J|-N6Yf@5pZ@8O>$|=>
z!RB((wao=K^T(%FvpAWbJov@x-<>@C@w+!Cf<NRBg*^GM)J~_E<|G$3!;YmXy~7_)
zW}yr8`1It#$LHx`p-<-L9fsWh?tfM?4AvXV(J_gVWM~91P|h$3PG+96Gb*2+G~ogt
zpFWyj%pRTNFJU^FpR?IC%uCx{7|o`Wg6S$vk&);oC8T-(C{CJ3e_3Dq@a6j2hff#%
zyv<h+-;fmgM;YVd?5?XG=IQkGq+2vBhdF*K<<+BAq>oSEe?CJ$zd8K~Ryw-BpWE);
z<I`XM)=cIP8=Rk9suNvh{CkhthoFp3bb08`@Us`xPvdFZ;gi|y4}V_+dRaf6(-Z8v
zw(IG4BTT|%ih1?lVZ<~o-OQp{zI0aUpmz~M(0#s}%x3HNT*-ibs+4~9-Rk|9@A1v*
zan6_b+s?yu^$vgf<3~2upKulU$tRD}r*U%E$MYf+qLca6UuX%n-PO}6hGsEaK6mn9
zlD|3OXk`+*@EmnP&3Oo>SO>ZQideuR1mk=xbl-K+CTTn>6M~B-8=Zf*UhSMa#|sH7
zO2f3GB@c-g=D=hvXvGVzJ`?mN$!T?q;JDWt1xJs6COCS7KmD;nv;3VTNHn?#77Wi!
zOQ{*qt&l<zE-7KM;`n~`+(V8JpUi*io)l7I-~BS1bXRYmwoRFI7yWm?&|0g+K56pv
z4*T<G*i6FoL0d|X^EZ?9t|$4E0O`5KloZKJlEeGErkj)Q=1ueE<Exv#V=YMz+=JLk
z7uL(QQPzFbg;%rtiRJX<0_W!~eGUWVLpRngx`eC{ETq@Y+Y5a3nHhLA>)QXts>j~z
zyGQT!-J|(ZcipAie(mE-{fFORI%()P(6nkFeZrLuf!m0j6{iE!-ej_DQZhxDc@#dl
z414EF+{V>rrdEIMW=eY{wceg75W@H)Q+G0zM2yOm#CR#5X-#ShJn2s;MTO#XPO9vk
zDQ`7@|7Rq;Kp1HT;3dh+JmPgHUg%}87y~n?G$S9zvB0@t$T=$MZ7!1U0<Uk8UVv+%
zJ!`T5v7M9b<|MV3U3)pn*nN%^8%a+YX*Ia5K_b&<C5!;l76Lgqc$E1Zo-9R<ib6MU
z9r;l<NLD>7hDmaY?GoA;_XE%P3dtxZycxg|P|$niXd_2HV+bh_I5u&nOUf+7WSLD#
zdWvnMDR~zhJq=Alijwk^53+fM^F`OZ7u@Q**Nc>}?;p^I8m3RrF}?fd<awC(c)Lel
zBfCvBQFOB%O<MW^-jf9hN{&F&FbN$TSftxOCriXcjzCN-C~Yay)0}w~6cbJr<XR;O
zY0qT6=@vMSigH}g->spdP~5-~s=J}eTIm1?B)*z_%g!+`tY;MaDU93%M6nB~UUv9F
zbhoPZTNDzPx3D9(WJf#aw^vBcdi&9<+95kz$>7j{5%zOnf|D)_vTB0F&osG|EEJ)b
zc0d-dxNNiA1u)N-S+q~WkGHSzB21e8B{q4|oIR*%wnc~Eoc#BH{BzOH|Fh<lL!9FJ
z+mY0eqZPD$wu!hS&uRpe?vl>IaU>*s&XEdGLM(+9M{$~V!7|S+XD&$!;gl%o(_T30
zubRZ>=Uw~r$RvSKetVL<^vWJd+CUN*$kLFrZ55A9Yl|$Hnh@8T{xuu|n!S(|z7|Se
zDLVuuPk)anZ9oaV6hiCBlviZj&<GYo$n-^9Md6QA8ux<IZ0#Sgv~(aw#>rbhz{Y`)
zLLYJ3fK#EE*~?S~hMMW9B@37{Q^p4X&B?(IyTECguwAB2FxR~8Tk<q}>@Kl?-gY=S
zYu3r!?<beIj!fEa9Ckn{KOjs4kbVqR^o0^RG@yWA(@!}=k|i)_ogttKXEF1_P+LKQ
zM~zPHL_e*B^a>MBX7jf%`d3(Q8}qkUuTwk2d`HZ7KZraRT5hj^Ja^oTk&YZ7g+q(l
z__Z+KYJx-4^RZ;oMA{_g9cdz8lnH{{lJico+pp)f=I4W8-oB~DMn5g)Pd-|<EfbOE
zAy=Cck;})o<VbMgVY@OZ{ZI)C?aUb4BX<hr@Dz`}meod-oC5pElFsB~d#xy1OP&?2
zv!Iwb1dm^zpI=Egn$75Jqk>i}IBWIR6?Q@yW5f}iP3V}SKyj;BkhU-l$e9;LF(-vV
zuN-K-lMLhcZ9jjK-ZkydB(O8ARg2^>&buqp-)*hid`&;%8C}}7S1YIB_JrdtHA#Gn
znxrKT+8DPzJyI$y#z;7RiAW9&CnrD0Nty^!aK}JJ!b#Nq5My*L!~-5}v{LQFr+$Vd
z5F4-XY<1_8wt4$k#99Btr0;3rt>5XxG8`dyy<|htT7m6RG9WY`0i~hhI5dp-HBZ`L
zB6Cj?ERinK1PvoyM6OXzNe7~cRMSp3)BbTzqIj>zUz|1MTlR0-ci4OkP1e^gg?rOa
z`ojr5gU{=yaOW28y{WTa`2o~{g;6p}Un;%BbI7H=?vT3#5y^w{kcqky8CX>q&%p%%
z>!r;H%-^qV^b%&bR1k2nC4S(Aa$Do4IC$Vk{088MS!It>5q?22Z8N!NKu(zr^_+pv
zA#isv_*K}p*M47~dswn0xOQ@T^vKoI_K4m9^aL-G3niGa9GFfNPX^;1iB@0|fm@C7
zaM2s5!exyTfEhsErzZMH-WKE~GP<TjWG)2?9kMkHv=1y~;NF)aOp3eP-C4#}i{&Av
zyLNX<)n}va=`3lHS`Uawi7qkX7UAupIlRC@$m{hlA|Nkyk6B2jW8^HLbu3$#EqE`4
zD0_kED{MBe(Xd<Y@T@M^Tpvl=L{iPjXKw>ju7;o{6G=8nmQ(;PJy;$2UPuaG4JECu
z9e|Q2KjDbd29%PQf?Fq;FR7NmNQx#&9g%lsELt0Vw);WpYt=2R_yFa^_Bd%_wLRjr
z0VjikTqNt6LtiU9MPacjD&zp#7LG>k1g96xCA~JkXl9G~+gjs+{ptwFhm$a6g46tg
zrrcIJ-T~45(+|oBF4guRSwe4&^uwLvIXIH+HISTvm^I-ewaQE@u33~;ko-vT=aLfc
zCe1JuBm#%tV`P`xDlh|*SmafWa)|ifpQ2=)5r){YUKw(fif4hR!Z2E&qALZ^?}MV%
zFFi6u-ni{My(q`I)JKRmKoo?ukac8ekW^6&nR(0Ui`S?kL{dd@KdNY1T{JF5j+-H=
zB`1`HBSjl1iYT;HB{9QEAL*hgX7iFWp=|<mI)Tvpq3Ci_`~HlK5ouvJ1S6-#5u*(l
zS)Y-D*~}7>m#s^&EC3)2D1)%Ynr!!?jBt7MdTfY54zXyGpgTge0U}Q}OLFfRsUp%v
zGI$0m2&RgT$&kjEJnaLb*`k}ZW9PG-lzu?0@9Ha#6m6hLXNU>`YJ-B>!O#PXE>oOV
zjE~xbH$v@$qSVgll93@&6i^I+NP{y+h&Dh}4B0GBahc=wFlWex>{*P=nS`nYZ$d2l
zfN0)L&d#tKnIdD%fGoE2QXEy$#%vL{0J*lD<2eyUQnIMBVB{;Mj1ddsUc90P&%%6i
zIkrXfUJlry^<43zGTMX@k6y_DlG$8$Xazb3K>^FsJ5!8QT<;}&{V(j=aS`%-P_oDi
zsgDS4LP!7zuU%m&hQKuEhDDBvz8EbuX(r5K9|%qI$N&in@dp4Ttv_ZPH^yepKuQ4^
z$5K!Jn!q4h&P))ZNNW`@`!QvU>6Br8rWreP?G>$}0W8rfJF>KK`vWCdBySphDHNfR
zMhY@T2BKIkL>3P8-dLKo(@7dRW90QPvu30?X0JDIq(KPA0&|W6er*DUX<S4W1CLf)
zuR?*nku+Jq;m9nRA?G@kw{nykk$-=d>SzW7JTd7!xkx}EzZbf;3l~NgnY31WVJR<G
zr_)Bp$SO5NC0S#S#%*)@F)JRlV|5$@sU)MAx6v|-sIsw6I<*($R*%P)>DUF3-da1v
zNAetw7=41#qmP3^b~H-l{W;bSJN)Nj(og32$s7^Xp~%2<$w}E1q(Dobi4IDj3Jw$5
zQ2|<K86$;>wT~USZY4@?1qDl()^kLHs~a>5nb5)3&WV{kx|mo!2A(**vVgdAj)?V}
z=7<n|R&Az|f{steh|udls~{CZK|P>PzwWAcdepQSg#Z)1q1a{nc6_(V@ZpQTU*O-d
zo0O|3O@4)W($B&?o!3L@VbXlxo=sAiz6ask#rzePRr{yugx2HLtrdUDZoXw@w;duP
zIJW~DV7pHaWpB3-ZxhNVtMJ~^8Gn)9`qAE4p+;OgamcX_(~&F}E^1*i0hY;MFhWs8
zoGj1s{(wC_56v0oCyV)cl~M|G%+IS{|D<`(^&+n2V6RbYN>@#>!SDes)-6RnYS)Bq
zBc8*e$X%l-I?_*8h?>!@v%-KP7~>)<sgNelTJx;-K~d9=nNL{}p+BiI-U1-cxjzDQ
z9UyDb2FnABqIP>o!LtBLvEYi~G7D9}>;s@><;{ru-D@V~5G!Xv!%+v_KnS^yT#=W=
z>#P}ViY09n1yi+c9XW_DZhvlTmk3R&|By!}2tW-W$VoMHa?$?>f~3r*sG`W(&b5b;
ziY!KqOcdm)_z*b2J_wqwT+DG1qLAJUaD<#TM@MLbBjllYa5eK*&$2787+4dSP%0Yl
z&~O{}fl!{D)p4flIk_W~q(Khwh@91497(!Pl7mdqzAB93Gm8ABmE;Wh$UM2&<WdJL
zIq!oc`asXHn{|_BJ~Bqu+aWfsp7B3obi*r>IfvvEvpyT9gRv}drx>>>3vR4Y((ePK
z`C>NmOc3MXlTD(#0rLEl7THIDKIkFL#@5Qw<V$9xswv2?H8+KqY*6PQ%3kzP3X6W+
z9i=rDTKfSI3FGAvq8pwy7o=$j9TQ`n;#g;acwb8aW%9Y0+E;Eb5Vc*0)31S%uxh|W
zP%n)*W{GZK#5F+9WXv#-)U@S>K}x`~PW2#((k9|QFd|~cJ#)N<0?Yuv+8e`<_Ur~j
zLSRnD36=$cq!U<Th`b`FtyWnisq<bKnqw2jcByNl9yx#@r_?b`bTh^;JOw2qbD<8{
ztLJNiiBL<C$xm`R>)IUq>l4(@u=^UO%sHb6Km-bortAhpDxg*=I)<rE{;=c>dG9ro
zHd*QGDJ>oMvhO-MYbe71I+-F5&TKzW#(;3@Xwhz<q)Rct$&UF#T1ZPMjG~svjL1bs
zk7VrRew^!ugp)?LX&{9e5aok%yDt&>_h%?%$y2NWS^#8P1jT$Xi6yI~21Ee4m#q)N
zXL31a_3;V{E#v@`2G;PSUvx9bciK2EGE?LQF-<WU3mgIidIBNPQ*JMw_0MhhuGWmQ
zNkb%zUJwF~J2e0%ua638gF^BqxD*RR>6eqOfO+dZvl&w0&PH@)uQ0uvG$U{7gLkmK
zR<fK6ut$(?XeAS@EJ3sArDVEJ*Q=wgn01-gfS3onm(*j|j=cXtK-dA}8ZA9&j~i$=
z_yHb`S0b^TYY&YSC{W`nGnWOq+e)Iqy%fmC$+Bzh$OtKVcmP7$X?;ZKM#|AzBVy(Z
zlu^}|2^LFCOk^3d6DjDjmso#BpZ?Xz02#jJY2(%97Q`{azo~-~7-yAWV3(zh+%s|x
zA_GoZC}|gr7ki4%+HPckoV0ELKpy0A|Mh0JQKce_6RZ@dox@FLQP#OqU{Rs)(rDZp
zK<q1QeOM0O58y}$>yC=(21nTlgW4-*K!#c-8(0tk%n?n}@ffVyOPXl(V9W~12b9Ow
zvG4Y{>#?Dz(L?qzGjBMO9@1pU>Xa3vXt&6ev(EcTGcNK3$Ic896iyCd$OwB3=x=Vn
zX6<}EYle^o(=r(rHF#!i%sF{uvXy%~=P|N3B#aqQjb?@8M{npRLEfO!K4*rmM5Y6h
zKH?PDTWO4OGFun-;!cxDs=P)<$SF6(5%S0VvQ6g|o#u+dcE)8z2uc6=Toj8n*NDnv
zN_#=5ZCJ$fFfAiP1Y(FKliVN688_sGg0@&{Q)kdnd-WiTA~^Dfg!hHVL`rEdrD$Q~
zw4x+$dce+x)W<W^n{<${m@>IPCaGG<=qv+nkvwWLa3zME;r?mGmn)mJlGd|DH)+mZ
zVc$*{>j`ShoBwH=U#%xyj0BAPq#pWiV3bpK=h6FcNgNV9@_OSBZGzHVa+WM|qDNR3
zqP490&$`gF9`3&{_OfP}pLd9VSzLtX^?8^fNs2BZNS@awDRud^UG19^`bqzw#_`{r
zJo!{eBdH**6(6uUW+d>P?`#Z|&S3#1q`Q`5G)c!Kkf~fd&&B0gMHN_-1yqbk;eFZ@
zQ15@rNGuszNCUK!an7nEOTWfak)Bp)%8ZjkM4pkhgc0XVhO9ymku=l}@<h|N$NZ0V
zLB~pDPeaqR^QWQfu9kxQAhk6~Yrp74GsjxmICh1st>M~${WD8``R>!!L*#T|V5w_h
zr4UVV0%|KY`ofiAF?q`nZA_(P@Vlr6`EsycU-fgmctgmHoJq1^%mA>Q^dn@FenVhM
z5kO_&3`25c{gPs}DHbC|g%B0jnBzXd`g84^+0^Ps@?+=t6;zi~(#rvaX=BWhuwPMK
zPLNwJ3iAmv*+j0GcMzFz9x<Ej0PKO-#AoDn)Sca{A0}sC;Ybp(1C|d!%}Sw<sNIg5
zgIaBpYkh#0OnY8f2q`egN}vdYnh5(u?P=T8^GQpxp5YYhzogb?;p}U`X360n0GsE^
z9$~v3HY)_z$`(czQZa3kVZm_0LW&N3u`%x8IOQ?~dfzsAovV4B+F5PvT9Y-e@Ul%y
zc%C%*r^P&+;e*sRc~T!5c1D*_lD8lxw?xcK?zS(xbi7Wf+BITdAh83>c=78)g1wRC
z>qm0?NtbDXh5`BgEa{}dBo*cMJ<Uf3ZR}Z{D{M3oIe;?o#vLiUi!z7OIY7s}c8T17
z2`m;bm>|31Z8jlgdmxm3jWfPZa0%NdTAiph!$->QqRgW+9u+c~IImrq0t+$L^O`)%
z9Kq;tD7vhptKN;=0M!X4xEeqinF4jB>@LcJs$+R54nUDS)4FE!1DVYdGlq~8xa1Fn
zvUd5z$dGw30}Q5=${*dbJ0Z)#8&f1_%H~Y|zeg5PlPW6?0!JPl+#j-6?R+VepUCD-
z!|d&MP41t!-FFm6PDyy*F6KYB^H-SKE@OVSY?k_<jXidkVTIZAFzxYIT>e{zdvLWS
zTeYvs_IpUqfKofp$9{p34lK6xHxGXnSu_dIYx4V5E-dD%&y)hoJrCZR{j>F^Ti{44
z%YgF(yf3RqNb~>puo8rovm&yLid2>^iUpKam{O3^@)S|-;7n+kPO*F1Ud-ACn|c4k
zVkO3ZoRdu6TY%S#eufRn?d`J`dy?a-Jms^hrse#o2dfUy<0~@j#elTG18{ct^`$!%
z-GSN9>!aF2<OaFa461;&l8y!7g>eL)AajKh4;9AoG@q}RZEsLL*ItPuhxg!6p_3|t
zBEhOUJxxXyIe6wG3WFA+_dBR&ZJRksWo0Ga{<)oIZ2oc)5;?msS2?p6eY<MtIuce+
z4`|BaMQvKPbF794;gFcQv89=$)<izDv{sph$QfZ$J1NQ-v=m<L!1aHIB@hPBaJkBw
zboHY7-_`#2+vj02?bmxL^wtzZ(xS|Z<&!U$6O?balkOIf+?F5-&(-#xnFk5G&sf}3
zGzW#2(Vw$r0@BiRmgHE+bC)cc2d<ejCRrnm5c>XX{V?hKNptp7w;t8Gt`UCwBVN|%
zoL1o__J75zenZCMV)fclQx+b`DcJtB56MTv&UePQ7Se%v{r0-o?;I8+_e#1NOcv#8
z?U-XeDiK^Am?+F%iCPUS8#mJm+_(Xwrht33`(2~o3Q<9z$fDHZR<Fo)H-$k_oE0Rv
zjF1_!-YZf6!mg!XzF<XWWUjnZLo^jw4M(mvbH!cAF3WnHta4>tAx$L=s}qF^DKAtz
z7-V)It{SYBI+w`$clh(TTnWzg_6|D;!FS(d__xB9GzBS~b1YcYOiN|Rr!t-;>$uT8
zM{oRLaMiAIjU%Haj8y~BlG>;vTAR^I*#?nvV%ZtVbf7C^Ec8DTu~srg3}XMLuol$)
zYvhWFdbE1L+$y@<@BVb`Zv`t8IQq;jlQ98dr80x)fR)UnOHe=o-z!!<Aw@J`P3TO<
zCyUhe0J4;Tqr$p{EDbhVmB^{`w$?}oU{RaMqy?lEi6I2HSF)-JtkuCIBc-JD1CY{Y
z_Zy7A8Mob&p(CxtPzFcJ%9Ep|YC>Il4N}J1uXqPLIZeXpDm(mbyJ&L%W7|A!*TZ8j
zKWs}pY4Wp4Uz5rG_hF7Cz6U*4%03=?UvDP<;bhyr#e3F8gK!|XKePr~fZqLS$S;=P
zfhA0@*WqYkleRH3aCLrhQGo%}cB85e=q<6D(!-*!?jCLl-#xXQmYNR%&pUAhZ(Hze
z6cUW8Jp-_|!7-M_!ZS-KT<temrGL<BZ@gV>&}1<OWG{>yp)Bk-3trKMBKRDc^R<ar
zgv20A!9sz^LrOyCaL5{OMD9$jh#0_~{iLbfk-Iy%b9EP~mQ{N$i93-6i$*I}ViMY-
zE;`&lJ^P}`>uufn^y<4`X3K_FPunKdSo7(6G}p4%)v`%Q9*H9l1Ik*#YGHPs%NQ<+
z!|IK~uW2nFV<<?X%bqi>g(g4Ed!~|)IlB_`E(UUkrfnuEOjp$<-(Jy=e@mCus#W~k
zya@X}HfQtmo)+Je*pp~${rRH*?qW8-8V4n7ISk0QI?J^hK_%TsEC*G;1=mg#g-B{A
zMV3oF9m#EBLFm9N`mAHph&yNz|GEjY{=A((ZD$kge;{q$O~Ukp0uR#{{cB?TP5X*g
zY?zN$eM3q~`K|4n;zsG+w_+YFjDs?2_FBD7K#b%zL>8e=5F{1FVgS!11|PgEA(#8_
z4gI~QcP8_y#cN}$RTsYJlO{}Ww&Ht*Qw%*mSa;Q3zU1oe9EG@rEU^_?gbIl5)s(8W
zlCbmhj$a_A15-}wn$hzqMgbUDR>9P%-G)VLkR=Dc4ghsrh(n-jG?$!6(Q^A?xr%#k
zcHau@w+B{nw%FQfF6E?1ZPl(ik*uH!F-sIKYjgkO+HWRwyJyQ7CUm6h-#+Pxm^5AV
z6vmLkCnvN_&K4ti;i}ePakD)-Ahg(dE&LD}9F+fE%S1ohsTM{#p~yYT1dGn1Fi_U8
z+SHiSx8zyi4%+VGI{d{<!g+nwr1P$A+C~52;rHS1+h^_a{M2%_y<Vh*6`Zlm8&HI{
zz2hW=awGIjy{~W%%8^<Z#Xm|d{o>2|YWjvgHN1$^tB?PsCG)5M16&C-E&2ul02gNp
AQ2+n{

literal 0
HcmV?d00001


diff --git a/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/pre-change-source-snapshots.json b/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/pre-change-source-snapshots.json
new file mode 100644
index 0000000..eb064a6
--- /dev/null
+++ b/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/pre-change-source-snapshots.json
@@ -0,0 +1 @@
+{"/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/appsettings.json": "{\n  \"Logging\": {\n    \"LogLevel\": {\n      \"Default\": \"Information\",\n      \"Microsoft.AspNetCore\": \"Warning\"\n    }\n  },\n  \"AllowedHosts\": \"*\",\n  \"Tenants\": {\n    \"Enabled\": false,\n    \"ServiceName\": \"tenants\",\n    \"PubSubName\": \"pubsub\",\n    \"TopicName\": \"system.tenants.events\",\n    \"CommandApiAppId\": \"parties\"\n  },\n  \"Parties\": {\n    \"MemoriesSearch\": {\n      \"Enabled\": false,\n      \"Endpoint\": null,\n      \"ApiToken\": null,\n      \"RequireApiToken\": true,\n      \"TenantId\": null,\n      \"CaseId\": null,\n      \"EnabledAxes\": [ \"hybrid\", \"syntactic\", \"semantic\", \"graph\" ]\n    }\n  }\n}\n", "/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Hexalith.Parties.Tests.csproj": "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <IsPackable>false</IsPackable>\n    <!--\n      xUnit1051 fires when an async test calls a method that accepts CancellationToken\n      without forwarding TestContext.Current.CancellationToken. Many pre-existing tests\n      in this project (controller, MCP, search, projection) predate that lint rule and\n      would need broad refactoring to plumb the token through every helper. Project-wide\n      suppression is documented as deferred work (story 11-4 review item D5); see\n      _bmad-output/implementation-artifacts/deferred-work.md.\n    -->\n    <NoWarn>$(NoWarn);xUnit1051</NoWarn>\n  </PropertyGroup>\n\n  <ItemGroup>\n    <ProjectReference Include=\"..\\..\\src\\Hexalith.Parties\\Hexalith.Parties.csproj\" />\n    <ProjectReference Include=\"..\\..\\src\\Hexalith.Parties.Testing\\Hexalith.Parties.Testing.csproj\" />\n    <ProjectReference Include=\"$(HexalithEventStoreRoot)\\src\\Hexalith.EventStore\\Hexalith.EventStore.csproj\" Condition=\"'$(HexalithEventStoreFromSource)' == 'true'\">\n      <Aliases>eventstore</Aliases>\n    </ProjectReference>\n    <ProjectReference Include=\"..\\Hexalith.Parties.EventStoreGateway.TestHost\\Hexalith.Parties.EventStoreGateway.TestHost.csproj\" Condition=\"'$(HexalithEventStoreFromSource)' != 'true'\">\n      <Aliases>eventstore</Aliases>\n    </ProjectReference>\n    <ProjectReference Include=\"$(HexalithEventStoreRoot)\\src\\Hexalith.EventStore.Testing\\Hexalith.EventStore.Testing.csproj\" Condition=\"'$(HexalithEventStoreFromSource)' == 'true'\" />\n    <PackageReference Include=\"Hexalith.EventStore.Testing\" Condition=\"'$(HexalithEventStoreFromSource)' != 'true'\" />\n    <ProjectReference Include=\"$(HexalithCommonsRoot)\\src\\libraries\\Hexalith.Commons.ServiceDefaults\\Hexalith.Commons.ServiceDefaults.csproj\" Condition=\"'$(HexalithCommonsServiceDefaultsFromSource)' == 'true'\" />\n    <PackageReference Include=\"Hexalith.Commons.ServiceDefaults\" Condition=\"'$(HexalithCommonsServiceDefaultsFromSource)' != 'true'\" />\n    <ProjectReference Include=\"$(HexalithTenantsRoot)\\src\\Hexalith.Tenants.Client\\Hexalith.Tenants.Client.csproj\" Condition=\"'$(HexalithTenantsFromSource)' == 'true'\" />\n    <PackageReference Include=\"Hexalith.Tenants.Client\" Condition=\"'$(HexalithTenantsFromSource)' != 'true'\" />\n    <ProjectReference Include=\"$(HexalithTenantsRoot)\\src\\Hexalith.Tenants.Testing\\Hexalith.Tenants.Testing.csproj\" PrivateAssets=\"all\" ExcludeAssets=\"analyzers\" Condition=\"'$(HexalithTenantsFromSource)' == 'true'\" />\n    <PackageReference Include=\"Hexalith.Tenants.Testing\" PrivateAssets=\"all\" ExcludeAssets=\"analyzers\" Condition=\"'$(HexalithTenantsFromSource)' != 'true'\" />\n  </ItemGroup>\n\n  <ItemGroup>\n    <PackageReference Include=\"coverlet.collector\">\n      <PrivateAssets>all</PrivateAssets>\n      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>\n    </PackageReference>\n    <PackageReference Include=\"Microsoft.AspNetCore.Mvc.Testing\" />\n    <PackageReference Include=\"Microsoft.NET.Test.Sdk\" />\n    <PackageReference Include=\"xunit.v3\" />\n    <PackageReference Include=\"xunit.runner.visualstudio\" />\n    <PackageReference Include=\"Shouldly\" />\n    <PackageReference Include=\"NSubstitute\" />\n  </ItemGroup>\n\n  <ItemGroup>\n    <Using Include=\"Xunit\" />\n  </ItemGroup>\n</Project>\n", "/home/administrator/projects/hexalith/parties/_bmad-output/planning-artifacts/actor-history-retention-policy-v1.md": "# Minimal opaque actor-history retention policy\n\nEngineering approach accepted by the user's \u201capply recommendation\u201d instruction on 2026-10-06. **Production policy status: proposed, disabled.** This document does not establish an approved duration, owner acceptance, or qualified storage/custody target.\n\nProposed policy reference: `party-actor-retention-v1`. Purpose: `party-actor-history-v1`. Product/Governance approves scope and duration; Parties owns the binding contract; Platform owns independent identity authority and custody; EventStore supplies the protected source seam.\n\n## Reuse assessment and selected approach\n\nNo located approved policy explicitly covers the tenant/Party-to-stable-human-actor relationship after profile erasure. Agents' 365-day rule starts at interaction terminal state and covers sensitive interaction content ([Agents specification](../../../agents/_bmad-output/specs/spec-agents/SPEC.md)). Parties' crypto/projection \u201cretention\u201d proposals retain implementation code during migration, rather than defining a data lifetime. The invoice retention examples in [integration guidance](../../docs/event-handler-patterns.md) do not authorize actor-history retention. None supplies this record's exact scope, duration, expiry, cleanup and restore rules.\n\nUse one narrow policy through the existing Party aggregate, EventStore `IdentityHistoryPolicy` and purpose-specific custody seam. Reuse an existing policy later only when its recorded approval explicitly covers these same requirements. Keep current configuration unset until approval; no new database, service or general policy engine is needed.\n\n## Data and access\n\nRetain only the exact tenant and Party IDs, opaque stable human ActorId, immutable binding version and actor-authority revision, effective interval, original establishment/rebind source position, and the minimal opaque provenance/logical-intent digest and custody references required to authenticate that lifecycle. Revoke/rebind boundaries are part of that lifecycle. Source certificates contain the exact head, observation/revision and excluded original positions needed to prove completeness.\n\nExclude names, contact details, birth dates, login issuer/subject mappings, role claims, credentials, decoded tokens, message content and general Party profile payloads. Treat the opaque relationship as protected identity data. Ordinary logs and cleanup receipts must not duplicate the relationship; use opaque custody references and content-free counts/status.\n\nOnly independently admitted, tenant/Party-scoped attribution queries may read it. Historical success identifies the recorded actor for a past action; it never grants present authority. Current human eligibility still requires active Party and current actor authority. Organization Branch B has no human binding and does not depend on this policy.\n\n## Lifetime and profile erasure\n\nThe duration `D` is an explicitly approved, strictly positive finite `TimeSpan`, with no default. The selected v1 trigger is `binding-effective-at`, already supported by the contract: `ExpiresAt = ValidFrom + D`. Arithmetic overflow denies admission. Expiry is exclusive: no release at or after ExpiresAt.\n\nClosing, revoking, rebinding, erasing the profile, restarting, restoring or retrying does not restart this clock. Every successor derives its own expiry from its own effective instant. No configuration change extends or relabels a retained record. A new duration requires a new approved versioned policy reference; this minimal implementation supports one configured version and returns Unavailable for another version. Serving several approved versions concurrently requires an explicit later extension.\n\nBinding validity is bounded by its custody expiry. A long-lived current binding therefore needs an explicit authorized new version before expiry; it cannot silently renew. Erasure immediately blocks current eligibility and destroys profile protection independently. Past attribution may remain readable only until its original expiry under the approved policy and fresh independent custody. A duration measured from profile erasure is a different, unsupported contract and remains disabled.\n\n## Cleanup and restore contract\n\nAt expiry, custody denies reads immediately, even if asynchronous cleanup has not finished. `DestroyExpiredAsync` must idempotently destroy the retention unit's ability to decrypt source events, snapshots and every derived copy, including caches, read models, replicas and lifecycle-covered backups/exports. Profile and actor-history protection must be independent. Cleanup success requires authenticated evidence covering the exact unit and all copies; outage or unknown acknowledgement is pending, never success. Immutable ciphertext may remain only when its decryption capability is irreversibly destroyed and no identity-bearing derived plaintext survives.\n\nRestore consults the fresh, independently durable lifecycle before decrypting anything. Old keys, snapshots or lifecycle revisions cannot roll back expiry/destruction or make a destroyed binding readable. Reject unavailable, revoked, expired or stale lifecycle evidence. Replayed events keep original actor/version/times/positions; restoring cannot substitute today's binding or recreate an expired identity relationship. Complete historical proof must remain demonstrable without recovering expired predecessors; the existing strict fold returns Unavailable if it cannot reconstruct a complete lifecycle. This limitation requires owner qualification, not fabricated missing events.\n\nThese are required provider behaviors. No cleanup worker, qualified custody backend, backup destruction or restore proof is claimed by the local query guard.\n\n## Runtime gate and activation record\n\n`Parties:Identity` must contain the exact approved versioned PolicyId, approved finite Retention and supported ExpiryTrigger. An unset/invalid policy returns Unavailable for binding reads and denies binding writes. A readable custody response alone is insufficient: policy ID, purpose, lifecycle flags and recorded expiry must match. Each read snapshots configuration and checks it again after awaits and before releasing evidence; changed/withdrawn configuration denies the result. Independent source authority, fresh custody, expiry and cancellation checks still apply.\n\nBefore production activation, the owners must record the approved reference/version and duration, explicit post-profile-erasure coverage, approval reference, supported expiry trigger, exact custody/source target and copy inventory, cleanup/recovery/restore evidence, and complete owner compatibility acceptance. No production settings are populated by this proposal. The test fixture's ten-day duration is synthetic and carries no approval.\n", "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/Hexalith.Platform.Custody.csproj": "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><GenerateDocumentationFile>true</GenerateDocumentationFile></PropertyGroup>\n  <ItemGroup><FrameworkReference Include=\"Microsoft.AspNetCore.App\" /></ItemGroup>\n</Project>\n", "/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/PlatformCustodyServiceCollectionExtensions.cs": "using Microsoft.Extensions.DependencyInjection;\nusing Microsoft.Extensions.DependencyInjection.Extensions;\n\nnamespace Hexalith.Platform.Custody;\n\n/// <summary>Explicit library registration with unavailable production defaults.</summary>\npublic static class PlatformCustodyServiceCollectionExtensions\n{\n    /// <summary>Adds cryptographic prerequisites; owners must independently supply custody/profile and domain/replay authorization.</summary>\n    public static IServiceCollection AddPlatformCustody(this IServiceCollection services)\n    {\n        ArgumentNullException.ThrowIfNull(services);\n        services.TryAddSingleton<TimeProvider>(TimeProvider.System);\n        services.TryAddSingleton<IPlatformHmacKeyProvider, UnavailablePlatformHmacKeyProvider>();\n        services.TryAddSingleton<IPlatformSigningProfileProvider, UnavailablePlatformSigningProfileProvider>();\n        services.TryAddSingleton<PlatformHmacService>();\n        services.TryAddSingleton<TrustedEnvelopeAuthenticator>();\n        return services;\n    }\n}\n", "/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/story-5-4-retention-custody-proposal-2026-10-06.md": "# Concrete retention and custody proposal\n\nPrepared in response to the user's request to supply the missing retention/custody behavior and compare pragmatic solutions. **Status: concrete recommendation for review; no production configuration or qualification receipt is created.** The earlier approval covered the local implementation/design before a numeric duration was proposed.\n\n## Proposed retention\n\nUse policy `party-actor-retention-v1`, purpose `party-actor-history-v1`, with **365 fixed days from binding-effective-at**. This proposes an annual operational review horizon; it is an engineering choice, not an inherited Agents-content policy or a statutory period. [EDPB principles](https://www.edpb.europa.eu/topics/key-gdpr-concepts/basic-principles_en) require purpose and storage limitation; they do not establish this particular number.\n\nThe configuration is in [the proposal JSON](../specs/spec-story-5-4-dependency-unblock/proposals/parties-identity-365-days.proposed.json), outside all host-loaded appsettings paths. Duration is `365.00:00:00`, exactly 31,536,000 seconds. The existing SDK derives `ExpiresAt = ValidFrom + duration`, with exclusive expiry and overflow denial. No erasure/rebind/retry/restore restarts the clock.\n\nAlternatives: 90 days reduces the retained identity relationship but limits older investigations; 365 days supports a longer bounded review period; multi-year retention increases exposure and needs a concrete purpose beyond this initial proposal. Neither short nor long retention fixes custody/restore on its own.\n\n**Timing limitation:** 365 days from establishment is not 365 days after every action or profile erasure. An action on binding day 364 has roughly one day of remaining binding evidence. Current binding validity is also bounded by that deadline. Authorized renewal requires a new version, not silent extension. If the required audit purpose is a full year after each action/erasure, v1 does not meet it; the owners must accept and implement a different lifecycle contract before activation. The proposed number does not resolve that mismatch by itself.\n\n## Custody alternatives\n\n| Option | How it works | Pros | Cons / condition |\n| --- | --- | --- | --- |\n| 1. Reuse existing production custody | Implement the SDK seam as a thin adapter to an existing independently operated, qualified lifecycle/key backend; reuse its destruction and restore receipts. | Least new code and operating burden; existing incident and recovery procedures. | No qualifying deployment or production `IIdentityHistoryCustody` implementation was found. General vault/audit approval is insufficient; validate this purpose and all copies. |\n| 2. Managed key service plus a narrow Platform adapter | Keep events/snapshots in EventStore; custody uses purpose-specific protection and an independently durable lifecycle authority. Before decrypting, check current lifecycle and immutable expiry. A bounded cleanup worker records exact destruction outcomes. | Uses managed infrastructure and existing SDK contracts; small domain integration; no new policy engine. | Provider recovery/deletion semantics must satisfy the existing strict contract. A tombstone/read denial alone does not prove key/copy destruction. Provider credentials, independent lifecycle target, copy inventory and live restore evidence remain necessary. |\n| 3. Existing Vault Transit | Use an already operated Vault cluster for purpose-specific crypto, with the same fresh lifecycle authority and copy/destruction checks. | Good fit when Vault is already the organizational standard; explicit crypto API and ACLs. | Raising `min_decryption_version` is reversible; key backups and cluster snapshots require their own treatment. Starting a new cluster solely for this feature adds substantial operations. |\n| 4. Local/self-built custody | Persist keys and lifecycle ourselves, implement replication, expiry, cleanup and restore fencing. | Complete design control; useful synthetic development fixture. | Largest security/recovery burden. The current local Parties backend uses in-memory dictionaries and cannot establish production durability or restore safety. Not recommended for launch. |\n\nManaged-provider caveats are real contract constraints: [Azure soft deletion](https://learn.microsoft.com/en-us/azure/key-vault/general/soft-delete-overview) is recoverable; [Azure backups/restored keys](https://learn.microsoft.com/en-us/azure/key-vault/general/backup) remain independent of the original key. [AWS KMS](https://docs.aws.amazon.com/kms/latest/developerguide/deleting-keys.html) has a mandatory 7\u201330-day deletion wait. [Vault Transit](https://developer.hashicorp.com/vault/docs/secrets/transit) permits lowering the minimum decryption version again. None of those individual operations proves this contract's irreversible expiry.\n\n## Required behavior and qualification\n\nUse separate profile and actor-history protection. Scope custody to tenant, purpose, binding/retention unit, version and immutable expiry, so destroying one expired unit does not destroy a live successor or another tenant. Do not retain offline plaintext key copies. Every snapshot/cache/read-model/replica/export/backup copy belongs to the inventory or is explicitly forbidden.\n\nAt expiry, all reads deny immediately. Cleanup is idempotent and outcome lookup resolves crashes/lost acknowledgements; unknown outcomes remain pending. `DestroyExpiredAsync` returns success only after authenticated irreversible destruction and copy-cleanup evidence. Recoverable soft deletion, a delayed scheduled purge, or changing a read threshold cannot be reported as completed destruction. A provider with incompatible timing either fails strict v1 or requires a separately accepted lifecycle-contract change; there is no hidden grace period.\n\nRestore quarantines source/snapshot/key copies until it can consult fresh independent lifecycle state. A backup of the application cannot roll that authority back. Missing/stale authority denies before decryption. Expired/destroyed units cannot be revived by recovering a key, resetting a clock or restoring an older ledger. Unexpired retained bindings must still reproduce their original actor/version/interval/positions without profile decryption.\n\n| Qualification exercise | Required persisted/result evidence |\n| --- | --- |\n| Erase profile before binding expiry | Profile unavailable; exact historical actor/version and opening position remain readable under independent history custody. |\n| At exclusive expiry | Event, snapshot and cache reads deny; no stale-authority or cached-key bypass. |\n| Destroy then restore a pre-destruction backup | Expired relationship remains unreadable; fresh lifecycle state cannot roll back; every key/copy receipt is resolved. |\n| Restore a still-live successor after its predecessor expired | Successor proof remains complete and exact without reconstructing the expired predecessor's identity. The current strict fold limitation must be resolved/proven. |\n| Crash/outage/lost acknowledgement during destruction | Same unit/outcome is recovered; unknown is pending, never a fabricated success or renewed expiry. |\n| Wrong tenant/purpose/version and lifecycle rollback | Denial before key release/decryption; foreign persisted state unchanged. |\n\nQualify on an isolated owner-approved target using shortened synthetic intervals, then verify the production 365-day configuration and exact deployed target separately. Record source/provider versions, target/copy inventory, failure model, authenticated test identity, commands, persisted outcomes, signed/verified receipts and owner acceptance. Mock flags and a completed checklist cannot establish a pass.\n\n## Recommendation and observed limits\n\nPrefer option 1. If no suitable custody exists, use option 2 in the current platform/cloud with the smallest adapter and already qualified independent lifecycle infrastructure. Use option 3 only if Vault is already operated. Do not build new self-managed custody or a generic policy system for this feature.\n\nWorkspace investigation found interfaces and synthetic tests, but no production actor-history custody implementation. Azure account metadata was cached; actual vault enumeration failed because the account's MSAL token was unavailable. No deployment was selected, provisioned or exercised. The proposed policy parses and derives its deadline through the existing SDK contract; [configuration validation](tests/actor-retention-proposal-2026-10-06/configuration-validation.json) confirms 365 days and the expected exclusive deadline. The temporary PowerShell check initially could not load the Parties Web assembly because the ASP.NET runtime was absent from that host; it then used the SDK policy contract directly. This is not a new domain test or custody/restore qualification. Full Story 5.4 and the existing dependency register remain unchanged.\n"}
\ No newline at end of file

diff --git a/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/sdk-contract-source.txt b/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/sdk-contract-source.txt
new file mode 100644
index 0000000..28a52aa
--- /dev/null
+++ b/agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/sdk-contract-source.txt
@@ -0,0 +1,244 @@
+/home/administrator/projects/hexalith/eventstore/src/Hexalith.EventStore.Contracts/Security/IIdentityHistoryCustody.cs
+using Hexalith.EventStore.Contracts.Identity;
+
+namespace Hexalith.EventStore.Contracts.Security;
+
+/// <summary>Provider seam for independently governed attribution protection, destruction and restore safety.</summary>
+public interface IIdentityHistoryCustody
+{
+    /// <summary>Accepts writes only when the provider enforces the full finite purpose lifecycle.</summary>
+    Task<IdentityHistoryCustodyEvidence?> AdmitAsync(AggregateIdentity identity, IdentityHistoryPolicy policy,
+        DateTimeOffset effectiveAt, CancellationToken cancellationToken = default);
+
+    /// <summary>Checks current non-rollback lifecycle authority before retained evidence is read.</summary>
+    Task<bool> CanReadAsync(AggregateIdentity identity, IdentityHistoryCustodyEvidence evidence,
+        CancellationToken cancellationToken = default);
+
+    /// <summary>Irreversibly destroys expired source and derived evidence and records restore-safe evidence.</summary>
+    Task<bool> DestroyExpiredAsync(AggregateIdentity identity, IdentityHistoryCustodyEvidence evidence,
+        CancellationToken cancellationToken = default);
+
+    /// <summary>Protects attribution using purpose keys independent of profile erasure.</summary>
+    Task<PayloadProtectionResult> ProtectEventAsync(AggregateIdentity identity, string eventType,
+        byte[] payload, string format, IdentityHistoryCustodyEvidence evidence, CancellationToken cancellationToken = default);
+
+    /// <summary>Unprotects retained attribution only under current non-rollback lifecycle authority.</summary>
+    Task<PayloadProtectionResult> UnprotectEventAsync(AggregateIdentity identity, string eventType,
+        byte[] payload, string format, CancellationToken cancellationToken = default);
+
+    /// <summary>Protects history fields in profile-protected snapshots with independent purpose keys.</summary>
+    Task<object> ProtectSnapshotAsync(AggregateIdentity identity, object snapshot, CancellationToken cancellationToken = default);
+
+    /// <summary>Checks restore-safe lifecycle and unprotects retained snapshot history.</summary>
+    Task<object?> UnprotectSnapshotAsync(AggregateIdentity identity, object snapshot, CancellationToken cancellationToken = default);
+}
+
+
+/home/administrator/projects/hexalith/eventstore/src/Hexalith.EventStore.Contracts/Security/IdentityHistoryPolicy.cs
+namespace Hexalith.EventStore.Contracts.Security;
+
+/// <summary>Required finite purpose policy; no production default is supplied.</summary>
+/// <param name="PolicyId">The approved versioned policy reference.</param>
+/// <param name="Retention">The approved finite retention duration.</param>
+/// <param name="ExpiryTrigger">The approved irreversible expiry trigger.</param>
+public sealed record IdentityHistoryPolicy(string PolicyId, TimeSpan Retention, string ExpiryTrigger)
+{
+    /// <summary>Gets whether every mandatory policy field is explicitly configured.</summary>
+    public bool IsValid => !string.IsNullOrWhiteSpace(PolicyId)
+        && Retention > TimeSpan.Zero && ExpiryTrigger == "binding-effective-at";
+
+    /// <summary>Derives supported expiry without allowing out-of-range policy durations.</summary>
+    public DateTimeOffset? DeriveExpiry(DateTimeOffset effectiveAt)
+    {
+        if (!IsValid)
+        {
+            return null;
+        }
+
+        try
+        {
+            return effectiveAt + Retention;
+        }
+        catch (ArgumentOutOfRangeException)
+        {
+            return null;
+        }
+    }
+}
+
+
+/home/administrator/projects/hexalith/eventstore/src/Hexalith.EventStore.Contracts/Security/IdentityHistoryCustodyEvidence.cs
+namespace Hexalith.EventStore.Contracts.Security;
+
+/// <summary>Provider-owned purpose protection and non-rollback lifecycle evidence.</summary>
+/// <param name="PolicyId">The exact accepted policy reference.</param>
+/// <param name="Purpose">The purpose-separated protection scope.</param>
+/// <param name="ExpiresAt">The derived irreversible expiry instant.</param>
+/// <param name="LifecycleRevision">The monotonic anti-resurrection observation.</param>
+/// <param name="EvidenceId">The opaque provider evidence reference.</param>
+/// <param name="SourceExpiryEnforced">Whether events, snapshots and backups become unrecoverable.</param>
+/// <param name="RestoreSafe">Whether rollback and restore cannot resurrect expired evidence.</param>
+/// <param name="DerivedCopiesCovered">Whether projections and caches are covered.</param>
+public sealed record IdentityHistoryCustodyEvidence(
+    string PolicyId, string Purpose, DateTimeOffset ExpiresAt, long LifecycleRevision, string EvidenceId,
+    bool SourceExpiryEnforced, bool RestoreSafe, bool DerivedCopiesCovered)
+{
+    /// <summary>Validates enforceable custody for the exact policy and immutable effective time.</summary>
+    public bool Satisfies(IdentityHistoryPolicy policy, DateTimeOffset effectiveAt)
+        => policy is not null && policy.IsValid && PolicyId == policy.PolicyId && Purpose == "party-actor-history-v1"
+            && policy.DeriveExpiry(effectiveAt) is { } expectedExpiry && ExpiresAt == expectedExpiry && LifecycleRevision > 0
+            && !string.IsNullOrWhiteSpace(EvidenceId) && SourceExpiryEnforced && RestoreSafe && DerivedCopiesCovered;
+}
+
+
+/home/administrator/projects/hexalith/eventstore/src/Hexalith.EventStore.Contracts/Identity/AggregateIdentity.cs
+
+using System.Text.RegularExpressions;
+
+namespace Hexalith.EventStore.Contracts.Identity;
+/// <summary>
+/// Canonical identity tuple for an aggregate, providing all key derivation properties
+/// used by DAPR actors, state store, pub/sub, and queue sessions.
+/// </summary>
+public record AggregateIdentity {
+    private static readonly Regex _tenantDomainRegex = new(@"^[a-z0-9]([a-z0-9-]*[a-z0-9])?$", RegexOptions.Compiled);
+    private static readonly Regex _aggregateIdRegex = new(@"^[a-zA-Z0-9]([a-zA-Z0-9._-]*[a-zA-Z0-9])?$", RegexOptions.Compiled);
+
+    /// <summary>
+    /// Initializes a new instance of the <see cref="AggregateIdentity"/> record.
+    /// TenantId and Domain are forced to lowercase. All components are validated against
+    /// security-critical regex patterns and length constraints.
+    /// </summary>
+    /// <param name="tenantId">Tenant identifier.</param>
+    /// <param name="domain">Domain name.</param>
+    /// <param name="aggregateId">Aggregate identifier.</param>
+    /// <exception cref="ArgumentNullException">Thrown when any component is null.</exception>
+    /// <exception cref="ArgumentException">Thrown when any component is empty, whitespace, exceeds length, or fails regex validation.</exception>
+    public AggregateIdentity(string tenantId, string domain, string aggregateId) {
+        ArgumentNullException.ThrowIfNull(tenantId);
+        ArgumentNullException.ThrowIfNull(domain);
+        ArgumentNullException.ThrowIfNull(aggregateId);
+
+        tenantId = tenantId.ToLowerInvariant();
+        domain = domain.ToLowerInvariant();
+
+        ValidateTenantOrDomain(tenantId, nameof(tenantId));
+        ValidateTenantOrDomain(domain, nameof(domain));
+        ValidateAggregateId(aggregateId);
+
+        TenantId = tenantId;
+        Domain = domain;
+        AggregateId = aggregateId;
+    }
+
+    /// <summary>Gets the tenant identifier (always lowercase).</summary>
+    public string TenantId { get; }
+
+    /// <summary>Gets the domain name (always lowercase).</summary>
+    public string Domain { get; }
+
+    /// <summary>Gets the aggregate identifier (case-sensitive).</summary>
+    public string AggregateId { get; }
+
+    /// <summary>
+    /// Gets the DAPR actor ID in canonical colon-separated form.
+    /// Isolation guarantee: each actor instance's state is scoped by DAPR to this ID,
+    /// preventing cross-tenant state access. Colons are forbidden in components to ensure
+    /// structural disjointness between tenants (FR15, FR28).
+    /// </summary>
+    public string ActorId => $"{TenantId}:{Domain}:{AggregateId}";
+
+    /// <summary>
+    /// Gets the event stream key prefix for state store lookups (append sequence number for full key).
+    /// Pattern: {tenant}:{domain}:{aggId}:events: — isolation is guaranteed because colons are
+    /// forbidden in tenant/domain/aggregateId components, making each tenant's key space structurally
+    /// disjoint from all other tenants (D1, FR15, FR28).
+    /// </summary>
+    public string EventStreamKeyPrefix => $"{TenantId}:{Domain}:{AggregateId}:events:";
+
+    /// <summary>
+    /// Gets the metadata key for state store lookups.
+    /// Pattern: {tenant}:{domain}:{aggId}:metadata — tenant-scoped by the same 4-layer isolation model:
+    /// (1) input validation rejects colons, (2) composite key prefixing, (3) DAPR actor scoping,
+    /// (4) JWT tenant enforcement (FR15, FR28).
+    /// </summary>
+    public string MetadataKey => $"{TenantId}:{Domain}:{AggregateId}:metadata";
+
+    /// <summary>
+    /// Gets the snapshot key for state store lookups.
+    /// Pattern: {tenant}:{domain}:{aggId}:snapshot — tenant-scoped by the same 4-layer isolation model:
+    /// (1) input validation rejects colons, (2) composite key prefixing, (3) DAPR actor scoping,
+    /// (4) JWT tenant enforcement (FR15, FR28).
+    /// </summary>
+    public string SnapshotKey => $"{TenantId}:{Domain}:{AggregateId}:snapshot";
+
+    /// <summary>
+    /// Gets the pipeline state key prefix for state machine checkpoints (append correlationId for full key).
+    /// Pattern: {tenant}:{domain}:{aggId}:pipeline: — used by ActorStateMachine to track in-flight
+    /// command lifecycle stages for crash-recovery resume (NFR25, Story 3.11).
+    /// </summary>
+    public string PipelineKeyPrefix => $"{TenantId}:{Domain}:{AggregateId}:pipeline:";
+
+    /// <summary>
+    /// Gets the pub/sub topic in dot-separated form.
+    /// Platform-level commands use the domain topic; tenant-scoped commands keep the tenant prefix.
+    /// </summary>
+    public string PubSubTopic => TenantId == "system"
+        ? $"{Domain}.events"
+        : $"{TenantId}.{Domain}.events";
+
+    /// <summary>Gets the queue session identifier (same as ActorId).</summary>
+    public string QueueSession => $"{TenantId}:{Domain}:{AggregateId}";
+
+    /// <summary>Returns the canonical colon-separated form of the identity.</summary>
+    /// <returns>A string in the format "tenantId:domain:aggregateId".</returns>
+    public override string ToString() => ActorId;
+
+    private static void ValidateTenantOrDomain(string value, string parameterName) {
+        if (string.IsNullOrWhiteSpace(value)) {
+            throw new ArgumentException($"{parameterName} cannot be empty or whitespace.", parameterName);
+        }
+
+        if (value.Length > 64) {
+            throw new ArgumentException($"{parameterName} cannot exceed 64 characters. Got {value.Length}.", parameterName);
+        }
+
+        if (ContainsInvalidCharacters(value)) {
+            throw new ArgumentException($"{parameterName} contains control characters (< 0x20) or non-ASCII characters (> 0x7F).", parameterName);
+        }
+
+        if (!_tenantDomainRegex.IsMatch(value)) {
+            throw new ArgumentException($"{parameterName} must match pattern ^[a-z0-9]([a-z0-9-]*[a-z0-9])?$ (lowercase alphanumeric + hyphens, no leading/trailing hyphen). Got '{value}'.", parameterName);
+        }
+    }
+
+    private static void ValidateAggregateId(string value) {
+        const string parameterName = "aggregateId";
+
+        if (string.IsNullOrWhiteSpace(value)) {
+            throw new ArgumentException("aggregateId cannot be empty or whitespace.", parameterName);
+        }
+
+        if (value.Length > 256) {
+            throw new ArgumentException($"aggregateId cannot exceed 256 characters. Got {value.Length}.", parameterName);
+        }
+
+        if (ContainsInvalidCharacters(value)) {
+            throw new ArgumentException("aggregateId contains control characters (< 0x20) or non-ASCII characters (> 0x7F).", parameterName);
+        }
+
+        if (!_aggregateIdRegex.IsMatch(value)) {
+            throw new ArgumentException($"aggregateId must match pattern ^[a-zA-Z0-9]([a-zA-Z0-9._-]*[a-zA-Z0-9])?$ (alphanumeric + dots/hyphens/underscores). Got '{value}'.", parameterName);
+        }
+    }
+
+    private static bool ContainsInvalidCharacters(string value) {
+        foreach (char c in value) {
+            if (c is < (char)0x20 or >= (char)0x7F) {
+                return true;
+            }
+        }
+
+        return false;
+    }
+}

diff --git a/platform/_bmad-output/implementation-artifacts/4-2-close-public-admin-exposure-and-anonymous-registry-reads.md b/platform/_bmad-output/implementation-artifacts/4-2-close-public-admin-exposure-and-anonymous-registry-reads.md
index a423080..37adb35 100644
--- a/platform/_bmad-output/implementation-artifacts/4-2-close-public-admin-exposure-and-anonymous-registry-reads.md
+++ b/platform/_bmad-output/implementation-artifacts/4-2-close-public-admin-exposure-and-anonymous-registry-reads.md
@@ -250,3 +250,23 @@ The second review's patch findings are resolved: denial covers every concrete ho
 [Final local verification](evidence/epic-4/4-2/20261006t124852z-review-verification/verification.json) records 87 passing synthetic tests with zero skips, including real SSH signatures, nested false/missing retained-delete denial for writers and replicators before and after cutover, and actual CLI success/failure exit codes. The parent ran the complete focused suite, all three CLI help commands and the diff check after the implementation agent's targeted checks. A new owner-only immutable private preparation and retained log bind the final source/test/story bytes. Historical attempts and receipts remain unchanged.
 
 All patch findings from both review passes were corrected; refuted claims and carried findings retain their logged verdicts. Ten grouped findings requiring protected operational evidence or work on the pre-existing Custody component are recorded in the [deferred-work ledger](deferred-work.md); none is asserted to be resolved by local tests. Local verification remains unsigned and production-unaccepted, with mutation authorization, operational acceptance and completion false. No remote operation or production mutation occurred. Actual private/recovery qualification, independent external denial/public OIDC evidence, fresh signed baseline and separate production go, complete registry consumer operations and retained-content GC acceptance remain pending. Story and sprint remain `in-progress`; live tasks are unchanged.
+
+## Administrator decision — live execution go, 2026-10-06
+
+After fresh read-only checks, the Administrator adopted all three recommendations in conversation. This is the production go, and it follows the sole-owner policy: a direct chat approval is the authority. Claude runs each phase in order. Every mutated object is backed up to owner-only custody first. Each phase stops and rolls back on a failed check. Raw records stay outside Git and sanitized results are committed. Detached SSH signatures are not required, except that the `console-closure.json` record consumed by the 4.27 executor is signed by the Administrator.
+
+1. **Admin path: browser via port-forward.**
+   - Delete `kubesphere-system/kubesphere-console` (UID/resourceVersion preconditions).
+   - Limit the Keycloak catch-all Ingress to `/realms/tache` and `/resources`, and delete `keycloak-admin-rate-limit` and `keycloak-master-token-rate-limit`. Keep `keycloak-reset-rate-limit`.
+   - Set `KC_HOSTNAME_ADMIN=http://localhost:8080` so the admin console is used through `kubectl port-forward`. If localhost admin login does not qualify, fall back to CLI-only `kcadm` over port-forward.
+   - Before route closure: the Administrator logs in privately, an unauthorized private request is refused, and the `tache` OIDC flow passes. Keycloak recovery uses a temporary bootstrap admin created through native cluster access, which is used for a read and then removed.
+   - After closure: external probes must be refused or not routed, and the private, recovery and OIDC checks are repeated.
+2. **Registry auth: read-only robot.**
+   - Add a Zot htpasswd read-only principal. Its credential lives only in an owner-only file and a Kubernetes pull Secret.
+   - Switch the `hexalith-memories` consumers to it and prove uncached audited pulls.
+   - Then remove `anonymousPolicy` read and prove anonymous catalog/tag/manifest/blob refusal. Existing `jpiquot` OIDC/API-key writes are unchanged.
+3. **Retained images and GC: retain all manifests.**
+   - Restore the running `memories-access-telemetry@sha256:b3790e08…` image, which currently returns 404, from node1's containerd cache at its exact digest under a retention tag.
+   - Investigate the nightly `eventstore` GC failure (`manifest not found`).
+   - Configure Zot retention to never delete tagged/untagged manifests or referrers, so GC removes only blobs no manifest references.
+   - After a GC run, prove every live and rollback digest pulls from an empty client.
diff --git a/platform/src/Hexalith.Platform.Custody/Hexalith.Platform.Custody.csproj b/platform/src/Hexalith.Platform.Custody/Hexalith.Platform.Custody.csproj
index 1c97b87..4c81545 100644
--- a/platform/src/Hexalith.Platform.Custody/Hexalith.Platform.Custody.csproj
+++ b/platform/src/Hexalith.Platform.Custody/Hexalith.Platform.Custody.csproj
@@ -1,4 +1,10 @@
 <Project Sdk="Microsoft.NET.Sdk">
   <PropertyGroup><GenerateDocumentationFile>true</GenerateDocumentationFile></PropertyGroup>
   <ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App" /></ItemGroup>
+  <ItemGroup Condition="'$(UseHexalithProjectReferences)' == 'true'">
+    <ProjectReference Include="$(HexalithEventStoreRoot)/src/Hexalith.EventStore.Contracts/Hexalith.EventStore.Contracts.csproj" />
+  </ItemGroup>
+  <ItemGroup Condition="'$(UseHexalithProjectReferences)' != 'true'">
+    <PackageReference Include="Hexalith.EventStore.Contracts" />
+  </ItemGroup>
 </Project>
diff --git a/platform/src/Hexalith.Platform.Custody/PlatformCustodyServiceCollectionExtensions.cs b/platform/src/Hexalith.Platform.Custody/PlatformCustodyServiceCollectionExtensions.cs
index ad19d53..ead4d0f 100644
--- a/platform/src/Hexalith.Platform.Custody/PlatformCustodyServiceCollectionExtensions.cs
+++ b/platform/src/Hexalith.Platform.Custody/PlatformCustodyServiceCollectionExtensions.cs
@@ -1,3 +1,5 @@
+using Hexalith.EventStore.Contracts.Security;
+
 using Microsoft.Extensions.DependencyInjection;
 using Microsoft.Extensions.DependencyInjection.Extensions;
 
@@ -15,6 +17,8 @@ public static class PlatformCustodyServiceCollectionExtensions
         services.TryAddSingleton<IPlatformSigningProfileProvider, UnavailablePlatformSigningProfileProvider>();
         services.TryAddSingleton<PlatformHmacService>();
         services.TryAddSingleton<TrustedEnvelopeAuthenticator>();
+        services.TryAddScoped<IdentityHistoryCleanup>(provider => new(
+            provider.GetRequiredService<TimeProvider>(), provider.GetService<IIdentityHistoryCustody>()));
         return services;
     }
 }

diff --git a/platform/docs/implementation/actor-history-lifecycle-2026-10-07.md b/platform/docs/implementation/actor-history-lifecycle-2026-10-07.md
new file mode 100644
index 0000000..cbc92a1
--- /dev/null
+++ b/platform/docs/implementation/actor-history-lifecycle-2026-10-07.md
@@ -0,0 +1,13 @@
+# Actor-history lifetime and cleanup
+
+The user accepted `party-actor-retention-v1`: 365 fixed days from binding-effective-at, including bounded post-profile-erasure attribution. [Parties host configuration](../../../parties/src/Hexalith.Parties/appsettings.json) now supplies the policy. Library options defaults remain unset; missing custody/source/authorization still denies production binding admission and reads.
+
+`IdentityHistoryCleanup` is a scoped Platform.Custody library operation for **one owner-inventoried unit**. `AddPlatformCustody` registers it but does not register `IIdentityHistoryCustody` or a backend. An owner with qualified custody registers that SDK interface in the existing host and invokes `ProcessAsync(identity, originalPolicy, originalEffectiveAt, originalEvidence, cancellationToken)` through a service scope. Source events and snapshots remain in EventStore. There is no new scheduler, inventory, database, service or proprietary CLI.
+
+The operation validates the exact policy/purpose/derived deadline and required lifecycle fields before contacting custody. Before expiry it returns NotExpired. At or after exclusive expiry it asks the provider to irreversibly destroy that exact unit, then performs a fresh CanRead check. ProviderConfirmedDestroyed requires the destruction result and a subsequent denied read. This is the trusted provider's confirmation, not independent backend qualification. The provider must authenticate the identity/evidence pairing and supply durable all-copy receipts; flags from a caller or fixture are not proof.
+
+Each provider await is bounded to five seconds. Exceptions, stalls, provider cancellation, missing providers, false destruction and a subsequently readable unit remain Pending. Caller cancellation propagates even if a provider ignores its token; late faults are observed without publishing diagnostics. A timed-out operation can still complete in the provider. Retry **the same immutable identity/evidence/deadline**, and recover that provider's original receipt; never allocate replacement custody or extend expiry. The provider owns idempotency and actual I/O deadlines. The caller owns durable pending-work and receipt persistence through its existing owner mechanisms.
+
+No production adapter could be selected: no actor-history backend was found, and read-only Azure vault discovery failed for an unavailable MSAL token. Do not substitute recoverable soft deletion or a tombstone for irreversible source/derived-copy destruction. Independent lifecycle durability, copy inventory, destruction receipts, restored-backup quarantine, outage/lost-ack recovery and a still-live successor after predecessor expiry must pass actual qualification before activation. The existing strict Parties history fold can deny a successor when its predecessor proof is gone; this remains an explicit live qualification blocker.
+
+Verification: normal Debug source build, zero warnings/errors; all 111 Platform custody tests passed with no errors, failures, skips or not-run cases. This includes 43 new synthetic cleanup cases. The fixture contains no keys and deliberately cannot encrypt, decrypt, admit bindings or exercise restore. All 80 focused Parties configuration/query/admission tests also passed. [Exact source/test evidence](../../../agents/_bmad-output/implementation-artifacts/tests/actor-history-lifecycle-2026-10-07/evidence.json) records commands, hashes and limits. Full Story 5.4 and production owner commitments remain incomplete.

diff --git a/platform/src/Hexalith.Platform.Custody/IdentityHistoryCleanup.cs b/platform/src/Hexalith.Platform.Custody/IdentityHistoryCleanup.cs
new file mode 100644
index 0000000..6fa491a
--- /dev/null
+++ b/platform/src/Hexalith.Platform.Custody/IdentityHistoryCleanup.cs
@@ -0,0 +1,94 @@
+using Hexalith.EventStore.Contracts.Identity;
+using Hexalith.EventStore.Contracts.Security;
+
+namespace Hexalith.Platform.Custody;
+
+/// <summary>
+/// Bounds one expired-unit cleanup through existing custody, without owning keys, scheduling or receipt persistence.
+/// The provider must authenticate the exact tenant/unit and resolve idempotent irreversible outcomes across all copies.
+/// </summary>
+public sealed class IdentityHistoryCleanup(TimeProvider clock, IIdentityHistoryCustody? custody = null)
+{
+    private static readonly TimeSpan _providerWait = TimeSpan.FromSeconds(5);
+    private readonly TimeProvider _clock = clock ?? throw new ArgumentNullException(nameof(clock));
+
+    /// <summary>
+    /// Attempts cleanup only after immutable exclusive expiry; unknown outcomes remain pending.
+    /// Retries must pass the original evidence unchanged. Confirmation does not qualify a production backend.
+    /// </summary>
+    public async Task<IdentityHistoryCleanupOutcome> ProcessAsync(AggregateIdentity identity,
+        IdentityHistoryPolicy policy, DateTimeOffset effectiveAt, IdentityHistoryCustodyEvidence evidence,
+        CancellationToken cancellationToken = default)
+    {
+        cancellationToken.ThrowIfCancellationRequested();
+        if (identity is null || identity.Domain != "party" || policy is null || evidence is null
+            || !evidence.Satisfies(policy, effectiveAt))
+        {
+            return IdentityHistoryCleanupOutcome.Invalid;
+        }
+
+        if (_clock.GetUtcNow() < evidence.ExpiresAt)
+        {
+            return IdentityHistoryCleanupOutcome.NotExpired;
+        }
+
+        if (custody is null)
+        {
+            return IdentityHistoryCleanupOutcome.Pending;
+        }
+
+        try
+        {
+            bool destroyed = await AwaitProviderAsync(
+                () => custody.DestroyExpiredAsync(identity, evidence, cancellationToken), cancellationToken).ConfigureAwait(false);
+            cancellationToken.ThrowIfCancellationRequested();
+            if (!destroyed)
+            {
+                return IdentityHistoryCleanupOutcome.Pending;
+            }
+
+            bool readable = await AwaitProviderAsync(
+                () => custody.CanReadAsync(identity, evidence, cancellationToken), cancellationToken).ConfigureAwait(false);
+            cancellationToken.ThrowIfCancellationRequested();
+            return !readable && _clock.GetUtcNow() >= evidence.ExpiresAt
+                ? IdentityHistoryCleanupOutcome.ProviderConfirmedDestroyed
+                : IdentityHistoryCleanupOutcome.Pending;
+        }
+        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
+        {
+            throw;
+        }
+        catch (Exception)
+        {
+            cancellationToken.ThrowIfCancellationRequested();
+            return IdentityHistoryCleanupOutcome.Pending;
+        }
+    }
+
+    private static async Task<bool> AwaitProviderAsync(Func<Task<bool>> operation, CancellationToken token)
+    {
+        token.ThrowIfCancellationRequested();
+        Task<bool> pending = operation();
+        try
+        {
+            return await pending.WaitAsync(_providerWait, token).ConfigureAwait(false);
+        }
+        catch (Exception)
+        {
+            _ = ObserveLateAsync(pending);
+            throw;
+        }
+    }
+
+    private static async Task ObserveLateAsync(Task<bool> pending)
+    {
+        try
+        {
+            _ = await pending.ConfigureAwait(false);
+        }
+        catch (Exception)
+        {
+            // Observe unknown outcomes without disclosing provider diagnostics or fabricating a receipt.
+        }
+    }
+}

diff --git a/platform/src/Hexalith.Platform.Custody/IdentityHistoryCleanupOutcome.cs b/platform/src/Hexalith.Platform.Custody/IdentityHistoryCleanupOutcome.cs
new file mode 100644
index 0000000..927eddc
--- /dev/null
+++ b/platform/src/Hexalith.Platform.Custody/IdentityHistoryCleanupOutcome.cs
@@ -0,0 +1,17 @@
+namespace Hexalith.Platform.Custody;
+
+/// <summary>Content-free outcome of a single independently inventoried actor-history cleanup attempt.</summary>
+public enum IdentityHistoryCleanupOutcome
+{
+    /// <summary>The exact retention unit is not yet expired; custody was not contacted.</summary>
+    NotExpired,
+
+    /// <summary>The supplied scope, policy or custody evidence cannot authorize cleanup.</summary>
+    Invalid,
+
+    /// <summary>Custody has not confirmed irreversible destruction and fresh read denial.</summary>
+    Pending,
+
+    /// <summary>The trusted provider confirmed destruction and a fresh lifecycle check denied reads.</summary>
+    ProviderConfirmedDestroyed,
+}

diff --git a/platform/tests/Hexalith.Platform.Custody.Tests/IdentityHistoryCleanupFixture.cs b/platform/tests/Hexalith.Platform.Custody.Tests/IdentityHistoryCleanupFixture.cs
new file mode 100644
index 0000000..a8efdbd
--- /dev/null
+++ b/platform/tests/Hexalith.Platform.Custody.Tests/IdentityHistoryCleanupFixture.cs
@@ -0,0 +1,85 @@
+using Hexalith.EventStore.Contracts.Identity;
+using Hexalith.EventStore.Contracts.Security;
+
+namespace Hexalith.Platform.Custody.Tests;
+
+/// <summary>Synthetic retained-unit inventory and receipts; never a production crypto or restore provider.</summary>
+public sealed class IdentityHistoryCleanupFixture(CustodyFixtureClock clock) : IIdentityHistoryCustody
+{
+    private readonly Dictionary<(string Identity, string EvidenceId), IdentityHistoryCustodyEvidence> _inventory = [];
+    private readonly HashSet<(string Identity, string EvidenceId)> _destroyed = [];
+
+    /// <summary>Gets the original identity/evidence supplied by every cleanup attempt.</summary>
+    public List<(AggregateIdentity Identity, IdentityHistoryCustodyEvidence Evidence)> Attempts { get; } = [];
+
+    /// <summary>Gets the fresh read check count.</summary>
+    public int ReadChecks { get; private set; }
+
+    /// <summary>Gets or sets a provider-failure hook.</summary>
+    public Func<AggregateIdentity, IdentityHistoryCustodyEvidence, CancellationToken, Task<bool>>? DestructionHook { get; set; }
+
+    /// <summary>Gets or sets a fresh lifecycle-failure hook.</summary>
+    public Func<CancellationToken, Task<bool>>? ReadHook { get; set; }
+
+    /// <summary>Registers a synthetic exact scope and immutable receipt.</summary>
+    public void Register(AggregateIdentity identity, IdentityHistoryCustodyEvidence evidence)
+        => _inventory.Add((identity.ActorId, evidence.EvidenceId), evidence);
+
+    /// <summary>Gets synthetic inventory end-state for isolation and lost-acknowledgement assertions.</summary>
+    public bool IsDestroyed(AggregateIdentity identity, IdentityHistoryCustodyEvidence evidence)
+        => _destroyed.Contains((identity.ActorId, evidence.EvidenceId));
+
+    /// <summary>Completes an exact synthetic unit once; repeated calls recover its same receipt.</summary>
+    public bool CompleteDestruction(AggregateIdentity identity, IdentityHistoryCustodyEvidence evidence)
+    {
+        var key = (identity.ActorId, evidence.EvidenceId);
+        if (!_inventory.TryGetValue(key, out IdentityHistoryCustodyEvidence? expected)
+            || expected != evidence || clock.Now < expected.ExpiresAt)
+        {
+            return false;
+        }
+
+        _destroyed.Add(key);
+        return true;
+    }
+
+    /// <inheritdoc/>
+    public Task<bool> DestroyExpiredAsync(AggregateIdentity identity, IdentityHistoryCustodyEvidence evidence,
+        CancellationToken cancellationToken = default)
+    {
+        Attempts.Add((identity, evidence));
+        return DestructionHook is { } hook ? hook(identity, evidence, cancellationToken)
+            : Task.FromResult(CompleteDestruction(identity, evidence));
+    }
+
+    /// <inheritdoc/>
+    public Task<bool> CanReadAsync(AggregateIdentity identity, IdentityHistoryCustodyEvidence evidence,
+        CancellationToken cancellationToken = default)
+    {
+        ReadChecks++;
+        var key = (identity.ActorId, evidence.EvidenceId);
+        return ReadHook is { } hook ? hook(cancellationToken) : Task.FromResult(
+            _inventory.TryGetValue(key, out IdentityHistoryCustodyEvidence? expected) && expected == evidence
+            && !_destroyed.Contains(key) && clock.Now < expected.ExpiresAt);
+    }
+
+    /// <inheritdoc/>
+    public Task<IdentityHistoryCustodyEvidence?> AdmitAsync(AggregateIdentity identity, IdentityHistoryPolicy policy,
+        DateTimeOffset effectiveAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
+
+    /// <inheritdoc/>
+    public Task<PayloadProtectionResult> ProtectEventAsync(AggregateIdentity identity, string eventType, byte[] payload,
+        string format, IdentityHistoryCustodyEvidence evidence, CancellationToken cancellationToken = default) => throw new NotSupportedException();
+
+    /// <inheritdoc/>
+    public Task<PayloadProtectionResult> UnprotectEventAsync(AggregateIdentity identity, string eventType, byte[] payload,
+        string format, CancellationToken cancellationToken = default) => throw new NotSupportedException();
+
+    /// <inheritdoc/>
+    public Task<object> ProtectSnapshotAsync(AggregateIdentity identity, object snapshot,
+        CancellationToken cancellationToken = default) => throw new NotSupportedException();
+
+    /// <inheritdoc/>
+    public Task<object?> UnprotectSnapshotAsync(AggregateIdentity identity, object snapshot,
+        CancellationToken cancellationToken = default) => throw new NotSupportedException();
+}

diff --git a/platform/tests/Hexalith.Platform.Custody.Tests/IdentityHistoryCleanupTests.cs b/platform/tests/Hexalith.Platform.Custody.Tests/IdentityHistoryCleanupTests.cs
new file mode 100644
index 0000000..1d5022e
--- /dev/null
+++ b/platform/tests/Hexalith.Platform.Custody.Tests/IdentityHistoryCleanupTests.cs
@@ -0,0 +1,281 @@
+using Hexalith.EventStore.Contracts.Identity;
+using Hexalith.EventStore.Contracts.Security;
+
+using Microsoft.Extensions.DependencyInjection;
+
+using Shouldly;
+
+namespace Hexalith.Platform.Custody.Tests;
+
+/// <summary>Behavior evidence for bounded cleanup, separate from actual custody/restore qualification.</summary>
+public sealed class IdentityHistoryCleanupTests
+{
+    private readonly AggregateIdentity _identity = new("tenant-a", "party", "party-1");
+    private readonly IdentityHistoryPolicy _policy = new("party-actor-retention-v1", TimeSpan.FromDays(365), "binding-effective-at");
+    private readonly DateTimeOffset _effectiveAt = new(2026, 10, 7, 0, 0, 0, TimeSpan.Zero);
+    private readonly CustodyFixtureClock _clock = new();
+    private readonly IdentityHistoryCustodyEvidence _evidence;
+    private readonly IdentityHistoryCleanupFixture _provider;
+    private readonly IdentityHistoryCleanup _cleanup;
+
+    /// <summary>Creates an exact expired synthetic retention unit, with no actual cryptographic material.</summary>
+    public IdentityHistoryCleanupTests()
+    {
+        _evidence = new(_policy.PolicyId, "party-actor-history-v1", _effectiveAt.AddDays(365), 1, "receipt-1", true, true, true);
+        _clock.Now = _evidence.ExpiresAt;
+        _provider = new(_clock);
+        _provider.Register(_identity, _evidence);
+        _cleanup = new(_clock, _provider);
+    }
+
+    private Task<IdentityHistoryCleanupOutcome> RunAsync(CancellationToken? token = null)
+        => _cleanup.ProcessAsync(_identity, _policy, _effectiveAt, _evidence, token ?? TestContext.Current.CancellationToken);
+
+    /// <summary>Cleanup never contacts custody before exclusive expiry.</summary>
+    [Theory]
+    [InlineData(-1L)]
+    [InlineData(-864000000000L)]
+    public async Task BeforeExpiry_DoesNotContactProvider(long offsetTicks)
+    {
+        _clock.Now = _evidence.ExpiresAt.AddTicks(offsetTicks);
+        (await RunAsync()).ShouldBe(IdentityHistoryCleanupOutcome.NotExpired);
+        _provider.Attempts.ShouldBeEmpty();
+        _provider.ReadChecks.ShouldBe(0);
+        _provider.IsDestroyed(_identity, _evidence).ShouldBeFalse();
+    }
+
+    /// <summary>Exactly at and after expiry require provider destruction and fresh denial.</summary>
+    [Theory]
+    [InlineData(0L)]
+    [InlineData(1L)]
+    [InlineData(864000000000L)]
+    public async Task AtOrAfterExpiry_ConfirmsExactUnitAndFreshDenial(long offsetTicks)
+    {
+        _clock.Now = _evidence.ExpiresAt.AddTicks(offsetTicks);
+        (await RunAsync()).ShouldBe(IdentityHistoryCleanupOutcome.ProviderConfirmedDestroyed);
+        _provider.IsDestroyed(_identity, _evidence).ShouldBeTrue();
+        _provider.Attempts.Single().ShouldBe((_identity, _evidence));
+        _provider.ReadChecks.ShouldBe(1);
+    }
+
+    /// <summary>Invalid policy/scope/evidence cannot authorize any destruction attempt.</summary>
+    [Theory]
+    [InlineData("domain")][InlineData("null-identity")][InlineData("null-policy")][InlineData("null-evidence")]
+    [InlineData("id")][InlineData("zero")][InlineData("negative")][InlineData("duration")][InlineData("trigger")]
+    [InlineData("purpose")][InlineData("expiry")][InlineData("revision")][InlineData("receipt")]
+    [InlineData("source")][InlineData("restore")][InlineData("copies")][InlineData("overflow")]
+    public async Task InvalidInput_DeniesBeforeProvider(string field)
+    {
+        AggregateIdentity identity = field == "domain" ? new("tenant-a", "other", "party-1") : _identity;
+        IdentityHistoryPolicy policy = field switch
+        {
+            "id" => _policy with { PolicyId = "different-version" },
+            "zero" => _policy with { Retention = TimeSpan.Zero },
+            "negative" => _policy with { Retention = TimeSpan.FromDays(-1) },
+            "duration" => _policy with { Retention = TimeSpan.FromDays(366) },
+            "trigger" => _policy with { ExpiryTrigger = "profile-erasure" },
+            "overflow" => _policy with { Retention = TimeSpan.MaxValue },
+            _ => _policy,
+        };
+        IdentityHistoryCustodyEvidence evidence = field switch
+        {
+            "purpose" => _evidence with { Purpose = "party-profile" },
+            "expiry" => _evidence with { ExpiresAt = _evidence.ExpiresAt.AddTicks(1) },
+            "revision" => _evidence with { LifecycleRevision = 0 },
+            "receipt" => _evidence with { EvidenceId = " " },
+            "source" => _evidence with { SourceExpiryEnforced = false },
+            "restore" => _evidence with { RestoreSafe = false },
+            "copies" => _evidence with { DerivedCopiesCovered = false },
+            _ => _evidence,
+        };
+        (await _cleanup.ProcessAsync(field == "null-identity" ? null! : identity,
+            field == "null-policy" ? null! : policy, _effectiveAt, field == "null-evidence" ? null! : evidence,
+            TestContext.Current.CancellationToken)).ShouldBe(IdentityHistoryCleanupOutcome.Invalid);
+        _provider.Attempts.ShouldBeEmpty();
+        _provider.ReadChecks.ShouldBe(0);
+        _provider.IsDestroyed(_identity, _evidence).ShouldBeFalse();
+    }
+
+    /// <summary>Default registration supplies a usable pending operation, without installing a history provider.</summary>
+    [Fact]
+    public async Task DefaultRegistration_KeepsMissingCustodyPending()
+    {
+        using ServiceProvider services = new ServiceCollection().AddSingleton<TimeProvider>(_clock).AddPlatformCustody().BuildServiceProvider();
+        using IServiceScope scope = services.CreateScope();
+        scope.ServiceProvider.GetService<IIdentityHistoryCustody>().ShouldBeNull();
+        IdentityHistoryCleanup cleanup = scope.ServiceProvider.GetRequiredService<IdentityHistoryCleanup>();
+        (await cleanup.ProcessAsync(_identity, _policy, _effectiveAt, _evidence, TestContext.Current.CancellationToken))
+            .ShouldBe(IdentityHistoryCleanupOutcome.Pending);
+    }
+
+    /// <summary>False/failed/cancelled provider outcomes cannot become destruction receipts.</summary>
+    [Theory]
+    [InlineData("false")][InlineData("sync-throw")][InlineData("async-throw")][InlineData("provider-cancel")]
+    public async Task FailedDestruction_RemainsPending(string failure)
+    {
+        _provider.DestructionHook = (_, _, _) => failure switch
+        {
+            "false" => Task.FromResult(false),
+            "sync-throw" => throw new InvalidOperationException("synthetic failure"),
+            "async-throw" => Task.FromException<bool>(new InvalidOperationException("synthetic failure")),
+            _ => Task.FromCanceled<bool>(new CancellationToken(canceled: true)),
+        };
+        (await RunAsync()).ShouldBe(IdentityHistoryCleanupOutcome.Pending);
+        _provider.IsDestroyed(_identity, _evidence).ShouldBeFalse();
+        _provider.ReadChecks.ShouldBe(0);
+    }
+
+    /// <summary>Readable or unavailable lifecycle after provider confirmation cannot establish completion.</summary>
+    [Theory]
+    [InlineData("readable")][InlineData("sync-throw")][InlineData("async-throw")][InlineData("provider-cancel")]
+    public async Task FailedFreshDenial_RemainsPending(string failure)
+    {
+        _provider.ReadHook = _ => failure switch
+        {
+            "readable" => Task.FromResult(true),
+            "sync-throw" => throw new InvalidOperationException("synthetic failure"),
+            "async-throw" => Task.FromException<bool>(new InvalidOperationException("synthetic failure")),
+            _ => Task.FromCanceled<bool>(new CancellationToken(canceled: true)),
+        };
+        (await RunAsync()).ShouldBe(IdentityHistoryCleanupOutcome.Pending);
+        _provider.ReadChecks.ShouldBe(1);
+    }
+
+    /// <summary>A stalled destruction or final check has a finite wait and remains pending even after late success.</summary>
+    [Theory]
+    [InlineData(false)]
+    [InlineData(true)]
+    public async Task StalledProvider_TimesOutAsPending(bool finalCheck)
+    {
+        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
+        if (finalCheck)
+        {
+            _provider.ReadHook = _ => pending.Task;
+        }
+        else
+        {
+            _provider.DestructionHook = (_, _, _) => pending.Task;
+        }
+
+        try
+        {
+            (await RunAsync().WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken))
+                .ShouldBe(IdentityHistoryCleanupOutcome.Pending);
+        }
+        finally
+        {
+            pending.TrySetResult(!finalCheck);
+        }
+    }
+
+    /// <summary>Lost acknowledgement leaves pending; retry recovers exactly the original immutable receipt.</summary>
+    [Fact]
+    public async Task LostAcknowledgement_RetryRecoversSameDestroyedUnit()
+    {
+        _provider.DestructionHook = (identity, evidence, _) =>
+        {
+            _provider.CompleteDestruction(identity, evidence).ShouldBeTrue();
+            return Task.FromException<bool>(new IOException("synthetic lost acknowledgement"));
+        };
+        (await RunAsync()).ShouldBe(IdentityHistoryCleanupOutcome.Pending);
+        _provider.IsDestroyed(_identity, _evidence).ShouldBeTrue();
+        _provider.DestructionHook = null;
+        (await RunAsync()).ShouldBe(IdentityHistoryCleanupOutcome.ProviderConfirmedDestroyed);
+        _provider.Attempts.Count.ShouldBe(2);
+        _provider.Attempts.ShouldAllBe(attempt => attempt.Identity == _identity && attempt.Evidence == _evidence);
+        _evidence.ExpiresAt.ShouldBe(_effectiveAt.AddDays(365));
+    }
+
+    /// <summary>Cleanup does not sweep another tenant or a live successor, even when evidence IDs overlap.</summary>
+    [Fact]
+    public async Task ExactExpiredUnit_PreservesForeignUnitAndLiveSuccessor()
+    {
+        var foreign = new AggregateIdentity("tenant-b", "party", _identity.AggregateId);
+        IdentityHistoryCustodyEvidence successor = _evidence with { EvidenceId = "receipt-2", ExpiresAt = _clock.Now.AddDays(1), LifecycleRevision = 2 };
+        _provider.Register(foreign, _evidence);
+        _provider.Register(_identity, successor);
+        (await RunAsync()).ShouldBe(IdentityHistoryCleanupOutcome.ProviderConfirmedDestroyed);
+        _provider.IsDestroyed(_identity, _evidence).ShouldBeTrue();
+        _provider.IsDestroyed(foreign, _evidence).ShouldBeFalse();
+        _provider.IsDestroyed(_identity, successor).ShouldBeFalse();
+    }
+
+    /// <summary>The trusted provider authenticates tenant/unit references rather than treating flags as authority.</summary>
+    [Fact]
+    public async Task ForeignReceipt_IsRejectedByScopedProvider()
+    {
+        var foreign = new AggregateIdentity("tenant-b", "party", _identity.AggregateId);
+        (await _cleanup.ProcessAsync(foreign, _policy, _effectiveAt, _evidence, TestContext.Current.CancellationToken))
+            .ShouldBe(IdentityHistoryCleanupOutcome.Pending);
+        _provider.IsDestroyed(_identity, _evidence).ShouldBeFalse();
+        _provider.IsDestroyed(foreign, _evidence).ShouldBeFalse();
+        _provider.ReadChecks.ShouldBe(0);
+    }
+
+    /// <summary>Pre-cancellation prevents custody calls.</summary>
+    [Fact]
+    public async Task PreCancelled_DoesNotContactProvider()
+    {
+        using var cancellation = new CancellationTokenSource();
+        cancellation.Cancel();
+        await Should.ThrowAsync<OperationCanceledException>(() => RunAsync(cancellation.Token));
+        _provider.Attempts.ShouldBeEmpty();
+    }
+
+    /// <summary>Caller cancellation interrupts an ignoring provider without relabeling its late outcome.</summary>
+    [Theory]
+    [InlineData(false, false)][InlineData(false, true)][InlineData(true, false)][InlineData(true, true)]
+    public async Task CancelledWhileProviderIgnoresToken_PreservesCancellation(bool finalCheck, bool lateFault)
+    {
+        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
+        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
+        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
+        Task<bool> Stall()
+        {
+            entered.SetResult();
+            return pending.Task;
+        }
+        if (finalCheck)
+        {
+            _provider.ReadHook = _ => Stall();
+        }
+        else
+        {
+            _provider.DestructionHook = (_, _, _) => Stall();
+        }
+
+        Task<IdentityHistoryCleanupOutcome> running = RunAsync(cancellation.Token);
+        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
+        cancellation.Cancel();
+        await Should.ThrowAsync<OperationCanceledException>(() => running.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
+        if (lateFault)
+        {
+            pending.SetException(new IOException("synthetic late failure"));
+        }
+        else
+        {
+            pending.SetResult(!finalCheck);
+        }
+        running.IsCanceled.ShouldBeTrue();
+        _provider.ReadChecks.ShouldBe(finalCheck ? 1 : 0);
+    }
+
+    /// <summary>Cancellation at either provider return wins over a success result.</summary>
+    [Theory]
+    [InlineData(false)]
+    [InlineData(true)]
+    public async Task CancelledAtProviderReturn_DoesNotReturnSuccess(bool finalCheck)
+    {
+        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
+        if (finalCheck)
+        {
+            _provider.ReadHook = _ => { cancellation.Cancel(); return Task.FromResult(false); };
+        }
+        else
+        {
+            _provider.DestructionHook = (_, _, _) => { cancellation.Cancel(); return Task.FromResult(true); };
+        }
+        await Should.ThrowAsync<OperationCanceledException>(() => RunAsync(cancellation.Token));
+        _provider.ReadChecks.ShouldBe(finalCheck ? 1 : 0);
+    }
+}

diff --git a/parties/_bmad-output/implementation-artifacts/8-7-data-protection-extraction.md b/parties/_bmad-output/implementation-artifacts/8-7-data-protection-extraction.md
index cfad30a4..b3d8cc84 100644
--- a/parties/_bmad-output/implementation-artifacts/8-7-data-protection-extraction.md
+++ b/parties/_bmad-output/implementation-artifacts/8-7-data-protection-extraction.md
@@ -3,13 +3,13 @@ story_key: 8-7-data-protection-extraction
 story_id: "8.7"
 epic: "8"
 created: 2026-07-16T00:23:38+02:00
-revalidated: 2026-10-03
+revalidated: 2026-10-05
 source_status: backlog
 target_status: blocked
 baseline_commit_at_story_start: a35b151
-baseline_commit_at_revalidation: 06714c166c090200ac87373b11d8243aa11b2126
-eventstore_root_pin_at_revalidation: 2c58ffda41759e895ace4b9625c9bd931a217672
-eventstore_checkout_at_revalidation: 2c58ffda41759e895ace4b9625c9bd931a217672
+baseline_commit_at_revalidation: b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca
+eventstore_root_pin_at_revalidation: 865cd9e49273dffbb1cdae85efeaf1aac322e09e
+eventstore_checkout_at_revalidation: 865cd9e49273dffbb1cdae85efeaf1aac322e09e
 ---
 
 # Story 8.7: Data-protection extraction
@@ -18,25 +18,34 @@ Status: blocked
 
 <!-- This story is complete enough for workflow intake, but production source migration is hard-gated by the Story 8.3 G5 row and the authoritative Epic 8 sequence. -->
 
-## Current Gate Revalidation — 2026-10-03
-
-G5 remains `needs-additive-api`; Story 8.7 remains `blocked`. EventStore's matching
-root gitlink and clean checkout are `2c58ffda41759e895ace4b9625c9bd931a217672` (`v3.111.0`);
-Builds `688eec9a4333245cc0ff7772115c769094471863` selects EventStore packages `3.110.0`.
-These observations do not authorize adoption beyond the locked specification's
-`c21bd749154d701c3b7d68e40d1008d3475e35c4` / `3.95.0` identity.
-
-EventStore has delivered public policy/erasure contracts and an internal v2 core
-project with `IsPackable=false`. Story 8.2 is `done`, 8.3 is `in-progress`, and
-8.4-8.11 remain `backlog`. The production-backend project, package enrollment,
-approved runtime provider, dual-provider parity, post-v2 rollback, and 8.11
-closure/availability approval remain missing. The older absence statements below
-are historical; the 2026-10-03 G5 receipt in the Story 8.3 matrix supersedes them.
-
-Parties 8.6 is `done`. Static inventory confirmed all 18 MOVE files, 5 KEEP files,
-the adapter, and local DI remain intact. No production, dependency, or submodule
-changes were made. Product tests were not run or credited. Commands and results
-are recorded in `tests/test-summary.md` under the 2026-10-03 receipt.
+## Current Gate Revalidation — 2026-10-05
+
+G5 remains `needs-additive-api`; Story 8.7 remains `blocked`. At Parties
+`b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca`, EventStore's root gitlink and clean checkout match
+`865cd9e49273dffbb1cdae85efeaf1aac322e09e` (`v3.113.0`). Builds' root gitlink and clean checkout
+match `90f3836dd7482db35c2c187a50999b99215919b0` (`v4.29.1-21-g90f3836`); both its catalog and the
+Parties pre-import pin select EventStore `3.113.0`.
+
+EventStore's v3.113.0 package/source selection already has a dated I20 approval.
+The later Builds identity is an observation; the I20 approved selection remains
+`360a2b9c4e96809365a7de785be9a68152d5ac28`. Neither source presence nor identity
+selection is G5 availability approval, and this audit adopts no new dependency.
+The locked 8.7 spec block still names `c21bd749154d701c3b7d68e40d1008d3475e35c4`
+/ `3.95.0`; activation requires reconciling it with an approved G5 identity.
+
+Public policy/erasure contracts and an internal, non-packable v2 core exist.
+Owner 8.2 is `done`, 8.3 is `in-progress`, and 8.4-8.11 remain `backlog`.
+Missing receipts: compatibility/key-lifecycle integration, AzureKeyVault backend,
+package/release enrollment, a consumable runtime provider, dual-provider GDPR
+parity, post-v2 rollback, and the 8.11 G5 availability approval/closure packet.
+The I2/I19a policy-hook classification approval is also pending; Parties must
+remain the policy writer. Existing owner contract/spec approvals do not close G5.
+
+Parties 8.6 is `done`; accepted Epic 8 closure deferrals do not complete 8.7.
+All 24 MOVE/KEEP/adapter files, the local harness, and local DI remain intact.
+The 32 recorded G5 static checks passed. No production, DI, dependency, submodule,
+or frozen-spec changes were made. Product tests were not run or credited.
+Commands and results are recorded in `tests/test-summary.md` under this date.
 
 ## Story
 
@@ -68,6 +77,7 @@ so that Parties keeps GDPR policy without owning reusable crypto infrastructure.
   - [x] Mark Story 8.7 blocked and halt before production code, package, or submodule edits.
   - [x] 2026-09-07 spec execution: revalidated G5 at live gitlink/checkout `d45206f7cbd80a112519c1d4687d7279a745f0c5` (`v3.103.0-2-gd45206f7`) / package `3.103.0`. Row remains `needs-additive-api`; halted with no production, DI, or dependency changes. Spec frozen identity `c21bd749`/`3.95.0` still does not match live checkout.
   - [x] 2026-10-03 gate audit: G5 remains `needs-additive-api` at EventStore `2c58ffda41759e895ace4b9625c9bd931a217672` (`v3.111.0`) / catalog `3.110.0`. Public policy/erasure contracts and internal non-packable v2 core now exist; runtime/backend/package, parity, rollback, and 8.11 closure are still incomplete. Retained all 24 MOVE/KEEP/adapter files and local DI; no production or dependency changes.
+  - [x] 2026-10-05 gate audit: EventStore v3.113.0 and current Builds catalog still lack a consumable G5 provider, release enrollment, dual-provider parity, post-v2 rollback, 8.11 closure, and I2/I19a classification approval. All 32 recorded static checks and 24 retained-file checks passed; no production/dependency changes or product-test credit.
   - [ ] Before implementation resumes, update the existing G5 row with the named owner/reviewer, exact approved release or root gitlink, producer and consumer proof, frozen compatibility identities, and rollback instructions; change its status only when the review gate is genuinely satisfied.
   - [x] Resolve the `8.6 -> 8.7` sequence by completing 8.6 (done 2026-08-17). Parallel owner delivery still does not waive G5.
 
@@ -275,6 +285,7 @@ GPT-5 Codex
 
 ### Debug Log References
 
+- 2026-10-05 - Revalidated the closed G5 gate at Parties `b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca`, matching EventStore `865cd9e49273dffbb1cdae85efeaf1aac322e09e` / package `3.113.0`, and observed Builds `90f3836dd7482db35c2c187a50999b99215919b0`. Preserved the frozen spec, local engine, public APIs, and all rollback files. Recorded remaining provider/release/parity/rollback/approval receipts; no production or dependency edits.
 - 2026-10-03 - Revalidated G5 at Parties `06714c166c090200ac87373b11d8243aa11b2126` and matching EventStore `2c58ffda41759e895ace4b9625c9bd931a217672` (`v3.111.0`). Owner 8.2 is done, 8.3 in-progress, 8.4-8.11 backlog. Recorded partial contract/core delivery and remaining backend/package/parity/rollback/closure blockers; halted before production, dependency, or submodule changes.
 - 2026-07-16T00:23:38+02:00 - Loaded sprint status and selected requested story `8-7-data-protection-extraction` from `_bmad-output/implementation-artifacts/sprint-status.yaml` (`backlog`).
 - 2026-07-16T00:23:38+02:00 - Inspected the Story 8.3 G5 row; it remains `needs-additive-api` and owner routing explicitly remains non-delivery.
@@ -287,9 +298,10 @@ GPT-5 Codex
 
 ### Completion Notes List
 
+- 2026-10-05 static gate audit passed (32 G5 checks plus retained-file/DI checks). G5 remains `needs-additive-api`, Story 8.7 remains `blocked`, and the crypto-retention action remains `open`. I2/I19a classification and EventStore 8.11 closure are still prerequisites. Product/dual-provider/GDPR/post-v2 suites were not run or credited.
 - 2026-10-03 gate audit supersedes the older source-absence statements: contracts and internal v2 source now exist, but no approved consumable G5 provider or closure does. Source migration stays blocked. Static inventory passed; dual-provider/GDPR/post-v2 suites were not run or credited.
 - Source migration is blocked because no owner-approved G5 shared payload-protection engine, release/root pin, dual-path parity evidence, or exercised rollback is recorded. Story 8.6 is `done`; G5 remains the live halt.
-- The current root-approved and checked-out EventStore versions both lack `pdenc-v2`, `IPersonalDataPolicy`, `IErasureStateProvider`, and a shared payload engine; ASP.NET Core cursor Data Protection is not a substitute.
+- Historical story-creation absence claims are superseded by the current gate receipt: public policy/erasure contracts and internal v2 core now exist, but no approved consumable shared provider does. ASP.NET Core cursor Data Protection is not a substitute.
 - No production source, project/package dependency, or submodule pointer was changed and no product tests were run during story creation. The local implementation remains the required rollback path.
 - 2026-09-07 spec execution confirmed the same halt at EventStore `d45206f7` / package `3.103.0`. Dual-provider, GDPR, and post-v2 rollback suites were not run and are not credited.
 
diff --git a/parties/_bmad-output/implementation-artifacts/epic-8-context.md b/parties/_bmad-output/implementation-artifacts/epic-8-context.md
index c9012718..6f27c60e 100644
--- a/parties/_bmad-output/implementation-artifacts/epic-8-context.md
+++ b/parties/_bmad-output/implementation-artifacts/epic-8-context.md
@@ -1,57 +1,70 @@
-# Epic 8 Context: Domain-Focus Refactoring and Platform Extraction (Class C)
-
-<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->
-
-## Goal
-
-Remove reusable platform infrastructure from Hexalith.Parties while retaining domain substance, policy, typed clients, UI, and samples. Mechanics move to their platform owners only after ownership, compatibility, rollback, and validation are proven. This approved post-MVP maintenance adds no PRD functional requirements and must not be reported as feature delivery.
-
-## Stories
-
-- Story 8.1: Baseline and release-blocker stabilization
-- Story 8.2: Identifier correctness and zero-risk hygiene
-- Story 8.3: Platform API prerequisites
-- Story 8.4: Leaf-project retirement
-- Story 8.5: EventStore domain-service SDK host cutover
-- Story 8.6: Projection and query SDK migration
-- Story 8.7: Data-protection extraction
-- Story 8.8: Client, MCP, AppHost, build, and runtime-boundary cleanup
-- Story 8.9: UI FrontComposer and Fluent consolidation
-- Story 8.10: Final readiness, documentation, and retirement gate
-- Story 8.11: Validation fallback ladder runner and guidance
-- Story 8.12: Parties-only Zot container publish CI
-- Story 8.13: Retire legacy in-repo deployment artifacts
-
-## Requirements & Constraints
-
-Preserve command/query behavior, tenant isolation, consumer own-data checks including `aggregateId == party_id`, public Client/Contracts shapes, and Picker/AdminPortal/ConsumerPortal compatibility. Contract changes require approved versioning. Diagnostics remain PII-free and low-cardinality.
-
-Every remaining migration or activated deferral needs a specification declaring prerequisites, all touched repositories, an exercised rollback, validation lanes, non-goals, and a parity checklist. Broad migrations must be split or hard-gated. Replacement adapters prove parity before deletion. Named baseline tests cannot be weakened; successors need owner approval.
-
-Consumption and parity evidence name exact package versions or root gitlink identities matching the dependency mode. Identity movement immediately invalidates affected claims and stops covered deletions until revalidation, even without a written marker. Approvals require prerequisite-matrix table rows naming decision, artifact, human, and date, written by that human or the reviewer gate. Executors cannot manufacture approval; a missing table blocks approval-gated actions.
-
-Keep .NET 10, `.slnx`, central packages, warnings-as-errors, MinVer, root-only submodules, and established unit, topology, package, and accessibility lanes. Run xUnit v3 projects individually; focused runs invoke built assemblies. Production regulated data still requires production KMS/secret-backed keys.
-
-## Technical Decisions
-
-Continue Epic 7's adapter-first migration. EventStore owns hosting, projection/query mechanics, envelopes, and freshness; Commons owns cross-cutting utilities and paging; FrontComposer owns UI mechanics; Builds owns shared build logic. Parties retains semantic policy and compatibility mapping. Shared Contracts anchors remain defined once; authentication retirement cannot authorize Contracts breakage.
-
-The domain host has no public API. Gateway/DAPR invocation stays deny-by-default and EventStore-only; subscription delivery remains separate. Route changes need approval; external ACL copies attest equivalent policy. The SDK host retains domain registrations, Parties policy, and approved hooks.
-
-Preserve replay from zero, checkpoints, set-based idempotency, duplicate/out-of-order tolerance, and rebuild-vs-replay verification. Use SDK projection/query handlers, read-model/write-policy and cursor seams. Freshness has one versioned grammar; pending the shared contract, emit Current/Stale/Unavailable with defined degraded metadata. Erased IDs remain tombstoned. ULID acceptance and GUID-shaped replay survive; allocation checks aggregate/event-stream tombstones.
-
-Classify every touched store as an event-derived, rebuildable read model or an operational side-effect ledger retained across rebuilds with separate consistency tests. Personal-data ledgers still obey erasure. A ledger cannot use rebuildable persistence without approved reclassification and parity.
-
-Generic payload protection and key-management mechanics move behind shared contracts. Parties alone writes erasure, restriction, personal-data classification, and lawful-basis policy. Policy/state adapters are stateless and read aggregate/event-stream state. Before adoption, owners must separate engine capabilities from Parties hooks. Preserve `json+pdenc-v1`, `json-redacted`, legacy reads, key zeroing, typed-unreadable outcomes, no-leak diagnostics, exports, processing records, and the single versioned certificate/report shape. Erasure has exactly Admin and Consumer doors; MCP deletion remains soft-deactivation.
-
-`Hexalith.McpCli` is the CLI/MCP target through decorated Contracts and approved enrollment/authorization; replacement or approved withdrawal proves parity before module MCP retirement. AppHost retirement needs integrated topology, security, publish, rollback, and dependent-deferral proofs. Runtime orchestration is external; Parties publishes immutable image tags without deployment manifests.
-
-## UX & Interaction Patterns
-
-Use FrontComposer and Fluent V5/Fluent 2; purge FAST/v4 tokens. Preserve WCAG 2.2 AA semantics, keyboard/pointer parity, focus, skip links, forced colors, reduced motion, non-color cues, typed destructive confirmation, polite status/assertive errors, and no optimistic focus stealing.
-
-Stale/degraded reads show last-known values and freshness; accepted commands never promise read-your-write. Parties owns legal strings: consent differs from lawful basis, restriction permits consent edits unless erasure is underway, cancellation differs from permanent erasure, and export copy makes no unsupported timing promise.
-
-## Cross-Story Dependencies
-
-Continue Epic 7 parity/rollback. Core order: `8.1 -> 8.2 -> 8.3 -> 8.4 -> 8.5 -> 8.6 -> 8.7 -> 8.8 -> 8.9 -> 8.10`; 8.5-8.7 require platform readiness. Deferrals/children inherit `8.6 -> 8.7 -> 8.8 -> 8.9`, taking the later parent slot. Concurrency needs disjoint non-Parties repositories and touched-surface parity invariants. Final readiness closes or explicitly defers work with owners, proof, rollback, and evidence. Deployment retirement follows replacement publication.
+# Epic 8 Context: Domain-Focus Refactoring and Platform Extraction (Class C)
+
+<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->
+
+## Goal
+
+Extract platform mechanics while preserving Parties domain behavior, policy, clients, UI, and samples. This maintenance adds no PRD requirements. Epic 8/Story 8.10 closed through deferrals on 2026-10-05; Stories 8.7–8.9 stay blocked with rollback retained. Live `sprint-status.yaml` remains authoritative.
+
+## Stories
+
+- Story 8.1: Baseline and release-blocker stabilization
+- Story 8.2: Identifier correctness and zero-risk hygiene
+- Story 8.3: Platform API prerequisites
+- Story 8.4: Leaf-project retirement
+- Story 8.5: EventStore domain-service SDK host cutover
+- Story 8.6: Projection and query SDK migration
+- Story 8.7: Data-protection extraction
+- Story 8.8: Client, MCP, AppHost, build, and runtime-boundary cleanup
+- Story 8.9: UI FrontComposer and Fluent consolidation
+- Story 8.10: Final readiness, documentation, and retirement gate
+- Story 8.11: Validation fallback ladder runner and guidance
+- Story 8.12: Parties-only Zot container publish CI
+- Story 8.13: Retire legacy in-repo deployment artifacts
+
+## Requirements & Constraints
+
+Preserve command/query behavior, tenant isolation, `aggregateId == party_id` own-data checks, public Client/Contracts and UI RCL compatibility. Breaking contracts require approved versioning. Diagnostics remain PII-free and low-cardinality.
+
+Migration/deferral specs must name prerequisites, touched repositories, exercised rollback, validation lanes, non-goals, and parity checklist. Split or hard-gate broad migrations. Prove parity before deletion; retain baseline tests absent approved successors, canonical sprint keys, and `blocked` status.
+
+Evidence names exact consumed packages/gitlinks. Identity movement invalidates affected claims and stops covered deletion until revalidation. Approval requires a prerequisite-ledger row naming decision, artifact, human, and date, written by that human/reviewer gate. Selection/compile never imply parity or adoption approval.
+
+Dated 2026-10-05 approved selections:
+
+| Dependency | Root gitlink | Package |
+| --- | --- | --- |
+| EventStore | `865cd9e49273dffbb1cdae85efeaf1aac322e09e` | `3.113.0` |
+| Builds | `360a2b9c4e96809365a7de785be9a68152d5ac28` | — |
+| Commons HTTP | `116d26815eb81e35b3c161e1799e5ee12805fc0a` | fallback `2.30.1` |
+| FrontComposer | `2cc8dd3a3ac76c03f5ea6f6f92e65829306db470` | `4.5.0` |
+| Memories | `5b43fe2f8a0f04dc021921a077dff1a573c2ce5e` | `2.27.1` |
+| Tenants | `72b8e4f508176b69826549e87b7b2a286f607fd1` | `5.7.0` |
+| PolymorphicSerializations | `98de6e013840ece9f0fa7c68ab7dcdf2bba3b375` | — |
+| AI.Tools | `3f194e17174994d308ec84af9ee2b5aa68674d0d` | — |
+
+Later Builds, FrontComposer, Memories, and Tenants gitlinks differ. Observed Builds `90f3836dd7482db35c2c187a50999b99215919b0` selects EventStore `3.113.0`; this grants no approval. Revalidate against the actual graph.
+
+Keep .NET 10, `.slnx`, CPM, warnings-as-errors, MinVer, root-only submodules, and established validation gates. Run xUnit v3 projects individually; focused runs invoke built assemblies. Regulated production data requires production KMS/secret-backed keys.
+
+## Technical Decisions
+
+Continue adapter-first extraction: EventStore owns hosting/projection/query/envelope mechanics; Commons owns utilities/paging; FrontComposer owns UI mechanics; Builds owns shared build logic. Parties retains semantic policy, compatibility mapping, and singly defined Contracts anchors.
+
+The host has no public API. DAPR remains deny-default/EventStore-only; subscriptions stay separate. Approve route changes; external ACLs attest equivalent tuples. AppHost retirement requires topology, security, publish, rollback, and dependent-deferral proof. Runtime orchestration stays external; Parties publishes immutable images.
+
+Preserve replay/checkpoints, idempotency, duplicate/out-of-order tolerance, rebuild-vs-replay proof, last-known fallback, versioned freshness, and permanent tombstones. Identifier acceptance never authorizes tombstoned allocation. Classify stores as rebuildable read models or operational ledgers; ledgers survive rebuilds with consistency tests and obey erasure.
+
+G5 payload protection remains `needs-additive-api`; available key-ring/cursor DataProtection APIs do not authorize engine adoption. Parties alone decides erasure, restriction, personal-data classification, and lawful basis. Stateless policy/erasure hooks read aggregate/event-stream state; owner approval must classify them before G5 adoption. Preserve `json+pdenc-v1`, `json-redacted`, legacy reads, key zeroing, typed unreadability, no-leak diagnostics, exports, processing records, and one versioned certificate/report shape. Erasure has Admin and Consumer doors; MCP deletion is soft-deactivation.
+
+Before crypto deletion, decide backend/readability window. One approval binds security and AppHost cutover: prove predecessor key-ring, cursor-purpose, and payload continuity or approve explicit invalidation with typed outcomes. McpCli replacement or approved withdrawal proves parity before module MCP retirement.
+
+## UX & Interaction Patterns
+
+Inherit FrontComposer and Fluent V5/Fluent 2; purge FAST/v4 tokens. Preserve WCAG 2.2 AA, keyboard/pointer parity, focus, skip links, forced colors, reduced motion, non-color cues, destructive confirmation, polite status/assertive errors, and no optimistic focus stealing. I13 remains deferred: DW-111 accepts missing runtime content-control proof without certifying parity.
+
+Stale/degraded reads show last-known data; acceptance never promises read-your-write. Parties owns legal strings: consent versus lawful basis, restriction allowing consent edits except during erasure, cancellation versus permanence, and honest export timing.
+
+## Cross-Story Dependencies
+
+Order: `8.1 -> 8.2 -> 8.3 -> 8.4 -> 8.5 -> 8.6 -> 8.7 -> 8.8 -> 8.9 -> 8.10`; 8.5–8.7 require platform readiness. Deferrals/children inherit `8.6 -> 8.7 -> 8.8 -> 8.9`, taking the later parent slot. Concurrency requires disjoint repositories/parity invariants beyond Parties. Deferral closure never authorizes migration/deletion.
diff --git a/parties/_bmad-output/implementation-artifacts/spec-8-7-data-protection-extraction.md b/parties/_bmad-output/implementation-artifacts/spec-8-7-data-protection-extraction.md
index 1d4d6d56..79207dd9 100644
--- a/parties/_bmad-output/implementation-artifacts/spec-8-7-data-protection-extraction.md
+++ b/parties/_bmad-output/implementation-artifacts/spec-8-7-data-protection-extraction.md
@@ -62,6 +62,8 @@ context:
 
 ## Spec Change Log
 
+- 2026-10-05: Closed-gate audit at Parties `b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca`. EventStore root gitlink/clean checkout `865cd9e49273dffbb1cdae85efeaf1aac322e09e` (`v3.113.0`); observed Builds gitlink/clean checkout `90f3836dd7482db35c2c187a50999b99215919b0` (`v4.29.1-21-g90f3836`); both catalog and Parties pin select `3.113.0`. The existing I20 EventStore identity selection is not G5 approval; the later Builds observation is not a new approval. Owner 8.2 remains done, 8.3 in-progress, 8.4-8.11 backlog. Public policy/erasure contracts and internal non-packable v2 core exist; compatibility/lifecycle/backend/runtime/release delivery, dual-provider GDPR parity, post-v2 rollback, 8.11 availability closure, and I2/I19a policy-hook classification approval remain missing. All 32 recorded G5 checks and retained-file/DI checks passed. Story 8.7 stays blocked; no production, dependency, submodule, frozen-block, or original-baseline change; no product-test credit. Activation must reconcile the frozen historical identity with the eventual approved G5 identity.
+
 - 2026-10-03: Revalidated the closed gate at Parties `06714c166c090200ac87373b11d8243aa11b2126`. EventStore gitlink/clean checkout `2c58ffda41759e895ace4b9625c9bd931a217672` (`v3.111.0`); Builds gitlink/checkout `688eec9a4333245cc0ff7772115c769094471863` selects packages `3.110.0`. Observed identities differ from the frozen `c21bd749154d701c3b7d68e40d1008d3475e35c4` / `3.95.0`; no new identity is adopted. Public `IPersonalDataPolicy` and `IErasureStateProvider`, plus an internal `pdenc-v2` core project (`IsPackable=false`), now exist. EventStore 8.2 is done and 8.3 in-progress; 8.4-8.11 remain backlog. The AzureKeyVault project, package catalog/release enrollment, consumable runtime provider, dual-provider parity, post-v2 rollback, named G5 availability approval, and 8.11 closure remain missing. G5 stays `needs-additive-api`; this spec and Story 8.7 stay `blocked`. Parties 8.6 is done. Static inventory passed; all 24 MOVE/KEEP/adapter files and local DI remain. No production/dependency changes; no product tests run or credited. The frozen block and original baseline remain unchanged.
 
 - 2026-09-07: Closed-gate halt. Live EventStore gitlink and checkout `d45206f7cbd80a112519c1d4687d7279a745f0c5` (`v3.103.0-2-gd45206f7`); package graph `3.103.0`. Spec frozen identity `c21bd749154d701c3b7d68e40d1008d3475e35c4` / `3.95.0` does not match live checkout; Ask First forbids adopting a new identity. G5 remains `needs-additive-api`. Missing receipts: EventStore `8-11-g5-evidence-and-approval-closure.md`; `Hexalith.EventStore.PayloadProtection` and `Hexalith.EventStore.PayloadProtection.AzureKeyVault` source projects and catalog package versions; runtime `pdenc-v2`, `IPersonalDataPolicy`, and `IErasureStateProvider`; EventStore Stories 8.2-8.11 remaining backlog; named G5 `available` approvals; dual-provider parity; post-v2 rollback; production KMS. `AddEventStoreDataProtection` and `NoOpEventPayloadProtectionService` are not the shared engine. No production, DI, or dependency changes. Local engine, adapter, DI, MOVE/KEEP files, and public APIs retained.
diff --git a/parties/_bmad-output/implementation-artifacts/sprint-status.yaml b/parties/_bmad-output/implementation-artifacts/sprint-status.yaml
index 3eb307c9..a5a78353 100644
--- a/parties/_bmad-output/implementation-artifacts/sprint-status.yaml
+++ b/parties/_bmad-output/implementation-artifacts/sprint-status.yaml
@@ -175,6 +175,11 @@ development_status:
   # parity, post-v2 rollback, and 8.11 closure/approval remain missing.
   # Parties 8.6 is done; G5 still gates 8.7. No dependency identity is adopted,
   # no production/dependency changes, and no product tests are credited.
+  # Revalidated 2026-10-05: EventStore v3.113.0 / package 3.113.0; observed
+  # Builds 90f3836dd7482db35c2c187a50999b99215919b0 also selects 3.113.0.
+  # All 32 G5 static checks passed; runtime/release, dual-provider parity,
+  # post-v2 rollback, 8.11 closure, and I2/I19a classification remain missing.
+  # All 24 retained files and local DI intact; no production/dependency edits.
   8-7-data-protection-extraction: blocked
   # Story context revalidated 2026-07-31 after the latest root-submodule update.
   # All eight checkouts match their clean root gitlinks; only Builds (b529b66) and
@@ -310,6 +315,11 @@ action_items:
     # in-progress); G5 closure/parity/rollback remain absent. The earlier 19/19
     # total is historical; no product tests rerun. All 24 retained files and
     # local DI verified intact. Story 8.6 done; 8.7 blocked; action stays open.
+    # 2026-10-05: EventStore v3.113.0 still lacks G5 runtime/release enrollment,
+    # dual-provider parity, post-v2 rollback, and 8.11 availability closure.
+    # I2/I19a classification approval also remains pending. All 24 files and
+    # local DI retained; 32 static G5 checks pass; product suites unrun.
+    # Story 8.7 remains blocked; retention action stays open.
     action: "Keep Parties crypto/key-management implementation until an approved shared provider proves payload compatibility, typed unreadable outcomes, no-leak diagnostics, exports, processing records, certificates, and rollback."
     owner: "Winston (Architect) + Amelia (Developer)"
     status: open
diff --git a/parties/_bmad-output/implementation-artifacts/story-8-3-platform-api-prerequisite-matrix.md b/parties/_bmad-output/implementation-artifacts/story-8-3-platform-api-prerequisite-matrix.md
index 3be812fb..e080307e 100644
--- a/parties/_bmad-output/implementation-artifacts/story-8-3-platform-api-prerequisite-matrix.md
+++ b/parties/_bmad-output/implementation-artifacts/story-8-3-platform-api-prerequisite-matrix.md
@@ -126,7 +126,7 @@ rerun at the stamped identity.
 | EventStore projection/query SDK | Hexalith.EventStore | available | references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainProjectionHandler.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IAsyncDomainProjectionHandler.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainQueryHandler.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelBatchStore.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelConditionalEraser.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/ReadModelWritePolicy.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Queries/IQueryCursorCodec.cs | Proof required: Story 8.6 must validate release or submodule-pin availability and the distributed consumer boundary before deleting rollback code. The formally owner-approved identity `fa2d1c9910f8976553adb33dcdb1c9ff2ea75594` (EventStore Story 1.20, `final_decision: available`, `authorize_consumer_migration: true`) failed the required source-mode projection build with CS0246: it does not expose `IAsyncDomainSharedProjectionRebuildHandler`, `DomainSharedProjectionRebuildIdentity`, or `DomainSharedProjectionRebuildCandidate`, which the Parties tenant-shared index handler requires. Story 8.6 completed its projection/query SDK migration on 2026-08-01 against EventStore v3.89.0 (`c590590bc581a3f72ef6e67148eda988ba4b8fe6`), which does expose the required surfaces and closes the routed Fable-platform gaps G3 (read-model erasure hooks), G6 (freshness mapping), and G10 (index batching), under the Administrator's explicit direct authorization to use the latest EventStore release after the `fa2d1c99` compile failure. This authorization is recorded in `sprint-change-proposal-2026-08-02-story-8-6-eventstore-identity-authorization-backfill.md`, approved 2026-08-02. All 18 previously-governed rollback artifacts and all 10 `catch (NotImplementedException)` fallbacks are removed; full regression passed 452/452 at `c590590b`. The current root gitlink has since moved to `4bcf2484a09eb26490cb2d32ceb6df8949f90cc6` (Story 8.7's independent G5 revalidation) — Story 8.6's regression evidence is not yet re-confirmed at that later pin; re-run the focused projection/query suite before treating `4bcf2484` as validated for Story 8.6's purposes. Story 8.6 selected EventStore `v3.91.0` (`1d6e9321acfc416768c1c78e9facf573c9c41f71`) under the Administrator's recorded direct authorization; `sprint-change-proposal-2026-08-04.md` (approved) formalizes that identity. The matching Builds catalog identity is `824d7ef100455423aabbcd399c8364074000b2e0` and selects `HexalithEventStoreVersion=3.91.0`. A current package-mode restore and Release projection build completed with zero warnings and zero errors on 2026-08-04. | 8.6, 8.10 | No Parties source migration starts in Story 8.3. Keep rollback path was this row's original gate decision; Story 8.6 has since satisfied this row's own consumer-evidence gates (parity, GDPR reads, rebuild-vs-replay) and deleted the rollback path under Administrator authorization at `c590590b`, recorded in the linked SCP. That identity-and-parity evidence is narrow and does not mean Story 8.6 overall is complete: `sprint-status.yaml` and `8-6-projection-and-query-sdk-migration.md` record Story 8.6 as `in-progress` with material open review findings (diagnostic-logging, corrupt-vs-redacted observability, rebuild/live-write concurrency, remaining parity-test gaps) unrelated to this row. Open follow-up (not blocking): re-run Story 8.6's focused projection/query regression at the current EventStore pin before relying on it as re-validated evidence. | `git ls-tree 03ab938c637aa15f7a0af402afc8664dfc54d1a4 references/Hexalith.EventStore`; `rg -n -F 'IDomainProjectionHandler' references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainProjectionHandler.cs`; `rg -n -F 'IAsyncDomainProjectionHandler' references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IAsyncDomainProjectionHandler.cs`; `rg -n -F 'IDomainQueryHandler' references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainQueryHandler.cs`; `rg -n -F 'IReadModelStore' references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs`; `rg -n -F 'IReadModelBatchStore' references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelBatchStore.cs`; `rg -n -F 'IReadModelConditionalEraser' references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelConditionalEraser.cs`; `rg -n -F 'IReadModelFreshness' references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelFreshness.cs`; `rg -n -F 'IQueryCursorCodec' references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Queries/IQueryCursorCodec.cs`; `rg -n -F 'project/v2' references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Projections/NamedProjectionDispatchCoordinator.cs`; `rg -n -F 'MethodName = "query"' references/Hexalith.EventStore/src/Hexalith.EventStore/Queries/DaprDomainQueryInvoker.cs`; `rg -n -F 'name: /process' src/Hexalith.Parties.AppHost/DaprComponents/accesscontrol.parties.yaml`; `git ls-tree <2026-08-04-HEAD> references/Hexalith.EventStore` -> `160000 commit 1d6e9321acfc416768c1c78e9facf573c9c41f71 references/Hexalith.EventStore`; `git -C references/Hexalith.EventStore describe --tags --exact-match <2026-08-04-HEAD>` -> `v3.91.0`; `git ls-tree <2026-08-04-HEAD> references/Hexalith.Builds` -> `160000 commit 824d7ef100455423aabbcd399c8364074000b2e0 references/Hexalith.Builds`; `rg -n -F 'HexalithEventStoreVersion' references/Hexalith.Builds/Props/Directory.Packages.props`; `dotnet restore src/Hexalith.Parties.Projections/Hexalith.Parties.Projections.csproj -p:UseHexalithProjectReferences=false -p:HexalithEventStoreFromSource=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0`; `dotnet build src/Hexalith.Parties.Projections/Hexalith.Parties.Projections.csproj --configuration Release --no-restore -nr:false -m:1 -p:UseHexalithProjectReferences=false -p:HexalithEventStoreFromSource=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -> 0 warnings, 0 errors. |
 | EventStore DataProtection | Hexalith.EventStore | available | references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDataProtectionServiceCollectionExtensions.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DaprXmlRepository.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Registration/QueryCursorCodecServiceCollectionExtensions.cs | Proof required: Story 8.6 must validate release or submodule-pin availability, cursor purpose/scope compatibility, DAPR key-ring persistence, and rollback. The formally owner-approved identity `fa2d1c9910f8976553adb33dcdb1c9ff2ea75594` (`v3.82.0-24-gfa2d1c99`) verified the three cited surfaces but failed the required tenant-shared rebuild build (see the projection/query SDK row above). Consumption availability for these three surfaces was revalidated at exact approved source `fa2d1c9910f8976553adb33dcdb1c9ff2ea75594` on 2026-08-01. Story 8.6 adopted the cursor and DataProtection surfaces at EventStore v3.89.0 (`c590590bc581a3f72ef6e67148eda988ba4b8fe6`) on 2026-08-01, under the same Administrator authorization recorded in `sprint-change-proposal-2026-08-02-story-8-6-eventstore-identity-authorization-backfill.md`. Cursor purpose/scope compatibility and persisted DAPR key-ring behavior passed as part of Story 8.6's 452/452 regression at `c590590b`. The current root gitlink has since moved to `4bcf2484a09eb26490cb2d32ceb6df8949f90cc6` (Story 8.7's independent G5 revalidation); the same current-pin re-validation caveat from the row above applies here. Story 8.6 selected EventStore `v3.91.0` (`1d6e9321acfc416768c1c78e9facf573c9c41f71`) under the Administrator's recorded direct authorization; `sprint-change-proposal-2026-08-04.md` (approved) formalizes that identity. The matching Builds catalog identity is `824d7ef100455423aabbcd399c8364074000b2e0` and selects `HexalithEventStoreVersion=3.91.0`. A current package-mode restore and Release projection build completed with zero warnings and zero errors on 2026-08-04. | 8.6, 8.10 | No Parties source migration starts in Story 8.3. Keep rollback path was this row's original gate decision; Story 8.6 adopted these surfaces and deleted the rollback path under the same Administrator authorization at `c590590b`. That identity-and-parity evidence is narrow and does not mean Story 8.6 overall is complete: `sprint-status.yaml` and `8-6-projection-and-query-sdk-migration.md` record Story 8.6 as `in-progress` with material open review findings unrelated to this row. Same open follow-up as the row above: re-run at the current pin before relying on it as re-validated. | `git ls-tree 03ab938c637aa15f7a0af402afc8664dfc54d1a4 references/Hexalith.EventStore`; `rg -n -F 'AddEventStoreDataProtection' references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDataProtectionServiceCollectionExtensions.cs`; `rg -n -F 'DaprXmlRepository' references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DaprXmlRepository.cs`; `rg -n -F 'AddEventStoreQueryCursorCodec' references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Registration/QueryCursorCodecServiceCollectionExtensions.cs`; `git ls-tree <2026-08-04-HEAD> references/Hexalith.EventStore` -> `160000 commit 1d6e9321acfc416768c1c78e9facf573c9c41f71 references/Hexalith.EventStore`; `git -C references/Hexalith.EventStore describe --tags --exact-match <2026-08-04-HEAD>` -> `v3.91.0`; `git ls-tree <2026-08-04-HEAD> references/Hexalith.Builds` -> `160000 commit 824d7ef100455423aabbcd399c8364074000b2e0 references/Hexalith.Builds`; `rg -n -F 'HexalithEventStoreVersion' references/Hexalith.Builds/Props/Directory.Packages.props`. |
 | EventStore degraded response and DAPR health checks | Hexalith.EventStore | needs-additive-api | references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs; src/Hexalith.Parties/Middleware/DegradedResponseMiddleware.cs; src/Hexalith.Parties/HealthChecks/PartiesHealthCheckExtensions.cs; src/Hexalith.Parties/HealthChecks/DaprStateStoreHealthCheck.cs; src/Hexalith.Parties/HealthChecks/DaprPubSubHealthCheck.cs | Proof required: additive EventStore degraded-response middleware and `AddEventStoreDaprHealthChecks` option parity for G1 and G2, including status-header behavior and tag policy. | 8.5, 8.8, 8.10 | No Parties source migration starts in Story 8.3. Keep local rollback path: Parties degraded-response middleware and DAPR health checks stay until owner APIs prove parity. | `rg -n -F 'UseEventStoreDomainService' references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs`; `rg -n -F 'DegradedResponseMiddleware' src/Hexalith.Parties/Middleware/DegradedResponseMiddleware.cs`; `rg -n -F 'DaprStateStoreHealthCheck' src/Hexalith.Parties/HealthChecks/DaprStateStoreHealthCheck.cs`; `rg -n -F 'DaprPubSubHealthCheck' src/Hexalith.Parties/HealthChecks/DaprPubSubHealthCheck.cs`; explicit missing-surface note: no EventStore `AddEventStoreDaprHealthChecks` or shared degraded-response API was approved for G1/G2. |
-| Payload protection engine package | Hexalith.EventStore | needs-additive-api | references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IEventPayloadProtectionService.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/NoOpEventPayloadProtectionService.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Configuration/ServiceCollectionExtensions.cs; references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md; references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml; references/Hexalith.EventStore/_bmad-output/planning-artifacts/story-id-migration-2026-08-01.md; _bmad-output/implementation-artifacts/sprint-status.yaml; src/Hexalith.Parties.Security/PartyPayloadProtectionService.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IPersonalDataPolicy.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IErasureStateProvider.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.PayloadProtection/Hexalith.EventStore.PayloadProtection.csproj | Proof required: G5 additive shared payload-protection engine package, with PersonalDataAttribute, key storage/wrapping/rotation, audit/retry/circuit breaker, typed unreadable outcomes, production backend, AAD binding in pdenc-v2, json+pdenc-v1 reads, stable formats/state keys/actor names/metrics, and golden compatibility harnesses. IPersonalDataPolicy and IErasureStateProvider are present read-only Parties policy hooks pending I19a/I20 classification; their presence does not authorize engine adoption. Historical 2026-08-04 receipt: EventStore 1d6e9321acfc416768c1c78e9facf573c9c41f71 / v3.91.0 and Builds 824d7ef100455423aabbcd399c8364074000b2e0. Historical 2026-09-07 receipt: EventStore d45206f7cbd80a112519c1d4687d7279a745f0c5 / package 3.103.0 and Builds 7b0b1837ce368e314b5e11b011b73603637a17e1; these are dated inspection identities, not the retained set above. Current 2026-10-03 inspection (section below) supersedes absence claims: public policy/erasure contracts and the non-packable internal pdenc-v2 core exist; owner 8.2 is done, 8.3 in-progress, and 8.4-8.11 remain backlog. Missing: AzureKeyVault project, catalog/release enrollment, approved runtime/provider registration, dual-provider parity, post-v2 rollback, and Story 8.11 approval-closure packet. Story 8.11 alone may record G5 available and unblock Parties Story 8.7. G5 remains needs-additive-api, Parties 8.7 remains blocked, retention action stays open, and no crypto/key-management deletion is authorized. The 2026-10-04 current graph is unvalidated; identity selection is not G5 approval. | 8.7, 8.10 | No Parties source migration starts in Story 8.3. Keep local rollback path: `Hexalith.Parties.Security` and Parties GDPR policy stay intact until Story 8.7 proves compatibility and rollback. | `rg -n -F 'IEventPayloadProtectionService' references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IEventPayloadProtectionService.cs`; `rg -n -F 'NoOpEventPayloadProtectionService' references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/NoOpEventPayloadProtectionService.cs`; `rg -n -F 'TryAddSingleton<IEventPayloadProtectionService, NoOpEventPayloadProtectionService>' references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Configuration/ServiceCollectionExtensions.cs`; `rg -n -F 'IPersonalDataPolicy' references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IPersonalDataPolicy.cs`; `rg -n -F 'IErasureStateProvider' references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IErasureStateProvider.cs`; `rg -n -F 'status: approved-authorized' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md`; `rg -n -F 'decision: adopted-amendment' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md`; `rg -n -F 'story_8_2_authorized: true' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md`; `rg -n -F 'Hexalith.EventStore.PayloadProtection' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md`; `rg -n -F 'Hexalith.EventStore.PayloadProtection.AzureKeyVault' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md`; `rg -n -F 'pdenc-v2' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md`; `rg -n -F 'json+pdenc-v1' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md`; `rg -n -F 'IPersonalDataPolicy' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md`; `rg -n -F 'IErasureStateProvider' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md`; `rg -n -F '8-1-shared-payload-protection-security-spec-and-adr: done' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-2-payload-protection-contracts-and-golden-vectors: done' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-3-pdenc-v2-core-cryptographic-engine: in-progress' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-4-compatibility-readers-and-mixed-history-routing: backlog' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-5-policy-and-key-lifecycle-mechanics: backlog' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-6-azure-key-vault-production-adapter-conformance: backlog' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-7-server-persistence-and-snapshot-integration: backlog' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-8-package-and-release-integration: backlog' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-9-parties-dual-provider-parity: backlog' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-10-post-v2-write-rollback-rehearsal: backlog' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-11-g5-evidence-and-approval-closure: backlog' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F 'only this story can record' references/Hexalith.EventStore/_bmad-output/planning-artifacts/story-id-migration-2026-08-01.md`; `test ! -f references/Hexalith.EventStore/_bmad-output/implementation-artifacts/8-11-g5-evidence-and-approval-closure.md`; `test ! -f references/Hexalith.EventStore/src/Hexalith.EventStore.PayloadProtection.AzureKeyVault/Hexalith.EventStore.PayloadProtection.AzureKeyVault.csproj`; `rg -n -F 'Hexalith.EventStore.PayloadProtection' references/Hexalith.Builds/Props/Directory.Packages.props` (expected no matches); `rg -n -F 'Hexalith.EventStore.PayloadProtection' references/Hexalith.EventStore/tools/release-packages.json` (expected no matches); `rg -n -F 'PartyPayloadProtectionService' src/Hexalith.Parties.Security/PartyPayloadProtectionService.cs`; `rg -n -F 'EventStorePartyPayloadProtectionAdapter' src/Hexalith.Parties.Security/EventStorePartyPayloadProtectionAdapter.cs`; All 18 MOVE files, 5 KEEP files, and CryptoKeyManagementCompatibilityHarnessTests remain; historical 19/19 compatibility receipt is unvalidated at the refreshed graph. |
+| Payload protection engine package | Hexalith.EventStore | needs-additive-api | references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IEventPayloadProtectionService.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/NoOpEventPayloadProtectionService.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Configuration/ServiceCollectionExtensions.cs; references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md; references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml; references/Hexalith.EventStore/_bmad-output/planning-artifacts/story-id-migration-2026-08-01.md; _bmad-output/implementation-artifacts/sprint-status.yaml; src/Hexalith.Parties.Security/PartyPayloadProtectionService.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IPersonalDataPolicy.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IErasureStateProvider.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.PayloadProtection/Hexalith.EventStore.PayloadProtection.csproj | Proof required: G5 additive shared payload-protection engine package, with PersonalDataAttribute, key storage/wrapping/rotation, audit/retry/circuit breaker, typed unreadable outcomes, production backend, AAD binding in pdenc-v2, json+pdenc-v1 reads, stable formats/state keys/actor names/metrics, and golden compatibility harnesses. IPersonalDataPolicy and IErasureStateProvider are present read-only Parties policy hooks pending I19a/I20 classification; their presence does not authorize engine adoption. Historical 2026-08-04 receipt: EventStore 1d6e9321acfc416768c1c78e9facf573c9c41f71 / v3.91.0 and Builds 824d7ef100455423aabbcd399c8364074000b2e0. Historical 2026-09-07 receipt: EventStore d45206f7cbd80a112519c1d4687d7279a745f0c5 / package 3.103.0 and Builds 7b0b1837ce368e314b5e11b011b73603637a17e1; these are dated inspection identities, not the retained set above. Historical 2026-10-03 inspection superseded absence claims: public policy/erasure contracts and the non-packable internal pdenc-v2 core exist; owner 8.2 is done, 8.3 in-progress, and 8.4-8.11 remain backlog. Missing: AzureKeyVault project, catalog/release enrollment, approved runtime/provider registration, dual-provider parity, post-v2 rollback, and Story 8.11 approval-closure packet. Story 8.11 alone may record G5 available and unblock Parties Story 8.7. G5 remains needs-additive-api, Parties 8.7 remains blocked, retention action stays open, and no crypto/key-management deletion is authorized. Current 2026-10-05 inspection below confirms the same closed gate at EventStore v3.113.0 / package 3.113.0 and observed Builds 90f3836dd7482db35c2c187a50999b99215919b0. I2/I19a policy-hook classification approval remains pending. Identity selection is not G5 approval. | 8.7, 8.10 | No Parties source migration starts in Story 8.3. Keep local rollback path: `Hexalith.Parties.Security` and Parties GDPR policy stay intact until Story 8.7 proves compatibility and rollback. | `rg -n -F 'IEventPayloadProtectionService' references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IEventPayloadProtectionService.cs`; `rg -n -F 'NoOpEventPayloadProtectionService' references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/NoOpEventPayloadProtectionService.cs`; `rg -n -F 'TryAddSingleton<IEventPayloadProtectionService, NoOpEventPayloadProtectionService>' references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Configuration/ServiceCollectionExtensions.cs`; `rg -n -F 'IPersonalDataPolicy' references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IPersonalDataPolicy.cs`; `rg -n -F 'IErasureStateProvider' references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IErasureStateProvider.cs`; `rg -n -F 'status: approved-authorized' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md`; `rg -n -F 'decision: adopted-amendment' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md`; `rg -n -F 'story_8_2_authorized: true' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md`; `rg -n -F 'Hexalith.EventStore.PayloadProtection' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md`; `rg -n -F 'Hexalith.EventStore.PayloadProtection.AzureKeyVault' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md`; `rg -n -F 'pdenc-v2' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md`; `rg -n -F 'json+pdenc-v1' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md`; `rg -n -F 'IPersonalDataPolicy' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md`; `rg -n -F 'IErasureStateProvider' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-shared-payload-protection-engine.md`; `rg -n -F '8-1-shared-payload-protection-security-spec-and-adr: done' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-2-payload-protection-contracts-and-golden-vectors: done' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-3-pdenc-v2-core-cryptographic-engine: in-progress' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-4-compatibility-readers-and-mixed-history-routing: backlog' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-5-policy-and-key-lifecycle-mechanics: backlog' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-6-azure-key-vault-production-adapter-conformance: backlog' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-7-server-persistence-and-snapshot-integration: backlog' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-8-package-and-release-integration: backlog' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-9-parties-dual-provider-parity: backlog' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-10-post-v2-write-rollback-rehearsal: backlog' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F '8-11-g5-evidence-and-approval-closure: backlog' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml`; `rg -n -F 'only this story can record' references/Hexalith.EventStore/_bmad-output/planning-artifacts/story-id-migration-2026-08-01.md`; `test ! -f references/Hexalith.EventStore/_bmad-output/implementation-artifacts/8-11-g5-evidence-and-approval-closure.md`; `test ! -f references/Hexalith.EventStore/src/Hexalith.EventStore.PayloadProtection.AzureKeyVault/Hexalith.EventStore.PayloadProtection.AzureKeyVault.csproj`; `rg -n -F 'Hexalith.EventStore.PayloadProtection' references/Hexalith.Builds/Props/Directory.Packages.props` (expected no matches); `rg -n -F 'Hexalith.EventStore.PayloadProtection' references/Hexalith.EventStore/tools/release-packages.json` (expected no matches); `rg -n -F 'PartyPayloadProtectionService' src/Hexalith.Parties.Security/PartyPayloadProtectionService.cs`; `rg -n -F 'EventStorePartyPayloadProtectionAdapter' src/Hexalith.Parties.Security/EventStorePartyPayloadProtectionAdapter.cs`; All 18 MOVE files, 5 KEEP files, and CryptoKeyManagementCompatibilityHarnessTests remain; historical 19/19 compatibility receipt is unvalidated at the refreshed graph. |
 | EventStore client envelopes/freshness/error codes | Hexalith.EventStore | needs-additive-api | references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Gateway/IEventStoreGatewayClient.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/SubmitCommandRequest.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/QueryResponseMetadata.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/QueryProblemReasonCodes.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Problems/GatewayProblemDetailsExtensions.cs | Proof required: additive or approved adapter proof for G6 Parties `Current`, `Stale`, `Rebuilding`, `Degraded`, `Unavailable`, and `LocalOnly` freshness semantics, warning codes, ProblemDetails reason mapping, and typed command/query client outcome compatibility. | 8.6, 8.8, 8.9, 8.10 | No Parties source migration starts in Story 8.3. Keep local rollback path: Parties client envelopes, paging/freshness models, and bounded outcome mapping stay until Story 8.8 proves compatibility. | `rg -n -F 'IEventStoreGatewayClient' references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Gateway/IEventStoreGatewayClient.cs`; `rg -n -F 'QueryResponseMetadata' references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/QueryResponseMetadata.cs`; `rg -n -F 'QueryProblemReasonCodes' references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/QueryProblemReasonCodes.cs`; `rg -n -F 'GatewayProblemDetailsExtensions' references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Problems/GatewayProblemDetailsExtensions.cs`. |
 | Tenant claims transformation | Hexalith.EventStore and Hexalith.Commons | needs-additive-api | references/Hexalith.EventStore/src/Hexalith.EventStore/Authentication/EventStoreClaimsTransformation.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Identity/AggregateIdentity.cs; references/Hexalith.Commons/src/libraries/Hexalith.Commons.UniqueIds/UniqueIdHelper.cs; references/Hexalith.Tenants/src/Hexalith.Tenants/Program.cs; src/Hexalith.Parties.Authentication/PartiesClaimsTransformation.cs | Proof required: G7/G9 ownership was confirmed on 2026-07-16 in `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-g7-g9-tenant-claims-ownership.md`: EventStore.Contracts owns the public `eventstore:tenant` constant and `AggregateIdentity.IsValid(string)`; a lightweight `Hexalith.EventStore.Authentication` package owns the reusable `EventStoreTenantClaimsTransformation`; Commons owns `UniqueIdHelper.IsValidUlid(string)`. Delivery proof remains required: named owner acceptance of the exact APIs, released packages or approved root-submodule pins, package/API inventory, producer-consumer parity, and rollback evidence. | 8.4, 8.8, 8.10 | No Parties source migration starts in Story 8.3. Keep the `Hexalith.Parties.Authentication` rollback path; it remains after Story 8.4 until the approved APIs are delivered or pinned and Parties parity plus rollback pass. Ownership approval alone does not authorize deletion. | Approved G7/G9 decision: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-g7-g9-tenant-claims-ownership.md`; `rg -n -F 'TenantClaimType = "eventstore:tenant"' references/Hexalith.EventStore/src/Hexalith.EventStore/Authentication/EventStoreClaimsTransformation.cs`; `rg -n -F 'AggregateIdentity' references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Identity/AggregateIdentity.cs`; `rg -n -F 'CrockfordBase32Pattern' references/Hexalith.Commons/src/libraries/Hexalith.Commons.UniqueIds/UniqueIdHelper.cs`; additive `AggregateIdentity.IsValid(string)` and `UniqueIdHelper.IsValidUlid(string)` remain undelivered. |
 | Aspire publish helpers | Hexalith.EventStore.Aspire and Hexalith.EventStore and Hexalith.FrontComposer and platform AppHost owners | needs-additive-api | references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreDomainModuleExtensions.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreSecurityExtensions.cs; references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Registration/EventStoreServiceCollectionExtensions.cs; references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.AppHost/Program.cs; src/Hexalith.Parties.AppHost/Program.cs; src/Hexalith.Parties.Client/Extensions/PartiesClientServiceCollectionExtensions.cs | Proof required: G8 owner proof requires three accepted work packages: (A) EventStore.Aspire supplies `WithEventStoreJwtAuthentication(audience)` or owner-approved documentation/tests proving `WithJwtBearerSecurity(..., audience)` is the canonical replacement across local run and publish/external-orchestrator settings, including authority, issuer, workload plus EventStore audience relationships, HTTPS metadata, signing-key clearing, and no-secret manifest output; (B) EventStore.Client supplies independently selectable command/query/GDPR transport registration with deterministic handler and repeat-registration behavior that preserves Parties and FrontComposer module-typed clients; (C) FrontComposer.AppHost or an explicitly approved platform AppHost owns the integrated EventStore/Tenants/Parties local topology, while external platform operations retains runtime deployment. Story 8.8 must record named approval, exact release or root-submodule identity, producer tests, Parties consumer topology/client-coexistence evidence, source/package expectations, and tested rollback before deleting local wiring. | 8.5, 8.8, 8.10 | No Parties source migration starts in Story 8.3. Keep rollback path: the current Parties AppHost and its typed-client registrations remain until Packages A-C and consumer parity are accepted; then retire the domain-owned AppHost rather than retain a permanent thin host. | `rg -n -F 'AddEventStoreDomainModule' references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreDomainModuleExtensions.cs`; `rg -n -F 'WithJwtBearerSecurity' references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreSecurityExtensions.cs`; `rg -n -F 'AddEventStoreGatewayClient' references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Registration/EventStoreServiceCollectionExtensions.cs`; `rg -n -F 'AddPartiesClient' src/Hexalith.Parties.Client/Extensions/PartiesClientServiceCollectionExtensions.cs`; `rg -n -F 'WithJwtAuthentication' src/Hexalith.Parties.AppHost/Program.cs`; explicit missing-surface note: current EventStore.Aspire does not expose `WithEventStoreJwtAuthentication(audience)`, current EventStore registration exposes the generic gateway client but not granular module-typed registration, current Parties publish-mode security is not yet covered by owner-approved replacement evidence, and FrontComposer.AppHost already composes EventStore, Tenants, and Parties. Routed by `sprint-change-proposal-2026-07-16-g8-aspire-publish-helper-routing.md`; routing is not delivery and status remains `needs-additive-api`. |
@@ -165,7 +165,33 @@ The original selection row transcribes the Round 6 reviewer-recorded decision in
 | I19 store reclassification / class-(b) test set | Touched stores, semantics, and approved consistency tests | Pending — named accountable owner | Pending | Pending; no approval inferred from routing, source presence, or compile. |
 | §2 divergence from Epic 7 | G7/G9 historical SCP and 2026-09-27 McpCli course correction: authority backfill required | Pending — named accountable owner | Pending | Pending; no approval inferred from routing, source presence, or compile. |
 
-### Story 8.7 G5 gate revalidation — 2026-10-03
+### Story 8.7 G5 gate revalidation — 2026-10-05
+
+At Parties `b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca`, EventStore's root gitlink and clean checkout
+match `865cd9e49273dffbb1cdae85efeaf1aac322e09e` (`v3.113.0`). Builds' root gitlink and clean
+checkout match `90f3836dd7482db35c2c187a50999b99215919b0` (`v4.29.1-21-g90f3836`). Both the current
+catalog and Parties pre-import pin select EventStore `3.113.0`. The I20 table
+already approves EventStore's package/source selection; its dated Builds approval
+still selects `360a2b9c4e96809365a7de785be9a68152d5ac28`. The later Builds identity
+is observed only. This receipt changes no approval or consumption identity and
+does not rewrite the 2026-10-05 Story 8.10 reconciliation above.
+
+| Gate evidence | Current inspection | Disposition |
+| --- | --- | --- |
+| Owner contracts/core | Public policy/erasure hooks and internal v2 core exist; core is `IsPackable=false`. Owner 8.2 is `done`, 8.3 is `in-progress`. Existing owner contract/spec approvals are recorded. | Partial delivery; no approved consumable provider. |
+| Compatibility, lifecycle, backend, runtime, release | Owner 8.4-8.8 remain `backlog`; AzureKeyVault project absent; neither payload package is catalog/release-enrolled; server defaults to `NoOpEventPayloadProtectionService`. | Missing runtime/release receipts. |
+| Consumer parity and rollback | Owner 8.9/8.10 remain `backlog`; dual-provider GDPR/parity and post-v2 switch-back are unproven. | No adoption or deletion proof. |
+| Availability and policy ownership | Owner 8.11 remains `backlog`; closure packet absent; G5 availability and I2/I19a hook-classification approvals are missing. | G5 remains `needs-additive-api`; 8.7 remains `blocked`. |
+| Parties continuity | 8.6 is `done`; all 24 MOVE/KEEP/adapter files, local harness, and local DI remain. | Crypto-retention action stays `open`; accepted Epic closure deferrals do not complete 8.7. |
+
+All 32 recorded G5 static validation commands passed, as did retained-file/DI and
+matching-clean-identity assertions. Exact commands/results are in
+`tests/test-summary.md`. No product tests, dual-provider/GDPR parity, or post-v2
+rollback were run or credited. Production KMS remains a separate release gate.
+The frozen 8.7 identity is historical and requires reconciliation at activation;
+no production, dependency, submodule, frozen-spec, or approval-table edit occurred.
+
+### Historical Story 8.7 G5 gate revalidation — 2026-10-03
 
 At Parties baseline `06714c166c090200ac87373b11d8243aa11b2126`, EventStore's root gitlink and
 clean checkout both equal `2c58ffda41759e895ace4b9625c9bd931a217672` (`v3.111.0`). Builds' root
diff --git a/parties/_bmad-output/implementation-artifacts/tests/test-summary.md b/parties/_bmad-output/implementation-artifacts/tests/test-summary.md
index cb2c12e0..ddca138e 100644
--- a/parties/_bmad-output/implementation-artifacts/tests/test-summary.md
+++ b/parties/_bmad-output/implementation-artifacts/tests/test-summary.md
@@ -1428,3 +1428,72 @@ plus the full uncommitted diff (final identity set unchanged):
 The two producer receipts keep their 2026-10-05 reruns: PolymorphicSerializations
 `98de6e01` and FrontComposer `2cc8dd3a` are unchanged. DW-143, DW-144, and DW-145
 are open follow-ups; DW-111 (I13) stays an accepted deferral.
+
+## Story 8.7 G5 revalidation — closed-gate halt — 2026-10-05
+
+At Parties `b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca`, G5 remains
+`needs-additive-api`; Story 8.7 remains `blocked`. This receipt supersedes the
+2026-10-03 inspection for this story. It does not alter Story 8.10 closure,
+accepted deferrals, the I20 approval table, or the approved/frozen spec block.
+
+| Identity | Observation |
+| --- | --- |
+| EventStore root gitlink and clean checkout | `865cd9e49273dffbb1cdae85efeaf1aac322e09e`, `v3.113.0`, matching; the existing I20 identity selection does not approve G5. |
+| Builds root gitlink and clean checkout | `90f3836dd7482db35c2c187a50999b99215919b0`, `v4.29.1-21-g90f3836`, matching; both catalog and Parties pre-import pin select `3.113.0`. The later Builds identity is observed only; the dated I20 approved selection remains `360a2b9c4e96809365a7de785be9a68152d5ac28`. |
+| Frozen Story 8.7 identity | `c21bd749154d701c3b7d68e40d1008d3475e35c4` / `3.95.0`, unchanged; reconcile with the eventual approved G5 identity before activation. |
+
+| Inspection command | Result |
+| --- | --- |
+| `git rev-parse HEAD`; `git ls-tree HEAD references/Hexalith.EventStore references/Hexalith.Builds` | Parties baseline and matching root identities above. |
+| `git -C references/Hexalith.EventStore rev-parse HEAD`; `git -C references/Hexalith.EventStore status --short --branch`; `git -C references/Hexalith.EventStore describe --tags --always` | Matching EventStore identity, clean detached checkout, `v3.113.0`. |
+| `git -C references/Hexalith.Builds rev-parse HEAD`; `git -C references/Hexalith.Builds status --short --branch`; `git -C references/Hexalith.Builds describe --tags --always` | Matching Builds identity, clean checkout, `v4.29.1-21-g90f3836`. |
+| `rg -n 'HexalithEventStoreVersion\|PayloadProtection' Directory.Packages.props references/Hexalith.Builds/Props/Directory.Packages.props references/Hexalith.EventStore/tools/release-packages.json` | Both catalogs select `3.113.0`; no payload package enrollment. |
+| `cat references/Hexalith.EventStore/src/Hexalith.EventStore.PayloadProtection/Hexalith.EventStore.PayloadProtection.csproj` | Internal core exists with `IsPackable=false`; no consumable runtime provider. |
+| `rg -n 'PayloadProtection\|AzureKeyVault' references/Hexalith.EventStore/Hexalith.EventStore.slnx references/Hexalith.EventStore/tools/release-packages.json references/Hexalith.Builds/Props/Directory.Packages.props` | Exit 1, no payload project/package enrollment in these three files; expected absent delivery. |
+| Recorded G5 commands executed by the Python procedure below | 32/32 passed: public contracts/spec markers/statuses exist, no-op registration remains, closure/backend paths and catalog/release enrollment remain absent. |
+| `rg -n 'PayloadProtection\|IKeyStorageBackend' src/Hexalith.Parties/Extensions/PartiesServiceCollectionExtensions.cs` and Python retained-file/DI assertions | All 24 retained files, local harness, local backend, payload service, adapter, and factory registration remain intact. |
+
+Reproduce the 32 recorded G5 checks directly from the matrix without treating a
+static pass as runtime/provider parity:
+
+```bash
+python3 - <<'PY_CHECK'
+from pathlib import Path
+import re
+import subprocess
+matrix = Path('_bmad-output/implementation-artifacts/story-8-3-platform-api-prerequisite-matrix.md').read_text()
+row = next(line for line in matrix.splitlines() if line.startswith('| Payload protection engine package |'))
+assert row.split('|')[3].strip() == 'needs-additive-api'
+commands = re.findall(r'`(rg -n -F [^`]+|test ! -f [^`]+)`(\s*\(expected no matches\))?', row)
+for command, absent in commands:
+    result = subprocess.run(command, shell=True, capture_output=True, text=True)
+    assert result.returncode == (1 if absent else 0), (command, result.returncode, result.stderr)
+assert len(commands) == 32
+print('PASS: 32/32 recorded G5 static checks')
+PY_CHECK
+```
+
+Owner 8.2 is `done`; 8.3 is `in-progress`; 8.4-8.11 remain `backlog`.
+Public policy/erasure contracts and internal v2 core are partial delivery.
+Existing owner contract and requirements approvals do exist; missing receipts are
+compatibility/lifecycle/backend/runtime/release completion, dual-provider GDPR
+parity, post-v2 rollback, and the final 8.11 G5 availability approval/closure.
+The I2/I19a policy-hook classification approval is pending; Parties remains the
+policy writer. Production KMS is a separate release gate. Parties 8.6 is done;
+accepted Epic closure deferrals do not mark 8.7 complete. The crypto-retention
+action stays `open`.
+
+No production, DI, dependency, submodule, public-API, or frozen-spec changes were
+made. Product/unit/topology/dual-provider/GDPR/post-v2 suites were not run or
+credited while the start gate is closed. Documentation validation checks the
+unchanged frozen spec and original baseline, unchanged sprint YAML data, valid
+regenerated context, canonical revision fields, and exact six-file scope.
+
+Documentation checks passed: frozen spec block/original baseline byte-identical,
+sprint YAML data unchanged, all new revision fields canonical, regenerated
+context valid, and the story File List matches the six changed documentation
+artifacts. The recorded G5 reproduction command also passed 32/32 after the edits.
+`git diff --check` reports CRLF line terminators as trailing whitespace in the
+context and already-CRLF sprint file. `git -c core.whitespace=cr-at-eol diff --check`
+passes with the repository's `.editorconfig` CRLF convention; no whitespace or
+build policy file was changed.
diff --git a/parties/_bmad-output/planning-artifacts/adr-consumer-party-id-binding.md b/parties/_bmad-output/planning-artifacts/adr-consumer-party-id-binding.md
index c3575814..49cf234a 100644
--- a/parties/_bmad-output/planning-artifacts/adr-consumer-party-id-binding.md
+++ b/parties/_bmad-output/planning-artifacts/adr-consumer-party-id-binding.md
@@ -196,6 +196,8 @@ Consumer login aliases remain private UI/BFF issuer/subject → existing PartyId
 
 Trusted identity services submit explicit tenant/Party/actor commands with independently verified operator provenance through the shared gateway. Binding writes require a finite configured policy and independent purpose custody; no default production policy or custody provider is installed. The current development policy vocabulary supports only `binding-effective-at`; the recommended closure/revocation/erasure trigger remains a pending policy and custody design decision and is denied until implemented.
 
-Live qualification remains blocked by missing production trust/policy/custody and the shared SDK's lack of a purpose-scoped retained-history source read after profile erasure. Generic full replay continues to deny unreadable protected profile payloads. Local plaintext replay and synthetic custody tests demonstrate contracts and denial behavior only; they do not qualify erased-profile historical availability or backup/restore irreversibility. Serialized snapshots carrying attribution are denied when the typed personal-data protection graph is unavailable.
+The user accepted the minimal engineering approach on 2026-10-06: use [one narrow actor-history policy](actor-history-retention-policy-v1.md), the existing Party aggregate and shared EventStore source/custody contracts. Binding reads now require explicit matching policy and deny configuration changes during the read. The v1 proposal keeps the supported effective-at clock; its finite duration and post-profile-erasure retention approval remain owner inputs. Organization Branch B does not depend on a human retention policy.
+
+Live qualification remains blocked by missing production trust/policy/custody and exact-target retained-history qualification. The shared SDK now has a purpose-scoped retained-history transport/source seam, verified locally; this does not establish a production provider. Generic full replay continues to deny unreadable protected profile payloads. Local plaintext replay and synthetic custody tests demonstrate contracts and denial behavior only; they do not qualify erased-profile historical availability or backup/restore irreversibility. Serialized snapshots carrying attribution are denied when the typed personal-data protection graph is unavailable.
 
 Agent mapping v1 hashes UTF-8 `hexalith-agent-party-v1\0` + canonical tenant + `\0` + canonical Agent ULID with SHA-256, copies the first ten digest bytes after six zero timestamp bytes, and encodes the resulting 128 bits as a canonical Crockford ULID. For Agent `01HX0000000000000000000001`, canonical vectors are tenant-a → `0000000000TXYDY097JGTSDVEX` and tenant-b → `0000000000T5XCJN74D4TBDG0Y`. The zero-time namespace is reserved for new Agent provisioning only.
diff --git a/parties/eng/verify-ext-parties-1.ps1 b/parties/eng/verify-ext-parties-1.ps1
index 925ec667..3ec1059c 100644
--- a/parties/eng/verify-ext-parties-1.ps1
+++ b/parties/eng/verify-ext-parties-1.ps1
@@ -1,9 +1,12 @@
 [CmdletBinding()]
 param(
-    [ValidateSet('Local', 'Live')][string]$Mode = 'Local',
+    [ValidateSet('Local', 'LiveReadiness', 'Live')][string]$Mode = 'Local',
     [string]$EventStoreRoot = (Join-Path $PSScriptRoot '../../eventstore'),
     [string]$PlatformRoot = (Join-Path $PSScriptRoot '../../platform'),
-    [string]$EvidenceDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) ('ext-parties-1-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmss')))
+    [string]$EvidenceDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) ('ext-parties-1-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmss'))),
+    [string]$ArtifactsDirectory,
+    [string]$MemoriesRoot,
+    [string]$DependencyRegisterPath = (Join-Path $PSScriptRoot '../../agents/_bmad-output/planning-artifacts/external-dependency-register.md')
 )
 $ErrorActionPreference = 'Stop'
 $PartiesRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
@@ -11,6 +14,12 @@ $EventStoreRoot = [System.IO.Path]::GetFullPath($EventStoreRoot)
 $PlatformRoot = [System.IO.Path]::GetFullPath($PlatformRoot)
 New-Item -ItemType Directory -Path $EvidenceDirectory -Force | Out-Null
 
+if ($Mode -eq 'LiveReadiness') {
+    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'verify-ext-parties-history-readiness.ps1') -EvidenceDirectory $EvidenceDirectory -DependencyRegisterPath $DependencyRegisterPath
+    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
+    exit 0
+}
+
 if ($Mode -eq 'Live') {
     $required = @('EXT_PARTIES_GATEWAY_URL', 'EXT_PARTIES_TENANT_A', 'EXT_PARTIES_TENANT_B',
         'EXT_PARTIES_PROVISIONER_CREDENTIAL', 'EXT_PARTIES_IDENTITY_WRITER_CREDENTIAL', 'EXT_PARTIES_READER_CREDENTIAL',
@@ -18,19 +27,23 @@ if ($Mode -eq 'Live') {
         'EXT_PARTIES_RESTORE_TARGET', 'EXT_PARTIES_FAILURE_INJECTION_TARGET')
     $missing = @($required | Where-Object { [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($_)) })
     $gate = if ($missing.Count) { 'Missing live inputs: ' + ($missing -join ', ') } else {
-        'Live qualification remains blocked: no installed production custody or purpose-scoped retained-history source after profile erasure. Authenticated live P-01–P-10 persisted-state/restart/restore/failure-injection probing lanes are not implemented; local fixtures cannot qualify them.'
+        'Live qualification remains blocked: production custody/policy and complete authenticated P-01-P-10 persisted-state/restart/restore/failure-injection lanes are not installed. The purpose-scoped history contract and LiveReadiness read probes do not establish complete qualification.'
     }
     Set-Content -Path (Join-Path $EvidenceDirectory 'live-gate.txt') -Value $gate
     Write-Error $gate -ErrorAction Continue
     exit 1
 }
 
-$flags = @('-c', 'Debug', '-p:UseHexalithProjectReferences=true', '-p:UseNuGetDeps=false',
-    "-p:HexalithEventStoreRoot=$EventStoreRoot", "-p:HexalithCommonsRoot=$PartiesRoot/references/Hexalith.Commons", '-p:NuGetAudit=false', '-m:1')
+if ([string]::IsNullOrWhiteSpace($ArtifactsDirectory)) { $ArtifactsDirectory = Join-Path $EvidenceDirectory 'artifacts' }
+if ([string]::IsNullOrWhiteSpace($MemoriesRoot)) { $MemoriesRoot = Join-Path $EvidenceDirectory 'optional-memories-package-mode' }
+$ArtifactsDirectory = [System.IO.Path]::GetFullPath($ArtifactsDirectory)
+$flags = @('-c', 'Debug', '--artifacts-path', $ArtifactsDirectory, '-p:UseHexalithProjectReferences=true', '-p:UseNuGetDeps=false',
+    "-p:HexalithEventStoreRoot=$EventStoreRoot", "-p:HexalithCommonsRoot=$PartiesRoot/references/Hexalith.Commons",
+    "-p:HexalithMemoriesRoot=$MemoriesRoot", '-p:NuGetAudit=false', '-m:1')
 $lanes = @(
-    @{ Root=$EventStoreRoot; Project='Hexalith.EventStore.Contracts.Tests'; Classes=@('*StreamReadPageValidatorTests'); Matrix='Shared source contract validation' },
-    @{ Root=$EventStoreRoot; Project='Hexalith.EventStore.Client.Tests'; Classes=@('*AuthoritativeEventStreamReaderTests', '*EventStoreGatewayClientTests', '*EventStoreGatewayClientStreamTests'); Matrix='Shared source/isolation' },
-    @{ Root=$EventStoreRoot; Project='Hexalith.EventStore.Server.Tests'; Classes=@('*IdentityAdmissionTests', '*ActorRegistryTests', '*IdentityGatewayDenialTests', '*CommandsControllerTrustedExtensionTests', '*QueriesControllerTests', '*StreamsControllerTests'); Matrix='Isolation/actor authority' },
+    @{ Root=$EventStoreRoot; Project='Hexalith.EventStore.Contracts.Tests'; Classes=@('*StreamReadPageValidatorTests', '*RetainedIdentityHistoryValidatorTests'); Matrix='Shared source contract validation' },
+    @{ Root=$EventStoreRoot; Project='Hexalith.EventStore.Client.Tests'; Classes=@('*AuthoritativeEventStreamReaderTests', '*EventStoreGatewayClientTests', '*EventStoreGatewayClientStreamTests', '*RetainedIdentityHistoryReaderTests'); Matrix='Shared source/isolation' },
+    @{ Root=$EventStoreRoot; Project='Hexalith.EventStore.Server.Tests'; Classes=@('*IdentityAdmissionTests', '*ActorRegistryTests', '*IdentityGatewayDenialTests', '*CommandsControllerTrustedExtensionTests', '*QueriesControllerTests', '*StreamsControllerTests', '*RetainedIdentityHistorySourceReaderTests'); Matrix='Isolation/actor authority' },
     @{ Root=$PlatformRoot; Project='Hexalith.Platform.Identity.Tests'; Classes=@('*PlatformIdentityAdmissionTests'); Matrix='Global registry/bootstrap' },
     @{ Root=$PartiesRoot; Project='Hexalith.Parties.Contracts.Tests'; Classes=@('*PartyStateTests', '*PartyIdentityContractTests', '*ContractsPublicApiSnapshotTests'); Matrix='History/compatibility' },
     @{ Root=$PartiesRoot; Project='Hexalith.Parties.Server.Tests'; Classes=@('*AgentPartyProvisioningTests', '*HumanActorBindingTests', '*PartyAggregateCreateTests', '*PartyAggregateCompositeTests'); Matrix='Provision/history' },
@@ -41,32 +54,38 @@ $lanes = @(
 )
 $manifest = @()
 foreach ($lane in $lanes) {
-    $project = Join-Path $lane.Root "tests/$($lane.Project)/$($lane.Project).csproj"
-    if (-not (Test-Path $project)) { throw "Missing required owner project: $project" }
-    $buildLog = Join-Path $EvidenceDirectory "$($lane.Project)-build.log"
-    Write-Host "Building $($lane.Project)"
-    & dotnet build $project @flags *> $buildLog
-    if ($LASTEXITCODE -ne 0) { Get-Content $buildLog -Tail 25; throw "Build failed: $($lane.Project)" }
-    $assembly = Join-Path $lane.Root "tests/$($lane.Project)/bin/Debug/net10.0/$($lane.Project).dll"
-    $filter = @()
-    foreach ($class in $lane.Classes) { $filter += @('-class', $class) }
-    $testLog = Join-Path $EvidenceDirectory "$($lane.Project)-tests.log"
-    $xmlLog = Join-Path $EvidenceDirectory "$($lane.Project)-tests.xml"
-    & dotnet $assembly @filter -result-xml $xmlLog *> $testLog
-    if ($LASTEXITCODE -ne 0) { Get-Content $testLog; throw "Tests failed: $($lane.Project)" }
-    $summary = (Get-Content $testLog | Where-Object { $_ -match 'Total: (\d+), Errors: (\d+), Failed: (\d+), Skipped: (\d+), Not Run: (\d+)' } | Select-Object -Last 1)
-    if (-not $summary -or $summary -notmatch 'Total: (\d+), Errors: (\d+), Failed: (\d+), Skipped: (\d+), Not Run: (\d+)') { throw "No execution evidence: $($lane.Project)" }
-    if ([int]$Matches[1] -le 0 -or [int]$Matches[2] -ne 0 -or [int]$Matches[3] -ne 0 -or [int]$Matches[4] -ne 0 -or [int]$Matches[5] -ne 0) { throw "Required lane incomplete: $summary" }
-    [xml]$xmlEvidence = Get-Content -Raw $xmlLog
-    $executed = @($xmlEvidence.SelectNodes('//test'))
-    if ($executed.Count -eq 0 -or @($executed | Where-Object { $_.result -ne 'Pass' }).Count -ne 0) { throw "Incomplete XML execution evidence: $($lane.Project)" }
-    foreach ($pattern in $lane.Classes) {
-        $matched = @($executed | Where-Object { $_.type -like $pattern -and $_.result -eq 'Pass' })
-        if ($matched.Count -eq 0) { throw "Required class had no passing executed test: $pattern in $($lane.Project)" }
+    Push-Location $lane.Root
+    try {
+        $project = Join-Path $lane.Root "tests/$($lane.Project)/$($lane.Project).csproj"
+        if (-not (Test-Path $project)) { throw "Missing required owner project: $project" }
+        $buildLog = Join-Path $EvidenceDirectory "$($lane.Project)-build.log"
+        Write-Host "Building $($lane.Project)"
+        & dotnet build $project @flags *> $buildLog
+        if ($LASTEXITCODE -ne 0) { Get-Content $buildLog -Tail 25; throw "Build failed: $($lane.Project)" }
+        $assembly = Join-Path $ArtifactsDirectory "bin/$($lane.Project)/debug/$($lane.Project).dll"
+        if (-not (Test-Path $assembly)) { throw "Successful owner build did not produce the expected test assembly: $($lane.Project)" }
+        $filter = @()
+        foreach ($class in $lane.Classes) { $filter += @('-class', $class) }
+        $testLog = Join-Path $EvidenceDirectory "$($lane.Project)-tests.log"
+        $xmlLog = Join-Path $EvidenceDirectory "$($lane.Project)-tests.xml"
+        & dotnet $assembly @filter -result-xml $xmlLog *> $testLog
+        if ($LASTEXITCODE -ne 0) { Get-Content $testLog; throw "Tests failed: $($lane.Project)" }
+        $summary = (Get-Content $testLog | Where-Object { $_ -match 'Total: (\d+), Errors: (\d+), Failed: (\d+), Skipped: (\d+), Not Run: (\d+)' } | Select-Object -Last 1)
+        if (-not $summary -or $summary -notmatch 'Total: (\d+), Errors: (\d+), Failed: (\d+), Skipped: (\d+), Not Run: (\d+)') { throw "No execution evidence: $($lane.Project)" }
+        if ([int]$Matches[1] -le 0 -or [int]$Matches[2] -ne 0 -or [int]$Matches[3] -ne 0 -or [int]$Matches[4] -ne 0 -or [int]$Matches[5] -ne 0) { throw "Required lane incomplete: $summary" }
+        [xml]$xmlEvidence = Get-Content -Raw $xmlLog
+        $executed = @($xmlEvidence.SelectNodes('//test'))
+        if ($executed.Count -eq 0 -or @($executed | Where-Object { $_.result -ne 'Pass' }).Count -ne 0) { throw "Incomplete XML execution evidence: $($lane.Project)" }
+        foreach ($pattern in $lane.Classes) {
+            $matched = @($executed | Where-Object { $_.type -like $pattern -and $_.result -eq 'Pass' })
+            if ($matched.Count -eq 0) { throw "Required class had no passing executed test: $pattern in $($lane.Project)" }
+        }
+        $manifest += @{ Project=$lane.Project; Matrix=$lane.Matrix; Classes=$lane.Classes; BuildLog=$buildLog; TestLog=$testLog; XmlLog=$xmlLog; Summary=$summary }
+        $manifest | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $EvidenceDirectory 'local-evidence.json')
+        Write-Host $summary
     }
-    $manifest += @{ Project=$lane.Project; Matrix=$lane.Matrix; Classes=$lane.Classes; BuildLog=$buildLog; TestLog=$testLog; XmlLog=$xmlLog; Summary=$summary }
-    $manifest | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $EvidenceDirectory 'local-evidence.json')
-    Write-Host $summary
+    finally { Pop-Location }
+
 }
 Write-Host "Local verification passed. Evidence: $EvidenceDirectory"
-Write-Host 'Live qualification is not established; run -Mode Live for the explicit installed-target gate.'
+Write-Host 'Live qualification is not established; LiveReadiness performs only owner-configured read probes and Live retains the complete qualification gate.'
diff --git a/parties/src/Hexalith.Parties.Client/HttpPartiesIdentityClient.cs b/parties/src/Hexalith.Parties.Client/HttpPartiesIdentityClient.cs
index 5a156412..2193c974 100644
--- a/parties/src/Hexalith.Parties.Client/HttpPartiesIdentityClient.cs
+++ b/parties/src/Hexalith.Parties.Client/HttpPartiesIdentityClient.cs
@@ -59,7 +59,7 @@ public sealed class HttpPartiesIdentityClient(HttpClient httpClient) : IPartiesI
             || result.Evidence is { } evidence && (evidence.ContractVersion != 1 || evidence.TenantId != tenantId
             || evidence.PartyId != query.PartyId || evidence.SourcePosition <= 0 || string.IsNullOrWhiteSpace(evidence.ObservationId)
             || evidence.ObservedAt == default || evidence.Classification is not (PartyIdentityClassification.Human
-                or PartyIdentityClassification.Organization or PartyIdentityClassification.Agent)
+                or PartyIdentityClassification.Organization)
             || evidence.HumanBinding is { } binding && (evidence.Classification != PartyIdentityClassification.Human
                 || binding.ActorId != query.ExpectedActorId || !Matches(binding, tenantId, query.PartyId)
                 || evidence.ObservedAt < binding.ValidFrom || evidence.ObservedAt >= binding.ValidUntil))
@@ -87,7 +87,8 @@ public sealed class HttpPartiesIdentityClient(HttpClient httpClient) : IPartiesI
                 || binding.ActorId != query.ExpectedActorId || binding.BindingVersion != query.ExpectedBindingVersion
                 || query.ActionAt < binding.ValidFrom || binding.ValidUntil is { } until && query.ActionAt >= until)
             || result.Outcome == HumanActorBindingOutcome.Resolved && (result.Evidence is null
-                || result.SourcePosition <= 0 || string.IsNullOrWhiteSpace(result.ObservationId)))
+                || result.SourcePosition <= 0 || result.BindingSourcePosition <= 0
+                || result.BindingSourcePosition > result.SourcePosition || string.IsNullOrWhiteSpace(result.ObservationId)))
         {
             throw Unavailable();
         }
diff --git a/parties/src/Hexalith.Parties.Contracts/Models/HumanActorBindingResult.cs b/parties/src/Hexalith.Parties.Contracts/Models/HumanActorBindingResult.cs
index 69d171bf..07f2b781 100644
--- a/parties/src/Hexalith.Parties.Contracts/Models/HumanActorBindingResult.cs
+++ b/parties/src/Hexalith.Parties.Contracts/Models/HumanActorBindingResult.cs
@@ -18,4 +18,8 @@ public sealed record HumanActorBindingResult(
     DateTimeOffset ActionAt,
     long SourcePosition,
     string? ObservationId,
-    HumanActorBindingEvidence? Evidence);
+    HumanActorBindingEvidence? Evidence)
+{
+    /// <summary>Gets the original source position that established the returned binding interval.</summary>
+    public long BindingSourcePosition { get; init; }
+}
diff --git a/parties/src/Hexalith.Parties/Authorization/PartyIdentityOptions.cs b/parties/src/Hexalith.Parties/Authorization/PartyIdentityOptions.cs
index 1ba9f90d..eb81064e 100644
--- a/parties/src/Hexalith.Parties/Authorization/PartyIdentityOptions.cs
+++ b/parties/src/Hexalith.Parties/Authorization/PartyIdentityOptions.cs
@@ -2,7 +2,7 @@ using Hexalith.EventStore.Contracts.Security;
 
 namespace Hexalith.Parties.Authorization;
 
-/// <summary>Required attribution policy configuration; unset settings deny writes and live qualification.</summary>
+/// <summary>Required attribution policy configuration; unset settings deny binding writes, binding reads and live qualification.</summary>
 public sealed class PartyIdentityOptions
 {
     /// <summary>Gets or sets the approved versioned retention policy.</summary>
diff --git a/parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs b/parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs
index 91ec4de6..02cd1ce3 100644
--- a/parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs
+++ b/parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs
@@ -14,69 +14,116 @@ using Hexalith.Parties.Contracts.Queries;
 using Hexalith.Parties.Contracts.Security;
 using Hexalith.Parties.Contracts.State;
 using Hexalith.Parties.Contracts.ValueObjects;
+using Microsoft.Extensions.Options;
 
 namespace Hexalith.Parties.Queries;
 
 /// <summary>Strict current and action-time identity reads, with no stale fallback or profile result.</summary>
 public sealed class PartyIdentityQueryService(IPartyIdentityAuthority authority, TimeProvider timeProvider,
-    IAuthoritativeEventStreamReader? reader = null, IIdentityHistoryCustody? custody = null)
+    IAuthoritativeEventStreamReader? reader = null, IIdentityHistoryCustody? custody = null,
+    IRetainedIdentityHistoryReader? historyReader = null, IOptionsMonitor<PartyIdentityOptions>? identityOptions = null)
 {
     /// <summary>Resolves current classification and eligibility from complete source and actor evidence.</summary>
     public async Task<PartyIdentityResult> ResolveAsync(QueryEnvelope envelope, CancellationToken cancellationToken)
     {
         ArgumentNullException.ThrowIfNull(envelope);
+        cancellationToken.ThrowIfCancellationRequested();
         try
         {
+            IdentityHistoryPolicy? policy = identityOptions?.CurrentValue.Policy;
             ResolvePartyIdentity? query = JsonSerializer.Deserialize<ResolvePartyIdentity>(envelope.Payload, PartiesJsonOptions.Default);
             if (query is null || !Matches(envelope, query.TenantId, query.PartyId))
             {
+                cancellationToken.ThrowIfCancellationRequested();
                 return new(PartyIdentityOutcome.Unavailable, null);
             }
 
             IdentityAdmissionEvidence? admitted = authority.Admit(envelope).Evidence;
             if (admitted is null || reader is null)
             {
+                cancellationToken.ThrowIfCancellationRequested();
                 return new(PartyIdentityOutcome.Unavailable, null);
             }
 
-            AuthoritativeStreamReadResult read = await reader.ReadAsync(new(query.TenantId, "party", query.PartyId), cancellationToken).ConfigureAwait(false);
-            if (!read.IsAuthoritative || !MatchesSource(envelope, read.Stream))
+            cancellationToken.ThrowIfCancellationRequested();
+            AuthoritativeStreamReadResult read = await reader.ReadAsync(new(query.TenantId, "party", query.PartyId), cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
+            cancellationToken.ThrowIfCancellationRequested();
+            if (!read.IsAuthoritative || !MatchesSource(envelope, read.Stream)
+                || read.Stream!.ObservedAt > timeProvider.GetUtcNow())
             {
+                cancellationToken.ThrowIfCancellationRequested();
                 return new(PartyIdentityOutcome.Unavailable, null);
             }
 
             PartyState state = PartyIdentitySourceFold.Fold(read.Stream!);
             if (!state.HasBeenCreated)
             {
+                cancellationToken.ThrowIfCancellationRequested();
                 return new(PartyIdentityOutcome.Unavailable, null);
             }
 
             PartyIdentityClassification classification = state.Type == PartyType.Person ? PartyIdentityClassification.Human
-                : state.Type == PartyType.Organization ? state.AgentProvisioning is null
-                    ? PartyIdentityClassification.Organization : PartyIdentityClassification.Agent
+                : state.Type == PartyType.Organization ? PartyIdentityClassification.Organization
                 : PartyIdentityClassification.Unknown;
             bool eligible = state.IsActive && !state.IsRestricted && state.ErasureStatus == ErasureStatus.Active;
+            bool requiresHumanPolicy = classification == PartyIdentityClassification.Human && eligible;
             HumanActorBindingEvidence? binding = null;
-            if (classification == PartyIdentityClassification.Human)
+            if (requiresHumanPolicy)
             {
+                if (!PolicyIsCurrent(policy))
+                {
+                    cancellationToken.ThrowIfCancellationRequested();
+                    return new(PartyIdentityOutcome.Unavailable, null);
+                }
+
                 DateTimeOffset now = timeProvider.GetUtcNow();
                 HumanActorBindingEvidence[] candidates = [.. state.HumanActorBindings.Select(item => item.Evidence)
                     .Where(item => item.ValidFrom <= now && (item.ValidUntil is null || now < item.ValidUntil))];
                 if (candidates.Length == 1 && candidates[0].ActorId == query.ExpectedActorId
                     && admitted.TargetActorId == candidates[0].ActorId && admitted.ActorActive
-                    && admitted.ActorRevision >= candidates[0].ActorRevision && custody is not null
-                    && await custody.CanReadAsync(read.Stream!.Identity, candidates[0].Custody, cancellationToken).ConfigureAwait(false))
+                    && admitted.ActorRevision >= candidates[0].ActorRevision && custody is not null)
                 {
-                    binding = candidates[0];
+                    if (!candidates[0].Custody.Satisfies(policy!, candidates[0].ValidFrom))
+                    {
+                        cancellationToken.ThrowIfCancellationRequested();
+                        return new(PartyIdentityOutcome.Unavailable, null);
+                    }
+
+                    bool canRead = await custody.CanReadAsync(read.Stream!.Identity, candidates[0].Custody, cancellationToken)
+                        .WaitAsync(cancellationToken).ConfigureAwait(false);
+                    cancellationToken.ThrowIfCancellationRequested();
+                    if (!PolicyIsCurrent(policy))
+                    {
+                        cancellationToken.ThrowIfCancellationRequested();
+                        return new(PartyIdentityOutcome.Unavailable, null);
+                    }
+
+                    if (canRead)
+                    {
+                        binding = candidates[0];
+                    }
                 }
 
                 eligible &= binding is not null;
             }
 
+            IdentityAdmissionEvidence? currentAuthority = authority.Admit(envelope).Evidence;
+            cancellationToken.ThrowIfCancellationRequested();
+            DateTimeOffset completedAt = timeProvider.GetUtcNow();
+            if (!SameAuthority(admitted, currentAuthority, completedAt)
+                || requiresHumanPolicy && !PolicyIsCurrent(policy)
+                || binding is not null && (!currentAuthority!.ActorActive || completedAt >= binding.ValidUntil
+                    || completedAt >= binding.Custody.ExpiresAt))
+            {
+                cancellationToken.ThrowIfCancellationRequested();
+                return new(PartyIdentityOutcome.Unavailable, null);
+            }
+
             AuthoritativeEventStream source = read.Stream!;
             var evidence = new PartyIdentityEvidence(1, query.TenantId, query.PartyId, classification,
                 state.IsActive, state.IsRestricted, state.ErasureStatus != ErasureStatus.Active,
                 source.Head, source.ObservedAt, source.ObservationId, binding);
+            cancellationToken.ThrowIfCancellationRequested();
             return new(eligible && classification != PartyIdentityClassification.Unknown
                 ? PartyIdentityOutcome.Resolved : PartyIdentityOutcome.Ineligible, evidence);
         }
@@ -86,6 +133,7 @@ public sealed class PartyIdentityQueryService(IPartyIdentityAuthority authority,
         }
         catch (Exception)
         {
+            cancellationToken.ThrowIfCancellationRequested();
             return new(PartyIdentityOutcome.Unavailable, null);
         }
     }
@@ -94,51 +142,115 @@ public sealed class PartyIdentityQueryService(IPartyIdentityAuthority authority,
     public async Task<HumanActorBindingResult> ResolveAtAsync(QueryEnvelope envelope, CancellationToken cancellationToken)
     {
         ArgumentNullException.ThrowIfNull(envelope);
+        cancellationToken.ThrowIfCancellationRequested();
         ResolveHumanActorBindingAt? query = null;
         try
         {
+            IdentityHistoryPolicy? policy = identityOptions?.CurrentValue.Policy;
             query = JsonSerializer.Deserialize<ResolveHumanActorBindingAt>(envelope.Payload, PartiesJsonOptions.Default);
             if (query is null || !Matches(envelope, query.TenantId, query.PartyId)
                 || string.IsNullOrWhiteSpace(query.ExpectedActorId) || query.ExpectedBindingVersion <= 0
                 || authority.Admit(envelope).Evidence is not { } admitted
-                || admitted.TargetActorId != query.ExpectedActorId || reader is null || custody is null)
+                || admitted.TargetActorId != query.ExpectedActorId || historyReader is null || custody is null
+                || !PolicyIsCurrent(policy))
             {
+                cancellationToken.ThrowIfCancellationRequested();
                 return HistoryFailure(query, HumanActorBindingOutcome.Unavailable);
             }
 
-            AuthoritativeStreamReadResult read = await reader.ReadAsync(new(query.TenantId, "party", query.PartyId), cancellationToken).ConfigureAwait(false);
-            if (!read.IsAuthoritative || !MatchesSource(envelope, read.Stream))
+            cancellationToken.ThrowIfCancellationRequested();
+            RetainedIdentityHistoryReadResult read = await historyReader.ReadAsync(new(query.TenantId, "party", query.PartyId),
+                RetainedIdentityHistoryReadRequest.AttributionPurpose, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
+            cancellationToken.ThrowIfCancellationRequested();
+            if (!PolicyIsCurrent(policy) || !read.IsAuthoritative || read.Stream is not { } retainedSource
+                || retainedSource.Identity != new AggregateIdentity(query.TenantId, "party", query.PartyId)
+                || !RetainedIdentityHistoryValidator.IsComplete(new(retainedSource.Identity,
+                    RetainedIdentityHistoryReadRequest.AttributionPurpose), retainedSource, timeProvider.GetUtcNow()))
             {
+                cancellationToken.ThrowIfCancellationRequested();
                 return HistoryFailure(query, HumanActorBindingOutcome.Unavailable);
             }
 
-            if (query.ActionAt > read.Stream!.ObservedAt)
+            if (query.ActionAt > read.Stream!.ObservedAt || read.Stream.ObservedAt > timeProvider.GetUtcNow())
             {
+                cancellationToken.ThrowIfCancellationRequested();
                 return HistoryFailure(query, HumanActorBindingOutcome.Unavailable);
             }
 
-            PartyState state = PartyIdentitySourceFold.Fold(read.Stream!);
-            HumanActorBindingEvidence[] matches = [.. state.HumanActorBindings.Select(item => item.Evidence)
-                .Where(item => item.ValidFrom <= query.ActionAt && (item.ValidUntil is null || query.ActionAt < item.ValidUntil))];
+            IReadOnlyList<RetainedHumanActorBinding> history = RetainedHumanActorHistoryFold.Fold(read.Stream!);
+            if (!SameAuthority(admitted, authority.Admit(envelope).Evidence, timeProvider.GetUtcNow()) || !PolicyIsCurrent(policy))
+            {
+                cancellationToken.ThrowIfCancellationRequested();
+                return HistoryFailure(query, HumanActorBindingOutcome.Unavailable);
+            }
+
+            RetainedHumanActorBinding[] matches = [.. history
+                .Where(item => item.Evidence.ValidFrom <= query.ActionAt && (item.Evidence.ValidUntil is null || query.ActionAt < item.Evidence.ValidUntil))];
             if (matches.Length != 1)
             {
+                cancellationToken.ThrowIfCancellationRequested();
                 return HistoryFailure(query, matches.Length == 0 ? HumanActorBindingOutcome.Gap : HumanActorBindingOutcome.Ambiguous);
             }
 
-            HumanActorBindingEvidence evidence = matches[0];
+            HumanActorBindingEvidence evidence = matches[0].Evidence;
+            if (!evidence.Custody.Satisfies(policy!, evidence.ValidFrom))
+            {
+                cancellationToken.ThrowIfCancellationRequested();
+                return HistoryFailure(query, HumanActorBindingOutcome.Unavailable);
+            }
+
             if (evidence.ActorId != query.ExpectedActorId || evidence.BindingVersion != query.ExpectedBindingVersion)
             {
+                cancellationToken.ThrowIfCancellationRequested();
                 return HistoryFailure(query, HumanActorBindingOutcome.Mismatch);
             }
 
-            if (timeProvider.GetUtcNow() >= evidence.Custody.ExpiresAt
-                || !await custody.CanReadAsync(read.Stream!.Identity, evidence.Custody, cancellationToken).ConfigureAwait(false))
+            if (timeProvider.GetUtcNow() >= evidence.Custody.ExpiresAt)
+            {
+                cancellationToken.ThrowIfCancellationRequested();
+                return HistoryFailure(query, HumanActorBindingOutcome.Expired);
+            }
+
+            bool canRead = await custody.CanReadAsync(read.Stream!.Identity, evidence.Custody, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
+            cancellationToken.ThrowIfCancellationRequested();
+            DateTimeOffset completedAt = timeProvider.GetUtcNow();
+            if (!PolicyIsCurrent(policy))
+            {
+                cancellationToken.ThrowIfCancellationRequested();
+                return HistoryFailure(query, HumanActorBindingOutcome.Unavailable);
+            }
+
+            if (!canRead || completedAt >= evidence.Custody.ExpiresAt)
+            {
+                cancellationToken.ThrowIfCancellationRequested();
+                return HistoryFailure(query, HumanActorBindingOutcome.Expired);
+            }
+
+            IdentityAdmissionEvidence? currentAuthority = authority.Admit(envelope).Evidence;
+            completedAt = timeProvider.GetUtcNow();
+            if (!PolicyIsCurrent(policy))
             {
+                cancellationToken.ThrowIfCancellationRequested();
+                return HistoryFailure(query, HumanActorBindingOutcome.Unavailable);
+            }
+
+            if (completedAt >= evidence.Custody.ExpiresAt)
+            {
+                cancellationToken.ThrowIfCancellationRequested();
                 return HistoryFailure(query, HumanActorBindingOutcome.Expired);
             }
 
+            if (!SameAuthority(admitted, currentAuthority, completedAt)
+                || !RetainedIdentityHistoryValidator.IsComplete(new(retainedSource.Identity,
+                    RetainedIdentityHistoryReadRequest.AttributionPurpose), retainedSource, completedAt))
+            {
+                cancellationToken.ThrowIfCancellationRequested();
+                return HistoryFailure(query, HumanActorBindingOutcome.Unavailable);
+            }
+
+            cancellationToken.ThrowIfCancellationRequested();
             return new(HumanActorBindingOutcome.Resolved, 1, query.TenantId, query.PartyId, query.ActionAt,
-                read.Stream.Head, read.Stream.ObservationId, evidence);
+                read.Stream.Head, read.Stream.ObservationId, evidence) { BindingSourcePosition = matches[0].SourcePosition };
         }
         catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
         {
@@ -146,14 +258,23 @@ public sealed class PartyIdentityQueryService(IPartyIdentityAuthority authority,
         }
         catch (Exception)
         {
+            cancellationToken.ThrowIfCancellationRequested();
             return HistoryFailure(query, HumanActorBindingOutcome.Unavailable);
         }
     }
 
+    private bool PolicyIsCurrent(IdentityHistoryPolicy? policy)
+        => policy is { IsValid: true } && identityOptions?.CurrentValue.Policy == policy;
+
     private static HumanActorBindingResult HistoryFailure(ResolveHumanActorBindingAt? query, HumanActorBindingOutcome outcome)
         => new(outcome, 1, query?.TenantId ?? string.Empty, query?.PartyId ?? string.Empty,
             query?.ActionAt ?? default, 0, null, null);
 
+    private static bool SameAuthority(IdentityAdmissionEvidence original, IdentityAdmissionEvidence? current, DateTimeOffset now)
+        => current is not null && current.Scope == original.Scope && current.SourceId == original.SourceId
+            && current.TargetActorId == original.TargetActorId && current.ActorRevision == original.ActorRevision
+            && current.AuthorityRevision == original.AuthorityRevision && current.IssuedAt <= now && current.ExpiresAt > now;
+
     private static bool MatchesSource(QueryEnvelope envelope, AuthoritativeEventStream? stream)
         => stream is not null && stream.Identity.TenantId == envelope.TenantId
             && stream.Identity.Domain == envelope.Domain && stream.Identity.AggregateId == envelope.AggregateId;
diff --git a/parties/src/Hexalith.Parties/appsettings.json b/parties/src/Hexalith.Parties/appsettings.json
index 16024fc3..6b478186 100644
--- a/parties/src/Hexalith.Parties/appsettings.json
+++ b/parties/src/Hexalith.Parties/appsettings.json
@@ -14,6 +14,11 @@
     "CommandApiAppId": "parties"
   },
   "Parties": {
+    "Identity": {
+      "PolicyId": "party-actor-retention-v1",
+      "Retention": "365.00:00:00",
+      "ExpiryTrigger": "binding-effective-at"
+    },
     "MemoriesSearch": {
       "Enabled": false,
       "Endpoint": null,
diff --git a/parties/tests/Hexalith.Parties.Client.Tests/HttpPartiesIdentityClientTests.cs b/parties/tests/Hexalith.Parties.Client.Tests/HttpPartiesIdentityClientTests.cs
index 96164fcf..4d23376f 100644
--- a/parties/tests/Hexalith.Parties.Client.Tests/HttpPartiesIdentityClientTests.cs
+++ b/parties/tests/Hexalith.Parties.Client.Tests/HttpPartiesIdentityClientTests.cs
@@ -14,6 +14,33 @@ namespace Hexalith.Parties.Client.Tests;
 
 public sealed class HttpPartiesIdentityClientTests
 {
+    [Fact]
+    public async Task HistoricalReply_RequiresOriginalBindingPositionInsideCurrentCheckpoint()
+    {
+        DateTimeOffset at = DateTimeOffset.UtcNow.AddDays(-1);
+        const string actor = "01HX0000000000000000000001";
+        var evidence = new HumanActorBindingEvidence("tenant-a", "party-1", actor, 1, 1, at, at.AddDays(10), "writer", "operator",
+            new("synthetic", "party-actor-history-v1", at.AddDays(10), 1, "custody", true, true, true));
+        foreach (long position in new long[] { 0, 5, 2 })
+        {
+            var result = new HumanActorBindingResult(HumanActorBindingOutcome.Resolved, 1, "tenant-a", "party-1", at, 4, "observation", evidence)
+                { BindingSourcePosition = position };
+            using var http = new HttpClient(new Handler(_ => Task.FromResult(Json(new SubmitQueryResponse("c", JsonSerializer.SerializeToElement(result, PartiesJsonOptions.Default))))))
+                { BaseAddress = new Uri("https://gateway.test/") };
+            var client = new HttpPartiesIdentityClient(http);
+            if (position == 2)
+            {
+                HumanActorBindingResult resolved = await client.ResolveHumanActorBindingAtAsync("tenant-a", new("tenant-a", "party-1", at, actor, 1), TestContext.Current.CancellationToken);
+                resolved.BindingSourcePosition.ShouldBe(2);
+                resolved.SourcePosition.ShouldBe(4);
+            }
+            else
+            {
+                await Should.ThrowAsync<PartiesClientException>(() => client.ResolveHumanActorBindingAtAsync("tenant-a", new("tenant-a", "party-1", at, actor, 1), TestContext.Current.CancellationToken));
+            }
+        }
+    }
+
     [Fact]
     public async Task ConcurrentTenantCalls_KeepEveryRequestAndEvidenceInItsExplicitScope()
     {
@@ -39,6 +66,7 @@ public sealed class HttpPartiesIdentityClientTests
     [Theory]
     [InlineData(999, 1, "tenant-a", false)]
     [InlineData(1, 999, "tenant-a", false)]
+    [InlineData(1, 3, "tenant-a", false)]
     [InlineData(1, 2, "tenant-b", false)]
     [InlineData(1, 2, "tenant-a", true)]
     public async Task UnknownOutcomeClassificationForeignOrDegradedReply_IsUnavailable(int outcome, int classification, string tenant, bool degraded)
diff --git a/parties/tests/Hexalith.Parties.Contracts.Tests/Package/ContractsPublicApiSnapshot.txt b/parties/tests/Hexalith.Parties.Contracts.Tests/Package/ContractsPublicApiSnapshot.txt
index 14c48d8c..ee88b115 100644
--- a/parties/tests/Hexalith.Parties.Contracts.Tests/Package/ContractsPublicApiSnapshot.txt
+++ b/parties/tests/Hexalith.Parties.Contracts.Tests/Package/ContractsPublicApiSnapshot.txt
@@ -455,6 +455,7 @@ type class Hexalith.Parties.Contracts.Models.HumanActorBindingEvidence interface
 type class Hexalith.Parties.Contracts.Models.HumanActorBindingResult interfaces:System.IEquatable<Hexalith.Parties.Contracts.Models.HumanActorBindingResult>
   ctor (Hexalith.Parties.Contracts.ValueObjects.HumanActorBindingOutcome Outcome, System.Int32 ContractVersion, System.String TenantId, System.String PartyId, System.DateTimeOffset ActionAt, System.Int64 SourcePosition, System.String ObservationId, Hexalith.Parties.Contracts.Models.HumanActorBindingEvidence Evidence)
   property System.DateTimeOffset ActionAt getset
+  property System.Int64 BindingSourcePosition getset
   property System.Int32 ContractVersion getset
   property Hexalith.Parties.Contracts.Models.HumanActorBindingEvidence Evidence getset
   property System.String ObservationId getset
diff --git a/parties/tests/Hexalith.Parties.Contracts.Tests/Package/ContractsPublicApiSnapshotTests.cs b/parties/tests/Hexalith.Parties.Contracts.Tests/Package/ContractsPublicApiSnapshotTests.cs
index 21beede0..5233794f 100644
--- a/parties/tests/Hexalith.Parties.Contracts.Tests/Package/ContractsPublicApiSnapshotTests.cs
+++ b/parties/tests/Hexalith.Parties.Contracts.Tests/Package/ContractsPublicApiSnapshotTests.cs
@@ -110,8 +110,22 @@ public sealed class ContractsPublicApiSnapshotTests
         return type.FullName ?? type.Name;
     }
 
-    private static string FindProjectDirectory()
+    private static string FindProjectDirectory([CallerFilePath] string sourceFile = "")
     {
+        // SDK --artifacts-path places binaries outside the repository, and the
+        // runner may change its working directory to the assembly directory.
+        string sourceCandidate = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, ".."));
+        if (File.Exists(Path.Combine(sourceCandidate, "Hexalith.Parties.Contracts.Tests.csproj")))
+        {
+            return sourceCandidate;
+        }
+
+        string currentCandidate = Path.Combine(Directory.GetCurrentDirectory(), "tests", "Hexalith.Parties.Contracts.Tests");
+        if (File.Exists(Path.Combine(currentCandidate, "Hexalith.Parties.Contracts.Tests.csproj")))
+        {
+            return currentCandidate;
+        }
+
         DirectoryInfo? directory = new(AppContext.BaseDirectory);
         while (directory is not null)
         {
diff --git a/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs b/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs
index e652060d..203a7272 100644
--- a/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs
+++ b/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs
@@ -12,6 +12,7 @@ using Hexalith.Parties.Contracts.Queries;
 using Hexalith.Parties.Contracts.State;
 using Hexalith.Parties.Contracts.ValueObjects;
 using Hexalith.Parties.Queries;
+using Microsoft.Extensions.Options;
 using NSubstitute;
 using Shouldly;
 
@@ -26,7 +27,7 @@ public sealed class PartyIdentityQueryHandlerTests
     private static IEventPayload[] Events() => [new PartyCreated { Type = PartyType.Person, CreatedAt = Start, PersonDetails = new PersonDetails { FirstName = "Private", LastName = "Name" } },
         new HumanActorBindingEstablished(new(Binding(), "logical", "digest"), Start, 0)];
     private static QueryEnvelope Envelope<T>(T query) => new("tenant-a", "party", "party-1", typeof(T).FullName!, JsonSerializer.SerializeToUtf8Bytes(query, PartiesJsonOptions.Default), "correlation", "reader", "party-1");
-    private static (PartyIdentityQueryService Service, IPartyIdentityAuthority Authority, IAuthoritativeEventStreamReader Reader, IIdentityHistoryCustody Custody) Service(params IEventPayload[] events)
+    private static (PartyIdentityQueryService Service, IPartyIdentityAuthority Authority, IAuthoritativeEventStreamReader Reader, IIdentityHistoryCustody Custody, IRetainedIdentityHistoryReader HistoryReader, IOptionsMonitor<PartyIdentityOptions> Options) Service(params IEventPayload[] events)
     {
         IPartyIdentityAuthority authority = Substitute.For<IPartyIdentityAuthority>();
         authority.Admit(Arg.Any<QueryEnvelope>()).Returns(call => new PartyIdentityAdmissionResult(new(new("tenant-a", "party", "party-1", "Read", "correlation", "correlation", "digest"),
@@ -36,7 +37,228 @@ public sealed class PartyIdentityQueryHandlerTests
         reader.ReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<CancellationToken>()).Returns(new AuthoritativeStreamReadResult(new(new("tenant-a", "party", "party-1"), source.Length, DateTimeOffset.UtcNow, source, "observation"), null));
         IIdentityHistoryCustody custody = Substitute.For<IIdentityHistoryCustody>();
         custody.CanReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<IdentityHistoryCustodyEvidence>(), Arg.Any<CancellationToken>()).Returns(true);
-        return (new(authority, TimeProvider.System, reader, custody), authority, reader, custody);
+        IRetainedIdentityHistoryReader historyReader = Substitute.For<IRetainedIdentityHistoryReader>();
+        StreamReadEvent[] retained = [.. source.Where((item, index) => events[index] is IIdentityHistoryEvent)
+            .Select(item => item with { MessageId = string.Empty, UserId = null, CorrelationId = null, CausationId = null,
+                ProtectionMetadata = EventStorePayloadProtectionMetadata.Unprotected() })];
+        long[] excluded = [.. source.Where((item, index) => events[index] is not IIdentityHistoryEvent).Select(item => item.SequenceNumber)];
+        historyReader.ReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
+            .Returns(new RetainedIdentityHistoryReadResult(new(new("tenant-a", "party", "party-1"), RetainedIdentityHistoryReadRequest.AttributionPurpose,
+                source.Length, DateTimeOffset.UtcNow, retained, excluded, "history-observation")
+                { AuthorityRevision = "fixture-source-r1", ValidUntil = DateTimeOffset.UtcNow.AddMinutes(1) }, null));
+        IOptionsMonitor<PartyIdentityOptions> options = Substitute.For<IOptionsMonitor<PartyIdentityOptions>>();
+        options.CurrentValue.Returns(PolicyOptions());
+        return (new(authority, TimeProvider.System, reader, custody, historyReader, options), authority, reader, custody, historyReader, options);
+    }
+
+    [Fact]
+    public async Task CustodyExpiryCrossingAwait_DeniesHistoricalEvidence()
+    {
+        var fixture = Service(Events());
+        DateTimeOffset now = Start.AddDays(9);
+        TimeProvider clock = Substitute.For<TimeProvider>();
+        clock.GetUtcNow().Returns(_ => now);
+        var admitted = new IdentityAdmissionEvidence(new("tenant-a", "party", "party-1", "Read", "c", "c", "d"),
+            "reader", null, Actor, 1, false, Start, Start.AddDays(20), 1);
+        fixture.Authority.Admit(Arg.Any<QueryEnvelope>()).Returns(new PartyIdentityAdmissionResult(admitted, null));
+        RetainedIdentityHistoryReadResult source = await fixture.HistoryReader.ReadAsync(new("tenant-a", "party", "party-1"),
+            RetainedIdentityHistoryReadRequest.AttributionPurpose, TestContext.Current.CancellationToken);
+        fixture.HistoryReader.ReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
+            .Returns(source with { Stream = source.Stream! with { ValidUntil = Binding().Custody.ExpiresAt } });
+        fixture.Custody.CanReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<IdentityHistoryCustodyEvidence>(), Arg.Any<CancellationToken>())
+            .Returns(_ => { now = Binding().Custody.ExpiresAt; return true; });
+        var service = new PartyIdentityQueryService(fixture.Authority, clock, custody: fixture.Custody, historyReader: fixture.HistoryReader, identityOptions: fixture.Options);
+        HumanActorBindingResult result = await service.ResolveAtAsync(Envelope(new ResolveHumanActorBindingAt("tenant-a", "party-1", Start, Actor, 1)), TestContext.Current.CancellationToken);
+        result.Outcome.ShouldBe(HumanActorBindingOutcome.Expired);
+        result.Evidence.ShouldBeNull();
+        result.BindingSourcePosition.ShouldBe(0);
+    }
+
+    [Theory]
+    [InlineData("revoked")]
+    [InlineData("different-actor")]
+    [InlineData("different-revision")]
+    [InlineData("different-source")]
+    public async Task ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor(string change)
+    {
+        var fixture = Service(Events());
+        IdentityAdmissionEvidence original = fixture.Authority.Admit(Envelope(new ResolveHumanActorBindingAt("tenant-a", "party-1", Start, Actor, 1))).Evidence!;
+        IdentityAdmissionEvidence? changed = change switch
+        {
+            "revoked" => null,
+            "different-actor" => original with { TargetActorId = "01HX0000000000000000000002" },
+            "different-revision" => original with { AuthorityRevision = original.AuthorityRevision + 1 },
+            _ => original with { SourceId = "different-reader" },
+        };
+        fixture.Authority.Admit(Arg.Any<QueryEnvelope>()).Returns(new PartyIdentityAdmissionResult(original, null),
+            new PartyIdentityAdmissionResult(original, null), new PartyIdentityAdmissionResult(changed, changed is null ? "authority-revoked" : null));
+        HumanActorBindingResult result = await fixture.Service.ResolveAtAsync(Envelope(new ResolveHumanActorBindingAt("tenant-a", "party-1", Start, Actor, 1)), TestContext.Current.CancellationToken);
+        result.Outcome.ShouldBe(HumanActorBindingOutcome.Unavailable);
+        result.Evidence.ShouldBeNull();
+        result.BindingSourcePosition.ShouldBe(0);
+    }
+
+    [Fact]
+    public async Task RetainedCertificateExpiryCrossingCustodyAwait_DeniesWithoutRenewingObservation()
+    {
+        var fixture = Service(Events());
+        RetainedIdentityHistoryReadResult source = await fixture.HistoryReader.ReadAsync(new("tenant-a", "party", "party-1"),
+            RetainedIdentityHistoryReadRequest.AttributionPurpose, TestContext.Current.CancellationToken);
+        DateTimeOffset now = DateTimeOffset.UtcNow.AddSeconds(1);
+        DateTimeOffset deadline = now.AddSeconds(1);
+        source = source with { Stream = source.Stream! with { ValidUntil = deadline } };
+        fixture.HistoryReader.ReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(source);
+        TimeProvider clock = Substitute.For<TimeProvider>();
+        clock.GetUtcNow().Returns(_ => now);
+        fixture.Custody.CanReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<IdentityHistoryCustodyEvidence>(), Arg.Any<CancellationToken>())
+            .Returns(_ => { now = deadline; return true; });
+        var service = new PartyIdentityQueryService(fixture.Authority, clock, custody: fixture.Custody, historyReader: fixture.HistoryReader, identityOptions: fixture.Options);
+        HumanActorBindingResult result = await service.ResolveAtAsync(Envelope(new ResolveHumanActorBindingAt("tenant-a", "party-1", Start, Actor, 1)), TestContext.Current.CancellationToken);
+        result.Outcome.ShouldBe(HumanActorBindingOutcome.Unavailable);
+        result.Evidence.ShouldBeNull();
+        source.Stream!.ValidUntil.ShouldBe(deadline);
+        await fixture.Reader.DidNotReceiveWithAnyArgs().ReadAsync(default!, default);
+    }
+
+    [Fact]
+    public async Task ErasedProfile_CurrentReadFailsWhileIndependentHistoryKeepsOriginalPosition()
+    {
+        var fixture = Service([.. Events(), new PartyErased { TenantId = "tenant-a", PartyId = "party-1", ErasedAt = Start.AddHours(1) }]);
+        fixture.Reader.ReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<CancellationToken>())
+            .Returns(new AuthoritativeStreamReadResult(null, "profile-key-destroyed"));
+        PartyIdentityResult current = await fixture.Service.ResolveAsync(Envelope(new ResolvePartyIdentity("tenant-a", "party-1", Actor)), TestContext.Current.CancellationToken);
+        current.Outcome.ShouldBe(PartyIdentityOutcome.Unavailable);
+        HumanActorBindingResult historical = await fixture.Service.ResolveAtAsync(Envelope(new ResolveHumanActorBindingAt("tenant-a", "party-1", Start, Actor, 1)), TestContext.Current.CancellationToken);
+        historical.Outcome.ShouldBe(HumanActorBindingOutcome.Resolved);
+        historical.SourcePosition.ShouldBe(3);
+        historical.BindingSourcePosition.ShouldBe(2);
+        historical.Evidence!.ActorId.ShouldBe(Actor);
+        JsonSerializer.Serialize(historical, PartiesJsonOptions.Default).ShouldNotContain("Private");
+        await fixture.Reader.Received(1).ReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<CancellationToken>());
+    }
+
+    [Fact]
+    public async Task SerializedRetainedSource_ReconstructsBothIntervalsWithoutProfileOrCurrentBinding()
+    {
+        DateTimeOffset boundary = Start.AddHours(1);
+        string successor = "01HX0000000000000000000002";
+        HumanActorBindingEvidence next = Binding() with { ActorId = successor, BindingVersion = 2, ValidFrom = boundary, ValidUntil = boundary.AddDays(10),
+            Custody = Binding().Custody with { ExpiresAt = boundary.AddDays(10) } };
+        var fixture = Service([.. Events(), new HumanActorBindingRebound(new(next, "rebind", "digest-two"), boundary, 1),
+            new PartyErased { TenantId = "tenant-a", PartyId = "party-1", ErasedAt = boundary }]);
+        RetainedIdentityHistoryReadResult source = await fixture.HistoryReader.ReadAsync(new("tenant-a", "party", "party-1"),
+            RetainedIdentityHistoryReadRequest.AttributionPurpose, TestContext.Current.CancellationToken);
+        // This exercises the stored wire representation and a fresh domain service, not live storage/restore qualification.
+        byte[] stored = JsonSerializer.SerializeToUtf8Bytes(source, PartiesJsonOptions.Default);
+        stored.Length.ShouldBeGreaterThan(0);
+        string serialized = System.Text.Encoding.UTF8.GetString(stored);
+        serialized.ShouldNotContain("Private");
+        serialized.ShouldNotContain(nameof(PartyCreated));
+        RetainedIdentityHistoryReadResult restored = JsonSerializer.Deserialize<RetainedIdentityHistoryReadResult>(stored, PartiesJsonOptions.Default)!;
+        IRetainedIdentityHistoryReader restoredReader = Substitute.For<IRetainedIdentityHistoryReader>();
+        restoredReader.ReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(restored);
+        fixture.Authority.Admit(Arg.Any<QueryEnvelope>()).Returns(call =>
+        {
+            string actor = JsonSerializer.Deserialize<ResolveHumanActorBindingAt>(call.Arg<QueryEnvelope>().Payload, PartiesJsonOptions.Default)!.ExpectedActorId;
+            return new PartyIdentityAdmissionResult(new(new("tenant-a", "party", "party-1", "Read", "c", "c", "d"), "reader", null, actor,
+                3, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(1), 1), null);
+        });
+        var restarted = new PartyIdentityQueryService(fixture.Authority, TimeProvider.System, custody: fixture.Custody, historyReader: restoredReader, identityOptions: fixture.Options);
+        HumanActorBindingResult before = await restarted.ResolveAtAsync(Envelope(new ResolveHumanActorBindingAt("tenant-a", "party-1", boundary.AddTicks(-1), Actor, 1)), TestContext.Current.CancellationToken);
+        HumanActorBindingResult at = await restarted.ResolveAtAsync(Envelope(new ResolveHumanActorBindingAt("tenant-a", "party-1", boundary, successor, 2)), TestContext.Current.CancellationToken);
+        before.Outcome.ShouldBe(HumanActorBindingOutcome.Resolved);
+        at.Outcome.ShouldBe(HumanActorBindingOutcome.Resolved);
+        before.Evidence!.ValidUntil.ShouldBe(boundary);
+        before.BindingSourcePosition.ShouldBe(2);
+        at.BindingSourcePosition.ShouldBe(3);
+        at.SourcePosition.ShouldBe(4);
+    }
+
+    [Theory]
+    [InlineData("gap")]
+    [InlineData("overlap")]
+    [InlineData("profile-substitution")]
+    [InlineData("wrong-purpose")]
+    [InlineData("foreign-binding")]
+    [InlineData("transit-expiry")]
+    [InlineData("missing-authority")]
+    [InlineData("future-observation")]
+    [InlineData("short-name-substitution")]
+    [InlineData("assembly-qualified-substitution")]
+    [InlineData("namespace-alias-substitution")]
+    public async Task RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(string corruption)
+    {
+        var fixture = Service(Events());
+        RetainedIdentityHistoryReadResult original = await fixture.HistoryReader.ReadAsync(new("tenant-a", "party", "party-1"),
+            RetainedIdentityHistoryReadRequest.AttributionPurpose, TestContext.Current.CancellationToken);
+        RetainedIdentityHistoryStream stream = original.Stream!;
+        stream = corruption switch
+        {
+            "gap" => stream with { ExcludedSequences = [] },
+            "overlap" => stream with { ExcludedSequences = [2] },
+            "wrong-purpose" => stream with { Purpose = "party-profile" },
+            "transit-expiry" => stream with { ValidUntil = DateTimeOffset.UtcNow.AddTicks(-1) },
+            "missing-authority" => stream with { AuthorityRevision = null },
+            "future-observation" => stream with { ObservedAt = DateTimeOffset.UtcNow.AddMinutes(1) },
+            "short-name-substitution" => stream with { Events = [stream.Events[0] with { EventTypeName = nameof(HumanActorBindingEstablished) }] },
+            "assembly-qualified-substitution" => stream with { Events = [stream.Events[0] with { EventTypeName = typeof(HumanActorBindingEstablished).AssemblyQualifiedName! }] },
+            "namespace-alias-substitution" => stream with { Events = [stream.Events[0] with { EventTypeName = "Legacy.Parties.Contracts.Events.HumanActorBindingEstablished" }] },
+            "profile-substitution" => stream with { Events = [stream.Events[0] with
+                { EventTypeName = typeof(PartyCreated).FullName!, Payload = JsonSerializer.SerializeToUtf8Bytes(Events()[0], PartiesJsonOptions.Default) }] },
+            _ => stream with { Events = [stream.Events[0] with { Payload = JsonSerializer.SerializeToUtf8Bytes(
+                new HumanActorBindingEstablished(new(Binding() with { TenantId = "tenant-b" }, "logical", "digest"), Start, 0), PartiesJsonOptions.Default) }] },
+        };
+        fixture.HistoryReader.ReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
+            .Returns(new RetainedIdentityHistoryReadResult(stream, null));
+        HumanActorBindingResult result = await fixture.Service.ResolveAtAsync(Envelope(new ResolveHumanActorBindingAt("tenant-a", "party-1", Start, Actor, 1)), TestContext.Current.CancellationToken);
+        result.Outcome.ShouldBe(HumanActorBindingOutcome.Unavailable);
+        result.Evidence.ShouldBeNull();
+        result.BindingSourcePosition.ShouldBe(0);
+        await fixture.Reader.DidNotReceiveWithAnyArgs().ReadAsync(default!, default);
+        await fixture.Custody.DidNotReceiveWithAnyArgs().CanReadAsync(default!, default!, default);
+    }
+
+    [Fact]
+    public async Task MissingRetainedReaderOrExpiredCustody_CannotFallBackToAvailableFullProfile()
+    {
+        var fixture = Service(Events());
+        var missing = new PartyIdentityQueryService(fixture.Authority, TimeProvider.System, fixture.Reader, fixture.Custody, identityOptions: fixture.Options);
+        var query = new ResolveHumanActorBindingAt("tenant-a", "party-1", Start, Actor, 1);
+        (await missing.ResolveAtAsync(Envelope(query), TestContext.Current.CancellationToken)).Outcome.ShouldBe(HumanActorBindingOutcome.Unavailable);
+        fixture.Custody.CanReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<IdentityHistoryCustodyEvidence>(), Arg.Any<CancellationToken>()).Returns(false);
+        HumanActorBindingResult expired = await fixture.Service.ResolveAtAsync(Envelope(query), TestContext.Current.CancellationToken);
+        expired.Outcome.ShouldBe(HumanActorBindingOutcome.Expired);
+        expired.Evidence.ShouldBeNull();
+        await fixture.Reader.DidNotReceiveWithAnyArgs().ReadAsync(default!, default);
+    }
+
+    [Fact]
+    public async Task ProvisionedOrganization_KeepsBranchBClassificationAndNeverCarriesHumanBinding()
+    {
+        var intent = new AgentPartyIdentity("tenant-a", Actor, "party-1", 1, "provision", new string('a', 64), Start);
+        var fixture = Service(new PartyCreated { Type = PartyType.Organization, CreatedAt = Start,
+                OrganizationDetails = new OrganizationDetails { LegalName = "Agent" } },
+            new AgentPartyProvisioned(new(intent, 2, "provisioner")));
+        fixture.Options.CurrentValue.Returns(new PartyIdentityOptions());
+        PartyIdentityResult result = await fixture.Service.ResolveAsync(Envelope(new ResolvePartyIdentity("tenant-a", "party-1")), TestContext.Current.CancellationToken);
+        result.Outcome.ShouldBe(PartyIdentityOutcome.Resolved);
+        result.Evidence!.Classification.ShouldBe(PartyIdentityClassification.Organization);
+        result.Evidence.HumanBinding.ShouldBeNull();
+        await fixture.Custody.DidNotReceiveWithAnyArgs().CanReadAsync(default!, default!, default);
+    }
+
+    [Theory]
+    [InlineData(true)]
+    [InlineData(false)]
+    public async Task InactiveOrRestrictedHuman_ReturnsIneligibleWithoutUsableBinding(bool inactive)
+    {
+        IEventPayload transition = inactive ? new PartyDeactivated()
+            : new ProcessingRestricted { TenantId = "tenant-a", PartyId = "party-1", RestrictedAt = Start };
+        var fixture = Service([.. Events(), transition]);
+        PartyIdentityResult result = await fixture.Service.ResolveAsync(Envelope(new ResolvePartyIdentity("tenant-a", "party-1", Actor)), TestContext.Current.CancellationToken);
+        result.Outcome.ShouldBe(PartyIdentityOutcome.Ineligible);
+        result.Evidence!.HumanBinding.ShouldBeNull();
+        await fixture.Custody.DidNotReceiveWithAnyArgs().CanReadAsync(default!, default!, default);
     }
 
     [Fact]
@@ -67,7 +289,8 @@ public sealed class PartyIdentityQueryHandlerTests
     {
         DateTimeOffset boundary = Start.AddHours(1);
         string successor = "01HX0000000000000000000002";
-        HumanActorBindingEvidence second = Binding() with { ActorId = successor, BindingVersion = 2, ValidFrom = boundary };
+        HumanActorBindingEvidence second = Binding() with { ActorId = successor, BindingVersion = 2, ValidFrom = boundary, ValidUntil = boundary.AddDays(10),
+            Custody = Binding().Custody with { ExpiresAt = boundary.AddDays(10) } };
         var fixture = Service(Events().Append(new HumanActorBindingRebound(new(second, "rebind", "digest-two"), boundary, 1)).ToArray());
         fixture.Authority.Admit(Arg.Any<QueryEnvelope>()).Returns(call =>
         {
@@ -93,6 +316,9 @@ public sealed class PartyIdentityQueryHandlerTests
         PartyIdentityResult current = await fixture.Service.ResolveAsync(Envelope(new ResolvePartyIdentity("tenant-a", "party-1")), TestContext.Current.CancellationToken);
         current.Outcome.ShouldBe(PartyIdentityOutcome.Unavailable);
         current.Evidence.ShouldBeNull();
+        fixture.HistoryReader.ReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
+            .Returns(new RetainedIdentityHistoryReadResult(new(new("tenant-b", "party", "foreign-party"), RetainedIdentityHistoryReadRequest.AttributionPurpose,
+                0, DateTimeOffset.UtcNow, [], [], "history-observation"), null));
         HumanActorBindingResult history = await fixture.Service.ResolveAtAsync(Envelope(new ResolveHumanActorBindingAt("tenant-a", "party-1", Start, Actor, 1)), TestContext.Current.CancellationToken);
         history.Outcome.ShouldBe(HumanActorBindingOutcome.Unavailable);
         history.Evidence.ShouldBeNull();
@@ -171,7 +397,8 @@ public sealed class PartyIdentityQueryHandlerTests
         (await fixture.Service.ResolveAtAsync(Envelope(query), TestContext.Current.CancellationToken)).Outcome.ShouldBe(HumanActorBindingOutcome.Unavailable);
         using var canceled = new CancellationTokenSource();
         await canceled.CancelAsync();
-        fixture.Reader.ReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<CancellationToken>()).Returns(call => Task.FromCanceled<AuthoritativeStreamReadResult>(call.Arg<CancellationToken>()));
+        fixture.HistoryReader.ReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
+            .Returns(call => Task.FromCanceled<RetainedIdentityHistoryReadResult>(call.Arg<CancellationToken>()));
         await Should.ThrowAsync<OperationCanceledException>(() => fixture.Service.ResolveAtAsync(Envelope(query), canceled.Token));
     }
 
@@ -185,4 +412,306 @@ public sealed class PartyIdentityQueryHandlerTests
             (await Service(events).Service.ResolveAsync(Envelope(new ResolvePartyIdentity("tenant-a", "party-1", Actor)), TestContext.Current.CancellationToken)).Outcome.ShouldNotBe(PartyIdentityOutcome.Resolved);
         }
     }
+
+    /// <summary>Verifies pre cancelled query  does not consult authority or readers.</summary>
+    [Theory]
+    [InlineData(false)]
+    [InlineData(true)]
+    public async Task PreCancelledQuery_DoesNotConsultAuthorityOrReaders(bool historical)
+    {
+        var fixture = Service(Events());
+        using var caller = new CancellationTokenSource();
+        await caller.CancelAsync();
+        await Should.ThrowAsync<OperationCanceledException>(() => InvokeIdentityAsync(fixture.Service, historical, caller.Token));
+        fixture.Authority.DidNotReceiveWithAnyArgs().Admit(default(QueryEnvelope)!);
+        await fixture.Reader.DidNotReceiveWithAnyArgs().ReadAsync(default!, default);
+        await fixture.HistoryReader.DidNotReceiveWithAnyArgs().ReadAsync(default!, default!, default);
+        await fixture.Custody.DidNotReceiveWithAnyArgs().CanReadAsync(default!, default!, default);
+    }
+
+    /// <summary>Verifies reader cancels then returns valid source  does not release identity.</summary>
+    [Theory]
+    [InlineData(false)]
+    [InlineData(true)]
+    public async Task ReaderCancelsThenReturnsValidSource_DoesNotReleaseIdentity(bool historical)
+    {
+        var fixture = Service(Events());
+        using var caller = new CancellationTokenSource();
+        if (historical)
+        {
+            RetainedIdentityHistoryReadResult valid = await fixture.HistoryReader.ReadAsync(new("tenant-a", "party", "party-1"), RetainedIdentityHistoryReadRequest.AttributionPurpose, TestContext.Current.CancellationToken);
+            fixture.HistoryReader.ReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
+                .Returns(_ => { caller.Cancel(); return Task.FromResult(valid); });
+        }
+        else
+        {
+            AuthoritativeStreamReadResult valid = await fixture.Reader.ReadAsync(new("tenant-a", "party", "party-1"), TestContext.Current.CancellationToken);
+            fixture.Reader.ReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<CancellationToken>())
+                .Returns(_ => { caller.Cancel(); return Task.FromResult(valid); });
+        }
+
+        await Should.ThrowAsync<OperationCanceledException>(() => InvokeIdentityAsync(fixture.Service, historical, caller.Token));
+        await fixture.Custody.DidNotReceiveWithAnyArgs().CanReadAsync(default!, default!, default);
+    }
+
+    /// <summary>Verifies non cooperative reader or custody  caller can cancel outstanding read.</summary>
+    [Theory]
+    [InlineData(false, false)]
+    [InlineData(true, false)]
+    [InlineData(false, true)]
+    [InlineData(true, true)]
+    public async Task NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead(bool historical, bool inCustody)
+    {
+        var fixture = Service(Events());
+        using var caller = new CancellationTokenSource();
+        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
+        if (inCustody)
+        {
+            var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
+            fixture.Custody.CanReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<IdentityHistoryCustodyEvidence>(), Arg.Any<CancellationToken>())
+                .Returns(_ => { entered.TrySetResult(); return pending.Task; });
+        }
+        else if (historical)
+        {
+            var pending = new TaskCompletionSource<RetainedIdentityHistoryReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
+            fixture.HistoryReader.ReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
+                .Returns(_ => { entered.TrySetResult(); return pending.Task; });
+        }
+        else
+        {
+            var pending = new TaskCompletionSource<AuthoritativeStreamReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
+            fixture.Reader.ReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<CancellationToken>())
+                .Returns(_ => { entered.TrySetResult(); return pending.Task; });
+        }
+
+        Task result = InvokeIdentityAsync(fixture.Service, historical, caller.Token);
+        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
+        await caller.CancelAsync();
+        await Should.ThrowAsync<OperationCanceledException>(() => result.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
+    }
+
+    /// <summary>Verifies custody cancels then returns  does not consult final authority or release identity.</summary>
+    [Theory]
+    [InlineData(false, false)]
+    [InlineData(true, false)]
+    [InlineData(false, true)]
+    [InlineData(true, true)]
+    public async Task CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity(bool historical, bool canRead)
+    {
+        var fixture = Service(Events());
+        using var caller = new CancellationTokenSource();
+        fixture.Custody.CanReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<IdentityHistoryCustodyEvidence>(), Arg.Any<CancellationToken>())
+            .Returns(_ => { caller.Cancel(); return Task.FromResult(canRead); });
+        await Should.ThrowAsync<OperationCanceledException>(() => InvokeIdentityAsync(fixture.Service, historical, caller.Token));
+        fixture.Authority.Received(historical ? 2 : 1).Admit(Arg.Any<QueryEnvelope>());
+    }
+
+    /// <summary>Verifies final authority cancels then returns valid grant  does not release identity.</summary>
+    [Theory]
+    [InlineData(false)]
+    [InlineData(true)]
+    public async Task FinalAuthorityCancelsThenReturnsValidGrant_DoesNotReleaseIdentity(bool historical)
+    {
+        var fixture = Service(Events());
+        using var caller = new CancellationTokenSource();
+        PartyIdentityAdmissionResult grant = fixture.Authority.Admit(Envelope(new ResolvePartyIdentity("tenant-a", "party-1", Actor)));
+        int calls = 0;
+        fixture.Authority.Admit(Arg.Any<QueryEnvelope>()).Returns(_ =>
+        {
+            if (++calls == (historical ? 3 : 2))
+            {
+                caller.Cancel();
+            }
+
+            return grant;
+        });
+        await Should.ThrowAsync<OperationCanceledException>(() => InvokeIdentityAsync(fixture.Service, historical, caller.Token));
+    }
+
+    /// <summary>Missing or unsupported policy cannot release identity even when custody reports readable.</summary>
+    [Theory]
+    [InlineData("unregistered")]
+    [InlineData("missing-id")]
+    [InlineData("blank-id")]
+    [InlineData("missing-duration")]
+    [InlineData("zero-duration")]
+    [InlineData("missing-trigger")]
+    [InlineData("unsupported-trigger")]
+    public async Task UnconfiguredPolicy_DeniesBothBindingReads(string missing)
+    {
+        var fixture = Service(Events());
+        PartyIdentityOptions policy = PolicyOptions();
+        switch (missing)
+        {
+            case "missing-id": policy.PolicyId = null; break;
+            case "blank-id": policy.PolicyId = " "; break;
+            case "missing-duration": policy.Retention = null; break;
+            case "zero-duration": policy.Retention = TimeSpan.Zero; break;
+            case "missing-trigger": policy.ExpiryTrigger = null; break;
+            case "unsupported-trigger": policy.ExpiryTrigger = "party-erased-at"; break;
+        }
+
+        fixture.Options.CurrentValue.Returns(policy);
+        var service = new PartyIdentityQueryService(fixture.Authority, TimeProvider.System, fixture.Reader,
+            fixture.Custody, fixture.HistoryReader, missing == "unregistered" ? null : fixture.Options);
+        await AssertUnavailableAsync(service, historical: true);
+        await fixture.HistoryReader.DidNotReceiveWithAnyArgs().ReadAsync(default!, default!, default);
+        await fixture.Reader.DidNotReceiveWithAnyArgs().ReadAsync(default!, default);
+        await AssertUnavailableAsync(service, historical: false);
+        await fixture.Custody.DidNotReceiveWithAnyArgs().CanReadAsync(default!, default!, default);
+    }
+
+    /// <summary>An accepted custody response does not substitute for exact policy and expiry matching.</summary>
+    [Theory]
+    [InlineData("policy-version")]
+    [InlineData("duration")]
+    [InlineData("recorded-expiry")]
+    [InlineData("purpose")]
+    [InlineData("source-expiry")]
+    [InlineData("restore")]
+    [InlineData("derived-copies")]
+    public async Task MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(string mismatch)
+    {
+        IdentityHistoryCustodyEvidence custody = Binding().Custody;
+        custody = mismatch switch
+        {
+            "recorded-expiry" => custody with { ExpiresAt = custody.ExpiresAt.AddSeconds(1) },
+            "purpose" => custody with { Purpose = "party-profile" },
+            "source-expiry" => custody with { SourceExpiryEnforced = false },
+            "restore" => custody with { RestoreSafe = false },
+            "derived-copies" => custody with { DerivedCopiesCovered = false },
+            _ => custody,
+        };
+        var fixture = Service(Events()[0], new HumanActorBindingEstablished(
+            new(Binding() with { Custody = custody }, "logical", "digest"), Start, 0));
+        PartyIdentityOptions policy = PolicyOptions();
+        if (mismatch == "policy-version")
+        {
+            policy.PolicyId = "synthetic-v2";
+        }
+        else if (mismatch == "duration")
+        {
+            policy.Retention = TimeSpan.FromDays(11);
+        }
+
+        fixture.Options.CurrentValue.Returns(policy);
+        await AssertUnavailableAsync(fixture.Service, historical: true);
+        await AssertUnavailableAsync(fixture.Service, historical: false);
+        await fixture.Custody.DidNotReceiveWithAnyArgs().CanReadAsync(default!, default!, default);
+    }
+
+    /// <summary>Actual suspended reads cannot release identity after policy withdrawal or a changed duration.</summary>
+    [Theory]
+    [InlineData(false, false, false)]
+    [InlineData(true, false, false)]
+    [InlineData(false, true, false)]
+    [InlineData(true, true, false)]
+    [InlineData(false, false, true)]
+    [InlineData(true, false, true)]
+    [InlineData(false, true, true)]
+    [InlineData(true, true, true)]
+    public async Task PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(bool historical, bool inCustody, bool remove)
+    {
+        var fixture = Service(Events());
+        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
+        Action complete;
+        if (inCustody)
+        {
+            var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
+            fixture.Custody.CanReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<IdentityHistoryCustodyEvidence>(), Arg.Any<CancellationToken>())
+                .Returns(_ => { entered.TrySetResult(); return pending.Task; });
+            complete = () => pending.SetResult(true);
+        }
+        else if (historical)
+        {
+            RetainedIdentityHistoryReadResult valid = await fixture.HistoryReader.ReadAsync(new("tenant-a", "party", "party-1"),
+                RetainedIdentityHistoryReadRequest.AttributionPurpose, TestContext.Current.CancellationToken);
+            var pending = new TaskCompletionSource<RetainedIdentityHistoryReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
+            fixture.HistoryReader.ReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
+                .Returns(_ => { entered.TrySetResult(); return pending.Task; });
+            complete = () => pending.SetResult(valid);
+        }
+        else
+        {
+            AuthoritativeStreamReadResult valid = await fixture.Reader.ReadAsync(new("tenant-a", "party", "party-1"), TestContext.Current.CancellationToken);
+            var pending = new TaskCompletionSource<AuthoritativeStreamReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
+            fixture.Reader.ReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<CancellationToken>())
+                .Returns(_ => { entered.TrySetResult(); return pending.Task; });
+            complete = () => pending.SetResult(valid);
+        }
+
+        Task result = AssertUnavailableAsync(fixture.Service, historical);
+        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
+        WithdrawOrChangePolicy(fixture.Options, remove);
+        complete();
+        await result.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
+        Binding().Custody.ExpiresAt.ShouldBe(Start.AddDays(10));
+        if (!inCustody)
+        {
+            await fixture.Custody.DidNotReceiveWithAnyArgs().CanReadAsync(default!, default!, default);
+        }
+    }
+
+    /// <summary>The final authority check cannot withdraw policy and still release an otherwise valid binding.</summary>
+    [Theory]
+    [InlineData(false, false)]
+    [InlineData(true, false)]
+    [InlineData(false, true)]
+    [InlineData(true, true)]
+    public async Task PolicyChangesAtFinalAuthority_DenyBothReads(bool historical, bool remove)
+    {
+        var fixture = Service(Events());
+        PartyIdentityAdmissionResult grant = fixture.Authority.Admit(Envelope(new ResolvePartyIdentity("tenant-a", "party-1", Actor)));
+        int calls = 0;
+        fixture.Authority.Admit(Arg.Any<QueryEnvelope>()).Returns(_ =>
+        {
+            if (++calls == (historical ? 3 : 2))
+            {
+                WithdrawOrChangePolicy(fixture.Options, remove);
+            }
+
+            return grant;
+        });
+        await AssertUnavailableAsync(fixture.Service, historical);
+    }
+
+    private static void WithdrawOrChangePolicy(IOptionsMonitor<PartyIdentityOptions> options, bool remove)
+    {
+        PartyIdentityOptions replacement = remove ? new PartyIdentityOptions() : PolicyOptions();
+        if (!remove)
+        {
+            replacement.Retention = TimeSpan.FromDays(11);
+        }
+
+        options.CurrentValue.Returns(replacement);
+    }
+
+    private static async Task AssertUnavailableAsync(PartyIdentityQueryService service, bool historical)
+    {
+        if (historical)
+        {
+            HumanActorBindingResult result = await service.ResolveAtAsync(Envelope(new ResolveHumanActorBindingAt("tenant-a", "party-1", Start, Actor, 1)), TestContext.Current.CancellationToken).ConfigureAwait(false);
+            result.Outcome.ShouldBe(HumanActorBindingOutcome.Unavailable);
+            result.Evidence.ShouldBeNull();
+            result.BindingSourcePosition.ShouldBe(0);
+        }
+        else
+        {
+            PartyIdentityResult result = await service.ResolveAsync(Envelope(new ResolvePartyIdentity("tenant-a", "party-1", Actor)), TestContext.Current.CancellationToken).ConfigureAwait(false);
+            result.Outcome.ShouldBe(PartyIdentityOutcome.Unavailable);
+            result.Evidence.ShouldBeNull();
+        }
+    }
+
+    private static PartyIdentityOptions PolicyOptions() => new()
+    {
+        PolicyId = "synthetic",
+        Retention = TimeSpan.FromDays(10),
+        ExpiryTrigger = "binding-effective-at",
+    };
+
+    private static Task InvokeIdentityAsync(PartyIdentityQueryService service, bool historical, CancellationToken token)
+        => historical
+            ? service.ResolveAtAsync(Envelope(new ResolveHumanActorBindingAt("tenant-a", "party-1", Start, Actor, 1)), token)
+            : service.ResolveAsync(Envelope(new ResolvePartyIdentity("tenant-a", "party-1", Actor)), token);
 }
diff --git a/parties/tests/Hexalith.Parties.Tests/Hexalith.Parties.Tests.csproj b/parties/tests/Hexalith.Parties.Tests/Hexalith.Parties.Tests.csproj
index 5ce6a368..047bb2f7 100644
--- a/parties/tests/Hexalith.Parties.Tests/Hexalith.Parties.Tests.csproj
+++ b/parties/tests/Hexalith.Parties.Tests/Hexalith.Parties.Tests.csproj
@@ -47,4 +47,8 @@
   <ItemGroup>
     <Using Include="Xunit" />
   </ItemGroup>
+
+  <ItemGroup>
+    <None Include="../../src/Hexalith.Parties/appsettings.json" Link="Configuration/parties-appsettings.json" CopyToOutputDirectory="PreserveNewest" />
+  </ItemGroup>
 </Project>

diff --git a/parties/_bmad-output/implementation-artifacts/ext-parties-1-owner-prerequisites-2026-10-06.md b/parties/_bmad-output/implementation-artifacts/ext-parties-1-owner-prerequisites-2026-10-06.md
new file mode 100644
index 00000000..ba7b2560
--- /dev/null
+++ b/parties/_bmad-output/implementation-artifacts/ext-parties-1-owner-prerequisites-2026-10-06.md
@@ -0,0 +1,67 @@
+# EXT-PARTIES-1 owner prerequisite implementation — 2026-10-06
+
+This is an uncommitted owner implementation candidate for Story 5.4. It does not accept the dependency, an immutable launch target, an integration date or a compatibility command. The authoritative Agents register remains `Uncommitted` with the three fields `TBD`. No unavailable consuming seam or authenticated live endpoint was called.
+
+Observed base HEADs are Parties `b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca`, EventStore `785d58fc99ce4c2751c0546ca6368e993c654ed9`, and Platform `711a70fd94794ef1aea1b91f5524136b0d660468`. The tested sibling source trees contain owner changes beyond those commits. These observations are not accepted targets. Existing identity work at Parties commit `37d87f5a2869b076c651a58714d60f64647848bc` was reused.
+
+## Implemented behavior
+
+`PartyIdentityQueryService.ResolveAtAsync` now reads only the independent `IRetainedIdentityHistoryReader` attribution source. A destroyed profile key or unavailable current profile cannot force a historical read through the full profile or substitute the current actor. The exact tenant/Party/purpose certificate must cover a disjoint complete partition from original position 1 through the sampled head; sparse positions are never renumbered. Current observation time, `AuthorityRevision` and exclusive `ValidUntil` are required. The service rechecks query authority and certificate/custody expiry after awaited work.
+
+`RetainedHumanActorHistoryFold.Fold` accepts exactly the three established/rebound/revoked full contract names. It preserves finite non-overlapping intervals, original opening positions, actor/version/provenance and purpose custody evidence. Unknown, profile, simple-name, assembly-qualified and namespace-alias events fail closed. `HumanActorBindingResult.BindingSourcePosition` is additive and keeps the existing constructor/deconstruction signature; the narrow HTTP client requires it to be inside the source checkpoint on successful history replies.
+
+Current identity keeps the Branch B Organization classification for the provisioned Agent Party. Inactive or restricted humans return no usable human binding, and current reads recheck authority and finite binding/custody expiry before success. The pre-existing classification enum member remains part of the wire API, but this Branch B client does not accept it as an eligible classification.
+
+The owner host must provide `IRetainedIdentityHistoryReader`, trusted `IPartyIdentityAuthority`, and an actual `IIdentityHistoryCustody`. Missing history/custody remains unavailable. EventStore's new source reader and contracts are sibling-owner changes, not Parties implementations of a custody backend.
+
+## Executed source verification
+
+All three final focused Debug builds used explicit sibling project references, `--artifacts-path /tmp/hexalith-agents54-parties-artifacts`, `-m:1`, and succeeded with 0 warnings and 0 errors. Each test command followed a successful build under `set -e`.
+
+| Final lane | Executed | Passed | Failed / skipped |
+| --- | ---: | ---: | ---: |
+| Parties domain/admission | 38 | 38 | 0 / 0 |
+| Parties client/DI | 18 | 18 | 0 / 0 |
+| Parties identity contracts/API snapshot | 3 | 3 | 0 / 0 |
+| Total | 59 | 59 | 0 / 0 |
+
+The final fixture coverage includes erased-profile/current-history separation, original positions, serialized retained-source reconstruction in a fresh service, exact interval boundaries, gaps/overlap, actor/version mismatch, malformed source/purpose and all alias substitutions, future observation, missing source authority, certificate expiry in transit, custody expiry crossing an await, read-authority revocation/substitution, missing reader and cancellation. The serialized representation and fresh domain service are local fixtures; they are not Dapr persistence, process restart, backup restore, destruction or live erasure qualification.
+
+The exact project commands, XML/log paths, checksums and changed-source hashes are in [the separate source evidence JSON](tests/ext-parties-1-owner-source-evidence-2026-10-06.json). Main final logs have prefixes `/tmp/parties-story54-retained-domain-final3`, `/tmp/parties-story54-retained-client-final`, and `/tmp/parties-story54-retained-contracts-final2` followed by `-build-20261006.log` or `-tests-20261006.log`/`.xml`.
+
+## Executable verifier evidence and blocker
+
+The Local verifier now uses isolated artifacts, requires a successful fresh build before tests, validates every requested class was executed, and permits explicit optional `-MemoriesRoot`. Its default uses the packaged optional Memories dependency rather than initializing missing nested submodules. Both PowerShell scripts parse without errors.
+
+Executed from the Parties root:
+
+```sh
+pwsh -NoProfile -File eng/verify-ext-parties-1.ps1 -Mode Local -ArtifactsDirectory /tmp/parties-story54-local-verifier-20261006/artifacts -EvidenceDirectory /tmp/parties-story54-local-verifier-20261006
+```
+
+This command exited 1. Six lanes passed: EventStore contracts 23, client 98, server 137; Platform identity 2; Parties contracts 29 and server 94. Parties domain then executed 107 with 106 passing and one failure. The failing unchanged test is `PartyDomainProcessorValidationTests.ProcessAsync_ProtectedHistoricalPayloadWithDestroyedKey_RedactsAndContinuesRehydration` at `tests/Hexalith.Parties.Tests/Domain/PartyDomainProcessorValidationTests.cs:286`. The sibling strict rehydrator rejects its `json-redacted` serialization format at `DomainProcessorStateRehydrator.RequireSupportedReplayMetadata` (`../eventstore/src/Hexalith.EventStore.Client/Handlers/DomainProcessorStateRehydrator.cs:436`). Existing expected behavior and shared replay enforcement were preserved. This is a separate source compatibility blocker. The fail-fast verifier did not execute its remaining security/client/UI lanes. This Local run preceded the final two alias-negative fixtures; those passed in the final focused domain run above.
+
+`LiveReadiness` performs only owner-configured read probes after the real dependency register requires `Available`, the exact accepted target/date/command, a matching passing compatibility receipt and explicit policy/prerequisite proof. The child process exit is propagated. Its checks use the shared 32 MiB wire / 16 MiB decoded payload / 10,000-position bounds and reject expiry crossed during probes. It does not bypass dependency acceptance or provide full P-01–P-10 qualification. The full `Live` mode still reports its complete missing qualification gate and exits 1.
+
+Executed boundary smoke commands:
+
+```sh
+pwsh -NoProfile -File /tmp/parties-story54-verifier-parser-20261006.ps1
+python3 /tmp/parties-story54-readiness-gates-smoke-20261006.py
+```
+
+The smoke starts a local counting HTTP listener. Missing inputs and complete sentinel inputs against the real `Uncommitted` register both returned wrapper exit 1 with zero requests. The sentinel policy and receipt names exist only as rejected boundary fixtures and do not configure production. A separate `-Mode Live` missing-input run returned exit 1. Gate logs and reproducible smoke source are checksummed in the evidence JSON. `git -c core.whitespace=cr-at-eol diff --check -- eng src tests` passed; all eleven changed implementation/test/verifier files preserve CRLF.
+
+## Required owner/Product inputs and real remaining gaps
+
+No production retention duration or erasure decision was selected. `Parties:Identity:PolicyId`, a positive finite `Parties:Identity:Retention`, and `Parties:Identity:ExpiryTrigger` remain mandatory. The current contract supports only `binding-effective-at`; any different Product-selected trigger needs an explicit contract change. Product and owners must state the retention and the relationship between immediate profile erasure and purpose-limited attribution, including deletion timing for originals, derived copies and backup/restore material.
+
+A real provider must independently protect retained events/snapshot history, authorize the current lifecycle on every read, enforce irreversible expiry/destruction with non-rollback evidence across restart/restore, and cover derived copies and cleanup. HMAC signing or a self-reported evidence flag cannot establish those guarantees. Production admission/custody/retention and the authenticated persisted-state P-01–P-10, restart/restore, failure-injection and cross-tenant installed matrix remain unqualified. The strict profile-replay `json-redacted` compatibility failure above also remains open.
+
+The six pre-existing owner planning/test-summary edits were left untouched. No commits, pushes, submodule updates, owner acceptance changes or external messages were made.
+
+## Cancellation review fix
+
+Current and historical identity reads now reject pre-cancelled calls, cancel outstanding reads even when providers ignore the token, and check again before returning identity evidence. The normal Debug source build passed with zero warnings/errors. The focused domain/admission suite passed 52/52, including 14 new cancellation regressions. Reusing unchanged client 18/18 and contract 3/3 evidence yields 73 focused passing tests. Exact commands, hashes and XML are in the source-evidence JSON. This does not replace the historical broad Local result of 489/490; its unchanged json-redacted strict replay compatibility failure remains unresolved. Retention policy, production custody, expiry cleanup/restore and complete P-01–P-10/live qualification remain unavailable.
+
+Final review also verified that custody cancellation returning false stops before another authority lookup; both current/historical true/false cases pass.

diff --git a/parties/_bmad-output/implementation-artifacts/ext-parties-1-retention-application-2026-10-06.md b/parties/_bmad-output/implementation-artifacts/ext-parties-1-retention-application-2026-10-06.md
new file mode 100644
index 00000000..94e2f3d1
--- /dev/null
+++ b/parties/_bmad-output/implementation-artifacts/ext-parties-1-retention-application-2026-10-06.md
@@ -0,0 +1,21 @@
+# EXT-PARTIES-1 minimal retention application
+
+The user's accepted engineering recommendation is implemented locally. [The proposed policy](../planning-artifacts/actor-history-retention-policy-v1.md) selects one finite, purpose-specific actor-history lifetime on the existing aggregate/SDK seams. It has no approved production duration or provider and installs no runtime defaults.
+
+Current human-binding and historical reads now require explicit valid policy configuration, exact custody policy/purpose and effective-at-derived expiry. They snapshot policy and recheck after source/custody awaits and final authority before releasing bindings. A missing, mismatched or withdrawn policy returns Unavailable. Organization Branch B still resolves without human policy. Actor/version/half-open interval and original source positions remain unchanged under a valid policy.
+
+The rebind fixtures now derive successor expiry from the successor's own effective instant. The fixture's ten-day period is synthetic; it is not a production recommendation or approval. No new persistence, general policy engine, service, provider or erasure-trigger clock was added.
+
+## Verification
+
+Normal Debug source build: **0 warnings, 0 errors**. Focused xUnit v3 query/admission suite: **78 passed, 0 failed, 0 skipped, 0 errors, 0 not run**. The 26 new cases cover missing/unsupported configuration, wrong policy/duration/expiry/purpose/custody flags, actual suspended source/custody reads during policy change and final-authority policy withdrawal. Existing erasure, rebind/revoke, exclusive expiry, original-position and cancellation tests also ran. Both current and historical modes are exercised inside each configuration/mismatch case.
+
+[Exact source/artifact hashes and commands](tests/actor-retention-apply-2026-10-06/source-evidence.json), [build output](tests/actor-retention-apply-2026-10-06/build.log), [test output](tests/actor-retention-apply-2026-10-06/tests.log), [xUnit results](tests/actor-retention-apply-2026-10-06/tests.xml).
+
+The first build caught two CA2007 findings in the new private test helper; the helper was corrected with ConfigureAwait and the normal build rerun. No analyzer or assertion was suppressed. Previous evidence is preserved as earlier snapshots; this packet supersedes its current query-source coverage. The prior broader Local result remains 489/490 with the strict SDK json-redacted replay failure; no broad rerun or waiver is claimed here.
+
+## Remaining qualification
+
+Product/Governance must approve exact post-profile-erasure scope and finite duration. Owners must deliver/qualify independent custody, source and derived-copy cleanup, fresh nonrollback lifecycle and restore behavior. The strict history fold also needs owner proof that complete retained lifecycle remains demonstrable when older predecessors expire. Production activation, full P-01–P-10 acceptance and Story 5.4 remain blocked. One configured policy version is supported; changing configuration cannot extend old records and makes mismatched versions unavailable.
+
+The BMad independent review is separately pending; passing local tests is not a completed review or production acceptance.

diff --git a/parties/_bmad-output/implementation-artifacts/tests/actor-retention-apply-2026-10-06/source-evidence.json b/parties/_bmad-output/implementation-artifacts/tests/actor-retention-apply-2026-10-06/source-evidence.json
new file mode 100644
index 00000000..8a7ddd79
--- /dev/null
+++ b/parties/_bmad-output/implementation-artifacts/tests/actor-retention-apply-2026-10-06/source-evidence.json
@@ -0,0 +1,119 @@
+{
+  "date": "2026-10-06",
+  "scope": "local retention application; production disabled",
+  "baseline_commit": "b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca",
+  "observed_heads": {
+    "agents": "b252fcfde51bd04317ce5843a0a42bfe4dce5170",
+    "parties": "b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca",
+    "eventstore": "253980f9eb63cc5a7d96be7c8676197e78f39d67"
+  },
+  "source_files": [
+    {
+      "path": "/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs",
+      "sha256": "999280447dcd4969e19b62ace1d1f10328fff53d893c2c4dfac6fc93d6ebd928"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/Authorization/PartyIdentityOptions.cs",
+      "sha256": "a67302db5f47ee45fb972ce0a1ae28204f3e153f4ccc5aee6000b4524257e77c"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/Queries/RetainedHumanActorHistoryFold.cs",
+      "sha256": "bfa7ff1402b3641ca91cbc31a2902ea5fa5f8fdf41eb4ad672f786a2256d205f"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/Queries/PartyIdentitySourceFold.cs",
+      "sha256": "14bbb4de36e5378b4811f7f50567b3b19f182e7e4a9f12cc498615cf8ca89c34"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/Extensions/PartiesServiceCollectionExtensions.cs",
+      "sha256": "967c93a1d5acc22222884ebdcc31284de2129ed56c604ce34ee4cbabe4d35769"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs",
+      "sha256": "0bb08d7cebf4e02e8ad284cd8dc274a634d39e83a710bc37a761fd2caff723d7"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/_bmad-output/planning-artifacts/actor-history-retention-policy-v1.md",
+      "sha256": "2a502ed825f110fc73e16e16353ba2ded6bd125a09f25e03f9f7226ae6cab9fe"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/_bmad-output/planning-artifacts/adr-consumer-party-id-binding.md",
+      "sha256": "a313fdc6e88d56643aa119a654d6f545dcb5d69b30441ae4ed8dad4e3a8aacb1"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/_bmad-output/implementation-artifacts/ext-parties-1-retention-application-2026-10-06.md",
+      "sha256": "344697e7370fe57d2c5482ac38675e37032141b0b8327d2a23bb595058e6810c"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/eventstore/src/Hexalith.EventStore.Contracts/Security/IdentityHistoryPolicy.cs",
+      "sha256": "d32c3ba4c1724b29b918793e0f566f1d2905d6268af5ebccf62476e99721df27"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/eventstore/src/Hexalith.EventStore.Contracts/Security/IdentityHistoryCustodyEvidence.cs",
+      "sha256": "299291c4c38f8c6c089d36529517a0f3946bd375a41ea8594a0ed150375b2b70"
+    }
+  ],
+  "artifacts": [
+    {
+      "path": "/home/administrator/projects/hexalith/parties/_bmad-output/implementation-artifacts/tests/actor-retention-apply-2026-10-06/build.log",
+      "sha256": "c7a46da8103dfc36aef8ad7248f9d0d7979dcfb37f694ddec20599fbfde5ffd8"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/_bmad-output/implementation-artifacts/tests/actor-retention-apply-2026-10-06/tests.log",
+      "sha256": "884d93cf838be82e3ce6e64955ecf5482f89360a2d8171b0a26e738fd3176a93"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/_bmad-output/implementation-artifacts/tests/actor-retention-apply-2026-10-06/tests.xml",
+      "sha256": "71068a2ccb68c7ec829db84d52dfb76b6b1601a4f8b31445ea5963c084c8d19a"
+    },
+    {
+      "path": "/tmp/hexalith-agents54-retention-artifacts/bin/Hexalith.Parties.Tests/debug/Hexalith.Parties.Tests.dll",
+      "sha256": "8a83782c4ab61d505655e1419241b954f6b3de4010bb1af788b3cc03db23ea5a"
+    }
+  ],
+  "commands": [
+    "dotnet build tests/Hexalith.Parties.Tests/Hexalith.Parties.Tests.csproj -c Debug --artifacts-path /tmp/hexalith-agents54-retention-artifacts -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/eventstore -p:HexalithCommonsRoot=/home/administrator/projects/hexalith/parties/references/Hexalith.Commons -p:HexalithMemoriesRoot=/tmp/parties-no-memories-source -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -m:1",
+    "dotnet /tmp/hexalith-agents54-retention-artifacts/bin/Hexalith.Parties.Tests/debug/Hexalith.Parties.Tests.dll -class '*PartyIdentityQueryHandlerTests' -class '*PartyIdentityAdmissionTests' -result-xml /tmp/parties-story54-retention-tests-20261006.xml"
+  ],
+  "test_result": {
+    "total": 78,
+    "passed": 78,
+    "failed": 0,
+    "skipped": 0,
+    "errors": 0,
+    "not_run": 0,
+    "new_policy_cases": 26,
+    "new_tests": [
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: False, inCustody: False, remove: False)",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: True, inCustody: False, remove: False)",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: False, inCustody: True, remove: False)",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: True, inCustody: True, remove: False)",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: False, inCustody: False, remove: True)",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: True, inCustody: False, remove: True)",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: False, inCustody: True, remove: True)",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: True, inCustody: True, remove: True)",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \\\"unregistered\\\")",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \\\"missing-id\\\")",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \\\"blank-id\\\")",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \\\"missing-duration\\\")",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \\\"zero-duration\\\")",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \\\"missing-trigger\\\")",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \\\"unsupported-trigger\\\")",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesAtFinalAuthority_DenyBothReads(historical: False, remove: False)",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesAtFinalAuthority_DenyBothReads(historical: True, remove: False)",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesAtFinalAuthority_DenyBothReads(historical: False, remove: True)",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesAtFinalAuthority_DenyBothReads(historical: True, remove: True)",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \\\"policy-version\\\")",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \\\"duration\\\")",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \\\"recorded-expiry\\\")",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \\\"purpose\\\")",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \\\"source-expiry\\\")",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \\\"restore\\\")",
+      "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \\\"derived-copies\\\")"
+    ]
+  },
+  "production_policy_approved": false,
+  "live_qualified": false,
+  "independent_review_completed": false,
+  "prior_broad_result": "489/490; unchanged strict json-redacted SDK replay failure; not rerun for this slice"
+}

diff --git a/parties/_bmad-output/implementation-artifacts/tests/actor-retention-apply-2026-10-06/tests.xml b/parties/_bmad-output/implementation-artifacts/tests/actor-retention-apply-2026-10-06/tests.xml
new file mode 100644
index 00000000..13908168
--- /dev/null
+++ b/parties/_bmad-output/implementation-artifacts/tests/actor-retention-apply-2026-10-06/tests.xml
@@ -0,0 +1 @@
+<assemblies schema-version="3" id="44f3448a-4bad-4709-9492-f74a3cfd8374" computer="DESKTOP-VIOG240" user="administrator" start-rtf="2026-10-06T18:43:58.2072212+00:00" finish-rtf="2026-10-06T18:43:58.6349636+00:00" timestamp="10/06/2026 20:43:58"><assembly environment="64-bit (x64) .NET 10.0.12 [collection-per-class, parallel (collections, 14 threads)]" id="c530e4d6-bf4c-4249-a1d3-69420edff072" name="/tmp/hexalith-agents54-retention-artifacts/bin/Hexalith.Parties.Tests/debug/Hexalith.Parties.Tests.dll" run-date="2026-10-06" run-time="18:43:58" start-rtf="2026-10-06T18:43:58.2072212+00:00" test-framework="xUnit.net v3 4.0.1+8ed8aa354c" target-framework=".NETCoreApp,Version=v10.0" errors="0" failed="0" finish-rtf="2026-10-06T18:43:58.6349636+00:00" not-run="0" passed="78" skipped="0" time="0.428" time-rtf="00:00:00.4280000" total="78"><collection name="Test collection for Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests (id: 888973c97a01ea9dc810b716c23f8fb67bf522b21d9ff81f71e9f48acab88488)" id="a0a64598-ec0b-43d3-aa51-31907248fb1d" failed="0" not-run="0" passed="75" skipped="0" time="0.257" time-rtf="00:00:00.2570000" total="75"><test id="ce5f7caf-8236-44c8-b631-5b4fdb4996a1" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: False, inCustody: False, remove: False)" result="Pass" time="0.161" time-rtf="00:00:00.1610000" start-rtf="2026-10-06T18:43:58.3112152+00:00" finish-rtf="2026-10-06T18:43:58.4832270+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="604" /><test id="884f30ab-9b37-43c0-8348-86c8157acb55" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: True, inCustody: False, remove: False)" result="Pass" time="0.007" time-rtf="00:00:00.0070000" start-rtf="2026-10-06T18:43:58.4836829+00:00" finish-rtf="2026-10-06T18:43:58.4910353+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="604" /><test id="24cde70a-df6b-495e-a17c-9bc1f1db1fe3" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: False, inCustody: True, remove: False)" result="Pass" time="0.002" time-rtf="00:00:00.0020000" start-rtf="2026-10-06T18:43:58.4910922+00:00" finish-rtf="2026-10-06T18:43:58.4931807+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="604" /><test id="6e0928a7-d627-4b38-b44b-8135d4876824" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: True, inCustody: True, remove: False)" result="Pass" time="0.005" time-rtf="00:00:00.0050000" start-rtf="2026-10-06T18:43:58.4932814+00:00" finish-rtf="2026-10-06T18:43:58.4991618+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="604" /><test id="fd2d8e21-8f47-4aa6-bdc4-1478eb3857c8" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: False, inCustody: False, remove: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.4992096+00:00" finish-rtf="2026-10-06T18:43:58.4998840+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="604" /><test id="cd61c450-273a-46f4-95eb-f9d5326229a8" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: True, inCustody: False, remove: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.4999554+00:00" finish-rtf="2026-10-06T18:43:58.5006011+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="604" /><test id="1d8c9450-6151-4bab-bc52-3477f021faf5" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: False, inCustody: True, remove: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5006507+00:00" finish-rtf="2026-10-06T18:43:58.5013360+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="604" /><test id="af855704-22d6-4ea9-915f-7a9fdd4b2abe" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: True, inCustody: True, remove: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5013919+00:00" finish-rtf="2026-10-06T18:43:58.5019152+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="604" /><test id="85268b75-8970-4450-aea8-15677e22c363" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CurrentHuman_RequiresCompleteSourceAndCurrentExactActor" result="Pass" time="0.013" time-rtf="00:00:00.0130000" start-rtf="2026-10-06T18:43:58.5045950+00:00" finish-rtf="2026-10-06T18:43:58.5179061+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="CurrentHuman_RequiresCompleteSourceAndCurrentExactActor" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="264" /><test id="7641fba9-7df2-4dde-b7ba-bbfb799426d5" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.NonFiniteOrBeyondCustodyHistory_DeniesWithNoEvidence(missingEnd: True)" result="Pass" time="0.002" time-rtf="00:00:00.0020000" start-rtf="2026-10-06T18:43:58.5185304+00:00" finish-rtf="2026-10-06T18:43:58.5209033+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="NonFiniteOrBeyondCustodyHistory_DeniesWithNoEvidence" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="371" /><test id="30f4b15d-32b2-4800-ba75-3b7ec6ae072f" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.NonFiniteOrBeyondCustodyHistory_DeniesWithNoEvidence(missingEnd: False)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5209556+00:00" finish-rtf="2026-10-06T18:43:58.5215736+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="NonFiniteOrBeyondCustodyHistory_DeniesWithNoEvidence" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="371" /><test id="a883104e-0b2c-474b-92da-04d81e084850" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \&quot;unregistered\&quot;)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.5221113+00:00" finish-rtf="2026-10-06T18:43:58.5232694+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="UnconfiguredPolicy_DeniesBothBindingReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="532" /><test id="d85aa67d-2c43-4959-8e84-1137041aedf8" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \&quot;missing-id\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5233269+00:00" finish-rtf="2026-10-06T18:43:58.5237723+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="UnconfiguredPolicy_DeniesBothBindingReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="532" /><test id="3677fc4c-7a99-408b-b836-c8f262468e3d" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \&quot;blank-id\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5238156+00:00" finish-rtf="2026-10-06T18:43:58.5240638+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="UnconfiguredPolicy_DeniesBothBindingReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="532" /><test id="aabb2903-649b-495c-b540-6223aa14fe69" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \&quot;missing-duration\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5241115+00:00" finish-rtf="2026-10-06T18:43:58.5243485+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="UnconfiguredPolicy_DeniesBothBindingReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="532" /><test id="a251c690-cbdd-4c7b-839f-94682dba93a8" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \&quot;zero-duration\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5244030+00:00" finish-rtf="2026-10-06T18:43:58.5246528+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="UnconfiguredPolicy_DeniesBothBindingReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="532" /><test id="b27523b3-1236-4369-8330-e82dd70c37c7" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \&quot;missing-trigger\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5247024+00:00" finish-rtf="2026-10-06T18:43:58.5249369+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="UnconfiguredPolicy_DeniesBothBindingReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="532" /><test id="832557fb-8f5a-46c6-a610-15854d4ac930" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \&quot;unsupported-trigger\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5249775+00:00" finish-rtf="2026-10-06T18:43:58.5252160+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="UnconfiguredPolicy_DeniesBothBindingReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="532" /><test id="fb2a92cf-15cf-40df-a37c-813ef4751d1b" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.FinalAuthorityCancelsThenReturnsValidGrant_DoesNotReleaseIdentity(historical: False)" result="Pass" time="0.012" time-rtf="00:00:00.0120000" start-rtf="2026-10-06T18:43:58.5255559+00:00" finish-rtf="2026-10-06T18:43:58.5382538+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="FinalAuthorityCancelsThenReturnsValidGrant_DoesNotReleaseIdentity" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="510" /><test id="a643b8c8-00f2-4f75-bb22-5e87b8fd0eac" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.FinalAuthorityCancelsThenReturnsValidGrant_DoesNotReleaseIdentity(historical: True)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.5383489+00:00" finish-rtf="2026-10-06T18:43:58.5396206+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="FinalAuthorityCancelsThenReturnsValidGrant_DoesNotReleaseIdentity" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="510" /><test id="bf1cc01c-a5f6-49ea-a1bd-c2891893f087" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ForeignAuthoritativeSourceAndFutureAction_DenyWithNoEvidence" result="Pass" time="0.002" time-rtf="00:00:00.0020000" start-rtf="2026-10-06T18:43:58.5401428+00:00" finish-rtf="2026-10-06T18:43:58.5425048+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ForeignAuthoritativeSourceAndFutureAction_DenyWithNoEvidence" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="309" /><test id="7b6b693d-a451-49ce-b01a-cd6da2c6957c" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ProvisionedOrganization_KeepsBranchBClassificationAndNeverCarriesHumanBinding" result="Pass" time="0.003" time-rtf="00:00:00.0030000" start-rtf="2026-10-06T18:43:58.5428459+00:00" finish-rtf="2026-10-06T18:43:58.5468521+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ProvisionedOrganization_KeepsBranchBClassificationAndNeverCarriesHumanBinding" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="235" /><test id="b5bad2bb-92c4-4529-ad85-3c8b3443cad3" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.SerializedRetainedSource_ReconstructsBothIntervalsWithoutProfileOrCurrentBinding" result="Pass" time="0.017" time-rtf="00:00:00.0170000" start-rtf="2026-10-06T18:43:58.5471745+00:00" finish-rtf="2026-10-06T18:43:58.5652447+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="SerializedRetainedSource_ReconstructsBothIntervalsWithoutProfileOrCurrentBinding" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="140" /><test id="b212a446-298c-4d5d-a217-278099ad7537" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ReaderCancelsThenReturnsValidSource_DoesNotReleaseIdentity(historical: False)" result="Pass" time="0.002" time-rtf="00:00:00.0020000" start-rtf="2026-10-06T18:43:58.5658124+00:00" finish-rtf="2026-10-06T18:43:58.5683469+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ReaderCancelsThenReturnsValidSource_DoesNotReleaseIdentity" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="433" /><test id="dbe38b7d-60aa-48f5-9df4-4cae77ae7804" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ReaderCancelsThenReturnsValidSource_DoesNotReleaseIdentity(historical: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5683948+00:00" finish-rtf="2026-10-06T18:43:58.5692675+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ReaderCancelsThenReturnsValidSource_DoesNotReleaseIdentity" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="433" /><test id="5d49029a-007d-4219-9f75-942992a34c12" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesAtFinalAuthority_DenyBothReads(historical: False, remove: False)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.5697160+00:00" finish-rtf="2026-10-06T18:43:58.5708149+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesAtFinalAuthority_DenyBothReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="656" /><test id="05ea6942-abb5-479b-8e08-04f6253c7ef5" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesAtFinalAuthority_DenyBothReads(historical: True, remove: False)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5708727+00:00" finish-rtf="2026-10-06T18:43:58.5715056+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesAtFinalAuthority_DenyBothReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="656" /><test id="af192d61-817a-4e55-813e-fe09dfe204a0" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesAtFinalAuthority_DenyBothReads(historical: False, remove: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5715532+00:00" finish-rtf="2026-10-06T18:43:58.5719597+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesAtFinalAuthority_DenyBothReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="656" /><test id="9162b566-dfba-4223-84b3-30a3ad0f378e" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesAtFinalAuthority_DenyBothReads(historical: True, remove: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5720216+00:00" finish-rtf="2026-10-06T18:43:58.5724366+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesAtFinalAuthority_DenyBothReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="656" /><test id="82ff0eb7-f6e0-4855-9578-eadb43d7970a" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CustodyExpiryCrossingAwait_DeniesHistoricalEvidence" result="Pass" time="0.006" time-rtf="00:00:00.0060000" start-rtf="2026-10-06T18:43:58.5727416+00:00" finish-rtf="2026-10-06T18:43:58.5791762+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="CustodyExpiryCrossingAwait_DeniesHistoricalEvidence" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="54" /><test id="b2ddf417-eb54-44b1-b605-74ca35ef7743" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RevocationWithoutLivePredecessor_DeniesWithNoEvidence(missingPredecessor: True)" result="Pass" time="0.003" time-rtf="00:00:00.0030000" start-rtf="2026-10-06T18:43:58.5797948+00:00" finish-rtf="2026-10-06T18:43:58.5829322+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RevocationWithoutLivePredecessor_DeniesWithNoEvidence" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="355" /><test id="ad37642e-882c-4489-8281-5db75a51b9a2" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RevocationWithoutLivePredecessor_DeniesWithNoEvidence(missingPredecessor: False)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5829973+00:00" finish-rtf="2026-10-06T18:43:58.5840139+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RevocationWithoutLivePredecessor_DeniesWithNoEvidence" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="355" /><test id="dab83592-d066-470e-a49c-cb766fc650f5" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ErasedProfile_CurrentReadFailsWhileIndependentHistoryKeepsOriginalPosition" result="Pass" time="0.005" time-rtf="00:00:00.0050000" start-rtf="2026-10-06T18:43:58.5843761+00:00" finish-rtf="2026-10-06T18:43:58.5903707+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ErasedProfile_CurrentReadFailsWhileIndependentHistoryKeepsOriginalPosition" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="123" /><test id="39d23539-5091-4d1b-a73f-b4ca9e2134aa" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MissingRetainedReaderOrExpiredCustody_CannotFallBackToAvailableFullProfile" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.5907926+00:00" finish-rtf="2026-10-06T18:43:58.5918792+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="MissingRetainedReaderOrExpiredCustody_CannotFallBackToAvailableFullProfile" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="221" /><test id="b95a3539-99d6-4167-864b-b68532e94b17" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.WrongAuthorizedActor_DeniesBeforeSourceRead" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5921878+00:00" finish-rtf="2026-10-06T18:43:58.5928389+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="WrongAuthorizedActor_DeniesBeforeSourceRead" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="346" /><test id="64a4c94f-f732-454d-ad26-3e36825b627d" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.HistoricalReplay_BeforeBoundaryReturnsOriginalAndAtBoundaryReturnsSuccessor" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.5930819+00:00" finish-rtf="2026-10-06T18:43:58.5946347+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="HistoricalReplay_BeforeBoundaryReturnsOriginalAndAtBoundaryReturnsSuccessor" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="287" /><test id="cf245d36-5e66-4b8a-8890-a1140af44738" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UncreatedInactiveRestrictedUnknownAndErased_AreNeverResolved" result="Pass" time="0.003" time-rtf="00:00:00.0030000" start-rtf="2026-10-06T18:43:58.5949061+00:00" finish-rtf="2026-10-06T18:43:58.5988526+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="UncreatedInactiveRestrictedUnknownAndErased_AreNeverResolved" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="405" /><test id="5b80508d-fdd5-4d7f-be2b-85b3a0de7a40" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.HistoricalRead_DoesNotRequireCurrentActorActivityAndHonorsHalfOpenBoundary" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.5991289+00:00" finish-rtf="2026-10-06T18:43:58.6002372+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="HistoricalRead_DoesNotRequireCurrentActorActivityAndHonorsHalfOpenBoundary" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="276" /><test id="31e12ca0-0180-458e-8469-f7e7e1903036" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateExpiryCrossingCustodyAwait_DeniesWithoutRenewingObservation" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.6004756+00:00" finish-rtf="2026-10-06T18:43:58.6019887+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateExpiryCrossingCustodyAwait_DeniesWithoutRenewingObservation" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="101" /><test id="efc037c8-5fdd-430f-8c51-d8780ce549ed" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity(historical: False, canRead: False)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.6023475+00:00" finish-rtf="2026-10-06T18:43:58.6043029+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="494" /><test id="a02ebbba-33ae-4993-aacd-143baaf07a91" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity(historical: True, canRead: False)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6043433+00:00" finish-rtf="2026-10-06T18:43:58.6051375+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="494" /><test id="74ffeeff-5e2a-4b5b-a2ed-66aa2ea49dfe" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity(historical: False, canRead: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6051756+00:00" finish-rtf="2026-10-06T18:43:58.6057314+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="494" /><test id="d1285105-8294-4eb3-a05a-73eb92ca01d8" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity(historical: True, canRead: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6057700+00:00" finish-rtf="2026-10-06T18:43:58.6062505+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="494" /><test id="d62a3ce2-4e10-4ffe-9325-abebf1d7bf39" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.InactiveOrRestrictedHuman_ReturnsIneligibleWithoutUsableBinding(inactive: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6065948+00:00" finish-rtf="2026-10-06T18:43:58.6076067+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="InactiveOrRestrictedHuman_ReturnsIneligibleWithoutUsableBinding" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="250" /><test id="b4ad2d51-d21f-4e2e-b39e-fd8cf581c1ea" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.InactiveOrRestrictedHuman_ReturnsIneligibleWithoutUsableBinding(inactive: False)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6076439+00:00" finish-rtf="2026-10-06T18:43:58.6081655+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="InactiveOrRestrictedHuman_ReturnsIneligibleWithoutUsableBinding" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="250" /><test id="40177012-7e7d-406c-9911-fe08468d3422" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead(historical: False, inCustody: False)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.6084644+00:00" finish-rtf="2026-10-06T18:43:58.6097955+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="458" /><test id="f06cdd9d-054d-4cb4-ba7d-26d1d07dd128" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead(historical: True, inCustody: False)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6098402+00:00" finish-rtf="2026-10-06T18:43:58.6104264+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="458" /><test id="8f6fbb2d-2785-417e-b2ae-9b44390dac8c" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead(historical: False, inCustody: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6104687+00:00" finish-rtf="2026-10-06T18:43:58.6109079+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="458" /><test id="e86972eb-2f6e-4702-b828-fa5407b53ed8" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead(historical: True, inCustody: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6109501+00:00" finish-rtf="2026-10-06T18:43:58.6113286+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="458" /><test id="ad333cc1-f72f-4b8e-a8d0-8f12451e63e7" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor(change: \&quot;revoked\&quot;)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.6116932+00:00" finish-rtf="2026-10-06T18:43:58.6130057+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="77" /><test id="a13c6892-d5b9-47a0-a572-66c7119a6b22" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor(change: \&quot;different-actor\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6130581+00:00" finish-rtf="2026-10-06T18:43:58.6136568+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="77" /><test id="c4a11afc-d03c-4e02-b8b9-ba6bc93db21b" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor(change: \&quot;different-revision\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6136963+00:00" finish-rtf="2026-10-06T18:43:58.6139691+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="77" /><test id="d96d647a-b40b-46ed-9dca-4ea3e85d8dc0" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor(change: \&quot;different-source\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6140062+00:00" finish-rtf="2026-10-06T18:43:58.6142739+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="77" /><test id="9e474a06-1529-41d5-a67a-a39b7463c43e" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;gap\&quot;)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.6147056+00:00" finish-rtf="2026-10-06T18:43:58.6160436+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="8f982943-bd7a-41bf-b033-44c77d04b973" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;overlap\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6160842+00:00" finish-rtf="2026-10-06T18:43:58.6167951+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="7785cdf5-e882-409f-873f-69412fef130d" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;profile-substitution\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6168430+00:00" finish-rtf="2026-10-06T18:43:58.6177509+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="0d7fb32c-6db9-4cbb-888b-cbc0accdd92a" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;wrong-purpose\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6177926+00:00" finish-rtf="2026-10-06T18:43:58.6181237+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="9872d811-3a8a-4d96-8d9b-6acb204ee7e4" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;foreign-binding\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6181608+00:00" finish-rtf="2026-10-06T18:43:58.6184907+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="35e59c41-243b-4109-b931-017c0b0a288d" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;transit-expiry\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6185288+00:00" finish-rtf="2026-10-06T18:43:58.6187321+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="32783fb6-2225-4ea7-86f3-3e75bec1a110" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;missing-authority\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6187751+00:00" finish-rtf="2026-10-06T18:43:58.6189772+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="905d99d2-8ea2-471a-8884-b9e9e99f1f4f" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;future-observation\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6190165+00:00" finish-rtf="2026-10-06T18:43:58.6192797+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="fc731f44-de48-4043-8ea2-7ea89e2729f3" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;short-name-substitution\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6193170+00:00" finish-rtf="2026-10-06T18:43:58.6196419+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="c70e1fef-5889-4396-a65a-e5085acee273" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;assembly-qualified-substitution\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6196791+00:00" finish-rtf="2026-10-06T18:43:58.6199283+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="00e97e5d-798d-43c9-8702-55178a87f48f" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;namespace-alias-substitution\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6199651+00:00" finish-rtf="2026-10-06T18:43:58.6202213+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="99d91aab-2dfd-40e3-960e-b47c547b6ecf" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \&quot;policy-version\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6206161+00:00" finish-rtf="2026-10-06T18:43:58.6216592+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="MismatchedPolicy_DeniesBothBindingReadsWithoutCustody" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="565" /><test id="10f91549-20b7-4895-aa1f-f75d31732ada" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \&quot;duration\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6217000+00:00" finish-rtf="2026-10-06T18:43:58.6221405+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="MismatchedPolicy_DeniesBothBindingReadsWithoutCustody" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="565" /><test id="8eeeb786-fc61-4cec-abe2-cb2079497c3f" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \&quot;recorded-expiry\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6221882+00:00" finish-rtf="2026-10-06T18:43:58.6224631+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="MismatchedPolicy_DeniesBothBindingReadsWithoutCustody" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="565" /><test id="aeeabada-8e49-4ded-b856-3eadf06da3a9" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \&quot;purpose\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6225383+00:00" finish-rtf="2026-10-06T18:43:58.6228615+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="MismatchedPolicy_DeniesBothBindingReadsWithoutCustody" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="565" /><test id="f512afb5-f99b-4449-a525-387300dba02d" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \&quot;source-expiry\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6229011+00:00" finish-rtf="2026-10-06T18:43:58.6232210+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="MismatchedPolicy_DeniesBothBindingReadsWithoutCustody" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="565" /><test id="fba8a41d-95cf-4441-af19-668af5dec392" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \&quot;restore\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6232582+00:00" finish-rtf="2026-10-06T18:43:58.6236412+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="MismatchedPolicy_DeniesBothBindingReadsWithoutCustody" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="565" /><test id="5a94ceb4-492e-464b-a325-56cc5b7e942d" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \&quot;derived-copies\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6236780+00:00" finish-rtf="2026-10-06T18:43:58.6239924+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="MismatchedPolicy_DeniesBothBindingReadsWithoutCustody" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="565" /><test id="e1c6d3c5-8f2b-4ccd-b234-39aa55991b31" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CustodyFailureOrNetworkFailure_IsUnavailableAndCancellationPropagates" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.6242639+00:00" finish-rtf="2026-10-06T18:43:58.6263163+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="CustodyFailureOrNetworkFailure_IsUnavailableAndCancellationPropagates" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="390" /><test id="4b77352a-94a7-42ec-aea1-d8aa609113df" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ValidRevocationReplay_PreservesBeforeBoundaryAndReturnsGapAtBoundary" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6266035+00:00" finish-rtf="2026-10-06T18:43:58.6275436+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ValidRevocationReplay_PreservesBeforeBoundaryAndReturnsGapAtBoundary" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="331" /><test id="8cf484b5-2a1c-497b-bb2f-21504350f1a5" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PreCancelledQuery_DoesNotConsultAuthorityOrReaders(historical: False)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.6279387+00:00" finish-rtf="2026-10-06T18:43:58.6297538+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PreCancelledQuery_DoesNotConsultAuthorityOrReaders" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="417" /><test id="e5ed6178-16e5-4272-b77a-b6b459194283" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PreCancelledQuery_DoesNotConsultAuthorityOrReaders(historical: True)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.6298121+00:00" finish-rtf="2026-10-06T18:43:58.6308930+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PreCancelledQuery_DoesNotConsultAuthorityOrReaders" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="417" /></collection><collection name="Test collection for Hexalith.Parties.Tests.Domain.PartyIdentityAdmissionTests (id: f7fdbf70f8c51900592e7785366dc9f77df753f2a03bfcd9407945da147addf4)" id="fce5f16c-0827-4e09-8beb-e9d5b2782377" failed="0" not-run="0" passed="3" skipped="0" time="0.142" time-rtf="00:00:00.1419999" total="3"><test id="d2534ef4-15b6-41f3-8444-d88104231d42" name="Hexalith.Parties.Tests.Domain.PartyIdentityAdmissionTests.MissingOrUnsupportedPolicy_DeniesBeforeAnyProtectedUnwrap(trigger: null)" result="Pass" time="0.141" time-rtf="00:00:00.1409999" start-rtf="2026-10-06T18:43:58.3112048+00:00" finish-rtf="2026-10-06T18:43:58.4657478+00:00" type="Hexalith.Parties.Tests.Domain.PartyIdentityAdmissionTests" method="MissingOrUnsupportedPolicy_DeniesBeforeAnyProtectedUnwrap" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Domain/PartyIdentityAdmissionTests.cs" source-line="18" /><test id="6eddedbd-2946-4dd2-88be-6a261888e2cc" name="Hexalith.Parties.Tests.Domain.PartyIdentityAdmissionTests.MissingOrUnsupportedPolicy_DeniesBeforeAnyProtectedUnwrap(trigger: \&quot;unsupported\&quot;)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.4688356+00:00" finish-rtf="2026-10-06T18:43:58.4707523+00:00" type="Hexalith.Parties.Tests.Domain.PartyIdentityAdmissionTests" method="MissingOrUnsupportedPolicy_DeniesBeforeAnyProtectedUnwrap" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Domain/PartyIdentityAdmissionTests.cs" source-line="18" /><test id="0d7dcbae-9372-4d58-a53d-3961725bdb0c" name="Hexalith.Parties.Tests.Domain.PartyIdentityAdmissionTests.MissingOrUnsupportedPolicy_DeniesBeforeAnyProtectedUnwrap(trigger: \&quot;binding-closure\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.4708100+00:00" finish-rtf="2026-10-06T18:43:58.4710662+00:00" type="Hexalith.Parties.Tests.Domain.PartyIdentityAdmissionTests" method="MissingOrUnsupportedPolicy_DeniesBeforeAnyProtectedUnwrap" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Domain/PartyIdentityAdmissionTests.cs" source-line="18" /></collection></assembly></assemblies>
\ No newline at end of file

diff --git a/parties/_bmad-output/implementation-artifacts/tests/ext-parties-1-owner-source-evidence-2026-10-06.json b/parties/_bmad-output/implementation-artifacts/tests/ext-parties-1-owner-source-evidence-2026-10-06.json
new file mode 100644
index 00000000..9a74133f
--- /dev/null
+++ b/parties/_bmad-output/implementation-artifacts/tests/ext-parties-1-owner-source-evidence-2026-10-06.json
@@ -0,0 +1,434 @@
+{
+  "record": "EXT-PARTIES-1",
+  "date": "2026-10-06",
+  "scope": "Owner-local prerequisite implementation/source fixtures only",
+  "acceptedStatus": "Uncommitted",
+  "acceptedTargetVersionOrCommit": "TBD",
+  "acceptedIntegrationDate": "TBD",
+  "acceptedCompatibilityCommand": "TBD",
+  "baseHeads": {
+    "Parties": "b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca",
+    "EventStore": "785d58fc99ce4c2751c0546ca6368e993c654ed9",
+    "Platform": "711a70fd94794ef1aea1b91f5524136b0d660468"
+  },
+  "headsAreBaseObservationsOnly": true,
+  "workingTreesIncludeUncommittedChanges": true,
+  "focusedSourceLanes": [
+    {
+      "project": "Hexalith.Parties.Tests",
+      "build": {
+        "executable": "dotnet",
+        "arguments": [
+          "build",
+          "tests/Hexalith.Parties.Tests/Hexalith.Parties.Tests.csproj",
+          "-c",
+          "Debug",
+          "--artifacts-path",
+          "/tmp/hexalith-agents54-parties-artifacts",
+          "-p:UseHexalithProjectReferences=true",
+          "-p:UseNuGetDeps=false",
+          "-p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/eventstore",
+          "-p:HexalithCommonsRoot=/home/administrator/projects/hexalith/parties/references/Hexalith.Commons",
+          "-p:HexalithMemoriesRoot=/tmp/parties-no-memories-source",
+          "-p:NuGetAudit=false",
+          "-m:1",
+          "-p:MinVerVersionOverride=1.0.0"
+        ],
+        "workingDirectory": "/home/administrator/projects/hexalith/parties"
+      },
+      "test": {
+        "executable": "dotnet",
+        "arguments": [
+          "/tmp/hexalith-agents54-parties-artifacts/bin/Hexalith.Parties.Tests/debug/Hexalith.Parties.Tests.dll",
+          "-class",
+          "*PartyIdentityQueryHandlerTests",
+          "-class",
+          "*PartyIdentityAdmissionTests",
+          "-result-xml",
+          "/tmp/parties-story54-reviewfix-tests-20261006.xml"
+        ],
+        "workingDirectory": "/home/administrator/projects/hexalith/parties"
+      },
+      "total": 52,
+      "passed": 52,
+      "failed": 0,
+      "skipped": 0,
+      "warnings": 0,
+      "errors": 0,
+      "files": [
+        {
+          "path": "/tmp/parties-story54-reviewfix-build-20261006.log",
+          "bytes": 5360,
+          "sha256": "8f286cc870880eb6f0197ff97891ca7c5053ea79ea31238c30b63b9ea62724e8"
+        },
+        {
+          "path": "/tmp/parties-story54-reviewfix-tests-20261006.log",
+          "bytes": 425,
+          "sha256": "fe70d08b9d6966d4756da2505c50f20605fc63171f309877e5cc7b751da645a6"
+        },
+        {
+          "path": "/tmp/parties-story54-reviewfix-tests-20261006.xml",
+          "bytes": 35009,
+          "sha256": "3f54d045db8751176d39adc45deecd8ba0beb74bdccfedf7b5b2346f1e2ed589"
+        }
+      ]
+    },
+    {
+      "project": "Hexalith.Parties.Client.Tests",
+      "build": {
+        "executable": "dotnet",
+        "arguments": [
+          "build",
+          "tests/Hexalith.Parties.Client.Tests/Hexalith.Parties.Client.Tests.csproj",
+          "-c",
+          "Debug",
+          "--artifacts-path",
+          "/tmp/hexalith-agents54-parties-artifacts",
+          "-p:UseHexalithProjectReferences=true",
+          "-p:UseNuGetDeps=false",
+          "-p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/eventstore",
+          "-p:HexalithCommonsRoot=/home/administrator/projects/hexalith/parties/references/Hexalith.Commons",
+          "-p:NuGetAudit=false",
+          "-m:1"
+        ],
+        "workingDirectory": "/home/administrator/projects/hexalith/parties"
+      },
+      "test": {
+        "executable": "dotnet",
+        "arguments": [
+          "/tmp/hexalith-agents54-parties-artifacts/bin/Hexalith.Parties.Client.Tests/debug/Hexalith.Parties.Client.Tests.dll",
+          "-class",
+          "*HttpPartiesIdentityClientTests",
+          "-class",
+          "*DependencyInjectionTests",
+          "-result-xml",
+          "/tmp/parties-story54-retained-client-final-tests-20261006.xml"
+        ],
+        "workingDirectory": "/home/administrator/projects/hexalith/parties"
+      },
+      "total": 18,
+      "passed": 18,
+      "failed": 0,
+      "skipped": 0,
+      "warnings": 0,
+      "errors": 0,
+      "files": [
+        {
+          "path": "/tmp/parties-story54-retained-client-final-build-20261006.log",
+          "bytes": 3124,
+          "sha256": "f55c08a16476c0e8f3d95a30298cd5ebbd46f35c9d183f2f6770c87012aa3946"
+        },
+        {
+          "path": "/tmp/parties-story54-retained-client-final-tests-20261006.log",
+          "bytes": 460,
+          "sha256": "78a9f14252793ddb46db26be966a0b90926b418d2e57768901624ba65c0a87e2"
+        },
+        {
+          "path": "/tmp/parties-story54-retained-client-final-tests-20261006.xml",
+          "bytes": 12525,
+          "sha256": "748bedea5fd264be72d1bc5ab7a288f39105e89ef2ce14cd59b8c4957b6e0205"
+        }
+      ],
+      "reusedUnchangedEvidence": true
+    },
+    {
+      "project": "Hexalith.Parties.Contracts.Tests",
+      "build": {
+        "executable": "dotnet",
+        "arguments": [
+          "build",
+          "tests/Hexalith.Parties.Contracts.Tests/Hexalith.Parties.Contracts.Tests.csproj",
+          "-c",
+          "Debug",
+          "--artifacts-path",
+          "/tmp/hexalith-agents54-parties-artifacts",
+          "-p:UseHexalithProjectReferences=true",
+          "-p:UseNuGetDeps=false",
+          "-p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/eventstore",
+          "-p:HexalithCommonsRoot=/home/administrator/projects/hexalith/parties/references/Hexalith.Commons",
+          "-p:NuGetAudit=false",
+          "-m:1"
+        ],
+        "workingDirectory": "/home/administrator/projects/hexalith/parties"
+      },
+      "test": {
+        "executable": "dotnet",
+        "arguments": [
+          "/tmp/hexalith-agents54-parties-artifacts/bin/Hexalith.Parties.Contracts.Tests/debug/Hexalith.Parties.Contracts.Tests.dll",
+          "-class",
+          "*PartyIdentityContractTests",
+          "-class",
+          "*ContractsPublicApiSnapshotTests",
+          "-result-xml",
+          "/tmp/parties-story54-retained-contracts-final2-tests-20261006.xml"
+        ],
+        "workingDirectory": "/home/administrator/projects/hexalith/parties"
+      },
+      "total": 3,
+      "passed": 3,
+      "failed": 0,
+      "skipped": 0,
+      "warnings": 0,
+      "errors": 0,
+      "files": [
+        {
+          "path": "/tmp/parties-story54-retained-contracts-final2-build-20261006.log",
+          "bytes": 3133,
+          "sha256": "90141ceb6a5a05c7e1d8e1a97b80cc564c3efb79127d6ed4a9761637b3916cf0"
+        },
+        {
+          "path": "/tmp/parties-story54-retained-contracts-final2-tests-20261006.log",
+          "bytes": 474,
+          "sha256": "8bc888999533a52f1df06f274ff632ba0572f725942833b4657d4883d0eae6f1"
+        },
+        {
+          "path": "/tmp/parties-story54-retained-contracts-final2-tests-20261006.xml",
+          "bytes": 3449,
+          "sha256": "d123e2ae171392ef679303cd422617541eed385daf3631083f56933a7b4c3baf"
+        }
+      ],
+      "reusedUnchangedEvidence": true
+    }
+  ],
+  "focusedTotal": 73,
+  "sourceFiles": [
+    {
+      "path": "/home/administrator/projects/hexalith/parties/eng/verify-ext-parties-1.ps1",
+      "bytes": 8255,
+      "sha256": "81e5340d2efe060616d594ff56b9a8736dc564edf872e2e18286f77269704352"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/eng/verify-ext-parties-history-readiness.ps1",
+      "bytes": 12891,
+      "sha256": "0e080e653a931466a03c4597f7c2ff0551fc878d6d912b155d1e47cd2ccca230"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs",
+      "bytes": 13625,
+      "sha256": "747f33405744d46390b1aad2e337d72acb0a4accf76d7124af8e10cdf66d5255"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/Queries/RetainedHumanActorHistoryFold.cs",
+      "bytes": 6897,
+      "sha256": "bfa7ff1402b3641ca91cbc31a2902ea5fa5f8fdf41eb4ad672f786a2256d205f"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/Queries/RetainedHumanActorBinding.cs",
+      "bytes": 459,
+      "sha256": "8f5bd2a7de7fa65a54a2134efa33dfc62a03d7c212449f5f9c1230dc911d7349"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/src/Hexalith.Parties.Client/HttpPartiesIdentityClient.cs",
+      "bytes": 11710,
+      "sha256": "2399209e396a6182f5aae0cb918df1a7de6ce71505106a5760dc699af2453f3e"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/src/Hexalith.Parties.Contracts/Models/HumanActorBindingResult.cs",
+      "bytes": 1216,
+      "sha256": "2f8c6b2b165851080d03474c716de3e238f5351051b3961e0a7b003de5cc124b"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs",
+      "bytes": 38913,
+      "sha256": "9bac86fdaa45223a1a3b16e9ead7231feff93e585d2524b10793a11a5d02e3be"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Client.Tests/HttpPartiesIdentityClientTests.cs",
+      "bytes": 7653,
+      "sha256": "74db9bca1360fe9f2def41d995b444d7b7acc83edc5de1d7e98b543e82ace773"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Contracts.Tests/Package/ContractsPublicApiSnapshotTests.cs",
+      "bytes": 6468,
+      "sha256": "e24bc116414368e3108e2400fafb4d3fa9528633d12d4c4fefefd5160b2e2d16"
+    },
+    {
+      "path": "/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Contracts.Tests/Package/ContractsPublicApiSnapshot.txt",
+      "bytes": 86558,
+      "sha256": "8bb9bff4a030792e704b51767a0022c40d30fc68e6e20168b7d31566c319e406"
+    }
+  ],
+  "verifierGateSmoke": {
+    "scriptsParse": true,
+    "readinessMissingInputsExit": 1,
+    "readinessUncommittedRegisterExit": 1,
+    "httpCallsObserved": 0,
+    "liveMissingInputsExit": 1,
+    "files": [
+      {
+        "path": "/tmp/parties-story54-verifier-parser-20261006.ps1",
+        "bytes": 597,
+        "sha256": "0f89dce790bd3888a1f12ef38763b1e6dbcbbd8733afcc06aae17abef735418a"
+      },
+      {
+        "path": "/tmp/parties-story54-verifier-parser-20261006.log",
+        "bytes": 44,
+        "sha256": "0d7150276e3e95ca44f456ad663a83973714f4e3b047210f5885d301b15b4d87"
+      },
+      {
+        "path": "/tmp/parties-story54-readiness-gates-smoke-20261006.py",
+        "bytes": 2101,
+        "sha256": "1527b829e89448730f86ee6e27c0db1303e8d7af1045f0c6f5223d2f7f840721"
+      },
+      {
+        "path": "/tmp/parties-story54-readiness-gates-smoke-20261006.log",
+        "bytes": 125,
+        "sha256": "4f0f9fbc0c78e0518b77f0fc6c171ef23484be56924a13c01d1f1fdcced4933a"
+      },
+      {
+        "path": "/tmp/parties-story54-readiness-missing-inputs-20261006.log",
+        "bytes": 743,
+        "sha256": "b60add40db8bac1c2e8f01e3157ffebbfe362d011e6d3d5ecbae173cca040c99"
+      },
+      {
+        "path": "/tmp/parties-story54-readiness-register-gate-20261006.log",
+        "bytes": 448,
+        "sha256": "3f447630e9cc93c1c51913ca0f5455677e696d5c29b15f3943f78e7c42ad99dc"
+      },
+      {
+        "path": "/tmp/parties-story54-live-gate-20261006.log",
+        "bytes": 377,
+        "sha256": "494a7f949c8dadf3383453f1a00e4a0de3f31274188ca5bf97fab7eb4391dafc"
+      }
+    ]
+  },
+  "localVerifier": {
+    "command": [
+      "pwsh",
+      "-NoProfile",
+      "-File",
+      "eng/verify-ext-parties-1.ps1",
+      "-Mode",
+      "Local",
+      "-ArtifactsDirectory",
+      "/tmp/parties-story54-local-verifier-20261006/artifacts",
+      "-EvidenceDirectory",
+      "/tmp/parties-story54-local-verifier-20261006"
+    ],
+    "exit": 1,
+    "executedTests": 490,
+    "passed": 489,
+    "failed": 1,
+    "skipped": 0,
+    "sourceRevisionNote": "This run preceded the two final alias-negative fixtures. Those fixtures passed in the final focused domain lane.",
+    "blocker": "PartyDomainProcessorValidationTests.ProcessAsync_ProtectedHistoricalPayloadWithDestroyedKey_RedactsAndContinuesRehydration rejects json-redacted in the sibling EventStore strict replay path.",
+    "unrunProjects": [
+      "Hexalith.Parties.Security.Tests",
+      "Hexalith.Parties.Client.Tests",
+      "Hexalith.Parties.UI.Tests"
+    ],
+    "files": [
+      {
+        "path": "/tmp/parties-story54-local-verifier-20261006/local-evidence.json",
+        "bytes": 4225,
+        "sha256": "cd745ca35e7271caa7df564ba7654dc9c48d98bc392c71b287e96fc8e64341fd"
+      },
+      {
+        "path": "/tmp/parties-story54-local-verifier-20261006.log",
+        "bytes": 4007,
+        "sha256": "354abb32306389bda4881e0afdfe1780ff6f61f4b49a8d2cc0c8eafd52891f29"
+      },
+      {
+        "path": "/tmp/parties-story54-local-verifier-20261006/Hexalith.EventStore.Client.Tests-tests.xml",
+        "bytes": 68152,
+        "sha256": "15aa257795353f4271f395402fc2367e017838cec4ce8a73bbb35a2e51590817"
+      },
+      {
+        "path": "/tmp/parties-story54-local-verifier-20261006/Hexalith.EventStore.Contracts.Tests-tests.xml",
+        "bytes": 16014,
+        "sha256": "720bbf1cdc8b1402467081641a209fcab8b9febe9400e66f31b7acd25fecfab8"
+      },
+      {
+        "path": "/tmp/parties-story54-local-verifier-20261006/Hexalith.EventStore.Server.Tests-tests.xml",
+        "bytes": 92301,
+        "sha256": "a9b324aed77f94906c942e7cc928442ae8fb450b75046b327993ab402c7e5ccc"
+      },
+      {
+        "path": "/tmp/parties-story54-local-verifier-20261006/Hexalith.Parties.Contracts.Tests-tests.xml",
+        "bytes": 18693,
+        "sha256": "3bf41d81e7a211aaf93ef19fec0fbbbec1ab19a8721a2a9fe6c35c368acf99f8"
+      },
+      {
+        "path": "/tmp/parties-story54-local-verifier-20261006/Hexalith.Parties.Server.Tests-tests.xml",
+        "bytes": 63309,
+        "sha256": "53dc3f6193c8fd65df3ac6ea5246815fed336a5363201ab893a26682c64193fd"
+      },
+      {
+        "path": "/tmp/parties-story54-local-verifier-20261006/Hexalith.Parties.Tests-tests.xml",
+        "bytes": 72763,
+        "sha256": "8e5cad8f3375a6c407fa39dfc86421554691bcf60ab4623749e3b21ebda88821"
+      },
+      {
+        "path": "/tmp/parties-story54-local-verifier-20261006/Hexalith.Platform.Identity.Tests-tests.xml",
+        "bytes": 2521,
+        "sha256": "7006ec02c7d1c144161290ef2accdcff66c01de8c9bc68e6bac755d7bae8b13e"
+      }
+    ]
+  },
+  "liveCallsPerformed": 0,
+  "liveQualificationEstablished": false,
+  "productionPolicySelected": false,
+  "productionCustodyProviderInstalled": false,
+  "historicalFocusedSourceLanes": [
+    {
+      "project": "Hexalith.Parties.Tests",
+      "build": {
+        "executable": "dotnet",
+        "arguments": [
+          "build",
+          "tests/Hexalith.Parties.Tests/Hexalith.Parties.Tests.csproj",
+          "-c",
+          "Debug",
+          "--artifacts-path",
+          "/tmp/hexalith-agents54-parties-artifacts",
+          "-p:UseHexalithProjectReferences=true",
+          "-p:UseNuGetDeps=false",
+          "-p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/eventstore",
+          "-p:HexalithCommonsRoot=/home/administrator/projects/hexalith/parties/references/Hexalith.Commons",
+          "-p:HexalithMemoriesRoot=/tmp/parties-no-memories-source",
+          "-p:NuGetAudit=false",
+          "-m:1"
+        ],
+        "workingDirectory": "/home/administrator/projects/hexalith/parties"
+      },
+      "test": {
+        "executable": "dotnet",
+        "arguments": [
+          "/tmp/hexalith-agents54-parties-artifacts/bin/Hexalith.Parties.Tests/debug/Hexalith.Parties.Tests.dll",
+          "-class",
+          "*PartyIdentityQueryHandlerTests",
+          "-class",
+          "*PartyIdentityAdmissionTests",
+          "-result-xml",
+          "/tmp/parties-story54-retained-domain-final3-tests-20261006.xml"
+        ],
+        "workingDirectory": "/home/administrator/projects/hexalith/parties"
+      },
+      "total": 38,
+      "passed": 38,
+      "failed": 0,
+      "skipped": 0,
+      "warnings": 0,
+      "errors": 0,
+      "files": [
+        {
+          "path": "/tmp/parties-story54-retained-domain-final3-build-20261006.log",
+          "bytes": 5360,
+          "sha256": "8c6aeec82b0b1d1b0c65e4a23d9152b6170094cd081adcefa75271f9da8cfb44"
+        },
+        {
+          "path": "/tmp/parties-story54-retained-domain-final3-tests-20261006.log",
+          "bytes": 425,
+          "sha256": "4862071e94478c416f5baf7d2b0f24f416e29328d177b8296063fae5326db1ac"
+        },
+        {
+          "path": "/tmp/parties-story54-retained-domain-final3-tests-20261006.xml",
+          "bytes": 25965,
+          "sha256": "33f5cef8f0d72285b6d7855b003eb20a14b9489137b26d61abbf1d011df1b8d4"
+        }
+      ]
+    }
+  ],
+  "reviewFixObservedHead": "b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca",
+  "reviewFix": "Entry, bounded outstanding waits, post-await and terminal caller cancellation; 14 new cases, including cancelled false/true custody returns without final authority lookup."
+}

diff --git a/parties/_bmad-output/planning-artifacts/actor-history-retention-policy-v1.md b/parties/_bmad-output/planning-artifacts/actor-history-retention-policy-v1.md
new file mode 100644
index 00000000..caed9963
--- /dev/null
+++ b/parties/_bmad-output/planning-artifacts/actor-history-retention-policy-v1.md
@@ -0,0 +1,41 @@
+# Minimal opaque actor-history retention policy
+
+Engineering approach accepted by the user’s “apply recommendation” instruction on 2026-10-06. The concrete 365-day policy was accepted by “do recommended” on 2026-10-07 after the [duration/custody comparison](../../../agents/_bmad-output/implementation-artifacts/story-5-4-retention-custody-proposal-2026-10-06.md). **Policy scope and duration: user-approved. Production history: disabled pending qualified custody/restore and complete owner compatibility acceptance.** User acceptance is not a fabricated provider qualification or owner deployment receipt.
+
+Accepted policy reference: `party-actor-retention-v1`. Purpose: `party-actor-history-v1`. Product/Governance approves scope and duration; Parties owns the binding contract; Platform owns independent identity authority and custody; EventStore supplies the protected source seam.
+
+## Reuse assessment and selected approach
+
+Before this specific user approval, no located pre-existing policy explicitly covered the tenant/Party-to-stable-human-actor relationship after profile erasure. Agents' 365-day rule starts at interaction terminal state and covers sensitive interaction content ([Agents specification](../../../agents/_bmad-output/specs/spec-agents/SPEC.md)). Parties' crypto/projection “retention” proposals retain implementation code during migration, rather than defining a data lifetime. The invoice retention examples in [integration guidance](../../docs/event-handler-patterns.md) do not authorize actor-history retention. None supplies this record's exact scope, duration, expiry, cleanup and restore rules.
+
+Use one narrow policy through the existing Party aggregate, EventStore `IdentityHistoryPolicy` and purpose-specific custody seam. Reuse an existing policy later only when its recorded approval explicitly covers these same requirements. The accepted policy is now populated in the [Parties host configuration](../../src/Hexalith.Parties/appsettings.json). Options library defaults remain unset and the existing custody/source/authorization guards remain mandatory; no new database, service or general policy engine is needed.
+
+## Data and access
+
+Retain only the exact tenant and Party IDs, opaque stable human ActorId, immutable binding version and actor-authority revision, effective interval, original establishment/rebind source position, and the minimal opaque provenance/logical-intent digest and custody references required to authenticate that lifecycle. Revoke/rebind boundaries are part of that lifecycle. Source certificates contain the exact head, observation/revision and excluded original positions needed to prove completeness.
+
+Exclude names, contact details, birth dates, login issuer/subject mappings, role claims, credentials, decoded tokens, message content and general Party profile payloads. Treat the opaque relationship as protected identity data. Ordinary logs and cleanup receipts must not duplicate the relationship; use opaque custody references and content-free counts/status.
+
+Only independently admitted, tenant/Party-scoped attribution queries may read it. Historical success identifies the recorded actor for a past action; it never grants present authority. Current human eligibility still requires active Party and current actor authority. Organization Branch B has no human binding and does not depend on this policy.
+
+## Lifetime and profile erasure
+
+The accepted duration `D` is **365 fixed days**, configured as `365.00:00:00` (31,536,000 seconds). This is an explicit user-approved engineering period, not the Agents terminal-content policy or a statutory period. Library defaults remain unset. The selected v1 trigger is `binding-effective-at`, already supported by the contract: `ExpiresAt = ValidFrom + D`. Arithmetic overflow denies admission. Expiry is exclusive: no release at or after ExpiresAt.
+
+Closing, revoking, rebinding, erasing the profile, restarting, restoring or retrying does not restart this clock. Every successor derives its own expiry from its own effective instant. No configuration change extends or relabels a retained record. A new duration requires a new approved versioned policy reference; this minimal implementation supports one configured version and returns Unavailable for another version. Serving several approved versions concurrently requires an explicit later extension.
+
+Binding validity is bounded by its custody expiry. A long-lived current binding therefore needs an explicit authorized new version before expiry; it cannot silently renew. Erasure immediately blocks current eligibility and destroys profile protection independently. Past attribution may remain readable only until its original expiry under the approved policy and fresh independent custody. A duration measured from profile erasure is a different, unsupported contract and remains disabled.
+
+## Cleanup and restore contract
+
+At expiry, custody denies reads immediately, even if asynchronous cleanup has not finished. `DestroyExpiredAsync` must idempotently destroy the retention unit's ability to decrypt source events, snapshots and every derived copy, including caches, read models, replicas and lifecycle-covered backups/exports. Profile and actor-history protection must be independent. Cleanup success requires authenticated evidence covering the exact unit and all copies; outage or unknown acknowledgement is pending, never success. Immutable ciphertext may remain only when its decryption capability is irreversibly destroyed and no identity-bearing derived plaintext survives.
+
+Restore consults the fresh, independently durable lifecycle before decrypting anything. Old keys, snapshots or lifecycle revisions cannot roll back expiry/destruction or make a destroyed binding readable. Reject unavailable, revoked, expired or stale lifecycle evidence. Replayed events keep original actor/version/times/positions; restoring cannot substitute today's binding or recreate an expired identity relationship. Complete historical proof must remain demonstrable without recovering expired predecessors; the existing strict fold returns Unavailable if it cannot reconstruct a complete lifecycle. This limitation requires owner qualification, not fabricated missing events.
+
+These are required provider behaviors. Platform now provides `IdentityHistoryCleanup.ProcessAsync` for one owner-inventoried retention unit through the existing custody contract. It validates the original policy/effective time/evidence, never destroys before exclusive expiry, bounds each provider wait to five seconds, and reports only a content-free outcome. Missing providers, failures, unknown acknowledgements and fresh checks that still allow reads remain Pending; a retry passes the same immutable evidence. Provider-confirmed destruction requires both the provider’s irreversible-destruction result and a fresh read denial. The provider still owns authenticated receipts, all-copy coverage, idempotent outcome lookup and independent lifecycle durability. This operation supplies no inventory, scheduler, production backend, backup destruction or independent restore qualification.
+
+## Runtime gate and activation record
+
+`Parties:Identity` must contain the exact approved versioned PolicyId, approved finite Retention and supported ExpiryTrigger. An unset/invalid policy returns Unavailable for binding reads and denies binding writes. A readable custody response alone is insufficient: policy ID, purpose, lifecycle flags and recorded expiry must match. Each read snapshots configuration and checks it again after awaits and before releasing evidence; changed/withdrawn configuration denies the result. Independent source authority, fresh custody, expiry and cancellation checks still apply.
+
+The policy reference/version, 365-day duration, explicit post-profile-erasure scope, supported trigger and user approval are recorded above and in the [application spec](../../../agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md). Before production activation, the owners still need an exact custody/source target and copy inventory, independently authenticated cleanup/recovery/restore evidence and complete owner compatibility acceptance. The host now loads the accepted numeric policy; this does not enable history without custody. The older query fixture’s ten-day duration and the cleanup fixture’s receipts are synthetic and do not qualify a deployment.

diff --git a/parties/eng/verify-ext-parties-history-readiness.ps1 b/parties/eng/verify-ext-parties-history-readiness.ps1
new file mode 100644
index 00000000..9d91b500
--- /dev/null
+++ b/parties/eng/verify-ext-parties-history-readiness.ps1
@@ -0,0 +1,159 @@
+[CmdletBinding()]
+param(
+    [Parameter(Mandatory)][string]$EvidenceDirectory,
+    [Parameter(Mandatory)][string]$DependencyRegisterPath
+)
+$ErrorActionPreference = 'Stop'
+$required = @('EXT_PARTIES_GATEWAY_URL', 'EXT_PARTIES_READER_HEADERS_JSON', 'EXT_PARTIES_TARGET_SHA', 'EXT_PARTIES_COMPATIBILITY_RECEIPT',
+    'EXT_PARTIES_TENANT_A', 'EXT_PARTIES_HISTORY_PARTY_ID', 'EXT_PARTIES_HISTORY_ACTOR_ID',
+    'EXT_PARTIES_HISTORY_ACTION_AT', 'EXT_PARTIES_HISTORY_BINDING_VERSION', 'EXT_PARTIES_POLICY_ID', 'EXT_PARTIES_RETENTION', 'EXT_PARTIES_EXPIRY_TRIGGER')
+$missing = @($required | Where-Object { [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($_)) })
+if ($missing.Count) {
+    throw ('DependencyNotAvailable: EXT-PARTIES-1. Missing read-only live readiness inputs: ' + ($missing -join ', '))
+}
+if ($env:EXT_PARTIES_TARGET_SHA -cnotmatch '^[0-9a-f]{40}$') { throw 'An exact requested source target SHA is required.' }
+# Read probes execute the dependency seam. A local fixture exemption cannot bypass
+# the authoritative consumer execution gate or establish owner acceptance.
+if (-not (Test-Path -LiteralPath $DependencyRegisterPath -PathType Leaf)) { throw 'DependencyNotAvailable: EXT-PARTIES-1 authoritative register is missing.' }
+$register = Get-Content -LiteralPath $DependencyRegisterPath -Raw
+$section = [regex]::Match($register, '(?ms)^### EXT-PARTIES-1\b.*?(?=^### |\z)').Value
+function Get-Commitment([string]$Field) {
+    $pattern = '(?m)^\|\s*`' + [regex]::Escape($Field) + '`\s*\|\s*(.*?)\s*\|\s*$'
+    return [regex]::Match($section, $pattern).Groups[1].Value.Trim().Trim([char]'`')
+}
+$acceptedTarget = Get-Commitment 'TargetVersionOrCommit'
+$acceptedDate = Get-Commitment 'TargetIntegrationDate'
+$acceptedContract = Get-Commitment 'CompatibilityContractAndVerificationCommand'
+$acceptedCommand = [regex]::Match($acceptedContract, '(?s)Command:\s*`([^`]+)`').Groups[1].Value
+$integrationDate = [DateTimeOffset]::MinValue
+if ((Get-Commitment 'AcceptedStatus') -cne 'Available' -or $acceptedTarget -cne $env:EXT_PARTIES_TARGET_SHA -or
+    -not [DateTimeOffset]::TryParse($acceptedDate, [ref]$integrationDate) -or
+    [string]::IsNullOrWhiteSpace($acceptedCommand) -or $acceptedCommand -eq 'TBD') {
+    throw 'DependencyNotAvailable: EXT-PARTIES-1 requires Available, the exact accepted target/date/command and passing prerequisite evidence before any live read.'
+}
+try { $receipt = Get-Content -LiteralPath $env:EXT_PARTIES_COMPATIBILITY_RECEIPT -Raw | ConvertFrom-Json -AsHashtable }
+catch { throw 'DependencyNotAvailable: EXT-PARTIES-1 compatibility receipt is missing or malformed.' }
+if ($receipt.recordId -cne 'EXT-PARTIES-1' -or $receipt.targetVersionOrCommit -cne $acceptedTarget -or
+    $receipt.compatibilityCommand -cne $acceptedCommand -or $receipt.outcome -cne 'Pass' -or
+    $receipt.evidenceLevel -lt 4 -or $receipt.prerequisitesAvailable -ne $true) {
+    throw 'DependencyNotAvailable: EXT-PARTIES-1 exact-target compatibility/prerequisite proof does not match the accepted record.'
+}
+$retention = [TimeSpan]::Zero
+if (-not [TimeSpan]::TryParse($env:EXT_PARTIES_RETENTION, [ref]$retention) -or $retention -le [TimeSpan]::Zero -or
+    $env:EXT_PARTIES_EXPIRY_TRIGGER -cne 'binding-effective-at' -or
+    $receipt.policyId -cne $env:EXT_PARTIES_POLICY_ID -or $receipt.retention -cne $env:EXT_PARTIES_RETENTION -or
+    $receipt.expiryTrigger -cne $env:EXT_PARTIES_EXPIRY_TRIGGER) {
+    throw 'DependencyNotAvailable: EXT-PARTIES-1 requires matching explicit supported production retention policy and custody prerequisites.'
+}
+$base = [Uri]$env:EXT_PARTIES_GATEWAY_URL
+if (-not $base.IsAbsoluteUri -or $base.Scheme -notin @('http', 'https') -or $base.UserInfo -or $base.Query -or $base.Fragment) {
+    throw 'The authenticated owner transport must be an absolute HTTP(S) base URL without user information.'
+}
+try { $headers = ConvertFrom-Json -InputObject $env:EXT_PARTIES_READER_HEADERS_JSON -AsHashtable }
+catch { throw 'Owner transport headers are malformed.' }
+if ($headers -isnot [System.Collections.IDictionary] -or $headers.Count -eq 0 -or
+    @($headers.Keys | Where-Object { $_ -notin @('Authorization', 'dapr-api-token') }).Count) {
+    throw 'Supply owner-issued Authorization and/or dapr-api-token headers; caller identity headers cannot be fabricated.'
+}
+$bindingVersion = 0L
+$actionAt = [DateTimeOffset]::MinValue
+if (-not [long]::TryParse($env:EXT_PARTIES_HISTORY_BINDING_VERSION, [ref]$bindingVersion) -or $bindingVersion -le 0 -or
+    -not [DateTimeOffset]::TryParse($env:EXT_PARTIES_HISTORY_ACTION_AT, [ref]$actionAt) -or
+    $env:EXT_PARTIES_HISTORY_ACTOR_ID -cnotmatch '^[0-7][0-9ABCDEFGHJKMNPQRSTVWXYZ]{25}$') {
+    throw 'The owner fixture must supply an exact recorded actor, binding version and action instant.'
+}
+
+function Invoke-OwnerRead([string]$RelativePath, [hashtable]$Payload) {
+    $target = [Uri]::new($base.AbsoluteUri.TrimEnd('/') + '/' + $RelativePath)
+    $body = ConvertTo-Json -InputObject $Payload -Depth 12 -Compress
+    $response = Invoke-WebRequest -Uri $target -Method Post -Headers $headers -ContentType 'application/json' -Body $body -TimeoutSec 30 -SkipHttpErrorCheck
+    if ([int]$response.StatusCode -lt 200 -or [int]$response.StatusCode -ge 300) {
+        throw ('Owner read denied or unavailable at the authenticated transport (HTTP ' + [int]$response.StatusCode + ').')
+    }
+    if ([Text.Encoding]::UTF8.GetByteCount($response.Content) -gt 32MB) { throw 'Owner response exceeds the source bound.' }
+    try { return ConvertFrom-Json -InputObject $response.Content -AsHashtable }
+    catch { throw 'Owner response is malformed.' }
+}
+
+$identity = @{ tenantId=$env:EXT_PARTIES_TENANT_A; domain='party'; aggregateId=$env:EXT_PARTIES_HISTORY_PARTY_ID }
+$retained = Invoke-OwnerRead 'api/v1/identity-history/read' @{ identity=$identity; purpose='party-actor-history-v1' }
+$source = $retained.stream
+if ($null -eq $source -or $retained.failureReason -or $source.purpose -ne 'party-actor-history-v1' -or
+    $source.identity.tenantId -cne $identity.tenantId -or $source.identity.domain -cne 'party' -or
+    $source.identity.aggregateId -cne $identity.aggregateId -or $source.head -le 0 -or $source.head -gt 10000 -or
+    [string]::IsNullOrWhiteSpace($source.observationId) -or [string]::IsNullOrWhiteSpace($source.authorityRevision) -or
+    -not $source.observedAt -or [DateTimeOffset]$source.observedAt -gt [DateTimeOffset]::UtcNow -or
+    -not $source.validUntil -or [DateTimeOffset]$source.validUntil -le [DateTimeOffset]::UtcNow) {
+    throw 'Independent retained history has no exact authoritative source certificate.'
+}
+$covered = [Collections.Generic.HashSet[long]]::new()
+$payloadBytes = 0L
+$previous = 0L
+foreach ($item in $source.events) {
+    $position = [long]$item.sequenceNumber
+    if ($position -le $previous -or $position -gt $source.head -or -not $covered.Add($position) -or
+        $null -eq $item.protectionMetadata -or $item.protectionMetadata.state -notin @(0, 'Unprotected') -or
+        $item.eventTypeName -cnotmatch '^Hexalith\.Parties\.Contracts\.Events\.HumanActorBinding(Established|Rebound|Revoked)$' -or
+        $item.serializationFormat -cne 'json' -or $null -eq $item.payload -or
+        $item.messageId -cne '' -or $null -ne $item.userId -or $null -ne $item.correlationId -or $null -ne $item.causationId) {
+        throw 'The retained source contains an invalid original position, profile substitution or unreadable event.'
+    }
+    try { $payloadBytes += [Convert]::FromBase64String($item.payload).LongLength }
+    catch { throw 'The retained source payload is not readable base64 JSON.' }
+    if ($payloadBytes -gt 16MB) { throw 'The retained source exceeds the decoded payload bound.' }
+    $previous = $position
+}
+$previous = 0L
+foreach ($excluded in $source.excludedSequences) {
+    $position = [long]$excluded
+    if ($position -le $previous -or $position -gt $source.head -or -not $covered.Add($position)) { throw 'The source exclusion certificate overlaps or is unordered.' }
+    $previous = $position
+}
+if ($covered.Count -ne $source.head) { throw 'The retained source certificate has a gap.' }
+
+$queryPayload = @{ tenantId=$identity.tenantId; partyId=$identity.aggregateId; actionAt=$actionAt.ToString('O');
+    expectedActorId=$env:EXT_PARTIES_HISTORY_ACTOR_ID; expectedBindingVersion=$bindingVersion }
+$response = Invoke-OwnerRead 'api/v1/queries' @{ tenant=$identity.tenantId; domain='party'; aggregateId=$identity.aggregateId;
+    queryType='Hexalith.Parties.Contracts.Queries.ResolveHumanActorBindingAt'; projectionType='party'; entityId=$identity.aggregateId; payload=$queryPayload }
+$history = $response.payload
+if ($response.success -ne $true -or $response.metadata.isDegraded -eq $true -or $response.metadata.isStale -eq $true -or
+    $null -eq $history -or $history.outcome -notin @(1, 'Resolved') -or $history.contractVersion -ne 1 -or
+    $history.tenantId -cne $identity.tenantId -or $history.partyId -cne $identity.aggregateId -or
+    [DateTimeOffset]$history.actionAt -ne $actionAt -or $history.evidence.actorId -cne $env:EXT_PARTIES_HISTORY_ACTOR_ID -or
+    $history.evidence.bindingVersion -ne $bindingVersion -or $history.bindingSourcePosition -le 0 -or
+    $history.bindingSourcePosition -gt $history.sourcePosition -or $history.sourcePosition -lt $source.head -or
+    [DateTimeOffset]$history.evidence.validFrom -gt $actionAt -or $null -eq $history.evidence.validUntil -or
+    [DateTimeOffset]$history.evidence.validUntil -le $actionAt -or $history.evidence.custody.purpose -ne 'party-actor-history-v1' -or
+    $history.evidence.custody.policyId -cne $env:EXT_PARTIES_POLICY_ID -or
+    $history.evidence.custody.sourceExpiryEnforced -ne $true -or $history.evidence.custody.restoreSafe -ne $true -or
+    $history.evidence.custody.derivedCopiesCovered -ne $true -or $history.evidence.custody.lifecycleRevision -le 0 -or
+    [DateTimeOffset]$history.evidence.custody.expiresAt -le [DateTimeOffset]::UtcNow) {
+    throw 'The gateway did not reproduce the exact retained action-time binding and original source position.'
+}
+$openingEvent = @($source.events | Where-Object { $_.sequenceNumber -eq $history.bindingSourcePosition })
+if ($openingEvent.Count -ne 1) { throw 'The returned opening position is absent from the independently read retained source.' }
+$decoded = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($openingEvent[0].payload)) | ConvertFrom-Json -AsHashtable
+if ($decoded.binding.evidence.actorId -cne $history.evidence.actorId -or $decoded.binding.evidence.bindingVersion -ne $bindingVersion -or
+    $decoded.binding.evidence.tenantId -cne $identity.tenantId -or $decoded.binding.evidence.partyId -cne $identity.aggregateId) {
+    throw 'The returned binding does not match its original retained source payload.'
+}
+
+$current = Invoke-OwnerRead 'api/v1/queries' @{ tenant=$identity.tenantId; domain='party'; aggregateId=$identity.aggregateId;
+    queryType='Hexalith.Parties.Contracts.Queries.ResolvePartyIdentity'; projectionType='party'; entityId=$identity.aggregateId;
+    payload=@{ tenantId=$identity.tenantId; partyId=$identity.aggregateId; expectedActorId=$env:EXT_PARTIES_HISTORY_ACTOR_ID } }
+if ($current.success -ne $true -or $null -eq $current.payload -or
+    $current.metadata.isDegraded -eq $true -or $current.metadata.isStale -eq $true -or
+    $current.payload.outcome -notin @(0, 2, 'Unavailable', 'Ineligible') -or $current.payload.evidence.humanBinding) {
+    throw 'The owner erased-profile fixture unexpectedly remains currently eligible.'
+}
+if ([DateTimeOffset]$source.validUntil -le [DateTimeOffset]::UtcNow -or
+    [DateTimeOffset]$history.evidence.custody.expiresAt -le [DateTimeOffset]::UtcNow) {
+    throw 'The retained source certificate or custody expired during readiness probes.'
+}
+$evidence = @{ qualification='ReadinessOnly'; requestedTargetSha=$env:EXT_PARTIES_TARGET_SHA; observedAt=[DateTimeOffset]::UtcNow.ToString('O');
+    checks=@('complete-original-position-source-partition', 'exact-action-time-actor-and-version', 'original-binding-source-position', 'current-identity-ineligible');
+    sourceHead=$history.sourcePosition; bindingSourcePosition=$history.bindingSourcePosition;
+    incomplete=@('P-01-P-10-installed-matrix', 'independent-erasure-certification', 'persisted-restart-restore', 'failure-injection', 'production-custody-destruction-and-copy-cleanup') }
+New-Item -ItemType Directory -Path $EvidenceDirectory -Force | Out-Null
+$evidence | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $EvidenceDirectory 'live-readiness.json')
+Write-Host 'Authenticated retained-history readiness probes passed for the owner-provided fixture. Erasure certification and complete Live qualification remain unavailable.'

diff --git a/parties/src/Hexalith.Parties/Queries/RetainedHumanActorBinding.cs b/parties/src/Hexalith.Parties/Queries/RetainedHumanActorBinding.cs
new file mode 100644
index 00000000..97aa4d1f
--- /dev/null
+++ b/parties/src/Hexalith.Parties/Queries/RetainedHumanActorBinding.cs
@@ -0,0 +1,8 @@
+using Hexalith.Parties.Contracts.Models;
+
+namespace Hexalith.Parties.Queries;
+
+/// <summary>A retained interval and the immutable source position that opened it.</summary>
+/// <param name="Evidence">The recorded actor and half-open interval.</param>
+/// <param name="SourcePosition">The original establishment or rebind event position.</param>
+internal sealed record RetainedHumanActorBinding(HumanActorBindingEvidence Evidence, long SourcePosition);

diff --git a/parties/src/Hexalith.Parties/Queries/RetainedHumanActorHistoryFold.cs b/parties/src/Hexalith.Parties/Queries/RetainedHumanActorHistoryFold.cs
new file mode 100644
index 00000000..4fdcbd0f
--- /dev/null
+++ b/parties/src/Hexalith.Parties/Queries/RetainedHumanActorHistoryFold.cs
@@ -0,0 +1,139 @@
+using System.Text.Json;
+
+using Hexalith.EventStore.Contracts.Security;
+using Hexalith.EventStore.Contracts.Streams;
+using Hexalith.Parties.Contracts;
+using Hexalith.Parties.Contracts.Events;
+using Hexalith.Parties.Contracts.Models;
+using Hexalith.Parties.Contracts.State;
+
+namespace Hexalith.Parties.Queries;
+
+/// <summary>Folds an authenticated sparse attribution partition without reading an erased profile.</summary>
+internal static class RetainedHumanActorHistoryFold
+{
+    /// <summary>Preserves original binding positions and rejects incomplete, foreign or inconsistent lifecycle history.</summary>
+    public static IReadOnlyList<RetainedHumanActorBinding> Fold(RetainedIdentityHistoryStream stream)
+    {
+        ArgumentNullException.ThrowIfNull(stream);
+        if (stream.Identity.Domain != "party" || !RetainedIdentityHistoryValidator.IsComplete(
+                new(stream.Identity, RetainedIdentityHistoryReadRequest.AttributionPurpose), stream, stream.ObservedAt))
+        {
+            throw InvalidHistory();
+        }
+
+        var bindings = new List<RetainedHumanActorBinding>();
+        var logicalIds = new HashSet<string>(StringComparer.Ordinal);
+        long version = 0;
+        foreach (StreamReadEvent item in stream.Events)
+        {
+            string eventName = item.EventTypeName;
+            if (eventName == typeof(HumanActorBindingEstablished).FullName)
+            {
+                HumanActorBindingEstablished value = JsonSerializer.Deserialize<HumanActorBindingEstablished>(item.Payload, PartiesJsonOptions.Default)
+                    ?? throw InvalidHistory();
+                if (version != 0 || value.ExpectedBindingVersion != 0)
+                {
+                    throw InvalidHistory();
+                }
+
+                Append(value.Binding, value.EffectiveAt, value.ExpectedBindingVersion, item.SequenceNumber);
+            }
+            else if (eventName == typeof(HumanActorBindingRebound).FullName)
+            {
+                HumanActorBindingRebound value = JsonSerializer.Deserialize<HumanActorBindingRebound>(item.Payload, PartiesJsonOptions.Default)
+                    ?? throw InvalidHistory();
+                if (version == 0 || value.ExpectedBindingVersion != version)
+                {
+                    throw InvalidHistory();
+                }
+
+                Close(value.ExpectedBindingVersion, value.EffectiveAt, requireLivePredecessor: false);
+                Append(value.Binding, value.EffectiveAt, value.ExpectedBindingVersion, item.SequenceNumber);
+            }
+            else if (eventName == typeof(HumanActorBindingRevoked).FullName)
+            {
+                HumanActorBindingRevoked value = JsonSerializer.Deserialize<HumanActorBindingRevoked>(item.Payload, PartiesJsonOptions.Default)
+                    ?? throw InvalidHistory();
+                if (value.ExpectedBindingVersion != version || !ValidCustody(value.Custody)
+                    || value.EffectiveAt > stream.ObservedAt || string.IsNullOrWhiteSpace(value.IntentDigest)
+                    || string.IsNullOrWhiteSpace(value.LogicalId) || !logicalIds.Add(value.LogicalId))
+                {
+                    throw InvalidHistory();
+                }
+
+                Close(value.ExpectedBindingVersion, value.EffectiveAt, requireLivePredecessor: true);
+                version = checked(version + 1);
+            }
+            else
+            {
+                // An excluded profile position is certified separately; a readable profile or unknown
+                // event may never be substituted for a retained attribution transition.
+                throw InvalidHistory();
+            }
+        }
+
+        return bindings.AsReadOnly();
+
+        void Append(HumanActorBinding binding, DateTimeOffset at, long expectedVersion, long position)
+        {
+            HumanActorBindingEvidence? evidence = binding?.Evidence;
+            if (evidence is null || evidence.TenantId != stream.Identity.TenantId || evidence.PartyId != stream.Identity.AggregateId
+                || expectedVersion != version || evidence.BindingVersion != checked(expectedVersion + 1)
+                || evidence.ActorRevision <= 0 || !CanonicalActorId(evidence.ActorId)
+                || evidence.ValidFrom != at || at > stream.ObservedAt
+                || evidence.ValidUntil is not { } end || end <= at || !ValidCustody(evidence.Custody)
+                || end > evidence.Custody.ExpiresAt || string.IsNullOrWhiteSpace(evidence.SourceId)
+                || string.IsNullOrWhiteSpace(evidence.ProvenanceId) || string.IsNullOrWhiteSpace(binding!.IntentDigest)
+                || string.IsNullOrWhiteSpace(binding.LogicalId) || !logicalIds.Add(binding.LogicalId)
+                || bindings.Any(previous => previous.Evidence.ValidFrom >= at || previous.Evidence.ValidUntil > at))
+            {
+                throw InvalidHistory();
+            }
+
+            bindings.Add(new(evidence, position));
+            version = evidence.BindingVersion;
+        }
+
+        void Close(long expectedVersion, DateTimeOffset at, bool requireLivePredecessor)
+        {
+            if (version != expectedVersion || at > stream.ObservedAt)
+            {
+                throw InvalidHistory();
+            }
+
+            int index = bindings.FindIndex(binding => binding.Evidence.BindingVersion == expectedVersion);
+            if (index < 0)
+            {
+                if (requireLivePredecessor)
+                {
+                    throw InvalidHistory();
+                }
+
+                return;
+            }
+
+            RetainedHumanActorBinding previous = bindings[index];
+            if (previous.Evidence.ValidFrom >= at || requireLivePredecessor && previous.Evidence.ValidUntil <= at)
+            {
+                throw InvalidHistory();
+            }
+
+            if (previous.Evidence.ValidUntil > at)
+            {
+                bindings[index] = previous with { Evidence = previous.Evidence with { ValidUntil = at } };
+            }
+        }
+    }
+
+    private static bool ValidCustody(IdentityHistoryCustodyEvidence? evidence)
+        => evidence is { Purpose: RetainedIdentityHistoryReadRequest.AttributionPurpose, LifecycleRevision: > 0,
+                SourceExpiryEnforced: true, RestoreSafe: true, DerivedCopiesCovered: true }
+            && !string.IsNullOrWhiteSpace(evidence.PolicyId) && !string.IsNullOrWhiteSpace(evidence.EvidenceId);
+
+    private static bool CanonicalActorId(string? actorId)
+        => actorId is { Length: 26 } && actorId[0] <= '7'
+            && actorId.All(character => "0123456789ABCDEFGHJKMNPQRSTVWXYZ".Contains(character, StringComparison.Ordinal));
+
+    private static InvalidOperationException InvalidHistory() => new("Retained attribution history is inconsistent.");
+}

diff --git a/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityRetentionConfigurationTests.cs b/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityRetentionConfigurationTests.cs
new file mode 100644
index 00000000..e72f7883
--- /dev/null
+++ b/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityRetentionConfigurationTests.cs
@@ -0,0 +1,49 @@
+using Hexalith.EventStore.Contracts.Security;
+using Hexalith.Parties.Authorization;
+
+using Microsoft.Extensions.Configuration;
+using Microsoft.Extensions.DependencyInjection;
+using Microsoft.Extensions.Options;
+
+using Shouldly;
+
+namespace Hexalith.Parties.Tests.Gateway;
+
+/// <summary>Verifies the approved host policy through the same options binding used by Parties.</summary>
+public sealed class PartyIdentityRetentionConfigurationTests
+{
+    /// <summary>The actual host JSON supplies fixed days, without extending the clock after erasure or restore.</summary>
+    [Fact]
+    public void HostConfiguration_BindsApproved365FixedDays()
+    {
+        IConfigurationRoot configuration = LoadConfiguration();
+        using ServiceProvider services = new ServiceCollection()
+            .AddOptions<PartyIdentityOptions>().Bind(configuration.GetSection("Parties:Identity"))
+            .Services.BuildServiceProvider();
+        IdentityHistoryPolicy policy = services.GetRequiredService<IOptionsMonitor<PartyIdentityOptions>>().CurrentValue.Policy!;
+        policy.ShouldNotBeNull();
+        policy.PolicyId.ShouldBe("party-actor-retention-v1");
+        policy.ExpiryTrigger.ShouldBe("binding-effective-at");
+        policy.Retention.TotalSeconds.ShouldBe(31_536_000);
+        var effectiveAt = new DateTimeOffset(2027, 3, 1, 0, 0, 0, TimeSpan.Zero);
+        policy.DeriveExpiry(effectiveAt).ShouldBe(new DateTimeOffset(2028, 2, 29, 0, 0, 0, TimeSpan.Zero));
+        services.GetService<IIdentityHistoryCustody>().ShouldBeNull();
+    }
+
+    /// <summary>A later configuration source can withdraw policy without installing a default.</summary>
+    [Fact]
+    public void HostConfiguration_PolicyWithdrawalDeniesBindings()
+    {
+        IConfigurationRoot configuration = new ConfigurationBuilder().AddConfiguration(LoadConfiguration())
+            .AddInMemoryCollection(new Dictionary<string, string?> { ["Parties:Identity:PolicyId"] = "" }).Build();
+        using ServiceProvider services = new ServiceCollection()
+            .AddOptions<PartyIdentityOptions>().Bind(configuration.GetSection("Parties:Identity"))
+            .Services.BuildServiceProvider();
+        services.GetRequiredService<IOptionsMonitor<PartyIdentityOptions>>().CurrentValue.Policy.ShouldBeNull();
+        new PartyIdentityOptions().Policy.ShouldBeNull();
+    }
+
+    private static IConfigurationRoot LoadConfiguration() => new ConfigurationBuilder()
+        .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "Configuration", "parties-appsettings.json"), optional: false)
+        .Build();
+}
</unified-diff>

Do not invoke any skill, and do not spawn subagents of your own — you are the reviewer. Return your findings as text in your final message; do not route them through any findings-reporting tool the host may offer.
