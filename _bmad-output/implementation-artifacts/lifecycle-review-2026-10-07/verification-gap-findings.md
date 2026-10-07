### Current queries lack verification of the final authority comparison

- **Changed surface:** `ResolveAsync` now compares entry and completion authority before releasing evidence — [PartyIdentityQueryService.cs:113](/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs:113).
- **Impacted consumer or site:** `PartyIdentityQueryHandler.ExecuteAsync` publishes that result through the gateway — [PartyIdentityQueryHandler.cs:27](/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/Queries/PartyIdentityQueryHandler.cs:27).
- **Existing test evidence:** `Regression gap`. [ReadAuthorityChangesDuringCustody:82](/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs:82) changes authority but calls only `ResolveAtAsync`. The current success test uses stable authority; the final-authority policy test changes options while returning the same grant. Repository-wide searches for `PartyIdentityQueryService` and `Hexalith.Parties.Queries` found no other tests exercising this service.
- **Missing verification:** A current query must return `Unavailable` with no evidence when authority revision changes during its source or custody read.
- **Demonstration:** Remove only the current method’s `!SameAuthority(...)` condition, preserving the final `Admit` call and other guards. A changed, still-active authority revision would then release the original binding. The historical authority tests and current policy/cancellation tests would remain unaffected.
- **Consequence:** The gateway could publish current eligibility using authority superseded during the request.
- **Disposition:** `patch` — add `CurrentAuthorityChangesDuringAwait_DeniesWithoutEvidence` to `PartyIdentityQueryHandlerTests`.

### Current queries lack verification of expiry crossed during custody reads

- **Changed surface:** Current results now require completion before both binding and custody expiry — [PartyIdentityQueryService.cs:115](/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs:115).
- **Impacted consumer or site:** `PartyIdentityQueryHandler.ExecuteAsync` serializes the resulting current eligibility — [PartyIdentityQueryHandler.cs:28](/home/administrator/projects/hexalith/parties/src/Hexalith.Parties/Queries/PartyIdentityQueryHandler.cs:28).
- **Existing test evidence:** `Regression gap`. [CustodyExpiryCrossingAwait:55](/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs:55) advances a controlled clock during custody, but exercises only historical resolution. Current service tests use bindings valid for ten days. [The client expiry test:95](/home/administrator/projects/hexalith/parties/tests/Hexalith.Parties.Client.Tests/HttpPartiesIdentityClientTests.cs:95) supplies an already-expired, wrong-purpose reply without executing the service. None of these tests covers current completion crossing expiry; repository-wide symbol/import searches found no additional service tests.
- **Missing verification:** Advance a controlled clock to the exclusive deadline during a current custody read, keep authority valid, and assert `Unavailable` with null evidence.
- **Demonstration:** Delete the current method’s two completion-time expiry comparisons. A custody response returning `true` exactly at expiry would release `Resolved`; the historical clock-crossing test would still pass.
- **Consequence:** Current human eligibility could be returned after its binding or custody lifetime ends.
- **Disposition:** `patch` — add `CurrentBindingExpiryCrossingCustodyAwait_DeniesWithoutEvidence`, covering binding closure and custody expiry.

### Readiness smoke verification never reaches exact-target receipt validation

- **Changed surface:** Live readiness now validates the compatibility receipt’s target, command, outcome, evidence level, and prerequisites — [verify-ext-parties-history-readiness.ps1:36](/home/administrator/projects/hexalith/parties/eng/verify-ext-parties-history-readiness.ps1:36).
- **Impacted consumer or site:** The `LiveReadiness` wrapper invokes this gate before owner HTTP probes — [verify-ext-parties-1.ps1:17](/home/administrator/projects/hexalith/parties/eng/verify-ext-parties-1.ps1:17).
- **Existing test evidence:** `Broken-verification gap`. I read the recorded [counting-server smoke test:16](/tmp/parties-story54-readiness-gates-smoke-20261006.py:16). Its cases cover missing inputs and an `Uncommitted` register; both stop before receipt validation. Repository-wide searches for the readiness script and `EXT_PARTIES_COMPATIBILITY_RECEIPT` returned the two verifiers and their evidence JSON, without another test invocation.
- **Missing verification:** An accepted, synthetic `Available` register paired with a wrong-target receipt must fail before making any HTTP request.
- **Demonstration:** Remove `$receipt.targetVersionOrCommit -cne $acceptedTarget`. Both recorded smoke cases would still fail at their earlier gates, leaving this regression undetected.
- **Consequence:** Readiness probes could execute using compatibility proof for a different target.
- **Disposition:** `patch` — add `LiveReadiness_RejectsMismatchedReceiptBeforeHttp` to `Hexalith.Parties.Ci.Tests`, using synthetic register/receipt fixtures and a counting HTTP server.

## Other findings

- [The readiness verifier:42](/home/administrator/projects/hexalith/parties/eng/verify-ext-parties-history-readiness.ps1:42) parses retention and compares its string with the receipt, but never checks that returned custody expiry equals `ValidFrom + retention`. Its reply checks require only a future expiry, and its original-source comparison checks actor, version, tenant, and Party. Consequently, otherwise valid source/reply evidence with a 366-day expiry can pass readiness under a declared 365-day policy.
