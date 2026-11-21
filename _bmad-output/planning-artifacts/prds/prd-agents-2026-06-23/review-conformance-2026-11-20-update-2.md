# PRD conformance review, 2026-11-20

The revised PRD distinguishes development evidence from owner qualification. FR-5 requires a current, authorized observation that the credential resolves. FR-23 requires the retired status lookup to return its typed HTTP 422 refusal under normal authentication even during a dependency outage. FR-28 now includes the 18 required gate records, and §8.1 scopes A-26 and A-28 independently. SM-C1 through SM-C3 have owners, cohorts, reporting times and evidence requirements. The addendum correctly identifies the two discrepancies in the current implementation. [PRD validation](validation-report.md) · [Architecture validation](../../architecture/architecture-agents-2026-06-23-2/VALIDATION-REPORT-2026-11-20.md)

## C1 · High · New operation decisions need explicit catalog handoff

FR-28 item 10 names three existing records as the materialized binding deferred rows. OQ-35 through OQ-38 now block operations including routine expiry erasure, retry after an uncertain append, a same-attempt Provider retry and overlapping export-key delivery. The register requires a governed record for every binding deferred decision that blocks a runtime or delivery operation. Specify that these four branches fail closed until the responsible owners register and activate decisions for their exact operations. Identify the three existing records as examples and preserve the existing scope of `RQ-1`. [FR-28](prd.md#fr-28-define-launch-readiness-controls) · [Decision register](prd.md#13-v1-decision-register) · [Launch register, line 96](../../launch-readiness-register.md)

## C2 · Medium · The pre-invocation accounting decision has two names

FR-8 and OQ-6 call the durable disposition `NotInvoked`. FR-28 and AD-21 require `BudgetDispositionDecided(NotInvokedRelease)` before the ledger releases money. Use `NotInvokedRelease` consistently for the disposition. If `NotInvoked` is also presented as a reason, label it explicitly. The reservation held for a zero-use retry after invocation authorization is a separate path. [FR-8](prd.md#fr-8-call-agent-from-conversation) · [FR-28](prd.md#fr-28-define-launch-readiness-controls) · [AD-21](../../architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#ad-21---cost-reservation-rate-limits-and-settlement)

## C3 · Low · OQ-23 understates Automatic-mode reporting

OQ-23 says Automatic-mode tenants are covered by SM-C4 and the posting-failure rate “only.” SM-C1 also requires a qualitative report on their confirmed automatic posts. Narrow the exclusivity to existing safety-outcome monitoring and refer to the SM-C1 observation. Retain the separate pending decision on a Conversations retraction metric. [Success metrics](prd.md#12-success-metrics) · [OQ-23](prd.md#13-v1-decision-register)

## Post-correction disposition

**Pass. All three findings closed.**

- **C1.** FR-28 identifies the three pre-existing records. §13 explicitly requires a catalog entry and an approved, effective record at the required version for each binding row, including OQ-35 through OQ-38. A missing decision blocks its named operation. Its recorded scope determines whether it affects release qualification. [FR-28](prd.md#fr-28-define-launch-readiness-controls) · [Decision register](prd.md#13-v1-decision-register)
- **C2.** FR-8, FR-28 and OQ-6 consistently identify `NotInvokedRelease` as the disposition before durable invocation authorization. FR-8 and OQ-6 identify `NotInvoked` as its reason. A retry after authorization uses the held original reservation. [FR-8](prd.md#fr-8-call-agent-from-conversation) · [FR-28](prd.md#fr-28-define-launch-readiness-controls) · [OQ-6](prd.md#13-v1-decision-register)
- **C3.** OQ-23 identifies SM-C4 and the posting-failure rate as interim safety measures and explicitly retains qualitative SM-C1 reporting for Automatic mode while the retraction decision remains open. [OQ-23](prd.md#13-v1-decision-register) · [Success metrics](prd.md#12-success-metrics)
