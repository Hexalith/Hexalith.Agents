# Parties identity implementation proposal

Owner: Parties Maintainer, with Product for formal launch-contract acceptance and Platform identity maintainers for authenticated actor authority. Implements CAP-1–CAP-3 / EXT-PARTIES-1. The user selected Branch B as the implementation path on 2026-09-27. All new names and paths below are proposed, not published APIs. Implement in Hexalith.Parties; the later consumer changes belong in Hexalith.Agents and Hexalith.Conversations.

## Branch decision

| Choice | Required work | Consequence |
| --- | --- | --- |
| A: public AI Party type | Add an additive wire value without renumbering existing values; define AI details/validation; update creation, state fold, serialization, query/projection classification, clients and UI handling. Restrict Agent creation to the Agents Service Principal. Update Agents and Conversations to check exact ID **and** AI type. | Explicit AI taxonomy, but a larger compatibility change. Existing Organization identities need an explicitly accepted same-ID transition policy; replacement is forbidden. |
| B: accept Organization for V1 | Keep the provisioned Organization and its immutable ID; add restricted provisioning, exact-retry conflict checks, authoritative identity reads and human-binding history. Agents and Conversations validate Agent identity by ID; no runtime type-only Agent authorization. | Smallest path compatible with current code. Organization does not become Human or eligible to approve merely because it represents `hexa`. |

**Implement B.** `PartyType` currently contains only Unknown, Person and Organization; `PartiesAgentPartyDirectory` already requests Organization. No public AI-type contract was found. Branch A remains a comparison for reviewers, not an implementation task in this path. B removes only the need to introduce AI typing; it does not remove tenant authorization, liveness, freshness, history or conflict requirements. Product and the Parties Maintainer must explicitly accept the complete Branch B contract in EXT-PARTIES-1 before it is a launch contract.

## P1 — Close the public contract and authorization boundary

Add narrow contracts in `src/Hexalith.Parties.Contracts`, exposed through `src/Hexalith.Parties.Client` and Parties domain query/command handlers. Do not pass `PartyDetail` through Agents: it contains names, contact data and other PII.

| Proposed operation | Required input | Safe result |
| --- | --- | --- |
| `ProvisionAgentPartyAsync` | Explicit TenantId, AgentId, immutable PartyId, provisioning-contract version and stable logical provisioning identity; initial fixed non-personal label only if needed by Organization validation. Selected branch/type is server policy, not a caller override. | Created / AlreadyProvisioned with exact TenantId, PartyId, original provisioning revision and current identity evidence, or typed conflict/denied/unavailable. |
| `ResolvePartyIdentityAsync` | Explicit TenantId, PartyId; authenticated service/subject scope supplied by transport; server-selected current-evidence requirement. | Resolved evidence or Missing / Ambiguous / Stale / Denied / Unavailable. |
| `ResolveHumanActorBindingAtAsync` | Explicit TenantId, PartyId, authoritative ActionAt; recorded binding version/evidence position when verifying a known action. | Exactly one historical binding with its provenance, or MissingBinding / AmbiguousBinding / Stale / OverlappingHistory / Denied / Unavailable. A mismatch against a recorded actor/version is ActorMismatch. |
| Ingress subject resolution | Authenticated issuer/subject from the identity boundary, explicit authorized tenant; never a user-supplied actor or Party assertion. | One stable AuthenticatedHumanActorId and, when needed, its unique current tenant Party binding; ambiguity or missing authority fails closed. |

The current identity evidence is a closed non-PII result containing `ContractVersion`, exact `TenantId` and `PartyId`, classification Human/Organization/AI, active/restricted/erased state, authoritative evidence position and `ObservedAt`. Human success additionally requires `AuthenticatedHumanActorId`, `HumanActorBindingVersion`, `ValidFrom`, optional exclusive `ValidUntil`, and binding evidence position. Record all positions needed if Party and identity authority have distinct sources; do not manufacture one scalar revision. Non-human results carry no human binding and cannot satisfy an approver check. Under B, the provisioned Party's classification remains Organization. Unknown Party types fail closed; `OrganizationDetails.IsNaturalPerson` alone must not silently confer Human authority.

Final transport codes and public names are maintainer-owned. Internal typed distinctions must not leak existence: unauthorized/cross-tenant public responses have the same absent-resource shape, counts and disclosure. Authorized internal consumers retain the safe typed outcome needed to distinguish uncertainty from authoritative absence.

Tenant handling must be explicit per call. Extend `IPartiesQueryClient` / `IPartiesCommandClient` additively or provide a narrow identity client; use immutable request scope rather than changing shared `PartiesClientOptions.Tenant`. Authenticate and authorize before loading a Party. Cross-check authenticated tenant, envelope tenant, payload tenant, aggregate identity, returned tenant and returned Party ID. Apply the same check to caches, idempotency keys and history queries. Concurrent A/B calls through one client instance must not exchange scope.

The Agents Service Principal may perform only the accepted provision/read operations for the exact tenant and provisioned identity. A Platform human's authorization to enable a tenant is checked in Agents; it does not turn that human or a tenant role into a general Parties creator. Reject direct tenant-role calls, forged service credentials, arbitrary Agent IDs and unrelated Party IDs before mutation. Protect equivalent generic command routes against bypass.

## P2 — Make creation and retries authoritative

Implement a dedicated `ProvisionAgentParty` handler alongside `PartyAggregate.Handle(CreateParty, ...)`, with envelope checks in the existing processing boundary. This keeps the Agent-specific invariant out of general Organization creation while closing alternate creation paths for reserved/provisioned identities.

1. Parties and Agents accept one versioned deterministic mapping for **new** tenant/Agent identities, including TenantId in the derivation inputs and test vectors. The current adapter uses `agent-{AgentId}` and does not take TenantId; it is evidence of a compatibility issue, not a formula to silently replace. Existing stored Party links are immutable. Inventory them and keep their exact IDs. If a legacy mapping cannot be proven tenant-bound, fail readiness and request a same-ID owner-approved reconciliation; never allocate a substitute.
2. On a never-created Party stream, one conditional EventStore append records creation and a proposed content-free `AgentPartyProvisioned` marker. Persist the immutable creation intent: Branch B/Organization and provisioning contract version, AgentId, PartyId via stream identity, tenant via stream identity, and canonical fingerprint of every accepted creation input. Record trusted service provenance and original result revision. Mutable display details are not the retry comparator. No PII or secret enters this marker or result.
3. On replay, distinguish a real creation event from a non-null state produced only by rejection events. Non-null `PartyState` alone proves nothing. New binding/provisioning folds must never use wall-clock `PartyState.CreatedAt`: that field currently derives from `UtcNow` during Apply. Use persisted authoritative event instants and positions.
4. Same tenant, Party, Agent, branch/type and immutable input fingerprint returns the original provisioning identity/revision with no new creation event. Re-evaluate current liveness separately; an exact retry against an inactive/restricted/erased Party cannot report usable readiness or reactivate it.
5. A changed identity/type/creation input at the same logical provisioning identity, an ID occupied by another Party, or an existing Party without sufficient provisioning provenance returns typed `ProvisioningConflict`. An attempt to change this provisioned Organization to Person or a future AI type also conflicts. No second Party, relink, takeover, field overwrite or type conversion occurs. General `CreateParty` / `CreatePartyComposite` must not turn this conflict into a successful no-op for the reserved identity.
6. Concurrent creates serialize at the same tenant/Party stream. On an append conflict or lost acknowledgement, read that exact authoritative stream and compare immutable intent. Exact match returns the original result; mismatch conflicts; unavailable/unknown remains unavailable. Never try another ID.
7. Existing unmarked Organization records require a maintainer-approved provenance policy. A permitted reconciliation may only attest the already-linked ID after validating original creation and trusted ownership evidence; it cannot infer ownership from type/name, rebind an Agent or fabricate missing history. Until accepted and proven, return typed conflict/unavailable.

The current unconditional existing-state no-op in `PartyAggregate` is not sufficient. Update its affected retry tests and add focused dedicated provisioning tests. Preserve unrelated general Party behavior unless necessary to prevent an Agent-identity bypass.

## P3 — Supply current evidence and durable human history

Use EventStore domain commands/events and pure folds. Add binding lifecycle events/state to the owning Party stream so Party liveness and that Party's binding transitions serialize on the same revision; reuse domain-service SDK query and projection seams. The UI's `IdentityBindingProvisioningService` and in-memory store may become clients of that authority, not a second source of truth. An IdP `party_id` attribute is a routing hint and must be checked against the authoritative binding.

The platform identity authority must resolve an opaque, stable human actor ID from authenticated issuer/subject. It is independent of tenant, role, display name, email and Party ID. Role/tenant changes and approved login rotation preserve that ID; a new or ambiguous login is never linked by guessing. The owner must specify issuer trust and continuity proof. Parties stores only the stable actor reference and binding evidence; private subject mapping remains with its identity authority. Party-free Administrator/Platform principals resolve the same actor authority without inventing a target-tenant Party.

Proposed binding transitions are establish, close/revoke and rebind, each authorized by a separately accepted identity-administration policy and conditional on the expected Party/binding revision. A single transition at authoritative instant T closes the previous interval at T and opens the successor at T, using `[ValidFrom, ValidUntil)` intervals. Establishing a binding requires current Human classification and active liveness. Rebinding cannot change the actor recorded on past actions. Suspend/restrict/erase immediately blocks current eligibility; historical attribution follows the accepted retention policy and does not authorize a current action.

Use monotonically increasing binding versions and immutable action/effective instants in events. Exact command retry retains its original time/version. Reject concurrent inconsistent assignments and interval overlap. A history gap returns MissingBinding; overlapping candidates return OverlappingHistory; multiple inconsistent identity authorities return AmbiguousBinding. Corrupt, incomplete or unavailable history must not fall back to the latest binding. If a proposed privacy/erasure policy cannot retain historical identity required by the contract, seek Product/Governance acceptance of a compatible design; do not mark the record complete.

For a current read, obtain Party/binding state at an authoritative EventStore checkpoint and establish that any read model is contiguous through that checkpoint. `ObservedAt` is the server's observation instant, not proof of currency. A missing position, nullable freshness, gap, projection rebuild, unavailable high-water, contradictory duplicate or unverified identity-source revision returns Stale/Unavailable/Ambiguous as applicable. A cache hit cannot refresh its own evidence age. Multi-source reads require a bounded, accepted consistency/freshness policy; future/mismatched evidence fails closed.

For historical resolution, select the unique interval containing ActionAt and return its binding version, effective interval and source position. With recorded version/actor evidence, require an exact match. Example: v1 actor X `[t0,t1)` and v2 actor Y `[t1,...)` resolves an action before t1 to X and one at t1 to Y. Replays, restores and later login or Party remapping must reproduce that answer. Check current actor authority independently before a new command; historical success never permits a revoked actor to act now.

If the SDK lacks a public, authorized authoritative-checkpoint read suitable for this handler, implement that reusable seam in Hexalith.EventStore first. Existing actor stream-range/high-water methods are source primitives, not an accepted public identity contract. Do not call Dapr directly from Parties or treat pending EventStore verified-read specifications as delivered capability.

## P4 — Integrate consumers without changing their ownership

- **Agents:** Replace the ambient-tenant `PartiesAgentPartyDirectory` calls and unchecked success mapping with exact tenant/ID evidence. Require freshness, classification/liveness and binding where applicable. Provision through the accepted service-only command, then obtain authoritative outcome/evidence before Agent creation. Keep the linked Party immutable.
- **Approvers:** Extend the result carried by `ProjectedApproverPolicyResolver` to retain each Party/actor/binding-version/validity/position tuple. Validate predefined Parties at configuration; resolve Facilitator and tenant-role candidates against current Conversation membership/read access and Parties evidence at every AD-8 boundary. Exclude caller, last editor and regeneration requester by stable actor identity, including aliases. Non-human candidates contribute no Approver; missing/stale/ambiguous bindings fail the action closed. Unavailability must not become an authoritative empty roster or terminalize a proposal.
- **Human ingress:** Match every Party-bearing human command to its authenticated actor ID and binding version; reject/audit mismatches. Persist the observed tuple with the action. Party-free Administrator and Platform paths preserve their current role/global-administrators evidence without an invented Party. Caller stays wire-deserializable but is rejected before mutation and removed from the selectable policy UI.
- **Conversations:** Membership uses exact provisioned Party ID, plus AI type only under A. `ParticipantType.AiAgent` remains the Conversations participant classification under B; it does not require inventing a Parties AI enum value. Only Member is granted through this service operation.

These are full-story consumer tasks after the register's applicable gates. Pure contract work does not authorize calls to Uncommitted/Committed seams.

## Implementation map

| Owning area | Concrete change |
| --- | --- |
| Parties `Contracts/Commands`, `Events`, `Models`, `State`, `ValueObjects` | Add provision/binding commands and events, closed safe evidence/results, immutable provisioning marker, binding versions and intervals; one C# type per file. Keep PartyType unchanged for Branch B. |
| Parties `Domain/PartyAggregate.cs`, `PartyDomainProcessor.cs`, `Validation` | Conditional create/retry decision, envelope identity binding, reserved-identity bypass denial, pure binding transitions and rejection-only-state handling. |
| Parties `Queries`, `Projections`, `Extensions/PartiesServiceCollectionExtensions.cs` | Authoritative identity/history handlers and checkpoint-aware evidence; SDK read models; no cached detail as authority. |
| Parties `Client/Abstractions`, `HttpPartiesQueryClient.cs`, `HttpPartiesCommandClient.cs` | Explicit tenant-scoped provision/current/history APIs; content-free results; concurrent tenant isolation. |
| Parties `UI/IdentityBinding` | Route mutations to durable owner once available; replace in-memory authority and rollback-as-history behavior. No new UI design is required for the contract slice. |
| EventStore SDK / Platform identity | Supply only missing shared checkpoint/authorization support and the accepted stable actor authority, before live Parties qualification. |

## Required acceptance vectors

Each row is work to implement and run, not a claimed result. Contract/domain tests can use deterministic values; Level 4 requires live persistence, service authorization and owner adapters.

| ID | Exercise | Required persisted/result evidence |
| --- | --- | --- |
| P-01 | First provision; repeated, concurrent and lost-ack exact retry | One tenant Party creation/marker, one immutable result revision, no second Agent or Party; same ID after restart. |
| P-02 | Changed Party/Agent/type/label/contract intent, same logical key; occupied unmarked ID | Typed conflict; original state/identity unchanged; alternate generic/composite create cannot bypass it. |
| P-03 | Rejection-only replay, then valid provision | Rejection history is not a successful creation marker; valid authorized create remains possible under the defined stream precondition. |
| P-04 | Tenant role, wrong service, forged tenant/ID, concurrent tenants using one client | Denied before target lookup/effect; no foreign result/count/name/log/accessibility disclosure; persisted foreign streams unchanged. |
| P-05 | Wrong returned ID/tenant; same-type unrelated Organization; attempted type change | Exact identity mismatch blocks; Organization classification never substitutes for immutable ID. |
| P-06 | Missing/inactive/restricted/erased/unknown-type Party; null/gapped/stale/future evidence; source outage | Typed fail-closed result; exact provision retry never repairs liveness or returns a fresh usable verdict without evidence. |
| P-07 | Current human with binding; non-human; missing/ambiguous/mismatched actor | Only matching active human passes; no PII serialization/logging; non-human cannot configure or resolve as an Approver. |
| P-08 | Bind/rebind at T, role/tenant/login rotation, action before/at T | Exact historical actor/version/interval reproduced across replay, restart and restore; stable actor survives role/tenant changes. |
| P-09 | History gap/overlap, conflicting revision, stale authority, cross-tenant historical read | Safe distinct typed failure; no fallback to current or fabricated historical actor. |
| P-10 | Approver aliases, caller/editor/regenerator exclusion; current read revocation | Stable-actor exclusions hold; unavailable evidence does not become NoEligibleApprover or proposal abandonment. |

Extend existing Parties Contracts, Server, Client, Projections and IntegrationTests projects, especially `PartyAggregateCreateTests`; add a proposed `eng/verify-ext-parties-1.ps1` owner runner covering P-01–P-10 and both active/historical identity. Its exact executable invocation and target must be supplied and accepted by the maintainer. That path is a deliverable to create, not today's register command.
