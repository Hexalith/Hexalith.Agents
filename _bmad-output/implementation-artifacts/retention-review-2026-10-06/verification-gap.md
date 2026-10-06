Read `# Verification Gap Review

**Goal:** Find changed behavior that could break without reliable verification catching it. Ask one question — "if the behavior this change is supposed to produce broke where it's actually used, would verification fail?" Do not hunt for correctness bugs, but report genuine problems you notice while tracing verification.

The main verification gap shapes are:

1. **Regression gap:** the changed code regresses where it's used, and no test covering that use would fail.
2. **Missing-adoption gap:** a place that should now use the new behavior doesn't; it handles the same case its own way, or not at all, and no test would flag the omission.
3. **Broken-verification gap:** a test appears to cover the changed behavior, but would not actually protect it because it is skipped, flaky, not run in the normal verification path, or too weak to observe the regression.

## Evidence Rules

- Read a test before claiming what it covers, runs, asserts, or misses.
- Before claiming no test exists, search the whole repo by the symbol under test and by import references; expected file locations are not enough.
- Never assert what you did not verify. If a finding cannot be grounded, drop it.
- In a finding, say what you actually checked — "none of the tests I read cover this" — and show how far you looked. Say a test doesn't exist anywhere only when the symbol/import-reference search actually shows that.
- Do not assign severity, confidence, priority, or ranking.

## Review Sequence

### Step 1: Screen for behavioral change

Screen each part of the change separately. If a part is non-behavioral, skip it. Call a part non-behavioral only when the changed code does not alter return values, thrown errors, caller-visible side effects, or observable state (including iteration order and emitted messages). Once a part meets that test, move on; do not inspect callers or tests for extra confirmation.

Common non-behavioral examples: formatting, comments, whitespace; pure renames; trivial getters/setters and pass-throughs; type-only or compiler-enforced changes with no runtime effect; etc.

Only outcomes produced by deterministic code are worth automatically testing; tests are useless on static source text and brittle on LLM output. Skip those parts.

If every part is skipped, output the clean result (see Output Format).

### Step 2: Find the behavior that changed

Identify what behavior changed compared to the previous version: output, side effect, branch, error path, schema/event shape, config default, validation/authorization rule, external contract, etc. If the change affects more than one behavior, handle each separately.

Treat broad-impact changes as behavioral even when no single changed line looks important: dependency, toolchain, build/config, data-file, etc.

### Step 3: Trace where that behavior is used

Trace the changed behavior to the places that observe it. Start with direct callers and registered entry points (routes, commands, DI), contract consumers (schemas, events, APIs, database readers), and reverse-dependency info if already available.

Follow a path only while the changed behavior is reachable and unverified. Stop when a test at that boundary would fail, the consumer does not observe the changed behavior, or the next hop is guesswork (dynamic dispatch, reflection, outside-repo consumers, etc.). Prefer the nearest observable boundary, often one to three hops away, especially across contract, integration, or service edges. If there are more than five similar consumers, group obvious repeats and check representative paths; expand only when a consumer observes the behavior differently.

### Step 4: Qualify the consumer, then check its test

For each consumer, name the smallest realistic regression this consumer would observe: invert the branch, drop the default, omit the field, return the old error code, skip the integration call, etc. This is the Demonstration. If no such regression exists, drop the path; untested downstream code is not a finding.

A `Missing-adoption gap` qualifies not by the adoption failure alone but by a supersession signal: the change gives clear evidence the new behavior is meant to replace the local one — PR intent, naming or docs, a replaced sibling site, deleted duplicate logic, or a test defining the new rule — and the local site shares the same observable contract. Without a supersession signal and a shared observable contract, it is a refactor suggestion, not a verification-gap finding. Once both hold, check whether any test for that site would flag the non-adoption; missing coverage of the non-adoption is the gap itself, not a disqualifier.

Find and read the relevant test. Ask whether the Demonstration would make an assertion fail.

- If yes, the behavior is verified. No finding.
- For a regression-style Demonstration: if no test runs the path, the test is skipped/flaky/not run normally, or the test runs the code without checking the changed result, report a `Regression gap` or `Broken-verification gap`.
- For a qualifying Missing-adoption case: if none of the site tests you found assert it adopts the new behavior, report a `Missing-adoption gap`.

A test counts only if it runs normally and an assertion observes the changed output, branch, or contract. These do not count: no execution; source-text assertions that match a file's wording instead of running it; success/no-throw/snapshot-only checks; mock/log-call checks; human-only checks; tests that mock away the integration; e2e tests that pass through without checking the changed output; stale assertions or fixtures.

For example, `expect(x ?? DEFAULT).toBe(DEFAULT)` passes when `x` is missing.

Common patterns:

- **Caller-path gap** — helper test covers the branch, but caller values skip it.
- **Contract drift** — payload/schema/event changes must be verified at the consumer.
- **Migration compatibility** — tests only create new-format rows or fresh schemas.
- **Phantom exception** — handled partial-failure path has no test.
- **Missing-adoption gap** — sibling site should use the new rule/helper and does not.
- **Removed verification** — deleted test or weakened assertion leaves behavior unpinned; removing a source-text assertion is not this, since it never counted.

### Step 5: Confirm each finding is real

Before writing a finding, re-open the specific tests or search results the finding relies on. Verify the Demonstration would not make any test you checked fail, or that the absence claim is backed by the symbol/import-reference search. Do not claim more than you verified; drop any finding you cannot ground.

Explain why the test misses the bug using what the test sets up and checks.

Do not report: compiler/type-checker-enforced cases; behavior already verified by an integration, contract, or e2e test; implementation-detail or mock-only tests; low coverage or a missing test file by itself; legacy untested code the change did not affect.

Report genuine problems you noticed while tracing verification, even if they are not verification gaps. Put them under `Other findings` in the output. This permits reporting what you already reached, not extra hunting. A claim that code misbehaves is a defect, not a gap — it goes under `Other findings` for standard triage, however you found it.

## OUTPUT FORMAT

Emit each verification-gap finding as one block. No general advice, no severity or confidence. Triage trusts a gap finding as filed and does not re-verify it, so each block must stand on its own evidence.

```markdown
### <one-line title naming the gap>

- **Changed surface:** the exact behavior or contract that changed — `file:line`.
- **Impacted consumer or site:** named concretely with `file:line` (e.g. "the `createInvoice` mutation used by the billing dashboard at `billing/dashboard.ts:88`," not "callers of this function").
- **Existing test evidence:**
  - `Regression gap`: what the relevant test actually asserts, with `file:line`; or, if none, the symbol/import-reference searches run and their result.
  - `Missing-adoption gap`: tests for the impacted site, and whether any assert it adopts the new behavior.
  - `Broken-verification gap`: the apparent test or verification path, and why it does not count.
- **Missing verification:** the precise assertion or check that's absent.
- **Demonstration:**
  - `Regression gap` / `Broken-verification gap`: the concrete regression that would ship undetected, and why the tests you checked would not fail.
  - `Missing-adoption gap`: the case the site mishandles by not adopting the new behavior, and that none of the tests you read assert adoption.
- **Consequence:** the concrete thing that ships wrong — a regression the checked evidence would not catch, or a site that should use the new behavior and doesn't.
- **Disposition:** `patch` — name the test to add, fit to the repo's own way of verifying (don't impose a generic test pyramid) — or `defer` when the gap is real but not worth closing as part of this change, with one sentence of why.
```

If you noticed genuine non-gap problems while tracing verification, append:

```markdown
## Other findings

- <description only; no severity, confidence, priority, or ranking>
```

When you find no verification gaps and no other findings, output exactly this single line, not an empty response:

`No verification gaps found.`

## CONTENT SOURCE

"Review content:" in the message that launched you gives the content itself or a path to read it from. Read the file when it is a path; either way that is the content under review, and this instruction file never is. If no content is supplied, or the file it points to is missing, empty, or unreadable, say exactly that and stop — never report a clean review for content you could not read.
` completely and follow it as your review instructions.

Review content: the unified diff at `diff --git a/agents/_bmad-output/implementation-artifacts/spec-5-4-prove-trusted-principal-tenant-party-and-approver-readiness.md b/agents/_bmad-output/implementation-artifacts/spec-5-4-prove-trusted-principal-tenant-party-and-approver-readiness.md
index 4eace03..177556e 100644
--- a/agents/_bmad-output/implementation-artifacts/spec-5-4-prove-trusted-principal-tenant-party-and-approver-readiness.md
+++ b/agents/_bmad-output/implementation-artifacts/spec-5-4-prove-trusted-principal-tenant-party-and-approver-readiness.md
@@ -41,12 +41,12 @@ context:
 
 ## Open Questions
 
-- **Owner packets (delivery blocker):** Complete owner-accepted commitments for EXT-PARTIES-1, EXT-CONV-AI-1, EXT-SECRETS-1 and EXT-HOST-1 remain pending. Each needs the full contract, immutable target, integration date and executable verification command. Supply packet links to reconcile the register; until delivery, retain draft. The user approved this spec on 2026-10-04; full scope and Branch B are settled and need no further spec approval.
+- **Owner packets (delivery blocker; rechecked 2026-10-06):** Complete owner-accepted commitments for EXT-PARTIES-1, EXT-CONV-AI-1, EXT-SECRETS-1 and EXT-HOST-1 remain pending. Each needs the full contract, immutable target, integration date and executable verification command. Parties and Platform sibling implementation has advanced, but installed references and acceptance gates have not. Supply packet links to reconcile the register; until delivery, retain draft. The user approved this spec on 2026-10-04; full scope and Branch B are settled and need no further spec approval.
 
 ## Code Map
 
 - `src/Hexalith.Agents.Server/Ports/HttpAgentAdministrationContextProvider.cs` — replace JWT role authority with fresh operation-specific evidence; Tenants `TenantProjectionEventHandler` still accepts sequence gaps/equal conflicts and has no global-administrator consumer.
-- `src/Hexalith.Agents.Server/Ports/PartiesAgentPartyDirectory.cs` — verify explicit tenant/ID/freshness and preserve Organization ID; sibling identity/history work is uncommitted and its Live verifier remains fail-closed.
+- `src/Hexalith.Agents.Server/Ports/PartiesAgentPartyDirectory.cs` — verify explicit tenant/ID/freshness and preserve Organization ID. The installed Parties reference lacks the identity/history contract; sibling `IPartiesIdentityClient` and `PartyIdentityQueryService` are now committed, but production custody/retained-history and the Live verifier remain incomplete. Map provisioned Organization type and safe Agent classification only through the final accepted contract.
 - `src/Hexalith.Agents.Server/Ports/IApproverPolicyResolver.cs` and `src/Hexalith.Agents.Server/Application/Agents/ApproverPolicyVerdict.cs` — add actor/binding/source correspondence; reject unknown outcomes and Caller. Preserve historical deserialization/folds.
 - `src/Hexalith.Agents.Server/Application/Queries/{AgentSetupQueryHandlerBase,AgentInteractionAuditQueryHandlerBase}.cs` — replace global-admin flag bypasses before protected reads; `AgentInteractionGateOrchestrator` must stop downstream reads on authority denial.
 - `src/Hexalith.Agents.EventStore/AgentsTrustedCommandExtensionPolicy.cs` — replace unsigned flags; existing digest/identity primitives do not supply AD-30 custody/replay. Preserve Story 5.3 outcomes.
@@ -70,6 +70,7 @@ context:
 ## Implementation Notes
 
 - 2026-10-04: Recorded the user's "I accept" as specification approval. No owner target, integration date or executable command was supplied by that response, and no dependency gate was waived. Preserve the approved frozen intent and acceptance criteria; retain draft/backlog pending complete owner commitments. Resume from this approval when the register's entry gate is satisfied.
+- 2026-10-06: Rechecked owner issues, installed references and sibling source. Parties identity/history and Platform identity hosting are now committed in sibling repositories; full live contracts and owner packets remain missing. Preserve prior approval, frozen intent, acceptance criteria, full scope and Branch B; retain draft/backlog. No implementation or consumer seam execution occurred.
 
 ## Spec Change Log
 
@@ -78,12 +79,13 @@ context:
 - 2026-10-03: Updated source/owner recheck and Code Map after reference updates. All four records remain Uncommitted; no complete new contract or owner acceptance was found. KEEP full scope, Branch B direction, frozen intent, original acceptance and draft/backlog gates.
 - 2026-10-03: Rechecked latest references and sibling identity progress; history custody/live verification remain incomplete. KEEP all prior scope, intent, acceptance and dependency gates.
 - 2026-10-04: Rechecked four owner requests and reference/sibling contracts. New Tenants/Conversations owner revisions add no required authority/six-seam source contract; Parties Live verification and complete custody/host commitments remain missing. Expanded the Code Map; KEEP frozen intent, original acceptance, full scope, Branch B and draft/backlog.
+- 2026-10-06: Corrected the Code Map's stale description of sibling Parties work as uncommitted; its identity implementation and Platform identity hosting have since been committed. No complete acceptance packet or live verifier was delivered. KEEP approved frozen intent, original acceptance criteria, full scope, Branch B and all entry/execution gates.
 
 ## Review Triage Log
 
 ## Design Notes
 
-[Recheck](story-5-4-dependency-recheck.md#owner-and-contract-gate-recheck--2026-10-04). No irreversible planning action. Footprint: domain, Server, EventStore, UI, tests. Contract entry remains blocked; source inspection cannot supply owner acceptance.
+[Recheck](story-5-4-dependency-recheck.md#owner-and-contract-gate-recheck--2026-10-06). No new intent decision or irreversible planning action. Footprint: domain, Server, EventStore, UI, tests. Contract entry remains blocked; source inspection cannot supply owner acceptance.
 
 ## Verification
 
diff --git a/agents/_bmad-output/implementation-artifacts/story-5-4-dependency-recheck.md b/agents/_bmad-output/implementation-artifacts/story-5-4-dependency-recheck.md
index 9433f47..9c09342 100644
--- a/agents/_bmad-output/implementation-artifacts/story-5-4-dependency-recheck.md
+++ b/agents/_bmad-output/implementation-artifacts/story-5-4-dependency-recheck.md
@@ -172,3 +172,41 @@ All four complete records remain `Uncommitted`. Required next input is links to
 Updated only this recheck and the draft's open question, Code Map and planning notes. Preserve frozen intent, original acceptance criteria, full scope, Branch B and draft/backlog. Verification is limited to preservation, document references and whitespace; no source implementation, build/test, owner message, Git mutation, deployment, compatibility command or consumer seam execution occurred. `eng/verify-story-5.4.ps1` remains absent and no live readiness or story completion is claimed.
 
 **Subsequent specification approval — 2026-10-04:** The user replied "I accept". Recorded approval in the spec frontmatter and notes; no further specification approval is required. The response supplies no immutable owner target, integration date or executable verification command and does not waive the register's entry/execution gates. All four records remain Uncommitted; draft/backlog and the approved frozen intent/acceptance criteria are preserved. This approval update changes documentation only.
+
+## Owner and contract gate recheck — 2026-10-06
+
+Resumed `bmad-build 5.4` from the accepted specification. The initial two documentation edits were committed externally before the VCS gate was evaluated; Agents was then clean on `main`, two commits ahead of its tracked remote. Those commits and all sibling work were preserved. Full scope, Branch B and the 2026-10-04 specification approval remain settled.
+
+### Inspected source positions
+
+| Checkout | Full HEAD |
+| --- | --- |
+| Agents | `b252fcfde51bd04317ce5843a0a42bfe4dce5170` |
+| Parties reference | `5388884eec84b16545fdc008b2fc04547b0ed5b6` |
+| Sibling Parties owner | `b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca` |
+| Tenants reference | `a46c127c8be1d8d6ecccdf0d710e25befa644cc6` |
+| Sibling Tenants owner | `f92bf34159c40b6f7fe12eb746565868422dc035` |
+| Conversations reference | `f415298801097caa5f745e6976d67a79e4e2aab4` |
+| Sibling Conversations owner | `9938aa8aa5cfc082de6be3d28eede22957b14a26` |
+| EventStore reference | `b51978dd1d2a3721ad239db2623e1560377c7583` |
+| Builds reference | `688eec9a4333245cc0ff7772115c769094471863` |
+| Platform reference | `c3c473a4aa8da421896e4795fcdb0f4d2771a0be` |
+| Sibling Platform owner | `3d8eaf400582f0698297d393f4330a54709c9a07` |
+
+Every identifier was obtained directly with `git rev-parse HEAD` in the owning checkout. Installed references are clean and unchanged from the prior recheck. Sibling Parties has six pre-existing planning/tracking document edits; sibling Platform has five staged administration-exposure files; sibling Tenants has a modified EventStore gitlink. Their HEADs do not describe those working-tree changes. Sibling Conversations is clean. No reference or sibling was mutated and no submodule operation occurred.
+
+### New implementation evidence and remaining contract gaps
+
+- **Parties:** Identity work previously described as uncommitted is now tracked in the sibling, introduced by `37d87f5a2869b076c651a58714d60f64647848bc`. `src/Hexalith.Parties.Client/Abstractions/IPartiesIdentityClient.cs` exposes per-call tenant provisioning/current/history operations. `src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs` checks admission before source reads, exact scope, current evidence and action-time actor/version with no current-binding fallback. `src/Hexalith.Parties/Domain/PartyAggregate.Identity.cs` and `src/Hexalith.Parties.Contracts/State/PartyState.cs` supply pure binding transitions and folds. The installed reference still lacks these contracts. The owner implementation preserves `PartyType.Organization` while safe evidence uses `PartyIdentityClassification.Agent`; the accepted contract must settle their mapping without treating either type alone as identity proof.
+- **Parties live delivery:** `../parties/eng/verify-ext-parties-1.ps1` still unconditionally exits 1 in Live mode. It identifies missing production custody, purpose-scoped retained-history source after profile erasure and authenticated persisted P-01–P-10/restart/restore/failure-injection lanes. `PartyPayloadProtectionService` has an optional history-custody seam, but `PartiesServiceCollectionExtensions` does not register a production provider. Owner notes still leave retention policy/duration/expiry and cleanup/restore guarantees pending. Static source progress and recorded owner unit-test results establish no new live evidence; no verifier was executed here.
+- **Platform:** Sibling `src/Hexalith.Platform.Identity/PlatformIdentityServiceCollectionExtensions.cs` and `PlatformActorRegistry.cs` now commit purpose-separated admission signing/verification and a private EventStore actor registry. `src/Hexalith.Platform.EventStoreHost/Program.cs` and `apphost.cs` compose that identity host for the development preview. These are reusable primitives, not the complete AD-29/30 canonical HMAC/custody profile, issuer/nonce replay, replicated denial spool/recovery worker, restricted replay/security capabilities or expanded v21/v23 host contract. Both `eng/verify-agents-host.sh` copies still explicitly defer live Agents composition to Story 5.6; `docs/ext-host-1-agents-composition.md` remains the historical scaffold contract.
+- **Conversations/Tenants:** The newer Conversations owner has no `src/` change since the previous inspected owner revision. `IConversationClient` still supplies general get/append and `ConversationDetailResult.Hidden` still collapses deletion/removal into Forbidden; complete six-seam delivery is missing. The Tenants owner's new source changes affect UI; `TenantProjectionEventHandler.ApplyAsync` still accepts equal/jumping sequences, and registration defaults to an in-memory projection store with no global-administrator consumer. Reuse `TenantIdentity.ForGlobalAdministrators()` for the exact authority address, without treating nullable last-event metadata as current contiguous proof.
+- **Agents:** Authority/approver readers remain deferred; `HttpAgentAdministrationContextProvider.GetContext()` still derives roles from JWT claims. `PartiesAgentPartyDirectory` still uses ambient tenant scope and nullable freshness, while `ApproverPolicyVerdict` lacks exact actor/binding/source correspondence. Caller remains accepted/offered, setup/audit queries retain global-admin flag bypasses, and `AgentInteractionGateOrchestrator.ExecuteAsync` continues downstream reads after tenant denial. Trusted extensions remain unsigned literals; there is no replay/security-spool implementation or `eng/verify-story-5.4.ps1`. Story 5.3's verified catalog, tenant enablement and command-outcome behavior remain continuity constraints.
+
+### Owner acceptance and disposition
+
+Fresh authenticated `gh issue view --json number,title,state,url,body,comments` reads found [Parties #54](https://github.com/Hexalith/Hexalith.Parties/issues/54), [Conversations #4](https://github.com/Hexalith/Hexalith.Conversations/issues/4), [Platform #1](https://github.com/Hexalith/Hexalith.Platform/issues/1) and [Platform #2](https://github.com/Hexalith/Hexalith.Platform/issues/2) all OPEN with zero comments. Their full bodies remain requests for commitments, not owner acceptance packets. No complete accepted contract, immutable target, integration date or executable compatibility command was found in issues or inspected owner artifacts.
+
+The four complete records remain `Uncommitted` with required fields `TBD`. The [register's entry gate](../planning-artifacts/external-dependency-register.md#authority-and-entry-gate) prevents ready-for-dev until complete commitments are accepted; consumer execution additionally requires `Available` and passing exact-target evidence. The limited Branch-B development provision supplies neither the other three commitments nor readiness for this approved full scope. Required input remains the four [owner acceptance packets](../specs/spec-story-5-4-dependency-unblock/evidence-and-acceptance.md#per-owner-acceptance-packet); further spec approval is unnecessary.
+
+Updated only this report and the spec's open question, Code Map and notes. Validation covers unchanged approved frozen intent/acceptance criteria, frontmatter approval, register/sprint gates, local document references and whitespace. No source code, test, dependency pointer or acceptance field changed; no build, test suite, consumer seam, deployment, owner message or compatibility command ran. Story 5.4 remains approved in draft/backlog with implementation blocked by delivery evidence.
--- /dev/null
+++ b/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-retention-recommendation.md
@@ -0,0 +1,78 @@
+---
+title: '5.4 Apply minimal actor-history retention recommendation'
+type: 'feature'
+created: '2026-10-06'
+status: 'in-review'
+route: 'dispatch'
+human_approval: 'accepted-engineering-scope'
+approval_source: 'User: apply recommendation'
+baseline_commit: 'b252fcfde51bd04317ce5843a0a42bfe4dce5170'
+parties_baseline_commit: 'b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca'
+review_loop_iteration: 0
+context:
+  - '/home/administrator/projects/hexalith/parties/AGENTS.md'
+  - '/home/administrator/projects/hexalith/agents/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
+  - '/home/administrator/projects/hexalith/agents/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
+  - '/home/administrator/projects/hexalith/parties/.editorconfig'
+  - '/home/administrator/projects/hexalith/agents/_bmad-output/specs/spec-story-5-4-dependency-unblock/parties-identity-contract.md'
+---
+
+<frozen-after-approval reason="User authorized applying the engineering recommendation; production policy approval remains separate">
+
+## Intent
+
+**Problem:** No located policy explicitly authorizes opaque actor history after Party profile erasure. Binding writes require explicit finite policy, but current and historical queries can release bindings without checking the configured policy against retained custody expiry.
+
+**Approach:** Record one minimal actor-history policy proposal and enforce explicit matching policy on binding reads. Reuse Party aggregates, EventStore policy/custody contracts and existing options registration. Keep production configuration unset until an owner approves a finite duration and qualifies custody/cleanup/restore.
+
+## Boundaries & Constraints
+
+**Always:** Preserve existing edits, original Story 5.4 and external availability gates. Use versioned policy ID and exact existing `binding-effective-at` expiry derivation. Snapshot policy per call and recheck before evidence release and after asynchronous boundaries; removal/change denies without extending or relabeling old expiry. Use fresh independent custody as well as policy. Organization Branch B remains usable without human policy. Preserve historical actor/version/original positions, half-open intervals, expiry and cancellation behavior.
+
+**Never:** Invent an approved retention duration, register a production provider, enable a live route, modify the register, or claim cleanup/restore qualification from mocks. No policy engine, new storage/service, erasure-trigger state machine, current-profile fallback, assertion suppression, Git mutations or dependency updates. This slice does not complete full Story 5.4.
+
+## I/O & Edge-Case Matrix
+
+| Scenario | Input / State | Expected Output / Behavior | Error Handling |
+| --- | --- | --- | --- |
+| Explicit valid policy | Exact ID/purpose/derived expiry and fresh custody | Original current or historical binding | Preserve exact attribution |
+| Unconfigured policy | Missing/blank ID, duration, or unsupported trigger | Unavailable without binding | Historical path performs no source/custody read |
+| Mismatched retention | Wrong policy version, duration, purpose or custody flags | Unavailable without binding | No custody release or profile fallback |
+| Configuration changes | Policy removed/changed during source/custody/final authority | Unavailable without binding | Preserve caller cancellation |
+| Organization | Active provisioned Branch B, policy absent | Organization resolved with no human binding | No custody call |
+| Existing history boundaries | Erased profile, rebind/revoke, expiry, stalled/cancelled provider | Existing exact attribution or typed denial | No changed test expectations to mask faults |
+
+</frozen-after-approval>
+
+## Code Map
+
+- `../parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs` — current and purpose-limited retained reads; currently lacks policy configuration dependency.
+- `../parties/src/Hexalith.Parties/Authorization/PartyIdentityOptions.cs` — nullable finite policy; registration already supplies options monitoring.
+- `../eventstore/src/Hexalith.EventStore.Contracts/Security/IdentityHistoryPolicy.cs` and `IdentityHistoryCustodyEvidence.cs` — exact version/purpose/expiry matching; reuse.
+- `../parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs` — existing synthetic custody/source fixture and cancellation/boundary regression cases. Rebind fixtures must derive each successor's custody expiry from its own ValidFrom rather than inheriting the predecessor expiry.
+- `../parties/_bmad-output/planning-artifacts/adr-consumer-party-id-binding.md` — opaque actor extension; distinguish UI issuer/subject routing from attribution history.
+
+## Tasks & Acceptance
+
+- [x] `../parties/_bmad-output/planning-artifacts/actor-history-retention-policy-v1.md` — concrete minimal proposal: versioned policy reference, purpose, allowed fields, required finite duration, immutable effective-at expiry, independent profile erasure, source/derived-copy cleanup and nonrollback restore rules, approval/qualification gate. Explain why Agents 365-day terminal-content and generic Party audit/implementation-retention rules cannot be reused without explicit coverage. No runnable production defaults.
+- [x] Query/options/tests — matching explicit policy and freshness guards for current human binding and historical reads; meaningful matrix tests plus existing query/admission regression suite. Keep Organization classification and existing historical outcomes intact under valid configuration.
+- [x] ADR and separate owner evidence — record applied engineering decision, exact source hashes and test artifacts/commands, pending duration/provider qualification, and prior broad replay blocker separately.
+
+**Acceptance Criteria:** A valid fixture can still attribute past actions after profile erasure with exact recorded positions. Missing/mismatched/changed policy cannot release binding evidence. Local Debug source build passes normal analyzers; all new matrix tests and query/admission regression tests execute with zero skips/failures. Policy design is reviewable, while production enablement and complete parent acceptance remain pending.
+
+## Implementation Notes
+
+- 2026-10-06: Implemented directly after the required context-free handoff failed with `agent thread limit reached`. Loaded all context files before changing source. Existing options registration supplies monitoring; no new service/provider or production configuration was installed.
+- Matching configured policy is checked for current human bindings and retained historical reads, including after actual suspended source/custody operations and final authority. Missing/mismatched/withdrawn policy returns Unavailable with no binding. Organization remains independent of the human policy.
+- Successor fixtures derive their own expiry; the unchanged rebind/revoke, profile-erasure, original-position, expiry and cancellation expectations pass. New private helper CA2007 findings were corrected without suppression.
+- [Owner packet](../../../parties/_bmad-output/implementation-artifacts/ext-parties-1-retention-application-2026-10-06.md): normal Debug build 0 warnings/errors and 78 focused passes, including 26 new policy cases, with all matrix rows executed and no skips. Prior 489/490 broad replay blocker remains separate. No production duration or cleanup/restore qualification is claimed.
+
+## Spec Change Log
+
+## Review Triage Log
+
+Independent review has not run: the required fresh agent launch failed with `agent thread limit reached`. No reviewer findings or completed-review claim is recorded. Standalone prompts for all three layers are prepared under `retention-review-2026-10-06/` as required by step-04. The full baseline diff includes earlier preserved work; the new recommendation's incremental diff was read completely and its matrix verified against the 78-pass result XML.
+
+## Verification
+
+Build the Parties test project in Debug using owning project-reference switches and isolated `/tmp` artifacts, then execute the built xUnit v3 assembly with `-class '*PartyIdentityQueryHandlerTests' -class '*PartyIdentityAdmissionTests'` and result XML. Preserve previous evidence; use new retention-application artifact paths. Prior full Local 489/490 strict json-redacted replay failure is not waived or in this slice.
--- /dev/null
+++ b/agents/_bmad-output/implementation-artifacts/spec-5-4-conversations-client-review-fix.md
@@ -0,0 +1,42 @@
+---
+title: '5.4 Conversations client cancellation review fix'
+type: 'feature'
+created: '2026-10-06'
+status: 'in-progress'
+route: 'dispatch'
+human_approval: 'accepted'
+baseline_commit: '9938aa8aa5cfc082de6be3d28eede22957b14a26'
+review_loop_iteration: 0
+context:
+  - '/home/administrator/projects/hexalith/conversations/AGENTS.md'
+  - '/home/administrator/projects/hexalith/conversations/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
+  - '/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-conversations-owner-implementation.md'
+---
+<frozen-after-approval reason="authorized prerequisite review fix">
+## Intent
+Finish cancellation handling on the new restricted Conversations client operations. The server provider awaits are already bounded against caller cancellation; the new client HTTP awaits currently still rely on cooperative transports. Preserve all response checks and live closure.
+## Constraints
+Only ConversationClient's two new SubmitAgent helpers, new client tests/fixtures and owner evidence in sibling Conversations. Do not modify legacy client methods or unrelated commits. No staging, commit, remote, deployment, nested submodules, suppressed checks or new deadline/profile. This is the already-rendered bmad-build handoff; do not render again.
+## I/O & Edge-Case Matrix
+| Input | Expected |
+| --- | --- |
+| Command HTTP send never completes; caller cancels | Prompt caller cancellation, no accepted/persisted success |
+| Query HTTP send never completes; caller cancels | Prompt cancellation, no content release |
+| Response JSON read ignores token | Caller cancellation stops waiting; owned eventual response/body safely disposed |
+| Ordinary valid and malformed responses | All previous client response checks and behavior unchanged |
+</frozen-after-approval>
+## Code Map
+- src/Hexalith.Conversations.Client/ConversationClient.cs: SubmitAgentCommandAsync and SubmitAgentQueryAsync: bound SendAsync and ReadFromJsonAsync tasks using WaitAsync(cancellationToken). Preserve cancellation checks and exception handling; safely observe/dispose eventual owned responses after a cancelled noncooperative send. Keep tiny private helper in same type, no framework.
+- tests/Hexalith.Conversations.Client.Tests/ConversationAgentClientTests.cs and helper files: meaningful never-completing send and body tests with caller cancellation and a short watchdog; one documented type per file, normal owning format.
+- docs/implementation/ext-conv-ai-1-owner-implementation-2026-10-06.md and ext-conv-ai-1-source-evidence-2026-10-06.json: refresh changed source hashes, client XML/build/logs and exact new commands. Preserve original execution HEAD/full 1535-test suite evidence as earlier evidence; do not claim later source/HEAD passed unchanged full solution. No availability or full C4 claim.
+## Tasks & Acceptance
+- [x] Enforce bounded new-client awaits and safe eventual response disposal.
+- [x] Build normal Debug/source-reference client tests; execute focused and full Client assembly, no skip.
+- [x] Refresh exact source/evidence and unchanged production blockers.
+## Verification
+Use existing /tmp/hexalith-agents54-conversations-artifacts, unique client-reviewfix logs/XML. Properties UseHexalithProjectReferences=true, explicit EventStore/Commons/Tenants sibling roots, NuGetAudit=false, MinVerVersionOverride=1.0.0, -c Debug -m:1. The full Client assembly's doc tests need exact built bytes staged under ignored .artifacts/ext-conv-ai-1-tests/Hexalith.Conversations.Client.Tests/debug/run as the existing verifier does. Reuse unchanged server/domain/contracts results explicitly; do not rerun whole solution.
+
+
+## Root Verification
+
+Reviewed all 79 current selected Conversations source hashes and 24 artifact hashes, including the four changed/new client files; focused XML9 and full Client XML39 pass without failures/skips. Earlier full Contracts618/Domain185/Server698/Client34 evidence remains historical; only unchanged earlier lanes are reused. Full solution was not rerun after this client-only fix. Full C4 and live acceptance remain incomplete.
--- /dev/null
+++ b/agents/_bmad-output/implementation-artifacts/spec-5-4-conversations-owner-implementation.md
@@ -0,0 +1,73 @@
+---
+title: '5.4 Conversations owner implementation'
+type: 'feature'
+created: '2026-10-06'
+status: 'in-progress'
+route: 'dispatch'
+human_approval: 'accepted'
+approved_on: '2026-10-06'
+baseline_commit: '9938aa8aa5cfc082de6be3d28eede22957b14a26'
+review_loop_iteration: 0
+context:
+  - '/home/administrator/projects/hexalith/conversations/AGENTS.md'
+  - '/home/administrator/projects/hexalith/conversations/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
+  - '/home/administrator/projects/hexalith/conversations/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
+  - '/home/administrator/projects/hexalith/agents/_bmad-output/specs/spec-story-5-4-dependency-unblock/owner-implementation-plan.md'
+  - '/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/external-dependency-register.md'
+---
+
+<frozen-after-approval reason="human-owned prerequisite intent">
+
+## Intent
+
+Implement the Conversations-owned six-seam restricted Agents service contract C1–C4. The user authorized prerequisite implementation on 2026-10-06. Prepare working domain/client/query behavior and runnable local verification. Full owner qualification and Available status remain gated by real current authority, complete source enumeration, independent deletion approval, authenticated receiver and accepted exact targets/commands. Original Story 5.4 stays blocked until all original requirements are met.
+
+## Constraints
+
+Work only in the sibling Conversations owner checkout. Preserve the existing new `Contracts/Agents` files, original API compatibility, Facilitator, and unrelated edits. Domain persistence uses EventStore; aggregates are pure Handle/Apply. Infrastructure belongs to EventStore/Platform. Do not add raw storage, an AppHost, proprietary CLI, token authority or projection-based authority. No remote operations, commit, deployment, nested submodules, or live consumption of unavailable dependencies. Production ports must fail closed when unconfigured; local fixtures never establish live readiness. Read every required baseline. Build Debug with source references, isolated artifacts and `set -e`; run individual xUnit assemblies with valid exact class filters. Never alter an existing acceptance expectation merely to fit code.
+
+## I/O & Edge-Case Matrix
+
+| Case | Expected behavior |
+| --- | --- |
+| Exact Agent Organization/AiAgent/Member membership retry | One replayable membership, no extra event; changed identity/type/role conflicts |
+| Removed Agent principal | Durable removal evidence; service retry cannot rejoin |
+| Cross-tenant, unrelated Party or uncertain current authority | Fail before protected source lookup or effect; no existence/content disclosure |
+| Deterministic post retry/lost acknowledgement | Same MessageId and intent survive serialized replay; changed intent conflicts |
+| Current content/roster | Edit, delete/redaction and Facilitator reflected; typed deletion/removal/absence distinct after authorization |
+| Tenant active count | Counts active source Conversations including zero Agent Calls; missing complete catalogue is Unavailable, never zero |
+| Approved deletion | One durable approval event embeds immutable signal and original approval source revision |
+| Delivery retry/backfill/rollover | Same signal/revision; checkpoint advances only on exact authenticated acknowledgement |
+| Changed or poison acknowledgement | Durable quarantine, no checkpoint advance or success claim |
+
+</frozen-after-approval>
+
+## Code Map
+
+- `src/Hexalith.Conversations.Contracts/Agents` — existing portable outcome, read/count, provenance, deletion and delivery contracts; extend deliberately where needed.
+- `Contracts/Commands/AppendMessageCommand.cs`, `Contracts/Events/MessageAppended.cs`, `Contracts/Queries` — compatible deterministic posting/provenance and SDK query DTOs.
+- `src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs`, `State/ConversationState.cs`, `State/ConversationMessage.cs`, `Commands`, `Events`, `Validation` — pure membership/removal/post/edit/deletion/delivery state and exhaustive replay.
+- `src/Hexalith.Conversations.Server/Queries`, command/admission handlers and registration — SDK handlers plus narrow current authority, immutable Party, independent approval and authenticated receipt ports. Ports do not constitute delivered production providers.
+- `src/Hexalith.Conversations.Client/IConversationClient.cs` and HTTP client — publish all six owner seams with typed outcomes and exact returned MessageId checks; gateway submission, no alternate persistence.
+- Sibling EventStore `Client/Streams/IAuthoritativeEventStreamReader.cs` — bounded exact-stream complete prefix between stable sampled heads. Reconfirm authority after awaited reads. No complete tenant catalogue exists: require an authenticated complete catalogue/feed port and leave production count unavailable until its owner supplies one.
+- SDK `IDomainServiceAdmissionStage` and `ITrustedCommandExtensionPolicy` — reusable admission before pure processing, with explicit denied defaults. Existing `/process`/`query` remain transport boundaries; avoid domain-specific HTTP infrastructure.
+
+## Tasks & Acceptance
+
+- [ ] Publish and implement all six portable client/service seams, source/current reads and restricted command behavior.
+- [ ] Extend the existing aggregate/state with deterministic event-only membership, removal tombstones, posting/provenance/current message mutations and deletion publication/delivery/quarantine.
+- [ ] Require authenticated current scope and exact immutable Organization Party. Independent approval and receiver evidence cannot be minted by the Agents membership authority.
+- [ ] Preserve original source positions: any tracked revision must count every supported source event once and handle rejection/tail/snapshot input safely. Reject uncertified old snapshots and mismatched heads. Never embed a caller-claimed revision as persisted proof.
+- [ ] Add complete local tests for the matrix, serialized fresh-state replay and concurrent intent behavior. Distinguish local persisted-event simulation from live storage/restart evidence.
+- [ ] Add `eng/verify-ext-conv-ai-1.ps1` with named six-seam Local checks, executed-class evidence and a full Live gate that rejects missing accepted targets/providers before any calls. Required lanes cannot be skipped as a success.
+- [ ] Produce a separate owner implementation/evidence note listing exact changed files, commands, source revision, results and every remaining complete-record gap. Do not update acceptance/register fields.
+
+Acceptance: Given a current admitted service operation, when complete source is replayed, then the typed result and durable event intent match the matrix. Given missing production authority/catalogue/receiver, when service or verification runs, then it fails closed without inferring readiness. Given the original full owner record, when local work is reported, then unimplemented or unavailable full-contract portions remain explicit.
+
+## Verification
+
+Use isolated `/tmp/hexalith-agents54-conversations-artifacts` outputs. Keep exact build logs and XML. Report actual failures and compatibility blockers. Final report must distinguish implemented, tested and still externally gated behavior.
+
+## Implementation Notes
+
+- 2026-10-06: The user requested pragmatic solutions without over-engineering. Extend the existing aggregate, SDK query and client paths. Do not introduce a tenant catalogue database, delivery framework or additional service merely to make a missing provider appear available. Keep any proposed window semantics explicit and unaccepted until the owner confirms them; count authorization must not grant access to unjoined Conversation content.
--- /dev/null
+++ b/agents/_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md
@@ -0,0 +1,109 @@
+---
+title: '5.4 Owner Prerequisite Implementation'
+type: 'feature'
+created: '2026-10-06'
+status: 'in-progress'
+route: 'dispatch'
+human_approval: 'accepted'
+approved_on: '2026-10-06'
+baseline_commit: 'b252fcfde51bd04317ce5843a0a42bfe4dce5170'
+review_loop_iteration: 0
+context:
+  - '_bmad-output/specs/spec-story-5-4-dependency-unblock/owner-implementation-plan.md'
+  - '_bmad-output/specs/spec-story-5-4-dependency-unblock/parties-identity-contract.md'
+  - '_bmad-output/planning-artifacts/external-dependency-register.md'
+---
+
+<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">
+
+## Intent
+
+**Problem:** Story 5.4 has specification approval but lacks complete owner-delivered Parties, Conversations, custody and host prerequisites.
+
+**Approach:** The user authorized implementation across the owning repositories, with runnable verification and concrete results prepared for owner acceptance. Implement dependency-ordered owner work, including missing shared EventStore capabilities required by repository architecture. Preserve the original full Story 5.4 and its separate entry/execution gates.
+
+## Boundaries & Constraints
+
+**Always:** Preserve existing edits and source history. Use EventStore for domain persistence and technical modules for infrastructure. Keep Branch B's immutable Organization Party. Retained history requires independently governed expiry, encryption and restore protection; missing production policy/providers disable it. Complete owner contracts must precede availability claims. Run meaningful focused tests and retain exact verification evidence. Implement sequentially in dependency order.
+
+**Never:** Invent Product retention/erasure decisions, owner acceptance, production keys or successful live evidence. Do not run consuming unavailable seams, deploy, post owner messages, initialize nested submodules, commit or push. No fixtures or deferred ports establish Level 4 readiness.
+
+## I/O & Edge-Case Matrix
+
+| Scenario | Input / State | Expected Output / Behavior | Error Handling |
+| --- | --- | --- | --- |
+| Branch B identity | Provisioned Organization; inactive/restricted human | Organization identity; ineligible human has no usable binding | Unknown/new classification rejected |
+| Retained history | Erased profile; independently protected lifecycle events | Authorized exact-source history without profile decryption | Gaps, scope, expiry, missing custody or changed head block |
+| Trusted envelope | Scoped canonical bytes; configured current/retained key | Authenticated exact operation/principal/target | Forgery, time/profile, rotation/revocation failures block |
+| Conversations owner operations | Restricted service principal and exact Agent Party | Idempotent membership/posting and typed current evidence | Cross-tenant or uncertain authority blocks before lookup/effect |
+| Denial recovery | Independent replicated spool; EventStore outage | Stable pending observation and exactly acknowledged recovery | Missing replication/capability prevents processed-denial claim |
+
+</frozen-after-approval>
+
+## Code Map
+
+- `../parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs` and `../parties/src/Hexalith.Parties.Client/HttpPartiesIdentityClient.cs` — reuse tracked identity/binding work; correct Branch B and protect inactive-human evidence.
+- `../eventstore/src/Hexalith.EventStore.{Contracts,Client,Server}` — add purpose-limited retained-history read/certificate, preserving original source sequences and explicit exclusions; preserve unrelated current remediation edits.
+- `../conversations/src/Hexalith.Conversations.{Contracts,Client,Server}` — add the complete six restricted owner seams and pure state transitions, with EventStore source/outbox proof.
+- `../platform/src/Hexalith.Platform.Custody` — shared purpose/tenant/version custody and exact AD-29/30 authentication; required production policy and key provider.
+- `../platform/apphost.cs`, host composition and verifier — retain existing identity and Works work; add independent durability and restricted capability composition with truthful gates.
+
+## Tasks & Acceptance
+
+**Execution:**
+- [x] `../parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs` — complete current/historical correctness and regression verification.
+- [x] `../eventstore/src/Hexalith.EventStore.Client/Streams/IRetainedIdentityHistoryReader.cs` and related owner files — implement bounded authenticated retained-history transport/source verification.
+- [ ] `../parties/eng/verify-ext-parties-1.ps1` — bind the new history seam and executable owner verification without fabricated production readiness.
+- [ ] `../conversations/src/Hexalith.Conversations.Contracts/Agents` and owner runtime/client files — deliver all six restricted seams with persisted replay/delivery verification.
+- [ ] `../platform/src/Hexalith.Platform.Custody` and tests — deliver configured signing/digest/rotation/revocation and full applicable custody operations.
+- [ ] `../platform/apphost.cs`, restricted replay/security durability and verification — deliver the complete owner composition and recoverable persisted outcomes.
+- [ ] Owner evidence packets — record exact source/worktree evidence, contracts, runnable commands and pending acceptance inputs; reconcile Agents only when complete commitments are actually accepted.
+
+**Acceptance Criteria:**
+- Given a missing production policy, provider or owner acceptance, when implementation/verification runs, then the applicable live operation fails closed and no availability is inferred.
+- Given owner implementation changes, when focused and owning verification runs, then tested source behavior and persisted evidence are reported independently of outstanding full live delivery.
+- Given prior Story 5.4 approval, when prerequisite work proceeds, then its frozen intent, full scope and original acceptance criteria remain unchanged.
+
+## Implementation Notes
+
+- 2026-10-06: User answered yes to implementing prerequisite work across Parties, Conversations and Platform. Existing owner edits are preserved; source work proceeds separately from original Story 5.4 readiness.
+- Platform isolated Aspire baseline started and describe reports no resources in the default lane. Parties isolated baseline failed on a missing nested Memories/McpCli dependency; no nested submodule was initialized. Focused owner builds use isolated artifacts.
+- Historical initial Parties correctness slice (superseded by evidence below): two source and two test files; Debug builds have zero warnings/errors, 8 client and 18 domain identity tests passed. Full retained-history/custody/live delivery remains pending.
+
+## Spec Change Log
+
+## Review Triage Log
+
+Resolved local findings: SDK retained-history authority/custody waits and HTTP send/body reads did not bound noncooperative providers; Conversations client did not promptly cancel outstanding transport/body reads; Parties lacked entry/post-await/terminal cancellation and could consult final authority after a cancelled false custody result; Platform scaffold verifier incorrectly reported full compatibility. Meaningful cancellation/late-disposal/gate cases now pass. Full Parties json-redacted replay compatibility, Conversations C4, production custody/retention/profile/authority and H1–H4 remain unresolved; no acceptance or availability was manufactured.
+
+## Verification
+
+Verification commands and evidence are recorded per owning repository; no source/test success changes external acceptance fields by itself.
+
+## Current verified delivery — 2026-10-06
+
+The original Story 5.4 frozen intent is byte-equivalent to the initial baseline, implementation entry remains blocked-external-commitments, the four owner records remain Uncommitted and the sprint story remains backlog. The register is unchanged. There is no production policy/profile/provider approval, no live seam invocation, and no Git mutation/deployment by this workflow. Concurrent external commits are recorded as observations and preserved.
+
+- EventStore: bounded authenticated retained-history transport/source checks, strict purpose/schema/position partition and exclusive certificates; 54 focused passes (Client16 + Server27 + unchanged Contracts11). Caller cancellation/deadline tests actually suspend providers/transport and check no evidence release; late owned responses are disposed. Normal Debug builds pass with zero warnings/errors. [Owner evidence](../../../eventstore/_bmad-output/implementation-artifacts/evidence/agents54-retained-history-reviewfix/owner-source-evidence.md).
+- Parties: current and retained historical identity correctness with original positions and Branch B; cancellation before lookup, during stalled reads/custody and before terminal release. Domain/admission52 + reused Client18 + Contracts3 =73 focused passes. Normal Debug builds pass with zero warnings/errors. Broad Local remains the earlier489/490 with the unchanged json-redacted strict SDK replay compatibility failure; it is not waived. Production retention/custody, cleanup/restore and full P-01–P-10 qualification remain missing. [Owner evidence](../../../parties/_bmad-output/implementation-artifacts/ext-parties-1-owner-prerequisites-2026-10-06.md).
+- Conversations: pure restricted membership/current reads/deterministic posts/logical-deletion source transitions, HTTP client/handler registration and safe incomplete-provider defaults. Earlier four full local suites passed1535; the subsequent client cancellation fix passed9 focused and all39 current Client tests. Unchanged Contracts618/Domain185/Server698 evidence is explicitly reused; no whole-solution rerun is claimed after that client-only fix. Authenticated command-source/compare-append, complete catalogue/authority and full automatic C4 discovery/backfill/worker/real receiver/remote acknowledgement remain missing. [Owner evidence](../../../conversations/docs/implementation/ext-conv-ai-1-owner-implementation-2026-10-06.md).
+- Platform: 68 local signing/digest tests and a normal Debug source solution build pass with zero warnings/errors. Default/Live/invalid verifier modes make zero build/live calls. Exact-purpose fresh keys/profile, AD-29 framing, closed identity, canonical HMAC, exclusive time bounds, rotation/revocation, retained digest versions, suspended-provider input mutation and cancellation/disposal are tested. S1/S2 library fixtures do not deliver actual secret custody, approved numeric policy, independent decision authority or S3/S4/v23. [Owner evidence](../../../platform/docs/implementation/ext-secrets-1-local-prerequisites-2026-10-06.md).
+- Host: truthful default Full refusal, warning-free isolated Debug LocalScaffold build and six negative zero-invocation cases. An exact-source isolated host has zero application resources; explicit Agents+Works activation refuses before Works lookup. Exact owned host cleanup completed, Works/Identity source is unchanged. CLI runtime bundle/developer-certificate warnings are separately recorded. No replicated spool, private replay/recorder/worker capabilities, production protection/FR-34, or full H1–H4 persisted migration/guard/destruction/compromise/restore qualification is present. [Owner evidence](../../../platform/docs/implementation/ext-host-1-local-gates-2026-10-06.md).
+
+Full Conversations/custody/host tasks above remain unchecked because the complete required owner artifacts are absent. Local subtask specs record only their actually verified delivery; they do not narrow this parent or the original story.
+
+## Pragmatic decision recommendation
+
+1. Reuse an existing approved retention policy only if it explicitly covers the Party-to-stable-human-actor history after profile erasure, with a versioned duration/trigger and expiry/backup/restore rules. This avoids a parallel policy but a generic audit policy is insufficient evidence of coverage.
+2. Otherwise approve one narrow finite actor-binding policy retaining only attribution identity, binding interval/version and original source position, independently encrypted from the erasable profile. It provides past-action attribution without retaining names/contact details or granting current authority. It still requires approved expiry, cleanup and restore behavior. The prototype currently supports binding-effective-at expiry; erasure-triggered expiry needs an explicit contract adjustment.
+3. Keep historical attribution disabled until one of those policies is approved. Returning Unavailable without current-profile substitution is a valid temporary state, but cannot complete historical-proof acceptance.
+
+Recommended implementation: reuse the existing Party/Conversation aggregates and shared EventStore seams, use a small Platform library behind the existing qualified custody backend, and compose one durable denial spool on a backend already qualified for the owner's failure model. Do not create a new database/service or emulate production approvals to bypass the missing decisions. Owners still need to supply the policy ID/version, approved duration/trigger/cleanup/restore, numeric L/S/O/H/R profile, custody/authority/spool/credential/protection targets and complete exact-target qualification acceptance.
+
+## Applied recommendation — 2026-10-06
+
+The user instructed “apply recommendation”. Engineering now selects one minimal actor-history policy proposal because no located approved policy explicitly covers post-profile-erasure actor relationships. [Proposed policy](../../../parties/_bmad-output/planning-artifacts/actor-history-retention-policy-v1.md) defines allowed data, required finite duration, the supported immutable effective-at clock, independent profile erasure, cleanup and nonrollback restore obligations. No numeric production duration or owner approval was invented.
+
+The existing Parties query service now enforces explicit matching policy on human-binding reads and rejects configuration withdrawal/change across source/custody awaits and final authority. Organization Branch B remains usable without human policy. [New source evidence](../../../parties/_bmad-output/implementation-artifacts/ext-parties-1-retention-application-2026-10-06.md) records 78 current focused passes (26 new policy cases), normal Debug build with zero warnings/errors, exact hashes/commands and separate prior broad replay blocker. It supersedes the earlier query-source snapshot, without rewriting prior evidence.
+
+The selected construction remains the existing Party/Conversation aggregates and EventStore seams, the small shared Platform signing library behind qualified custody, and one independent spool on already-qualified infrastructure. Those local aggregate/SDK/signing changes are already implemented; no qualified production custody or spool target was found or fabricated. Production history, complete live owner acceptance, and full Story 5.4 therefore remain disabled/blocked. This engineering instruction does not provide formal retention/custody/spool/host qualification.
--- /dev/null
+++ b/agents/_bmad-output/implementation-artifacts/spec-5-4-parties-review-fixes.md
@@ -0,0 +1,57 @@
+---
+title: '5.4 Parties cancellation review fixes'
+type: 'feature'
+created: '2026-10-06'
+status: 'in-progress'
+route: 'dispatch'
+human_approval: 'accepted'
+baseline_commit: 'b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca'
+review_loop_iteration: 0
+context:
+  - '/home/administrator/projects/hexalith/parties/AGENTS.md'
+  - '/home/administrator/projects/hexalith/parties/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
+  - '/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md'
+---
+<frozen-after-approval reason="authorized prerequisite review fixes">
+
+## Intent
+
+Ensure current and retained historical Parties identity queries respect caller cancellation even when injected readers/custody/authority return normally after cancelling the caller. Preserve original source sequences, Branch B Organization, historical expiry and authority invariants. No production policy/provider or complete P-01–P-10 acceptance exists.
+
+## Constraints
+
+Edit only our query service, its identity tests and owner evidence in sibling Parties. Preserve six pre-existing modified documents and concurrent edits. No staging, commits, remote calls, deployments, nested submodules or analyzer suppression. This is the already-rendered bmad-build handoff; do not render again. One documented type per file, owning Allman/CRLF conventions; Debug isolated source builds. Do not fix or weaken the unrelated existing json-redacted strict replay compatibility test.
+
+## I/O & Edge-Case Matrix
+
+| Input | Expected |
+| --- | --- |
+| Pre-cancelled current/historical query | Caller cancellation before authority/source lookup |
+| Noncooperative current/history reader returns after cancellation | Cancellation propagated, no usable result |
+| Custody cancels caller then returns true | Cancellation propagated for both current and historical queries |
+| Synchronous terminal authority cancels before valid result | Cancellation propagated, no result released |
+| Valid/denied/expired/mismatched ordinary query | All previous focused behavior preserved |
+
+</frozen-after-approval>
+
+## Code Map
+
+- src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs: ResolveAsync and ResolveAtAsync lack entry, post-await and terminal token checks. Add checks before admission, after each awaited external operation, after synchronous final authority and certificate work, and before every result where cancellation may have occurred. Bound Task/ValueTask awaits against the caller token if injected providers ignore it. Caller cancellation must not become Unavailable through generic catch. No new arbitrary timeout/profile.
+- tests/Hexalith.Parties.Tests/Queries: extend our identity/history tests with meaningful pre-cancelled and noncooperative reader/custody/authority cases. Use a short watchdog for never-completing fixtures, not blocking sleeps. Test cancellation source belongs to the test and is passed explicitly.
+- _bmad-output/implementation-artifacts/ext-parties-1-owner-prerequisites-2026-10-06.md and tests/ext-parties-1-owner-source-evidence-2026-10-06.json: refresh exact query/test hashes, current observed base head, commands/logs/XML and focused totals; preserve existing blockers and immutable initial baseline.
+- Previous focused total 59 = 38 domain/admission + 18 client + 3 contract tests. Unchanged Client/Contract evidence may be reused explicitly. Full Local stopped at 489/490 with unchanged PartyDomainProcessorValidationTests.ProcessAsync_ProtectedHistoricalPayloadWithDestroyedKey_RedactsAndContinuesRehydration; keep it reported.
+
+## Tasks & Acceptance
+
+- [x] Enforce entry, bounded outstanding waits, post-await and terminal caller cancellation.
+- [x] Run all affected identity query tests and meaningful regressions with normal warning-free builds.
+- [x] Refresh exact source/evidence inventory and report full qualification/policy/provider/cleanup/restore gaps.
+
+## Verification
+
+Artifacts /tmp/hexalith-agents54-parties-artifacts, optional Memories pin /tmp/parties-no-memories-source to avoid missing nested McpCli; no nested init. Build -c Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0, with existing source flags. Execute built xUnit assemblies with single-dash class filters after successful builds. Preserve prior logs; use reviewfix names.
+
+
+## Implementation Notes
+
+Direct implementation because a fresh subagent thread was unavailable. Reviewed both entry points and executed the normal Debug source build (0 warnings/errors) and 52/52 domain/admission tests, including 14 new cancellation cases. Client 18 and Contracts 3 results are explicitly reused, yielding 73 focused tests. Owner evidence preserves immutable baseline and the broad 489/490 failure. Full parent scope and external acceptance remain incomplete.
--- /dev/null
+++ b/agents/_bmad-output/implementation-artifacts/spec-5-4-platform-host-gates.md
@@ -0,0 +1,62 @@
+---
+title: '5.4 Truthful Platform host qualification gates'
+type: 'feature'
+created: '2026-10-06'
+status: 'in-progress'
+route: 'dispatch'
+human_approval: 'accepted'
+baseline_commit: '3d8eaf400582f0698297d393f4330a54709c9a07'
+review_loop_iteration: 0
+context:
+  - '/home/administrator/projects/hexalith/platform/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
+  - '/home/administrator/projects/hexalith/agents/_bmad-output/specs/spec-story-5-4-dependency-unblock/owner-implementation-plan.md'
+  - '/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/external-dependency-register.md'
+---
+<frozen-after-approval reason="authorized prerequisite safety/verification work">
+
+## Intent
+
+Stop a Release build of an empty Platform scaffold from reporting EXT-HOST-1 compatibility PASS. Give the existing script a truthful local Debug scaffold mode and reject requested Agents activation while the full owner composition and current accepted prerequisites are absent. Preserve Works/Identity/admin work. Full H1–H4 remain undelivered.
+
+## Constraints
+
+Only apphost.cs, eng/verify-agents-host.sh, existing docs/ext-host-1-agents-composition.md and separate owner evidence. No new database/service/framework, made-up replicated spool, fake credentials/providers or fixtures presented as production. No domain persistence/Agents aggregate implementation before original entry commitments. No live external consumption, deployment, staging, commits, remote ops or submodule initialization. This is the already-rendered bmad-build handoff; do not render again.
+
+The explicit Agents enabled flag must throw a clear unavailable-composition error before resource creation. Default scaffold and explicit Works lane remain as before. Do not change existing Works profile or insert Identity into unrelated lanes.
+
+## I/O & Edge-Case Matrix
+
+| Input | Expected |
+| --- | --- |
+| Default/full verification without complete H1–H4 | Nonzero unavailable, no live invocation and no EXT-HOST-1 PASS |
+| Explicit LocalScaffold mode | Isolated Debug build succeeds; output says scaffold only, no compatibility/Available claim |
+| Unknown script argument | Nonzero before build |
+| Platform:Agents:Enabled=true | Startup refuses before resource composition, no empty green Agents host |
+| Default Platform startup | Existing scaffold unchanged |
+| Explicit Works lane | Source and wiring unchanged |
+
+</frozen-after-approval>
+
+## Code Map
+
+- eng/verify-agents-host.sh currently prints EXT-HOST-1 PASS after apphost.cs Release build and defers full live composition. Default Full mode must deny until actual H1–H4 and acceptance delivered; an explicit --mode LocalScaffold is allowed to build Debug with isolated caller-selectable artifacts path, no arbitrary success wording. No staged fake register may unlock the missing implementation. Preserve ownership checks.
+- apphost.cs: add narrow explicit Platform:Agents:Enabled guard immediately after builder creation before Works composition. Full unavailable error is content-free. Do not wire missing providers or rely on default-empty resource success.
+- docs/ext-host-1-agents-composition.md: exact runnable modes, partial evidence limits and full prerequisites; do not say S2/key-profile or scaffold alone supplies full H1–H4.
+- Separate owner source/evidence notes: source hashes, immutable original baseline/current observed base HEAD, exact commands/logs/outcomes, productionReady=false. List missing replicated spool technology/failure model/retention, private exact replay/recorder/worker credentials, production protection attestation/FR-34, S3/S4 and H3/H4 migration/guard/compromise, owner acceptance.
+
+## Tasks & Acceptance
+
+- [x] Truthful default Full gate and isolated local Debug scaffold build.
+- [x] Early explicit Agents activation guard, preserving unrelated composition.
+- [x] Execute script syntax, unknown/default Full zero-call checks, LocalScaffold build, and owned isolated AppHost default/enabled startup checks.
+- [x] Record exact source/evidence with full owner gaps.
+
+## Verification
+
+Use /tmp/hexalith-agents54-host-artifacts and owned isolated runtime paths. Aspire baseline was already run by root: default resources [], exact owned host safely stopped. Read /home/administrator/.agents/skills/aspire/SKILL.md and routed orchestration skill before lifecycle work. Changes require restarting only an exact owned host, never stopping another user's app. Use CLI resources/status output without dashboard tokens; verify absence/default and refusal/enabled. Do not print secrets. Tests must execute script behavior rather than weaken assertions. Set -e for build-before-run. Build is reversible and authorized; no approval needed. Record environment blocker separately if startup cannot execute; do not claim runtime proof from a text search.
+
+## Implementation Notes
+
+Direct implementation completed local gate tasks: warning-free Debug scaffold build, six zero-invocation negative cases, exact-source isolated default startup with zero application resources, explicit Agents+Works refusal before resource composition and exact owned cleanup. The original Works/Identity block is unchanged. CLI runtime ASPIRE010/developer-certificate warnings are recorded separately. Full H1–H4, production providers, persisted qualification and owner acceptance remain incomplete.
+
+Direct implementation completed local gate tasks: warning-free Debug scaffold build, six zero-invocation negative cases, exact-source isolated default startup with zero application resources, explicit Agents+Works refusal before resource composition and exact owned cleanup. The original Works/Identity block is unchanged. CLI runtime ASPIRE010/developer-certificate warnings are recorded separately. Full H1–H4, production providers, persisted qualification and owner acceptance remain incomplete.
--- /dev/null
+++ b/agents/_bmad-output/implementation-artifacts/spec-5-4-platform-signing-prerequisites.md
@@ -0,0 +1,81 @@
+---
+title: '5.4 Platform configured signing prerequisites'
+type: 'feature'
+created: '2026-10-06'
+status: 'in-progress'
+route: 'dispatch'
+human_approval: 'accepted'
+approved_on: '2026-10-06'
+baseline_commit: '3d8eaf400582f0698297d393f4330a54709c9a07'
+review_loop_iteration: 0
+context:
+  - '/home/administrator/projects/hexalith/platform/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
+  - '/home/administrator/projects/hexalith/platform/Directory.Build.props'
+  - '/home/administrator/projects/hexalith/platform/Directory.Packages.props'
+  - '/home/administrator/projects/hexalith/agents/_bmad-output/specs/spec-story-5-4-dependency-unblock/owner-implementation-plan.md'
+  - '/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/external-dependency-register.md'
+---
+
+<frozen-after-approval reason="authorized owner prerequisites; production policy remains human-owned">
+
+## Intent
+
+Implement the smallest reusable Platform signing/digest prerequisite library and runnable local verification within the complete S1–S4 owner plan. The user authorized prerequisite implementation and requested pragmatic solutions without over-engineering. This child implements the independently testable S1/S2 cryptographic behavior; it does not shrink the parent scope or establish EXT-SECRETS-1 readiness. Keep real custody, independently governed decision authority, export/migration/destruction protocols and numeric production policy explicitly required and unavailable where absent.
+
+## Constraints
+
+Work in the sibling Platform repository. Preserve existing Identity, Works and concurrently edited admin work. Extend the existing new Hexalith.Platform.Custody skeleton. One documented type per C# file, repository conventions, Debug, individual xUnit assemblies and isolated outputs. Do not implement a vault, new database, new service, generic policy framework, custom RFC 8785 approximation, or raw domain persistence. No live dependency consumption, remote operations, stage/commit/push, deployment or nested submodules. This is a handoff in the already-rendered parent bmad-build workflow; do not render/start a second workflow.
+
+No approved numeric profile or production key backend was supplied. Missing/invalid/stale profile or missing provider denies issuance and verification. Provider authorization must be current and exact tenant/purpose/version; no arbitrary tenant-to-secret naming or cached revoked state. Never log, serialize, return or persist key material. An independently issued decision approval cannot be created by custody or the recorder. Cryptographic validity is not domain authorization or replay registration.
+
+## I/O & Edge-Case Matrix
+
+| Input | Expected |
+| --- | --- |
+| Configured current exact tenant/purpose key and valid explicit profile | HMAC-SHA-256 tag over deterministic canonical bytes; content-free result with key version |
+| Missing/invalid/stale profile or unavailable provider | Closed typed failure, no default production numbers or keys |
+| Wrong tenant/purpose/version returned by provider | Refusal; reserved-system observation key cannot be used for tenant content or envelope signing |
+| Any altered envelope field/tag, wrong issuer/audience/schema/principal shape | Verification denied, constant-time tag comparison |
+| Future issue beyond configured skew, expired envelope, excessive lifetime | Denied using injected TimeProvider; expiry exclusive |
+| Routine rotation | Issue current active only; old envelope verifies only during configured overlap and lifecycle validity |
+| Emergency revocation during/after awaited resolution | Fresh revocation observed; no cached success or cancelled success |
+| Recorded digest key version after rotation | Recompute with exactly the original retained version; match or conflict, never current-key false conflict |
+| Redispatch | New nonce/time/current key/tag, unchanged logical ID, tuple and fingerprint/version |
+| Key disposal/diagnostics | Key copy zeroed and safe ToString/serialization; no secret evidence |
+| Full compatibility without S3/S4/provider/host delivery | Full gate fails before calls; Local fixture pass cannot become Available |
+
+</frozen-after-approval>
+
+## Code Map
+
+- src/Hexalith.Platform.Custody existing key scope, purpose, lifecycle, disposable snapshot, provider/result files: harden exact scope/lifecycle behavior and use a fail-closed default provider. A narrow injectable provider contract is acceptable; fixtures are not a production backend. Do not invent a backend data format or claim Dapr alone establishes current atomic custody/restore guarantees.
+- Add only these two new Custody projects to Hexalith.Platform.slnx for normal discovery if needed; preserve every existing entry. Remove an unused Dapr package from our new skeleton rather than inventing a backend to justify it.
+- New configured profile and authentication/digest services: required version/issuer/audience/current validity and explicit durations maximum lifetime L, skew S, overlap O, retry/recovery H and replay retention R, with R >= max(L+S+O,H). S/O may be zero, L/H/R positive; reject overflow and inconsistent lifecycle. Profile changes during awaited work must not authorize against the old revision.
+- AD-29 compatibility: read Agents src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionIdentity.cs read-only. Existing framing uses invariant decimal UTF-16 string.Length + ':' + component + U+001F, then UTF-8. Preserve shipped bytes, including non-ASCII. For command-payload components, put the required raw one-byte 0x00/0x01 presence marker before each framed component so absent and explicitly empty are distinct; nested/collection flattening belongs to the owning DTO adapter. Generic canonical component framing must preserve absent versus explicitly empty values for optional fields; no arbitrary JSON hashing or business-idempotency codec substitution. This technical library need not depend on Agents.
+- AD-30 envelope: purpose first; schema/profile version, issuer, principal kind; stable human actor when human; Party/binding version/role basis where present; closed Workflow kind/instance/activity/on-behalf identity for automation; concrete command contract and operation family; target tenant/resource; correlation/causation; immutable idempotency tuple, payload fingerprint and DigestKeyVersion; audience; issued/expires; LogicalCommandId; DeliveryNonce; signing key version. Validate closed principal shapes: User requires stable human actor plus Party/binding/role basis; Administrator requires stable human actor and role basis; Platform requires stable human actor, ActorTenantId=system and role basis; Workflow has no human/binding/role claims and uses only Interaction, SystemTimer, GovernanceProtection, InteractionDirectoryMigration or ConversationDeletionPropagation. Interaction alone carries OnBehalfOfPartyId; the other kinds do not. Workflow purpose/operation authorization remains external, never inferred by this crypto library. Validate expected scope/operation/identity supplied by trusted composition, not a broad family permission. Root architecture spine AD-29/AD-30 is authoritative if additional detail is needed.
+- Snapshot caller-owned component lists, tag arrays and digest bytes before awaits so concurrent mutation cannot change what was authenticated; retain immutable result bytes or defensive copies. Clear owned secret/key and temporary sensitive buffers; make caller cancellation prompt even for a noncooperative provider, and dispose any owned late key result without releasing it. Add a real substitution-after-await regression.
+- Envelope overlap applies only to TrustedEnvelope verification. Retained content/security digest versions must remain recomputable throughout their own provider-approved lifecycle even after the envelope overlap has ended; never manufacture an idempotency conflict after rotation. Test this boundary explicitly.
+- Test-only key providers may simulate fresh authorized inventories, rotation/revocation, denied exact scope, cancellation and outage. Their evidence must remain labeled local fixture.
+- eng/verify-ext-secrets-1.ps1: Local builds the exact library/tests with isolated artifacts, runs every required class and checks XML pass/no skip; full Live first checks complete accepted targets/commands/providers/prerequisites, and remains unavailable if S3/S4/exact persisted qualification are missing. Never print EXT-SECRETS-1 PASS from a subset.
+- Separate owner note/evidence packet: immutable initial baseline above, current base head observation, source file hashes, exact executed commands/logs/XML, partial scope and full S1–S4/v23/host gaps. Parent original Story 5.4 and register stay unchanged.
+
+## Tasks & Acceptance
+
+- [x] Complete reusable configured S1/S2 scope, lifecycle, profile, canonical-byte and HMAC issuance/verification services without production defaults.
+- [x] Implement tenant content digest and reserved-system observation digest with exact retained-version recomputation; use only owned opaque/provider material.
+- [x] Keep fresh provider/profile, cancellation and secret material handling safe; supply closed defaults and explicit DI.
+- [x] Run meaningful golden framing, field substitution, principal/scope, timing, rotation/revocation, retained digest, provider outage, cancellation and disposal tests. No reflection-derived test that merely mirrors implementation.
+- [x] Add truthful Local and full gated owner verification, including missing-input zero-call checks.
+- [x] Produce source/evidence notes distinguishing tested cryptographic behavior from real backend/authority/replay/export/migration/destruction delivery and acceptance.
+
+## Verification
+
+Use /tmp/hexalith-agents54-custody-artifacts. Build and test sequentially with set -e and source-reference flags where needed. No production keys; deterministic fixture bytes are test-only. Keep exact test names/results and logs.
+
+## Implementation Notes
+
+Full parent work remains required. No real production retention policy, numeric signing profile, secretstore, independent decision issuer, export store or protection-owner protocol is currently accepted. Implement safe independent code and report those exact missing dependencies; do not fabricate all-or-none destruction receipts or offline acceptance.
+
+Local tasks implemented directly after the fresh-thread limit prevented another coding handoff. Normal Debug source solution build and all 68 local cryptographic tests passed (0 warnings/errors/failures/skips). Executed default/Live/unknown gate checks performed zero build/live calls. Source/evidence is in sibling Platform docs/implementation/ext-secrets-1-*. Full S1–S4/provider/authority/live acceptance remains incomplete; this child closes only its explicit local prerequisite tasks.
+
+Local tasks implemented directly after the fresh-thread limit prevented another coding handoff. Normal Debug source solution build and all 68 local cryptographic tests passed (0 warnings/errors/failures/skips). Executed default/Live/unknown gate checks performed zero build/live calls. Source/evidence is in sibling Platform docs/implementation/ext-secrets-1-*. Full S1–S4/provider/authority/live acceptance remains incomplete; this child closes only its explicit local prerequisite tasks.
--- /dev/null
+++ b/agents/_bmad-output/implementation-artifacts/spec-5-4-sdk-history-review-fixes.md
@@ -0,0 +1,59 @@
+---
+title: '5.4 Retained history SDK bounded-await review fixes'
+type: 'feature'
+created: '2026-10-06'
+status: 'in-progress'
+route: 'dispatch'
+human_approval: 'accepted'
+baseline_commit: 'd24a03569ed0c8e4d773e24e630a2eab2ec9f074'
+review_loop_iteration: 0
+context:
+  - '/home/administrator/projects/hexalith/eventstore/AGENTS.md'
+  - '/home/administrator/projects/hexalith/eventstore/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
+  - '/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md'
+---
+
+<frozen-after-approval reason="authorized owner prerequisite review fixes">
+
+## Intent
+
+Finish the retained-history SDK review by enforcing its existing 30-second deadline even if an admission, custody or transport operation ignores cancellation. Preserve all existing history certificates, scope, retention, source positions and fail-closed behavior. No production policy/provider is approved or installed.
+
+## Constraints
+
+Work only in the sibling EventStore checkout and our retained-history files. Preserve the extensive unrelated staged/unstaged/committed 6.1/6.6 work. No remote, staging, commit, deployment, nested submodules or live calls. This is the already-rendered bmad-build implementation handoff; do not render again. Use documented one-type files and owning Allman/CRLF conventions, Debug, isolated outputs, individual xUnit assemblies and set -e.
+
+## I/O & Edge-Case Matrix
+
+| State | Expected |
+| --- | --- |
+| Admission/custody never completes and caller cancels | Read promptly throws caller cancellation; no successful/partial history |
+| Admission/custody ignores operation deadline | Existing bounded deadline returns content-free unavailable |
+| HTTP send/body read ignores caller cancellation | Client promptly stops; no certificate release |
+| Noncooperative operation eventually completes after cancellation | No source lookup/loop/release continues; dispose owned transport material safely |
+| Ordinary valid and invalid history | Existing 36 passing tests retain their behavior |
+
+</frozen-after-approval>
+
+## Code Map
+
+- src/Hexalith.EventStore.Server/Security/RetainedIdentityHistorySourceReader.cs: actor metadata/page awaits already use WaitAsync(readToken), but admission and custody Task awaits do not. Bound first/final admission, unprotect, first/final CanRead and any other outstanding work using the existing linked token. Keep final clock/validity and terminal token checks.
+- src/Hexalith.EventStore.Client/Streams/RetainedIdentityHistoryReader.cs: bound HTTP send, response/body acquisition and each Task/ValueTask body read against the existing deadline, including deliberately noncooperative test transports. Keep 32 MiB wire /16 MiB decoded and 10k position bounds, authenticated host-supplied credentials, certificate validation and content-free failures.
+- Existing tests in Client.Tests/Streams/RetainedIdentityHistoryReaderTests.cs and Server.Tests/Security/RetainedIdentityHistorySourceReaderTests.cs plus their owned helpers. Add meaningful never-completing-operation regression fixtures with a short test watchdog and caller cancellation; tests must fail promptly if the bounded wait is removed. Do not spend 30 seconds per test or change the production 30-second bound merely for tests. Reuse the already injected TimeProvider to drive the deadline timer, with an owned manually advanced timer fixture; execute the deadline-unavailable matrix row as well as caller cancellation. Deadline expiry must occur while a provider/transport task remains incomplete.
+- All our 16 retained-history files are new/untracked. Do not modify unrelated existing SDK internals, domain replay or tests.
+- Source evidence: exact original baseline above plus current observed HEAD 785d58fc99ce4c2751c0546ca6368e993c654ed9 are base observations only. Capture the exact changed file hashes and executed build/test commands/logs/XML; no accepted target or live readiness.
+
+## Tasks & Acceptance
+
+- [x] Enforce bounded outstanding SDK awaits and safe caller cancellation, including transports that ignore tokens.
+- [x] Add and execute negative regression cases and all affected focused history tests with warning-free normal builds.
+- [x] Produce a separate owner source/evidence note with exact source inventory and all current results; distinguish local fixtures from production provider/cleanup/restore and acceptance.
+
+## Verification
+
+Use /tmp/hexalith-agents54-history-artifacts. Build -c Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0. Reuse unchanged contract evidence (11 passing tests in /tmp/hexalith-agents54-history-contracts-final-results.xml) and rerun changed Client/Server classes only after successful normal builds. New log/XML names should include reviewfix to preserve the prior evidence.
+
+
+## Root Verification
+
+Reviewed all 23 selected SDK source hashes and 10 artifact hashes, plus executed XML: Client16, Server27 and reused Contracts11 =54 passes with zero failures/skips; normal Debug builds have zero warnings/errors. The full review-fix diff and actual cancellation/deadline cases were inspected. Production retained-history custody/provider/authority is still unavailable.
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
--- /dev/null
+++ b/parties/_bmad-output/implementation-artifacts/tests/actor-retention-apply-2026-10-06/tests.xml
@@ -0,0 +1 @@
+<assemblies schema-version="3" id="44f3448a-4bad-4709-9492-f74a3cfd8374" computer="DESKTOP-VIOG240" user="administrator" start-rtf="2026-10-06T18:43:58.2072212+00:00" finish-rtf="2026-10-06T18:43:58.6349636+00:00" timestamp="10/06/2026 20:43:58"><assembly environment="64-bit (x64) .NET 10.0.12 [collection-per-class, parallel (collections, 14 threads)]" id="c530e4d6-bf4c-4249-a1d3-69420edff072" name="/tmp/hexalith-agents54-retention-artifacts/bin/Hexalith.Parties.Tests/debug/Hexalith.Parties.Tests.dll" run-date="2026-10-06" run-time="18:43:58" start-rtf="2026-10-06T18:43:58.2072212+00:00" test-framework="xUnit.net v3 4.0.1+8ed8aa354c" target-framework=".NETCoreApp,Version=v10.0" errors="0" failed="0" finish-rtf="2026-10-06T18:43:58.6349636+00:00" not-run="0" passed="78" skipped="0" time="0.428" time-rtf="00:00:00.4280000" total="78"><collection name="Test collection for Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests (id: 888973c97a01ea9dc810b716c23f8fb67bf522b21d9ff81f71e9f48acab88488)" id="a0a64598-ec0b-43d3-aa51-31907248fb1d" failed="0" not-run="0" passed="75" skipped="0" time="0.257" time-rtf="00:00:00.2570000" total="75"><test id="ce5f7caf-8236-44c8-b631-5b4fdb4996a1" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: False, inCustody: False, remove: False)" result="Pass" time="0.161" time-rtf="00:00:00.1610000" start-rtf="2026-10-06T18:43:58.3112152+00:00" finish-rtf="2026-10-06T18:43:58.4832270+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="604" /><test id="884f30ab-9b37-43c0-8348-86c8157acb55" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: True, inCustody: False, remove: False)" result="Pass" time="0.007" time-rtf="00:00:00.0070000" start-rtf="2026-10-06T18:43:58.4836829+00:00" finish-rtf="2026-10-06T18:43:58.4910353+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="604" /><test id="24cde70a-df6b-495e-a17c-9bc1f1db1fe3" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: False, inCustody: True, remove: False)" result="Pass" time="0.002" time-rtf="00:00:00.0020000" start-rtf="2026-10-06T18:43:58.4910922+00:00" finish-rtf="2026-10-06T18:43:58.4931807+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="604" /><test id="6e0928a7-d627-4b38-b44b-8135d4876824" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: True, inCustody: True, remove: False)" result="Pass" time="0.005" time-rtf="00:00:00.0050000" start-rtf="2026-10-06T18:43:58.4932814+00:00" finish-rtf="2026-10-06T18:43:58.4991618+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="604" /><test id="fd2d8e21-8f47-4aa6-bdc4-1478eb3857c8" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: False, inCustody: False, remove: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.4992096+00:00" finish-rtf="2026-10-06T18:43:58.4998840+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="604" /><test id="cd61c450-273a-46f4-95eb-f9d5326229a8" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: True, inCustody: False, remove: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.4999554+00:00" finish-rtf="2026-10-06T18:43:58.5006011+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="604" /><test id="1d8c9450-6151-4bab-bc52-3477f021faf5" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: False, inCustody: True, remove: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5006507+00:00" finish-rtf="2026-10-06T18:43:58.5013360+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="604" /><test id="af855704-22d6-4ea9-915f-7a9fdd4b2abe" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry(historical: True, inCustody: True, remove: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5013919+00:00" finish-rtf="2026-10-06T18:43:58.5019152+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesDuringAwait_DenyWithoutRelabelingExpiry" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="604" /><test id="85268b75-8970-4450-aea8-15677e22c363" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CurrentHuman_RequiresCompleteSourceAndCurrentExactActor" result="Pass" time="0.013" time-rtf="00:00:00.0130000" start-rtf="2026-10-06T18:43:58.5045950+00:00" finish-rtf="2026-10-06T18:43:58.5179061+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="CurrentHuman_RequiresCompleteSourceAndCurrentExactActor" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="264" /><test id="7641fba9-7df2-4dde-b7ba-bbfb799426d5" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.NonFiniteOrBeyondCustodyHistory_DeniesWithNoEvidence(missingEnd: True)" result="Pass" time="0.002" time-rtf="00:00:00.0020000" start-rtf="2026-10-06T18:43:58.5185304+00:00" finish-rtf="2026-10-06T18:43:58.5209033+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="NonFiniteOrBeyondCustodyHistory_DeniesWithNoEvidence" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="371" /><test id="30f4b15d-32b2-4800-ba75-3b7ec6ae072f" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.NonFiniteOrBeyondCustodyHistory_DeniesWithNoEvidence(missingEnd: False)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5209556+00:00" finish-rtf="2026-10-06T18:43:58.5215736+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="NonFiniteOrBeyondCustodyHistory_DeniesWithNoEvidence" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="371" /><test id="a883104e-0b2c-474b-92da-04d81e084850" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \&quot;unregistered\&quot;)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.5221113+00:00" finish-rtf="2026-10-06T18:43:58.5232694+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="UnconfiguredPolicy_DeniesBothBindingReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="532" /><test id="d85aa67d-2c43-4959-8e84-1137041aedf8" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \&quot;missing-id\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5233269+00:00" finish-rtf="2026-10-06T18:43:58.5237723+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="UnconfiguredPolicy_DeniesBothBindingReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="532" /><test id="3677fc4c-7a99-408b-b836-c8f262468e3d" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \&quot;blank-id\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5238156+00:00" finish-rtf="2026-10-06T18:43:58.5240638+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="UnconfiguredPolicy_DeniesBothBindingReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="532" /><test id="aabb2903-649b-495c-b540-6223aa14fe69" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \&quot;missing-duration\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5241115+00:00" finish-rtf="2026-10-06T18:43:58.5243485+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="UnconfiguredPolicy_DeniesBothBindingReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="532" /><test id="a251c690-cbdd-4c7b-839f-94682dba93a8" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \&quot;zero-duration\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5244030+00:00" finish-rtf="2026-10-06T18:43:58.5246528+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="UnconfiguredPolicy_DeniesBothBindingReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="532" /><test id="b27523b3-1236-4369-8330-e82dd70c37c7" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \&quot;missing-trigger\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5247024+00:00" finish-rtf="2026-10-06T18:43:58.5249369+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="UnconfiguredPolicy_DeniesBothBindingReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="532" /><test id="832557fb-8f5a-46c6-a610-15854d4ac930" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UnconfiguredPolicy_DeniesBothBindingReads(missing: \&quot;unsupported-trigger\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5249775+00:00" finish-rtf="2026-10-06T18:43:58.5252160+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="UnconfiguredPolicy_DeniesBothBindingReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="532" /><test id="fb2a92cf-15cf-40df-a37c-813ef4751d1b" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.FinalAuthorityCancelsThenReturnsValidGrant_DoesNotReleaseIdentity(historical: False)" result="Pass" time="0.012" time-rtf="00:00:00.0120000" start-rtf="2026-10-06T18:43:58.5255559+00:00" finish-rtf="2026-10-06T18:43:58.5382538+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="FinalAuthorityCancelsThenReturnsValidGrant_DoesNotReleaseIdentity" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="510" /><test id="a643b8c8-00f2-4f75-bb22-5e87b8fd0eac" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.FinalAuthorityCancelsThenReturnsValidGrant_DoesNotReleaseIdentity(historical: True)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.5383489+00:00" finish-rtf="2026-10-06T18:43:58.5396206+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="FinalAuthorityCancelsThenReturnsValidGrant_DoesNotReleaseIdentity" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="510" /><test id="bf1cc01c-a5f6-49ea-a1bd-c2891893f087" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ForeignAuthoritativeSourceAndFutureAction_DenyWithNoEvidence" result="Pass" time="0.002" time-rtf="00:00:00.0020000" start-rtf="2026-10-06T18:43:58.5401428+00:00" finish-rtf="2026-10-06T18:43:58.5425048+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ForeignAuthoritativeSourceAndFutureAction_DenyWithNoEvidence" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="309" /><test id="7b6b693d-a451-49ce-b01a-cd6da2c6957c" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ProvisionedOrganization_KeepsBranchBClassificationAndNeverCarriesHumanBinding" result="Pass" time="0.003" time-rtf="00:00:00.0030000" start-rtf="2026-10-06T18:43:58.5428459+00:00" finish-rtf="2026-10-06T18:43:58.5468521+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ProvisionedOrganization_KeepsBranchBClassificationAndNeverCarriesHumanBinding" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="235" /><test id="b5bad2bb-92c4-4529-ad85-3c8b3443cad3" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.SerializedRetainedSource_ReconstructsBothIntervalsWithoutProfileOrCurrentBinding" result="Pass" time="0.017" time-rtf="00:00:00.0170000" start-rtf="2026-10-06T18:43:58.5471745+00:00" finish-rtf="2026-10-06T18:43:58.5652447+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="SerializedRetainedSource_ReconstructsBothIntervalsWithoutProfileOrCurrentBinding" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="140" /><test id="b212a446-298c-4d5d-a217-278099ad7537" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ReaderCancelsThenReturnsValidSource_DoesNotReleaseIdentity(historical: False)" result="Pass" time="0.002" time-rtf="00:00:00.0020000" start-rtf="2026-10-06T18:43:58.5658124+00:00" finish-rtf="2026-10-06T18:43:58.5683469+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ReaderCancelsThenReturnsValidSource_DoesNotReleaseIdentity" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="433" /><test id="dbe38b7d-60aa-48f5-9df4-4cae77ae7804" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ReaderCancelsThenReturnsValidSource_DoesNotReleaseIdentity(historical: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5683948+00:00" finish-rtf="2026-10-06T18:43:58.5692675+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ReaderCancelsThenReturnsValidSource_DoesNotReleaseIdentity" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="433" /><test id="5d49029a-007d-4219-9f75-942992a34c12" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesAtFinalAuthority_DenyBothReads(historical: False, remove: False)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.5697160+00:00" finish-rtf="2026-10-06T18:43:58.5708149+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesAtFinalAuthority_DenyBothReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="656" /><test id="05ea6942-abb5-479b-8e08-04f6253c7ef5" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesAtFinalAuthority_DenyBothReads(historical: True, remove: False)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5708727+00:00" finish-rtf="2026-10-06T18:43:58.5715056+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesAtFinalAuthority_DenyBothReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="656" /><test id="af192d61-817a-4e55-813e-fe09dfe204a0" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesAtFinalAuthority_DenyBothReads(historical: False, remove: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5715532+00:00" finish-rtf="2026-10-06T18:43:58.5719597+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesAtFinalAuthority_DenyBothReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="656" /><test id="9162b566-dfba-4223-84b3-30a3ad0f378e" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PolicyChangesAtFinalAuthority_DenyBothReads(historical: True, remove: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5720216+00:00" finish-rtf="2026-10-06T18:43:58.5724366+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PolicyChangesAtFinalAuthority_DenyBothReads" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="656" /><test id="82ff0eb7-f6e0-4855-9578-eadb43d7970a" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CustodyExpiryCrossingAwait_DeniesHistoricalEvidence" result="Pass" time="0.006" time-rtf="00:00:00.0060000" start-rtf="2026-10-06T18:43:58.5727416+00:00" finish-rtf="2026-10-06T18:43:58.5791762+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="CustodyExpiryCrossingAwait_DeniesHistoricalEvidence" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="54" /><test id="b2ddf417-eb54-44b1-b605-74ca35ef7743" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RevocationWithoutLivePredecessor_DeniesWithNoEvidence(missingPredecessor: True)" result="Pass" time="0.003" time-rtf="00:00:00.0030000" start-rtf="2026-10-06T18:43:58.5797948+00:00" finish-rtf="2026-10-06T18:43:58.5829322+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RevocationWithoutLivePredecessor_DeniesWithNoEvidence" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="355" /><test id="ad37642e-882c-4489-8281-5db75a51b9a2" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RevocationWithoutLivePredecessor_DeniesWithNoEvidence(missingPredecessor: False)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5829973+00:00" finish-rtf="2026-10-06T18:43:58.5840139+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RevocationWithoutLivePredecessor_DeniesWithNoEvidence" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="355" /><test id="dab83592-d066-470e-a49c-cb766fc650f5" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ErasedProfile_CurrentReadFailsWhileIndependentHistoryKeepsOriginalPosition" result="Pass" time="0.005" time-rtf="00:00:00.0050000" start-rtf="2026-10-06T18:43:58.5843761+00:00" finish-rtf="2026-10-06T18:43:58.5903707+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ErasedProfile_CurrentReadFailsWhileIndependentHistoryKeepsOriginalPosition" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="123" /><test id="39d23539-5091-4d1b-a73f-b4ca9e2134aa" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MissingRetainedReaderOrExpiredCustody_CannotFallBackToAvailableFullProfile" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.5907926+00:00" finish-rtf="2026-10-06T18:43:58.5918792+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="MissingRetainedReaderOrExpiredCustody_CannotFallBackToAvailableFullProfile" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="221" /><test id="b95a3539-99d6-4167-864b-b68532e94b17" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.WrongAuthorizedActor_DeniesBeforeSourceRead" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.5921878+00:00" finish-rtf="2026-10-06T18:43:58.5928389+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="WrongAuthorizedActor_DeniesBeforeSourceRead" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="346" /><test id="64a4c94f-f732-454d-ad26-3e36825b627d" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.HistoricalReplay_BeforeBoundaryReturnsOriginalAndAtBoundaryReturnsSuccessor" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.5930819+00:00" finish-rtf="2026-10-06T18:43:58.5946347+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="HistoricalReplay_BeforeBoundaryReturnsOriginalAndAtBoundaryReturnsSuccessor" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="287" /><test id="cf245d36-5e66-4b8a-8890-a1140af44738" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.UncreatedInactiveRestrictedUnknownAndErased_AreNeverResolved" result="Pass" time="0.003" time-rtf="00:00:00.0030000" start-rtf="2026-10-06T18:43:58.5949061+00:00" finish-rtf="2026-10-06T18:43:58.5988526+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="UncreatedInactiveRestrictedUnknownAndErased_AreNeverResolved" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="405" /><test id="5b80508d-fdd5-4d7f-be2b-85b3a0de7a40" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.HistoricalRead_DoesNotRequireCurrentActorActivityAndHonorsHalfOpenBoundary" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.5991289+00:00" finish-rtf="2026-10-06T18:43:58.6002372+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="HistoricalRead_DoesNotRequireCurrentActorActivityAndHonorsHalfOpenBoundary" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="276" /><test id="31e12ca0-0180-458e-8469-f7e7e1903036" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateExpiryCrossingCustodyAwait_DeniesWithoutRenewingObservation" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.6004756+00:00" finish-rtf="2026-10-06T18:43:58.6019887+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateExpiryCrossingCustodyAwait_DeniesWithoutRenewingObservation" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="101" /><test id="efc037c8-5fdd-430f-8c51-d8780ce549ed" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity(historical: False, canRead: False)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.6023475+00:00" finish-rtf="2026-10-06T18:43:58.6043029+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="494" /><test id="a02ebbba-33ae-4993-aacd-143baaf07a91" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity(historical: True, canRead: False)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6043433+00:00" finish-rtf="2026-10-06T18:43:58.6051375+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="494" /><test id="74ffeeff-5e2a-4b5b-a2ed-66aa2ea49dfe" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity(historical: False, canRead: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6051756+00:00" finish-rtf="2026-10-06T18:43:58.6057314+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="494" /><test id="d1285105-8294-4eb3-a05a-73eb92ca01d8" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity(historical: True, canRead: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6057700+00:00" finish-rtf="2026-10-06T18:43:58.6062505+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="CustodyCancelsThenReturns_DoesNotConsultFinalAuthorityOrReleaseIdentity" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="494" /><test id="d62a3ce2-4e10-4ffe-9325-abebf1d7bf39" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.InactiveOrRestrictedHuman_ReturnsIneligibleWithoutUsableBinding(inactive: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6065948+00:00" finish-rtf="2026-10-06T18:43:58.6076067+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="InactiveOrRestrictedHuman_ReturnsIneligibleWithoutUsableBinding" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="250" /><test id="b4ad2d51-d21f-4e2e-b39e-fd8cf581c1ea" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.InactiveOrRestrictedHuman_ReturnsIneligibleWithoutUsableBinding(inactive: False)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6076439+00:00" finish-rtf="2026-10-06T18:43:58.6081655+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="InactiveOrRestrictedHuman_ReturnsIneligibleWithoutUsableBinding" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="250" /><test id="40177012-7e7d-406c-9911-fe08468d3422" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead(historical: False, inCustody: False)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.6084644+00:00" finish-rtf="2026-10-06T18:43:58.6097955+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="458" /><test id="f06cdd9d-054d-4cb4-ba7d-26d1d07dd128" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead(historical: True, inCustody: False)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6098402+00:00" finish-rtf="2026-10-06T18:43:58.6104264+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="458" /><test id="8f6fbb2d-2785-417e-b2ae-9b44390dac8c" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead(historical: False, inCustody: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6104687+00:00" finish-rtf="2026-10-06T18:43:58.6109079+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="458" /><test id="e86972eb-2f6e-4702-b828-fa5407b53ed8" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead(historical: True, inCustody: True)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6109501+00:00" finish-rtf="2026-10-06T18:43:58.6113286+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="NonCooperativeReaderOrCustody_CallerCanCancelOutstandingRead" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="458" /><test id="ad333cc1-f72f-4b8e-a8d0-8f12451e63e7" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor(change: \&quot;revoked\&quot;)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.6116932+00:00" finish-rtf="2026-10-06T18:43:58.6130057+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="77" /><test id="a13c6892-d5b9-47a0-a572-66c7119a6b22" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor(change: \&quot;different-actor\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6130581+00:00" finish-rtf="2026-10-06T18:43:58.6136568+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="77" /><test id="c4a11afc-d03c-4e02-b8b9-ba6bc93db21b" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor(change: \&quot;different-revision\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6136963+00:00" finish-rtf="2026-10-06T18:43:58.6139691+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="77" /><test id="d96d647a-b40b-46ed-9dca-4ea3e85d8dc0" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor(change: \&quot;different-source\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6140062+00:00" finish-rtf="2026-10-06T18:43:58.6142739+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ReadAuthorityChangesDuringCustody_DenyWithoutSubstitutingActor" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="77" /><test id="9e474a06-1529-41d5-a67a-a39b7463c43e" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;gap\&quot;)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.6147056+00:00" finish-rtf="2026-10-06T18:43:58.6160436+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="8f982943-bd7a-41bf-b033-44c77d04b973" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;overlap\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6160842+00:00" finish-rtf="2026-10-06T18:43:58.6167951+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="7785cdf5-e882-409f-873f-69412fef130d" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;profile-substitution\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6168430+00:00" finish-rtf="2026-10-06T18:43:58.6177509+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="0d7fb32c-6db9-4cbb-888b-cbc0accdd92a" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;wrong-purpose\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6177926+00:00" finish-rtf="2026-10-06T18:43:58.6181237+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="9872d811-3a8a-4d96-8d9b-6acb204ee7e4" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;foreign-binding\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6181608+00:00" finish-rtf="2026-10-06T18:43:58.6184907+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="35e59c41-243b-4109-b931-017c0b0a288d" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;transit-expiry\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6185288+00:00" finish-rtf="2026-10-06T18:43:58.6187321+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="32783fb6-2225-4ea7-86f3-3e75bec1a110" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;missing-authority\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6187751+00:00" finish-rtf="2026-10-06T18:43:58.6189772+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="905d99d2-8ea2-471a-8884-b9e9e99f1f4f" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;future-observation\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6190165+00:00" finish-rtf="2026-10-06T18:43:58.6192797+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="fc731f44-de48-4043-8ea2-7ea89e2729f3" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;short-name-substitution\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6193170+00:00" finish-rtf="2026-10-06T18:43:58.6196419+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="c70e1fef-5889-4396-a65a-e5085acee273" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;assembly-qualified-substitution\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6196791+00:00" finish-rtf="2026-10-06T18:43:58.6199283+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="00e97e5d-798d-43c9-8702-55178a87f48f" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback(corruption: \&quot;namespace-alias-substitution\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6199651+00:00" finish-rtf="2026-10-06T18:43:58.6202213+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="RetainedCertificateOrEventSubstitution_DeniesWithoutProfileFallback" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="177" /><test id="99d91aab-2dfd-40e3-960e-b47c547b6ecf" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \&quot;policy-version\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6206161+00:00" finish-rtf="2026-10-06T18:43:58.6216592+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="MismatchedPolicy_DeniesBothBindingReadsWithoutCustody" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="565" /><test id="10f91549-20b7-4895-aa1f-f75d31732ada" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \&quot;duration\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6217000+00:00" finish-rtf="2026-10-06T18:43:58.6221405+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="MismatchedPolicy_DeniesBothBindingReadsWithoutCustody" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="565" /><test id="8eeeb786-fc61-4cec-abe2-cb2079497c3f" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \&quot;recorded-expiry\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6221882+00:00" finish-rtf="2026-10-06T18:43:58.6224631+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="MismatchedPolicy_DeniesBothBindingReadsWithoutCustody" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="565" /><test id="aeeabada-8e49-4ded-b856-3eadf06da3a9" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \&quot;purpose\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6225383+00:00" finish-rtf="2026-10-06T18:43:58.6228615+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="MismatchedPolicy_DeniesBothBindingReadsWithoutCustody" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="565" /><test id="f512afb5-f99b-4449-a525-387300dba02d" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \&quot;source-expiry\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6229011+00:00" finish-rtf="2026-10-06T18:43:58.6232210+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="MismatchedPolicy_DeniesBothBindingReadsWithoutCustody" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="565" /><test id="fba8a41d-95cf-4441-af19-668af5dec392" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \&quot;restore\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6232582+00:00" finish-rtf="2026-10-06T18:43:58.6236412+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="MismatchedPolicy_DeniesBothBindingReadsWithoutCustody" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="565" /><test id="5a94ceb4-492e-464b-a325-56cc5b7e942d" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MismatchedPolicy_DeniesBothBindingReadsWithoutCustody(mismatch: \&quot;derived-copies\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6236780+00:00" finish-rtf="2026-10-06T18:43:58.6239924+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="MismatchedPolicy_DeniesBothBindingReadsWithoutCustody" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="565" /><test id="e1c6d3c5-8f2b-4ccd-b234-39aa55991b31" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CustodyFailureOrNetworkFailure_IsUnavailableAndCancellationPropagates" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.6242639+00:00" finish-rtf="2026-10-06T18:43:58.6263163+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="CustodyFailureOrNetworkFailure_IsUnavailableAndCancellationPropagates" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="390" /><test id="4b77352a-94a7-42ec-aea1-d8aa609113df" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ValidRevocationReplay_PreservesBeforeBoundaryAndReturnsGapAtBoundary" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.6266035+00:00" finish-rtf="2026-10-06T18:43:58.6275436+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="ValidRevocationReplay_PreservesBeforeBoundaryAndReturnsGapAtBoundary" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="331" /><test id="8cf484b5-2a1c-497b-bb2f-21504350f1a5" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PreCancelledQuery_DoesNotConsultAuthorityOrReaders(historical: False)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.6279387+00:00" finish-rtf="2026-10-06T18:43:58.6297538+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PreCancelledQuery_DoesNotConsultAuthorityOrReaders" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="417" /><test id="e5ed6178-16e5-4272-b77a-b6b459194283" name="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.PreCancelledQuery_DoesNotConsultAuthorityOrReaders(historical: True)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.6298121+00:00" finish-rtf="2026-10-06T18:43:58.6308930+00:00" type="Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests" method="PreCancelledQuery_DoesNotConsultAuthorityOrReaders" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs" source-line="417" /></collection><collection name="Test collection for Hexalith.Parties.Tests.Domain.PartyIdentityAdmissionTests (id: f7fdbf70f8c51900592e7785366dc9f77df753f2a03bfcd9407945da147addf4)" id="fce5f16c-0827-4e09-8beb-e9d5b2782377" failed="0" not-run="0" passed="3" skipped="0" time="0.142" time-rtf="00:00:00.1419999" total="3"><test id="d2534ef4-15b6-41f3-8444-d88104231d42" name="Hexalith.Parties.Tests.Domain.PartyIdentityAdmissionTests.MissingOrUnsupportedPolicy_DeniesBeforeAnyProtectedUnwrap(trigger: null)" result="Pass" time="0.141" time-rtf="00:00:00.1409999" start-rtf="2026-10-06T18:43:58.3112048+00:00" finish-rtf="2026-10-06T18:43:58.4657478+00:00" type="Hexalith.Parties.Tests.Domain.PartyIdentityAdmissionTests" method="MissingOrUnsupportedPolicy_DeniesBeforeAnyProtectedUnwrap" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Domain/PartyIdentityAdmissionTests.cs" source-line="18" /><test id="6eddedbd-2946-4dd2-88be-6a261888e2cc" name="Hexalith.Parties.Tests.Domain.PartyIdentityAdmissionTests.MissingOrUnsupportedPolicy_DeniesBeforeAnyProtectedUnwrap(trigger: \&quot;unsupported\&quot;)" result="Pass" time="0.001" time-rtf="00:00:00.0010000" start-rtf="2026-10-06T18:43:58.4688356+00:00" finish-rtf="2026-10-06T18:43:58.4707523+00:00" type="Hexalith.Parties.Tests.Domain.PartyIdentityAdmissionTests" method="MissingOrUnsupportedPolicy_DeniesBeforeAnyProtectedUnwrap" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Domain/PartyIdentityAdmissionTests.cs" source-line="18" /><test id="0d7dcbae-9372-4d58-a53d-3961725bdb0c" name="Hexalith.Parties.Tests.Domain.PartyIdentityAdmissionTests.MissingOrUnsupportedPolicy_DeniesBeforeAnyProtectedUnwrap(trigger: \&quot;binding-closure\&quot;)" result="Pass" time="0" time-rtf="00:00:00.0000000" start-rtf="2026-10-06T18:43:58.4708100+00:00" finish-rtf="2026-10-06T18:43:58.4710662+00:00" type="Hexalith.Parties.Tests.Domain.PartyIdentityAdmissionTests" method="MissingOrUnsupportedPolicy_DeniesBeforeAnyProtectedUnwrap" source-file="/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Domain/PartyIdentityAdmissionTests.cs" source-line="18" /></collection></assembly></assemblies>--- /dev/null
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
--- /dev/null
+++ b/parties/_bmad-output/planning-artifacts/actor-history-retention-policy-v1.md
@@ -0,0 +1,41 @@
+# Minimal opaque actor-history retention policy
+
+Engineering approach accepted by the user's “apply recommendation” instruction on 2026-10-06. **Production policy status: proposed, disabled.** This document does not establish an approved duration, owner acceptance, or qualified storage/custody target.
+
+Proposed policy reference: `party-actor-retention-v1`. Purpose: `party-actor-history-v1`. Product/Governance approves scope and duration; Parties owns the binding contract; Platform owns independent identity authority and custody; EventStore supplies the protected source seam.
+
+## Reuse assessment and selected approach
+
+No located approved policy explicitly covers the tenant/Party-to-stable-human-actor relationship after profile erasure. Agents' 365-day rule starts at interaction terminal state and covers sensitive interaction content ([Agents specification](../../../agents/_bmad-output/specs/spec-agents/SPEC.md)). Parties' crypto/projection “retention” proposals retain implementation code during migration, rather than defining a data lifetime. The invoice retention examples in [integration guidance](../../docs/event-handler-patterns.md) do not authorize actor-history retention. None supplies this record's exact scope, duration, expiry, cleanup and restore rules.
+
+Use one narrow policy through the existing Party aggregate, EventStore `IdentityHistoryPolicy` and purpose-specific custody seam. Reuse an existing policy later only when its recorded approval explicitly covers these same requirements. Keep current configuration unset until approval; no new database, service or general policy engine is needed.
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
+The duration `D` is an explicitly approved, strictly positive finite `TimeSpan`, with no default. The selected v1 trigger is `binding-effective-at`, already supported by the contract: `ExpiresAt = ValidFrom + D`. Arithmetic overflow denies admission. Expiry is exclusive: no release at or after ExpiresAt.
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
+These are required provider behaviors. No cleanup worker, qualified custody backend, backup destruction or restore proof is claimed by the local query guard.
+
+## Runtime gate and activation record
+
+`Parties:Identity` must contain the exact approved versioned PolicyId, approved finite Retention and supported ExpiryTrigger. An unset/invalid policy returns Unavailable for binding reads and denies binding writes. A readable custody response alone is insufficient: policy ID, purpose, lifecycle flags and recorded expiry must match. Each read snapshots configuration and checks it again after awaits and before releasing evidence; changed/withdrawn configuration denies the result. Independent source authority, fresh custody, expiry and cancellation checks still apply.
+
+Before production activation, the owners must record the approved reference/version and duration, explicit post-profile-erasure coverage, approval reference, supported expiry trigger, exact custody/source target and copy inventory, cleanup/recovery/restore evidence, and complete owner compatibility acceptance. No production settings are populated by this proposal. The test fixture's ten-day duration is synthetic and carries no approval.
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
`. Read that file — it is the content under review.

Do not invoke any skill, and do not spawn subagents of your own — you are the reviewer. If the instruction file is unreadable, report that exact failure and stop. Return your findings as text in your final message; do not route them through any findings-reporting tool the host may offer.
