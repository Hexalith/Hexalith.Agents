---
title: 'Resolve five deferred provider recovery and context items'
type: 'bugfix'
created: '2026-11-16'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'ddddc5b53c8083f419367cfabffa2d088c61d748'
context:
  - /home/administrator/projects/hexalith/agents/AGENTS.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Four deferred provider-governance defects risk lost command identities, cross-account pending records, corrupt-state fallback and writes without observable terminal status. A fifth deferred item identifies two omitted Epic 5 context constraints.

**Approach:** Resolve these five entries together: gate unsupported platform writes; persist exact identities before dispatch; guard storage across authentication transitions; validate pending records; restore accepted context constraints. Independently review, verify, commit and push on `main`.

## Boundaries & Constraints

**Always:** Preserve authorization and exact receipt/projection confirmation. Scope storage to subject/tenant. Keep prepared intentions distinct from accepted effects. Retain potentially dispatched identities across ambiguous failures; unlock only with exact terminal truth. Cover catalog mutations, tenant enablement and terms decisions. Preserve valid legacy records and explicit host capability overrides. Follow repository UX/Git rules; use `run-with-temp` for disposable jobs and preserve useful logs outside TMPDIR.

**Never:** Invent privileged status routes, bypass SDK system refusal, persist command payloads/secrets in browser state, infer success from versions, erase unresolved locks, initialize nested submodules or claim live owner qualification.

## I/O & Edge-Case Matrix

| Scenario | Input | Behavior | Failure |
|----------|-------|----------|---------|
| Unsupported status | Authorized platform write; default/deferred reader | Zero dispatch | Existing unavailable result |
| Supported host | Explicit system/provider-catalog capability | Existing governed write/confirmation | Preserve authority/scope checks |
| First save fails | Prepared identity cannot persist | Zero submission | Closed controls; no acceptance claim |
| Ambiguous submission | Persisted identity; transport/receipt-save failure | Refresh retains exact lock | No replacement identity or optimistic success |
| Account transition | Change during gate/JS await, including change back | Refuse stale disclosure or redirected writes | Already delivered old-key JS writes cannot be rolled back |
| Corrupt storage | Null/root/member/identity/version/enum/resource errors or conflicting duplicates | Refuse load/save/remove without clearing state | Only absent storage or valid empty arrays mean empty |
| Context omission | Accepted 2026-10-08 owner inputs | Restore both constraints | No qualification promotion |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Agents.Server/Ports/IAgentCommandStatusReader.cs` and its EventStore/deferred implementations: additive exact-scope capability defaults to refusal; SDK system status is unsupported.
- `src/Hexalith.Agents.Server/Application/Agents/EventStoreProviderCatalogOperations.cs`: authorize platform writes before capability checking and dispatch; existing options accept idempotency/correlation identities.
- `src/Hexalith.Agents.UI/Services/Gateways/IProviderCatalogGateway.cs` and implementations: pass prepared identities through public client options; unsupported bindings refuse safely.
- `src/Hexalith.Agents.UI/State/BrowserSessionPendingProviderCommandStore.cs`, `PendingProviderCommand.cs`, `IPendingProviderCommandStore.cs`: reuse hashed keys and safe fields; validate records and transitions across semaphore/interop awaits. Detect change-back through authentication generation; dispose subscriptions.
- `src/Hexalith.Agents.UI/Components/Pages/ProviderCatalog.razor` and `TenantProviderCatalog.razor`: current submissions precede saving; reuse restore/check paths and per-resource locks.
- `test/Hexalith.Agents.UI.Tests` and `test/Hexalith.Agents.Server.Tests`: existing store, gateway, governance, operations and composition suites.
- `_bmad-output/implementation-artifacts/story-5-4-owner-commitments-2026-10-07.md`: accepted owner wording under “Further owner inputs accepted”.

## Tasks & Acceptance

**Execution:**
- [x] Server status seam/operations -- gate platform catalog dispatch on explicit scoped capability -- prevent unobservable mutations.
- [x] UI gateway/state/pages -- prepare canonical identities, persist before submission, validate returned identity, retain uncertain records -- survive refresh and first-save failure.
- [x] Browser pending store -- guard each awaited boundary and dispose authentication subscriptions -- prevent cross-account results.
- [x] Browser pending store -- validate persisted records and saves without overwriting corruption -- preserve valid legacy records and locks.
- [x] `_bmad-output/implementation-artifacts/epic-5-context.md` -- restore currently open/undeleted Conversations created in `[from,to)`, including zero Agent Calls, and a dedicated Conversations-owned service Party with explicit tenant enrollment/current machine binding -- retain accepted requirements.
- [x] Owning tests, `README.md`, deferred ledger and this spec -- verify edge cases, document capability requirements, close exactly five entries with evidence -- support review.

**Acceptance Criteria:**
- Given unsupported or explicitly capable status readers, when platform writes run, then authorization precedes capability checks and only supported writes can dispatch.
- Given storage or submission failures, when governance writes run and pages refresh, then persistence precedes effects and every potentially dispatched identity stays locked until exact terminal evidence.
- Given authentication transitions or corrupt records, when pending operations resume, then unsafe disclosure/mutation is refused without an empty-state fallback.
- Given accepted owner inputs, when the context is read, then both omitted constraints are explicit without changing dependency qualification.

## Implementation Notes

- Approved by the user on 2026-11-16. The implementation handoff performs local changes and verification only; the coordinator handles staging, independent review, commits and pushes afterward.

## Spec Change Log

## Review Triage Log

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| BH-1 | low — A prepared attempt can remain locked after the capability gate refuses dispatch. The public Unavailable category also represents ambiguous dispatch/transport failures and is not authenticated no-effect evidence. The approved safety rule requires retaining unresolved identities; adding a cancellation/readiness protocol is more than a direct correction for an intentionally unsupported host. Document the limitation. | reject additional protocol |
| BH-2 | low — Crash between durable preparation and dispatch is indistinguishable from a lost dispatch response to existing recovery readers. Clearing by absence would violate the approved retention rule. An authoritative cancellation/reconciliation mechanism adds a new protocol for an infrequent interruption. | reject additional protocol |
| BH-3 | medium — The new two-save lifecycle anchors preparation in one scope but recaptures scope for acknowledgement/removal. A suspended gateway can complete after a principal transition. Preserve the first command scope/generation internally and refuse later lifecycle operations across it, without adding public surface. | patch |
| BH-4 | medium — Restored prepared enablement sets null truth, but markup falls back to AuthoritativePending; its localized text claims acceptance. | patch |
| BH-5 | medium — Enablement selection/check and decision check promote unacknowledged records after inconclusive status. Keep prepared truth unknown and test manual checks. | patch |
| BH-6 | medium — Exact successful manual recovery changes truth but stale failed write fields still control markup in all three families. Clear/update those fields on verified recovery and test displayed outcomes. | patch |
| BH-7 | medium — A semaphore is per store instance and overlapping instances can lose read/modify/write updates. The baseline store has the same instance semaphore and separate JS calls; this is an existing cross-instance coordination defect, distinct from the selected in-flight authentication correction. | defer |
| BH-8 | low — Same-identity saves could change expectation fields or regress state, but current page callers only acknowledge their exact prepared record with unchanged expectations. No ordinary caller performs the proposed regression; extra transition machinery would protect speculative misuse. | reject complexity |
| BH-9 | medium — Duplicate JSON properties select their final occurrence in both validation and deserialization, allowing conflicting persisted fields through the new corruption boundary. Reject duplicate record/acceptance names and cover preservation. | patch |
| BH-10 | low — The evidence script has an absolute author checkout root and can verify the wrong tree elsewhere. Derive the root from its location. | patch |
| EC-1 | medium — Same verified restored-enablement markup defect as BH-4; use existing unknown/failure presentation without inventing an acceptance claim. | patch |
| EC-2 | medium — Same verified unacknowledged decision polling defect as BH-5; enablement polling/selection share this root. | patch |
| EC-3 | low — A coarse ValidationFailed response can leave an intention locked. The mapper combines downstream HTTP 400/422 with local validation and carries no scoped no-dispatch proof. Current local editor validation precedes preparation. General release on that category is unsafe; a definitive rejection protocol is additional machinery. | reject additional protocol |
| EC-4 | medium — Same verified acknowledgement scope recapture as BH-3. A private first-command scope fence can preserve the new preparation anchor across separate calls. | patch |
| VG-1 | medium — Pre-verified gap: browser-store tests only round-trip acknowledged records while UI recovery substitutes the memory store. Add real browser-store prepared/refresh/acknowledged round trips for each family. | patch |
| VG-2 | medium — Pre-verified gap: only the new Disable overload checks real client option propagation. Exercise all six prepared overloads with receipts derived from received options. | patch |
| VG-other-1 | medium — Same verified prepared acceptance promotion as BH-4/BH-5; existing recovery assertions omit AuthoritativePending and do not poll after refresh. | patch |

## Verification

- Attempt Debug/source builds in disposable workspaces; use the repository fallback ladder and separately record source-cohort blockers.
- Run suspended gate/read/write authentication tests, corruption controls, write-ahead ordering, failed-first-save, ambiguous-refresh, exact-receipt and status-capability/no-dispatch regressions.
- Run full owning UI/Server suites and affected Client/Contracts checks using built xUnit v3 assemblies. Preserve warnings/audits/skip gates.
- Independently review final changes/evidence and compare context with accepted inputs. Run whitespace and Conventional Commit validation.
- Require `python3 references/Hexalith.Builds/check.py` from the module root. Inspect divergence and all root submodules, then push `main` and verify remote SHA.

### Local implementation evidence — 2026-11-16

- Added explicit scoped status capability (default refusal) and authorization-first platform dispatch gating; SDK system refusal and host overrides remain intact.
- Catalog mutations, tenant enablement and terms decisions persist prepared canonical identities before effects, pass public client correlation/idempotency options, validate exact receipts and correlate recovery polling. Ambiguous effects remain locked across refresh; first-save failure submits nothing.
- Browser storage validates records/saves and guards authentication generation plus subject/tenant key across gate and interop awaits, including change-back. It unsubscribes on disposal and preserves corrupt storage and unresolved identities.
- Restored both accepted owner constraints in Epic 5 context; existing dependency qualification is unchanged. Exactly five deferred entries closed with [verification evidence](tests/provider-recovery-2026-11-16/README.md).
- Full runnable suites: UI Debug/source 1,307; Server Release/package fallback 1,888; Client Debug/source 7; Contracts Debug/source 2,005. Total 5,207 passed, zero failures/skips/not-run; successful builds have zero warnings/errors. Context verification: 13 assertions passed. `git -c core.whitespace=cr-at-eol diff --check` passes.
- Server Debug/source is blocked by Conversations requiring Platform tool-selected source mapping before compilation; [exact blocker](tests/provider-recovery-2026-11-16/source-Server.log.gz) is retained independently from passing fallback evidence. No audits or skip gates were weakened.
- Coordinator work remains: stage/read the complete diff, perform independent review, run the clean committed-module `references/Hexalith.Builds/check.py` gate, inspect root submodules/divergence, commit/push and verify remote SHA. This handoff performs no staging, commit or remote operations. No live owner qualification is claimed.

- Coordinator audit matched all 20 source/test hashes to current bytes, parsed the compressed XML (5,207 passing cases, zero failures/errors/skips), verified every matrix test selection ran, and reran the 13 context assertions. Three independent review layers completed; accepted patches are recorded above.

### Final coordinator verification — 2026-11-16

- All accepted review patches are implemented. Prepared state remains unknown through restore/reselection/manual checks, successful exact recovery replaces stale failure displays and reopens controls, the store retains the original command scope across separate lifecycle calls, duplicate JSON names refuse mutation, production browser round trips and every prepared gateway overload are covered, and the verification script derives its checkout root.
- Final full suites: UI Debug/source 1,344; Server Release/package 1,888; Client Debug/source 7; Contracts Debug/source 2,005. Total **5,244 passed**, zero failures/errors/skips/not-run. All successful builds have zero warnings/errors. The unchanged Server source-mapping prerequisite remains separately blocked; harness exit 1 preserves that result.
- Coordinator parsed every final full XML, checked the new review-case counts, refreshed all 20 source/test hashes, reran the 13 context assertions, and verified staged/unstaged whitespace. See [final evidence](tests/provider-recovery-2026-11-16/README.md).
- Independent reviews are fully triaged above; BH-7 is appended as pre-existing deferred work. Unobservable prepared attempts remain fail-closed until scoped authoritative evidence is available; no speculative absence-based unlock was added.
