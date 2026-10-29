---
title: 'Complete published setup status and reconcile five deferred records'
type: 'bugfix'
created: '2026-10-29'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
baseline_commit: '0b0bb5385f642b00e68dcdffdd35758b938a57f0'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete five deferred-work records concerning setup status: DW-7 live reader binding, DW-12 terminal rejection, DW-24 exact tenant/domain authorization, DW-26 unbound UI admission, and the published exact-domain SDK capability follow-up. Four have source-level closure addenda but stale original open statuses; reconcile that history honestly rather than count them as newly discovered fixes. Enable and verify the missing package-mode capability using the already selected compatible EventStore SDK. Preserve exact tenant/domain grants, identity/correlation checks, refusal without credentials, cancellation, host overrides, accepted-write uncertainty and terminal UI behavior. Include the user's existing staged Story 5.4 changes, verify the combined work, and push conventional commits on main as requested. Production topology, owner acceptance and dependency availability remain separately governed.

</frozen-after-approval>

## Implementation Notes

- Route facts: no unresolved user-visible choice, migration, runtime configuration, live qualification or deployment. Small correction to the existing assertion adapter and existing package contract/tests; no new public surface or dependency version. No owned AppHost exists in Agents. Preserve all root submodule pins and contents.
- Current Builds selects Client/DomainService 3.128.3. Published API inspection confirms exact-domain assertions, whereas source-only HEXALITH_EVENTSTORE_DOMAIN_ASSERTIONS guards still encode the former 3.127.0 limitation.
- Remove the obsolete package refusal and capability compilation branches from the adapter, Server/Server.Tests project files and status/composition tests. Both actual dependency modes must execute identical exact-domain positive and negative cases. Add a real published issuer-to-composed-reader regression where supported by existing public SDK registration.
- Raise the maintained Story 5.2 package floor to the first verified compatible stable version and update its boundary fixtures and contributor guidance. Retain audit-enabled builds and conservative unsupported-version refusal.
- Re-run maintained Story 5.4 Local verification for the authorized staged changes, focused and full Server/UI package suites, Story 5.2 source verification, and affected tooling fixtures. Record native totals, exact commands and canonical dependency revisions in a dated verification report. Independent blind review is required before committing.
- Reconcile the original DW-7, DW-12, DW-24 and DW-26 statuses with their 2026-10-25 addendum, retaining that historical evidence and recording fresh package capability evidence. DW-25 is an alias owned by DW-12 and does not count as another item. Close the unnumbered SDK follow-up only on passing current package tests; DW-6, DW-19, DW-21 and live owner qualification remain open.

- Published Client and DomainService 3.128.3 expose the required exact-domain API. Removed source-only switches and the legacy package refusal; identical reader/composition scenarios now run in both modes. Added six real-issuer cases across Agent, platform Provider catalog and tenant enablement domains with configured/missing credentials and undeclared-domain refusal. Strengthened the stable floor with positive boundary/build-metadata cases, every-prerelease refusal and required Client/DomainService rows.
- The authorized staged lifetime changes were independently reviewed together with this batch. Fifteen targeted regressions reproduced eight failures before correction. Provider operations now retain their originating context through nested reads, dispatch and receipt reconciliation. Agent payload-less receipt status checks retain the bound before and after their await. Faulted setup reads recheck authority. Invocation gates retain tenant clock history, preserve a shorter role interval through an internal restriction of the original bound, and return Denied for final authority refusal. Host admission documentation and a reconstructed-intent positive control explicitly preserve the Authority reference.
- Source validation: maintained Story 5.4 Local freshly passes 1,997 Contracts, 7 Client and 1,017 Domain tests, then blocks at Conversations' required Platform tool-selected source mapping. The maintained Story 5.2 source restore encounters the same blocker. A focused source UI build is warning-free and all 1,249 UI tests pass. No gate, source-mapping requirement, dependency record or submodule pointer was weakened. Package-mode builds and native regression suites provide focused evidence separately; full parent Story 5.4/live acceptance remains incomplete.
- The one-shot workflow configures Blind Hunter only. All ten findings were checked and patched; the reviewer rechecked the final parent-bound correction and reports no remaining concrete issues. No extra review layer is configured or skipped.

## Review Triage Log

- R1 — high / patch: nested provider writes replaced the original authority with a later context. Carry the first context through tenant dispatch; distinct-deadline admission controls reproduce refusal at the original boundary.
- R2 — medium / patch: data-handling preflight used a newly authorized reader. Retain the first context through its private tenant reader, so expiry/faults stop later protected reads.
- R3 — high / patch: provider receipt reconciliation could refresh its deadline. Pass the originating context through outcome resolution; delayed acknowledgements preserve known receipt truth without another protected status read.
- R4 — high / patch: payload-less Agent receipts read status without lifetime checks. Check before and after the status await; expiry/rollback retains UnableToVerify, while current controls still expose terminal rejection.
- R5 — medium / patch: faulted setup-store awaits returned dependency information after authority failed. Both API/query exception paths now recheck authority and return the same NotAuthorized shape as successful expired reads; eight fault/expiry/rollback cases cover both setup entry points.
- R6 — high / patch: gate evaluation lost the tenant reader's checked history, and replacing the bound could discard a separate shorter role interval. Validate the original bound at the captured gate instant and use an internal restriction retaining its parent deadline/history through admission. Rollback and shorter-role tests include valid controls.
- R7 — medium / patch: final admission expiry escaped as an exception. Translate only the expected authority refusal to Denied, preserving caller cancellation and preventing gateway submission.
- R8 — low / patch: higher-version prereleases passed the stable capability floor. Refuse all prerelease rows; stable floor/build-metadata/later-version controls remain accepted.
- R9 — medium / patch: partial package catalogs could pass without checking both delegation and issuance components. Require Client and DomainService rows; Contracts-only and either-component-only fixtures fail closed.
- R10 — low / patch: host admission did not document retaining the transient bound when reconstructing immutable intents. Document explicit Authority reference copying and exercise successful reconstruction with added trust metadata. AgentCommandIntent is a class, so record-copy syntax is deliberately not used.

- Final verification: the unchanged maintained Release/package gate passes all focused/composition gates and 6,104 owning tests (1,997 Contracts, 7 Client, 1,017 Domain, 1,834 Server, 1,249 UI). Final native lifetime/gate selection passes 223 cases. Complete tooling passes all 281 tests with no skips. Package builds are warning-free; CRLF-aware whitespace, Conventional Commit validation and remote divergence checks pass. Durable logs, XML, source fingerprints and canonical gitlinks are retained in `tests/published-setup-status-2026-10-29/verification.json` and its archive. Source-gate blockers remain separately recorded; parent Story 5.4 and live readiness are not marked complete.
