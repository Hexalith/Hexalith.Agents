# Deferred owner and setup verification — 2026-10-24

This batch closes five stale defect entries using existing implementations and new
maintained regression gates. It does not certify a deployed topology or advance
Story 5.4/5.6, readiness registers, or dependency acceptance. The original defect
descriptions remain in the ledger. Current source, commands and individual native
outcomes are preserved in the accompanying
[verification evidence](deferred-owner-setup-verification-2026-10-24.json).

Baseline Agents source: `f8ca548e1d55b26a4d9e0faaac04be96c511fc69`.
Pinned Parties source: `4fc1124eb995a40553cb23b042d4a3c1add3dfb2`.
Pinned EventStore source: `5ce5f1d84b88c808cc8a526b9decf149de977e55`.
No owner source or submodule pointer changes are part of this batch.

## Setup query authorization

DW-2 originally described deferred tenant authorization beside claim-based HTTP
checks. The current composition instead installs `ProjectedTenantAccessReader`,
and both setup queries require a verified originating subject bound to the exact
tenant, current actor/administrator evidence, and fresh tenant access before any
read-model access. The real SDK query-dispatcher tests distinguish the verified
human caller from the transport workload, deny altered caller/tenant/authority
bindings before any source access, and refuse a current non-administrator despite
an administrator role claim.

The Story 5.2 focused gate now explicitly requires `VerifiedSetupQueryCallerTests`
and `TenantAuthorityAdmissionOrderingTests`. The latter covers actor expiry during
the authority await and final role/authority rechecks. Controlled runner failures
prove either class returning zero, skipped or unrun cases stops the gate before
full-project verification. Three additional method gates require all twelve
verified-caller rows, all four role-await recheck rows and the actor-expiry case;
removing a method or theory row cannot hide behind another passing class method.
Full setup query, composition and context-provider
regressions remain required. Hosts still default to unavailable current authority
sources until their owner-qualified bindings are installed; no live tenant/Party
source is invented by this closure.

These are admission-time authority checks. The current handler does not revalidate
authority after awaiting read-model access and approver enrichment; this batch
supplies no completion-time freshness proof for those later awaits.

## Exact setup write correlation

The legacy correlation entry describes threshold-only polling. The current
operations require canonical message/correlation receipts and a domain-derived
effect and target configuration version. Exact retries retain command identity;
no-op receipts confirm their unchanged target without guessing a new version;
foreign or malformed receipts cannot establish progress. The lifecycle regression
carries an actual disable result through the acceptance and projection paths,
observes the old projection as pending, and checks persisted Disabled/version
state before confirmation. The UI regressions preserve the exact unresolved
attempt across retries within the existing component lifetime.

This closes the local correlation defect. `DeferredAgentCommandStatusReader`
remains the default, so a payload-less rejection stays unverifiable without a
qualified host reader. Production EventStore completion/replay/projection,
route/reload attempt lifetime, and draft-versus-newer-authority behavior remain
separately open. The injected-reader rejection test is local branch coverage,
not evidence of live status reachability.

## Successor continuity after predecessor expiry

B02 described a retained fold starting at version zero after the predecessor's
actor-bearing event was removed. The pinned Parties fold now accepts authenticated
actor-free expired-event certificates for exact source transitions. Fifteen
required query rows cover a retained successor, retained revocation and rebound,
two expired transitions before version three, serialized certificate transport,
and denial for omitted, foreign, expired, wrong-policy, wrong-transition,
overlapping or unsupported certificates. They also assert expired predecessors
and intermediate actors cannot be reconstructed from those certificates.

These synthetic source/custody proofs establish the corrected fold and query
behavior. Installed source authenticity, certificate/destruction custody and the
full owner qualification remain required; the separate B04 rollout inventory,
B05 registry revocation freshness and profile/schema privacy entries stay open.

## Parties endpoint security compatibility

The recorded startup blocker named missing sidecar policies and anonymous health
routes. The pinned host now applies the SDK sidecar-channel contract to its
subscriptions/actors, and the process fixture uses real workload assertions and
channel authentication without weakening the SDK endpoint audit.

The maintained command requires every original process endpoint case (13 rows)
and every domain-service security case (35 rows). They exercise payload/result
serialization, production replay boundaries, workload credential refusal, channel
requirements, exact endpoint inventory and inherited configuration isolation.
The full actor-route argument is preserved and checked against the exact method's
source attributes because xUnit abbreviates its display name. Immutable source
snapshots and binary hashes bind that argument inventory to the tested build.
The fixture substitutes backend health checks; passing it proves host/security
compatibility, not a running Dapr backend or deployed workload qualification.

## Standalone retained transport invocation

E2 described synchronous provider invocation before bounded waits and shared
cancellation callbacks. The pinned EventStore retained-history reader now uses
`AuthoritativeStreamReadDeadline` across invocation, asynchronous waits, body reads
and terminal validation. Its 32 required rows include positive and malformed
source controls, caller cancellation, synchronous/noncooperative providers,
blocking callbacks, cumulative budget expiry, late owned-material disposal and
blocking/failing response cleanup.

The corrected standalone reader bounds the verdict wait. Indefinitely blocked
providers can still retain execution resources; shared runtime admission,
isolation, cancellation cooperation and resource reclamation remain unqualified
under the existing B1/B4/B7 entries. This closure does not claim otherwise.

## Reproduction and evidence

Run from the repository root:

```bash
python3 scripts/verify-parties-identity.py --artifacts /tmp/agents-five-deferred-owner
pwsh -NoProfile -File eng/verify-story-5.2.ps1
python3 -m unittest discover -s tests/tooling -p '*_test.py'
git -c core.whitespace=cr-at-eol diff --check
python3 scripts/validate-deferred-verification.py _bmad-output/implementation-artifacts/deferred-owner-setup-verification-2026-10-24.json
```

The source lane builds Debug project references to the root-declared dependencies;
no package/source parity, package publication, hosted CI run or production approval
is inferred. Individual identities/outcomes are retained for all 173 owner rows
and eight supplementary root methods (34 rows). The full root suites, other
focused classes and UI scenarios are retained as aggregate runner summaries;
they have no individual-case inventory in this artifact.

Each embedded native report has an assembly SHA-256 and a reproducible canonical
results SHA-256. The canonical input is UTF-8 JSON for `assemblySha256`, `command`,
`summary` and `cases`, with sorted keys and separators `(',', ':')`. Original XML
hashes identify the temporary originals; the validator verifies the canonical
embedded representation after those originals disappear. It also checks current
source hashes, receipt/method inventories and internal result consistency, and
compares binaries when they remain available. It never executes embedded commands
and supplies no cryptographic authenticity or production qualification.

The maintained identity receipt fingerprints all eight consumed repositories,
including tracked diffs and untracked contents, compares them again before
success, and records the actual test binary hashes. Run it while those sources
are stable; repository edits during verification invalidate its result. Artifact
directories must be outside all consumed source trees and must not contain a
source tree; unsupported locations are rejected before any verification work.


Final verification passes with zero build warnings/errors, 5,655 full root tests,
173 native owner cases and 14 receipt regressions. The broad tooling run passes
247 tests; final focused runs pass all 47 identity-harness and 23 evidence-validator
tests. Those focused runs cover the two modules changed after broad discovery,
covering the current 256-test inventory together; the suite counts overlap.
Independent review findings are patched, with no new deferrals. The maintained
evidence validator passes against the final record.
