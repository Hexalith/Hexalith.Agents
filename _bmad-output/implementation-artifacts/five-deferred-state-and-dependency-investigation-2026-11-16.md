# Five deferred state and dependency items: investigation

Planning evidence only. No production changes or dependency qualification have been made. The requested five-item batch supplies the scope override; this is freeform maintenance, not a new epic story. The initial Agents working tree was clean on main at `f90e43f2e048f3b9ef77e7445eaa51544b76536d`. No Agents-owned AppHost exists; any necessary Platform lifecycle work must use the Aspire skill and obey owner execution restrictions.

## Selected ledger entries

1. Cross-instance browser pending-command read/modify/write coordination, appended by the prior provider-recovery review.
2. Revision-10 spool head/archive digest compatibility after reservation fields were added, from `spec-5-4-owner-prerequisites.md`.
3. EventStore gitlink versus released package floor reconciliation, from the second live EventStore setup spec.
4. Historical necessity of the Builds, Commons, Conversations, FrontComposer and PolymorphicSerializations gitlink changes, from that same setup spec.
5. Default Platform source regression graph NU1109 failures, from `spec-5-4-complete-conversations-internal-catalog.md`.

All other ledger records, including live host composition, replay authority, provisioning and workflow owner decisions, remain outside scope. These five questions may have different outcomes: a counterfactual proving an old pin sufficient settles a necessity question without justifying a rollback of today's unrelated changes. A restore failure alone settles neither necessity nor compatibility.

## Browser concurrency

The store semaphore is per instance; browser read and write are separate awaited interop calls. Preserve current `_commandScopes`, authentication generation, subject/tenant key, corruption validation and ambiguous-write retention. Cross-instance coordination must operate across distinct IJSRuntime instances addressing the same browser storage scope. A process-global semaphore keyed by a scoped IJSRuntime does not establish this property.

A browser-scoped conditional mutation can compare the exact read representation and update under browser-side coordination. A conflict must return control to C# for a new read, validation, merge and authentication guard. Bound contention retries and retain unresolved state on failure. Keep null/absent storage distinct from corrupt values. No existing Fluent component supplies this nonvisual storage primitive. Verify actual module behavior, not only a permissive interop mock.

## Historical spool representation

Platform selected revision: `17b1f688b332020fafbe1da07a5c04a41886c7bd`.

Historical pre-reservation snapshot: `8a56bd57837784806aae989159fbcbc71fbd41d4`.

Reservation-field addition: `bb6a8c011640844b65b8603f8a5f9256709c6239`.

Historical source path: `src/Hexalith.Platform.Custody/SecuritySpoolSnapshot.cs`. Its SHA-256 is `dc707e24caae265c9e2d545487cfc29ea8648e184d6ca79e2e95cf63e6ed41e7`.

A disposable no-package .NET 11 probe compiled the exact old/current snapshot sources with distinct namespaces and an empty record list. For epoch `installed-epoch-1`, revision zero and default fields, historical serialization hashes to `C63A578DF54A5C6F8918B9093660EE01CB6F0648341F8EFD4F4F158AF21F4137`; current serialization and historical-to-current round-trip hash to `95DB5C20B0030E256494E8DA83850A96BAE89236A0C6E4089F9A1338D3C412B0`. This probe confirms a source-level break; it is not a retained production inventory or live qualification.

`ReplicatedSecurityObservationSpool.StateDigest` and archive validation reserialize typed snapshots. EventStore `RecoverableAnchoredState<T>` independently serializes/deserializes pending-transition state. Therefore a local alternate digest check is insufficient. Global default omission would instead break already-written current default states.

Preserve a legacy/current layout marker across typed round trips and record copies. Both absent reservation members identify legacy; both present identify current. Reject partial, duplicate or malformed reservation layouts. New snapshots use current encoding. A real nondefault reservation transition promotes legacy encoding and retains that promotion. Tests must cover head, archived page and pending-transition recovery, current-default preservation, mixed chains after rollover, reservation crashes and integrity negatives. Keep every public/internal C# type in its own file.

## EventStore current source/package reconciliation

Current gitlink: `eecc0e4da70a0d64aaeff4fb75cdc06f94eb7563`.

Published selection: EventStore `5.1.1`; tag commit `f792bba6c0564d3c36dda6ad1c2fc55b0b0a6331`.

The gitlink is twelve commits ahead. Inspection found no changes between tag and gitlink under product DomainService/Gateway/Server/Contracts source directories; changes are test-oriented. Verify the complete consumed product graph, imports and transitive dependency selections rather than extending this preliminary observation to the whole repository. Preserve current pins and package authority. Retain source/package integration results and version-floor evaluation separately from the full Server source-mapping blocker recorded by the prior batch.

## Historical five-pin experiment

Reviewed Agents snapshot: `13857fd86e9fda7e2ba0f4017aadb042750e4284`.

Previous Agents baseline: `599208dd40efadef728363c227a0f75ebd888337`.

Keep EventStore `4bc61d9a60fae13a65f23963c1cb731065222f2a` fixed in both historical lanes. Vary only the five historical gitlinks:

| Owner | Previous | Reviewed |
| --- | --- | --- |
| Builds | `aee01323579adeda952bf41b2f3e0e61785075c6` | `410bd595f9e1c0f686e1edde7699517c47c8f126` |
| Commons | `19d7d4d6b21160557b7449f55a0ad0f55e6d7dc6` | `9f4809d37095e64e3732abc3e766535f9e836563` |
| Conversations | `589cdd711871d5a632b97df0b04375d1cf6e0864` | `c610fbb8c5491fb0d7987c2f8294199887f4a44c` |
| FrontComposer | `6e064785e765102fa1a90039f06e10e0b252fdec` | `50490798059df61d8ef9617fe8263b9f29ee670f` |
| PolymorphicSerializations | `8aeed1d27c9a050bc4bec6d89051aa00de306a69` | `7e95556c919ff1142864e54f306a6aa0101b2bf5` |

SDK `10.0.401` is installed. Export archives beneath the wrapper's TMPDIR and build the historical integration project with its original SDK/catalog; materialize only explicitly needed dependencies, without initializing nested submodules. Do not mutate today's gitlinks to run this experiment.

## Platform exact source tuple

Use the selected Platform revision above and its declared owners, whose objects are locally available:

| Owner | Exact revision |
| --- | --- |
| Builds | `e2bbbd0893f586e4d447ef8358ff747ea3d34435` |
| EventStore | `df976e0051b9e2eaa9d3592b3054a0317d83d0cb` |
| Tenants | `2ec938397c5b330cf1e694e8cc442cb4ad022f2b` |
| Memories | `f20e8b8145a6c00310a2aed3d0c8758844b65fdc` |
| Parties | `a494093da3e2656c49e539392284cd1b10f799b3` |

Existing `Directory.Build.targets` propagates source/SDK context; `eng/SourceRestore.props` addresses static-graph restore. Use the pinned Builds `prepare-build` mapping producer; retain every producer-selected root. Umbrella owner pins differ and cannot substitute for this experiment. The package-only `eng/run_tuple_ci.py` SDK4.2 runner is supporting evidence, not proof of the default source graph.

Build `tests/Hexalith.Platform.Composition.Tests/Hexalith.Platform.Composition.Tests.csproj` in Debug and run native classes `Hexalith.Platform.Composition.Tests.DomainEnrollmentBindingTests` and `Hexalith.Platform.Composition.Tests.PartiesAdoptionTests`. The original selection had eight cases, including three failed cases:

- `StaticValidityRequiresExplicitProjectAndScopedConfigurationBeforeComposition`
- `SignedPartiesRunUsesTheRunnerJwtSettingsAsync`
- `CompositionWiresSingularPartyGatewayAndDeclarativeTenantSubscription`

Every original case must execute; retain additional current cases without silently changing the selection. The original 38 Conversations catalogue cases additionally require `ConversationsCatalogSourceVerification=true` and `HEXALITH_CONVERSATIONS_CATALOG_INPUTS` JSON containing `producerMappingPath`, `producerMappingHash`, `mappingPath`, `mappingHash` and `evidenceRoot`. Its combined mapping preserves all producer-selected roots except the explicit Platform overlay.

## Route facts

No unresolved user-visible intent choice was identified. The user explicitly authorized pushing main; that authorization persists. Changes span browser coordination, shared serialization and owner verification across Agents and Platform, so the footprint requires the full dispatch route. No deployment, package publication or mutation of retained production data is authorized or needed. The invoked workflow's spec checkpoint must be completed before implementation. Cross-repository edits must read applicable owner instructions and pass owner checks before content/gitlink pushes.
