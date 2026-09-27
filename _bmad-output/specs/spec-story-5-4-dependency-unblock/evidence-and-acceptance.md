# Evidence and acceptance handoff

Inspected 2026-09-27. This is static source investigation and a proposed implementation contract. No runtime compatibility tests, builds, deployment, production approval or external messages occurred. Owner issue reads used `gh issue view --json number,state,title,body,comments,url`; all four were OPEN with empty comments. No target/date/command or branch acceptance was inferred.

## Reproducible source baseline

| Checkout | Inspected HEAD | Scope |
| --- | --- | --- |
| Agents | `c1a8a2f36cb4ed8d808eff3137dcbc47272beea0` | Clean main before this documentation work. |
| Parties reference | `a466a5cb7d13bd50ce01a2a91f8fb9a890f00aea` | Current consumer source reference, inspected read-only. |
| Parties owner, `../parties` | `14d249fde316b0002aec84351d7a7cdf953d1d30` | Clean main; key identity/create files compared with the reference. |
| Conversations reference | `30fffc2cd427e837c719b87cb33475e34a6fc8de` | Current consumer source reference, inspected read-only. |
| Conversations owner, `../conversations` | `aefe4003cc94f49021e942adb9cbb8ca9ccf86cd` | Ahead of its tracked remote and has unrelated untracked acceptance work; preserved. |
| EventStore reference | `410b8545576c70a56680e9129d4e3e38d769d8bc` | Source primitives inspected; pending specification work is not delivered compatibility. |
| Tenants reference | `49ca5b46c6cab066b5a939a5551a27aa8db1e541` | Projection continuity source inspected. |
| Platform owner, `../platform` | `98db8cf9fd6058c17e36d45b1935ec5301a68e00` | Pre-existing staged/unstaged/untracked files, including apphost and Dapr resources. Working-tree observations are not an immutable target. |

These hashes locate inspected source only. They deliberately do not populate TargetVersionOrCommit. No fetch, submodule update or nested initialization was performed.

## Findings tied to implementation work

| Evidence | Observation and resulting owner task |
| --- | --- |
| [PartyType](../../../references/Hexalith.Parties/src/Hexalith.Parties.Contracts/ValueObjects/PartyType.cs), [Agents adapter](../../../src/Hexalith.Agents.Server/Ports/PartiesAgentPartyDirectory.cs) | Unknown/Person/Organization; Agents provisions Organization using `agent-{AgentId}`. Tenant is ambient, returned ID is unchecked, absent freshness is accepted, and create result is ignored. This supports the user's Branch B implementation choice and P1/P2/P4; code does not establish formal acceptance. |
| [PartyAggregate](../../../references/Hexalith.Parties/src/Hexalith.Parties/Domain/PartyAggregate.cs), [create tests](../../../references/Hexalith.Parties/tests/Hexalith.Parties.Server.Tests/Aggregates/PartyAggregateCreateTests.cs) | Existing-state CreateParty returns NoOp before type/detail checks; tests codify existing and rejection-replay no-op. P2 adds immutable intent comparison and real-created-state distinction without treating non-null state as successful provisioning. |
| [PartyState](../../../references/Hexalith.Parties/src/Hexalith.Parties.Contracts/State/PartyState.cs) | Apply(PartyCreated) sets CreatedAt from UtcNow. P3 uses persisted authoritative instants/revisions, not this field, for history. |
| [PartyDetail](../../../references/Hexalith.Parties/src/Hexalith.Parties.Contracts/Models/PartyDetail.cs), [freshness](../../../references/Hexalith.Parties/src/Hexalith.Parties.Contracts/Models/ProjectionFreshnessMetadata.cs), [HTTP query client](../../../references/Hexalith.Parties/src/Hexalith.Parties.Client/HttpPartiesQueryClient.cs) | Detail includes PII and optional freshness status, but no stable actor/version/interval or authoritative position. Client takes tenant from options. P1/P3 supply the narrow per-call contract and evidence. |
| [identity binding record](../../../references/Hexalith.Parties/src/Hexalith.Parties.UI/IdentityBinding/IdentityBindingRecord.cs), [registration](../../../references/Hexalith.Parties/src/Hexalith.Parties.UI/IdentityBinding/IdentityBindingServiceCollectionExtensions.cs), [service](../../../references/Hexalith.Parties/src/Hexalith.Parties.UI/IdentityBinding/IdentityBindingProvisioningService.cs) | There is existing issuer/subject/Party linking, version and audit work. Registration uses in-memory store/IdP client; mutations can roll back and no public authoritative Party-to-stable-actor historical seam exists. Reuse concepts, not this storage as authority. |
| [Conversations client](../../../references/Hexalith.Conversations/src/Hexalith.Conversations.Client/IConversationClient.cs), [detail result](../../../references/Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Queries/ConversationDetailResult.cs), [AddParticipant handler](../../../references/Hexalith.Conversations/src/Hexalith.Conversations.Server/CommandHandlers/AddParticipantCommandHandler.cs) | Get/append and internal participant-validation/idempotency boundaries exist; public client lacks AddParticipant, removal, MessageId lookup and six-seam completion. Hidden collapses access outcomes. Implement C1–C4. |
| [Tenants handler](../../../references/Hexalith.Tenants/src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs), [Agents composition](../../../src/Hexalith.Agents.Server/Composition/AgentDomainHostComposition.cs) | Handler rejects lower sequence but accepts jumps/equal sequence; Agents still has deferred authority readers. Full 5.4 must prove continuity and current revocation, not rely on LastEvent as proof. |
| [EventStore actor interface](../../../references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/IAggregateActor.cs), [trusted extension policy](../../../references/Hexalith.EventStore/src/Hexalith.EventStore/Authorization/ITrustedCommandExtensionPolicy.cs), [idempotency adapter](../../../references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IIdempotencyIntentAdapter.cs) | Read-range/high-water, fenced operations and extension/admission hooks exist. They need exact contractual mapping and target ACL proof; no replay registrar or spool implementation was found in inspected Agents/platform source. E1/H1 specify the missing work. |
| [EventStore protection core](../../../references/Hexalith.EventStore/src/Hexalith.EventStore.PayloadProtection/PayloadProtectionCore.cs), [default registration](../../../references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Configuration/ServiceCollectionExtensions.cs) | New internal crypto core exists; DI still defaults to NoOpEventPayloadProtectionService. H2/E1 must prove production binding and full protection-owner behavior. This refines the older recheck's source picture without claiming verification. |
| [Platform host](../../../../platform/apphost.cs), [historical host contract](../../../../platform/docs/ext-host-1-agents-composition.md), [verifier](../../../../platform/eng/verify-agents-host.sh) | Host says Agents wiring remains 5.6 work; verifier only builds scaffold and explicitly defers live composition. Existing Works-specific preview changes do not implement Agents host/spool/custody. Replace the historical partial contract with H1–H4 under owner acceptance. |

The inspected owner/reference PartyAggregate, PartyDetail and IdentityBindingRecord files were byte-equal; Conversations IConversationClient and ConversationDetailResult were also byte-equal. Observations therefore cover the relevant newer owner checkouts without claiming the repositories as a whole are identical.

## Decisions requiring acceptance

| Decision | Who accepts | Concrete proposal / unresolved input |
| --- | --- | --- |
| D1: Party identity branch | Product + Parties Maintainer | The user selected B as the implementation direction, preserving the immutable Organization ID for V1. The Parties companion retains A for comparison. Formal acceptance of the complete B launch contract is still required from both named owners. |
| D2: final Party contract | Parties Maintainer + Platform identity maintainer; Product/Governance where identity/retention policy is affected | Proposed P1 operations, safe outcomes, tenant scope, authoritative checkpoint/freshness policy, stable actor authority/continuity, interval/version model, historical retention, new-ID derivation and legacy same-ID provenance procedure. Reject uncertain legacy identities until accepted. |
| D3: Conversations wire contract | Conversations Maintainer | All six seams; final method/failure names, count or source-feed denominator/window semantics, approved-deletion source and acknowledgement contract. |
| D4: custody ownership and bounds | Platform Maintainer, with security/trust authorities | Repository still TBD; accept S1–S4, independent approval issuer and actor/role authorities. Supply numeric L/S/O/H/R and trust-profile revisions; validate retention inequality. No example numbers stand in for acceptance. |
| D5: host durability and deployment | Platform Maintainer + EventStore/protection maintainers for their contracts | H1–H4, replicated spool topology/durability/restore behavior, restricted credential lifecycle, complete composition and exact target set. No spool vendor, replica number or recovery commitment is inferred. |
| D6: applicable Product dispositions | Owners named in adopted spine/register | Preserve each open governance decision and its conditional scope. Implement blocking outcomes and mechanical race protections; never select deletion/hold/export/privacy policy through implementation. |
| D7: all record commitments | Each accountable record owner | Accept all nine fields, consumers and Levels 4/5, including full target, integration date and executable command. Any material contract/target change invalidates prior acceptance. |

These are a handoff for later acceptance, not permission questions and not approval evidence. No external message has been prepared for posting or sent.

## Per-owner acceptance packet

| Record / existing request | Required implementation packet | Current accepted values |
| --- | --- | --- |
| EXT-PARTIES-1 / [Parties #54](https://github.com/Hexalith/Hexalith.Parties/issues/54) | Formal D1/D2 acceptance recorded by the right owners; P1–P4 code/API schema and P-01–P-10 fixtures; complete current/historical live evidence runner. | User-selected B path; formal owner acceptance and target/date/command TBD; Uncommitted. |
| EXT-CONV-AI-1 / [Conversations #4](https://github.com/Hexalith/Hexalith.Conversations/issues/4) | C1–C4 with all six seam contracts and test lanes, selected Parties branch compatibility and deletion-delivery recovery. | Target/date/command TBD; Uncommitted. |
| EXT-SECRETS-1 / [Platform #1](https://github.com/Hexalith/Hexalith.Platform/issues/1) | Selected repository, S1–S4, numeric profile, independent trust issuer, shared E1/H1 restrictions and v23 custody/compromise evidence. | Repository/target/date/command TBD; Uncommitted. |
| EXT-HOST-1 / [Platform #2](https://github.com/Hexalith/Hexalith.Platform/issues/2) | Complete H1–H4 manifest, production dependency targets, spool/replay/worker ACLs, FR-34 and migration/guard/protection-owner race/recovery evidence. | Target/date/command TBD; Uncommitted; scaffold target is historical only. |

Every packet must identify an exact package version or full immutable commit, accepted integration date, exact runnable command and working directory, installation/reset/seed procedure, authenticated role setup, failure injection, expected persisted outcomes, safe evidence location, full contract coverage and owner acceptance. No branch name, local checkout or proposed script filename suffices.

The proposed runner paths in the implementation companions do not exist as accepted commands today. First implement and review them, then obtain acceptance of the exact invocation/target. Owner compatibility execution at Committed is the sole exception that can establish Available; it must fail on missing components, fixture substitution, incompatible outcomes or skipped required lanes. Additional seams executed in that command must already be Available or explicitly covered by the same command's acceptance from their own owners.

## Entry and execution checklist

1. Keep `spec-5-4-...md` status draft and sprint status backlog now. The user's full-scope choice resolves the old narrowing question; it does not accept D1–D7.
2. Before ready-for-dev, inspect **each complete** record: owner/repository/artifact/immutable target/integration date/contract plus executable command/evidence level/consumers accepted, status Committed or Available, no required TBD. A source commit or issue without an accepted response cannot fill a field.
3. Before any consumer seam call, require Available and passing owner command evidence for the **exact** target being used. Otherwise fail closed with DependencyNotAvailable(record). Owner command failure blocks use even if the written status had been Available.
4. Retain Levels 4 and 5. Available is established by live Level 4 component evidence; Level 5 cross-system attainment is completed by the consuming launch gate/RQ-1, not required as a circular prerequisite to starting that same qualification.
5. Story completion still requires all original acceptance criteria: safe API/UI authority basis; replay exactness and changed-field audit; persisted spool recovery and no-command spool failure; cross-tenant non-disclosure; required local suites and live gates. A local Caller-policy change cannot finish 5.4.

## Validation of this planning deliverable

Validation is limited to document structure, local links, source/contract preservation, unchanged register commitment fields and sprint status, and the frozen Story 5.4 intent. The register now contains a dated Branch B implementation-direction note, with all four records still Uncommitted. No executable compatibility command was run and no Level 4/5 result is claimed. The spec memory records the coherence and preservation verdicts after the final checks.
