# Terminal setup status verification — 2026-10-25

Approved scope: DW-7, DW-12, DW-24, DW-25 (the carried pointer owned by DW-12), and DW-26. Original ledger entries and external owner commitments remain intact.

## Revisions and evidence boundary

- Agents baseline: `15fc5834d38fa1d769e3be5a77e989bf4c8dc132`.
- EventStore original checkout: `d41a3c55a684491d02f61c5ea98e0f74332b5d4d`.
- EventStore implementation baseline, after the parent fetched and fast-forwarded 14 inherited commits: `dee8497458bfc92e094ac66fb977b2eddabd6c93`. Those inherited updates are separate from this delivery; the changed issuer and existing client/controller files were unchanged by reconciliation.
- SDK: `10.0.402`; test runtime reports `.NET 10.0.13`.
- This report describes local uncommitted implementation verification. The workflow parent owns independent review, final canonical commit identifiers and main pushes. No deployment or production qualification occurred; no nested submodules were initialized.

## Delivered behavior and prerequisites

Configured setup composition resolves `EventStoreAgentCommandStatusReader`. The adapter creates the supported SDK client over the configured named HTTP/Dapr transport and delegates only `eventstore:command-status` with one exact domain to the host assertion source. Agent reads pass the authorized tenant, `agent` domain and validated receipt message/correlation. Provider operations also use scoped workload status, with exact platform/tenant domain selection and receipt correlation when present. Neither selected operation class falls back to the public user-status route. Existing host reader registrations survive both configured and deferred composition.

Only a matching tenant/domain/message/correlation and canonical `CommandStatus.Rejected` with a rejection event, no infrastructure failure and no automatic retry establishes a domain rejection. Missing, malformed, foreign, unsupported, infrastructure-only and transport/credential failures supply no rejection or completion evidence. Caller cancellation propagates, including cancellation after a non-cooperative read.

Fresh `Rejected` and `Blocked` operation results use the existing English/French rejection message and release the attempt, restore its exact instructions draft and permit a newly identified attempt. Ambiguous outcomes preserve the existing payload and identities. A retry cannot replace a prior accepted receipt/target with a rejection, no-op or inconsistent receipt. Loading, missing, unavailable, unknown and deferred setup surfaces refuse new attempts, including stale submit events.

The public Client **3.127.0** has workload status, constructor/options and three-argument assertion delegation, but **does not have the exact-domain overload** present in current owner source. NuGet's latest published Client remained 3.127.0 during verification (package metadata: built 2026-10-23, repository `e1d8b17160202761ab5c9606cafbcf4d1a4b92c3`). The standard DomainService **3.127.0 issuer also does not grant CommandStatus**. The initial Release build exposed CS1501 at `CommandStatusWorkloadAssertionSource.cs:23`; API inspection confirmed the mismatch.

The supported fix selects the exact-domain delegation at compile time only in source mode. Source mode requires the owner overload and the declared-domain issuer change from this delivery. Package mode refuses assertions before HTTP, even when a legacy source would supply credentials; the existing host-reader override remains available. Default package status transport requires a future compatible published Client/DomainService and enabling the matching capability. No reflection, dynamic dispatch, unscoped delegation, user-status fallback or local credential creation bridges the missing API. Dapr routing identity never substitutes for the workload grant.

## Verification commands

Commands below run from Agents, except owner commands from `references/Hexalith.EventStore`. Each test project is built individually and its assembly uses native xUnit selections. Native XML and the substantial gate log use deterministic gzip (empty filename, `mtime=0`); readable runner/build summaries, compressed and uncompressed XML hashes are retained under [tests/terminal-setup](tests/terminal-setup/).

```bash
dotnet build test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj -c Debug --artifacts-path /tmp/agents-terminal-source -p:UseHexalithProjectReferences=true -m:1 --nologo -v:q
dotnet /tmp/agents-terminal-source/bin/Hexalith.Agents.Server.Tests/debug/Hexalith.Agents.Server.Tests.dll -class Hexalith.Agents.Server.Tests.EventStoreAgentCommandStatusReaderTests -class Hexalith.Agents.Server.Tests.EventStoreAgentAdministrationOperationsTests -class Hexalith.Agents.Server.Tests.AgentSetupCompositionTests -class Hexalith.Agents.Server.Tests.SetupWriteCancellationAdmissionTests -class Hexalith.Agents.Server.Tests.EventStoreProviderCatalogOperationsTests -result-xml /tmp/terminal-server-focused.xml
dotnet build test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj -c Debug -m:1 --no-restore --nologo -v:q
dotnet test/Hexalith.Agents.UI.Tests/bin/Debug/net10.0/Hexalith.Agents.UI.Tests.dll -class Hexalith.Agents.UI.Tests.AgentConfigurationTests -class Hexalith.Agents.UI.Tests.AgentsClientSetupGatewayTests -result-xml /tmp/terminal-ui-focused.xml

dotnet build tests/Hexalith.EventStore.DomainService.Tests/Hexalith.EventStore.DomainService.Tests.csproj -c Debug -m:1 --nologo -v:q
dotnet tests/Hexalith.EventStore.DomainService.Tests/bin/Debug/net10.0/Hexalith.EventStore.DomainService.Tests.dll -class Hexalith.EventStore.DomainService.Tests.EventStoreGatewayWorkloadTransportTests -result-xml /tmp/terminal-owner-domain.xml
dotnet build tests/Hexalith.EventStore.Client.Tests/Hexalith.EventStore.Client.Tests.csproj -c Debug -m:1 --nologo -v:q
dotnet tests/Hexalith.EventStore.Client.Tests/bin/Debug/net10.0/Hexalith.EventStore.Client.Tests.dll -class Hexalith.EventStore.Client.Tests.Gateway.EventStoreGatewayWorkloadClientTests -class Hexalith.EventStore.Client.Tests.Gateway.EventStoreGatewayClientTests -result-xml /tmp/terminal-owner-client.xml
dotnet build tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj -c Debug -m:1 --nologo -v:q
dotnet tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests.dll -class Hexalith.EventStore.Server.Tests.Controllers.WorkloadGatewayControllerTests -class Hexalith.EventStore.Server.Tests.Commands.CommandStatusControllerTests -result-xml /tmp/terminal-owner-server.xml
```

Root focused source builds passed with zero warnings/errors; the final native server selection passed **299** cases and the UI selection passed **104**, with zero errors/failures/skips/not-run.

All owner builds above passed with zero warnings/errors. Owner native selections passed 11, 78 and 37 cases respectively, with zero errors, failures, skips or not-run cases. `dotnet-inspect` confirmed the published 3.127.0 workload-status API and, on detailed inspection, the absence of its exact-domain assertion overload; its installed CLI rejects the documented `--oneline` option, and the same query without that presentation option succeeded. A preliminary `dotnet test ... -- --help` command aborted inside the SDK help-message transport; all evidence uses direct native assemblies instead.

## Maintained Story 5.2 gate

`pwsh -NoProfile -File eng/verify-story-5.2.ps1` passed again on the final compile-time capability change, exit **0**, including the
warning-free solution source build, native focused gates, full project regressions
and composition checks. Full project totals: Contracts **1,997**, Client **7**,
domain **1,017**, Server **1,605**, UI **1,184** — all zero errors, failures,
skips and not-run cases. The anticipated Builds/workflow drift blocker did not
reproduce. The unchanged gate and full stdout are retained as
[story-5.2.log.gz](tests/terminal-setup/story-5.2.log.gz).

The matrix audit reads the durable native XML and requires nonzero passing tests
for every row. Exact rejection has 15 selected passing cases; uncertainty 20;
isolation 13; unbound 8; accepted 9. The exact names/results are retained in
[matrix-coverage.json](tests/terminal-setup/matrix-coverage.json).

## Release/package validation

Both package builds passed with zero warnings/errors. The focused native package
Server selection passed **281** cases and UI passed **104**, all zero errors,
failures, skips and not-run. The package reader suite includes two explicit
`PublishedSdkWithoutDomainDelegationRefusesStatusBeforeTransport` cases; the
source-only HTTP delegation matrix is intentionally absent in this lane. Package
composition proves refusal before HTTP and preserved host-reader overrides.
Effective package properties/project references are retained in
[package-capabilities.json](tests/terminal-setup/package-capabilities.json).

```bash
dotnet build test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj -c Release --artifacts-path /tmp/agents-terminal-package -p:UseHexalithProjectReferences=false -m:1 --nologo -v:q
dotnet /tmp/agents-terminal-package/bin/Hexalith.Agents.Server.Tests/release/Hexalith.Agents.Server.Tests.dll -class Hexalith.Agents.Server.Tests.EventStoreAgentCommandStatusReaderTests -class Hexalith.Agents.Server.Tests.EventStoreAgentAdministrationOperationsTests -class Hexalith.Agents.Server.Tests.AgentSetupCompositionTests -class Hexalith.Agents.Server.Tests.SetupWriteCancellationAdmissionTests -class Hexalith.Agents.Server.Tests.EventStoreProviderCatalogOperationsTests -result-xml /tmp/terminal-package-server.xml
dotnet build test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj -c Release --artifacts-path /tmp/agents-terminal-package -p:UseHexalithProjectReferences=false -m:1 --nologo -v:q
dotnet /tmp/agents-terminal-package/bin/Hexalith.Agents.UI.Tests/release/Hexalith.Agents.UI.Tests.dll -class Hexalith.Agents.UI.Tests.AgentConfigurationTests -class Hexalith.Agents.UI.Tests.AgentsClientSetupGatewayTests -result-xml /tmp/terminal-package-ui.xml
```

An intervening default `--no-restore` source build encountered stale dependency
assets (`EventIdentityValidator.cs:1`, CS0234, missing `Hexalith.Commons`). The
fresh isolated source build above, with explicit global source mode and restore,
passed and reran all 299 focused server cases. No source or gate was changed to
hide that mixed-assets failure.

## Frozen matrix coverage

| Matrix row | Executed coverage |
| --- | --- |
| Exact rejection | `ExactDomainRejectionUsesWorkloadRouteAndExplicitDomainDelegation`, `A_domain_rejection_is_reported_as_rejected_rather_than_retryable_unverifiable`, `FreshRejectionReleasesTheAttemptAndTheNextSubmitUsesNewIdentities`, terminal draft restoration and gateway typed mappings |
| Uncertainty | `ForeignUnknownAndInfrastructureEvidenceCannotClaimRejection`, `CredentialAndTransportFailuresRemainUnverifiable`, existing exact-retry and UnableToVerify UI tests |
| Isolation | Real issuer `StatusGrantsUseTheRealIssuerAndOneDeclaredDomain`, SDK workload client tests, workload controller tenant/domain refusal tests, reader mismatches and provider authorization tests |
| Unbound | `MissingAuthoritativeSetupRefusesStaleSubmitEventsWithoutStartingAnAttempt` including the real deferred gateway, composition deferred/override cases |
| Accepted | `Retrying_an_accepted_pending_attempt_never_erases_its_acceptance`, including Rejected and inconsistent Submitted receipt; existing incomplete refresh and exact retry tests |

## Remaining qualification

These are unit and in-process transport/controller proofs with substituted HTTP or backend state as stated by their tests. The real issuer tests inspect actual issued claims; they do not exercise deployed workload trust configuration, an external Dapr state store or a live receiving gateway. Existing production qualification, DW-6 advisory success, DW-19 owner evidence, DW-21 and other owner commitments remain separate. The independent provider migration service is unchanged by this setup-status delivery.


## Independent review and final verification

All three review layers ran. Their twelve findings are individually triaged in the implementation spec. Patches make scoped evidence conservative for legacy readers, resolve credential sources in the operation scope, preserve cancellation, refresh stale authority after rejection without replacing drafts, and strengthen signed HTTP, provider-domain, blank-correlation and zero-allocation coverage. One compatible-SDK publication/pin/capability follow-up remains explicitly deferred; additional diagnostic machinery was rejected as unnecessary for this change.

Before final verification, EventStore advanced over five further upstream commits to `12784fde729a4756564ae388fb6c2217c7323446`. SHA-256 comparison confirmed that all three implementation files were unchanged by that fast-forward. The owner implementation was subsequently committed and pushed on main as `4e91e552be41bfba984d67d68335e6b1709917b5`.

The parent reran the maintained Story 5.2 gate on the reconciled and patched source: **5,821** full owning tests pass (Contracts 1,997; Client 7; domain 1,017; Server 1,612; UI 1,188), with zero failures, errors, skips or unrun tests. Final owner authentication/controller selections pass **81** cases; final Release/package UI selections pass **108**. Both final builds are warning-free. The implementation agent's targeted review suites also pass: source Server **205**, package Server **187**, UI **80**. No broader deployment or production qualification is claimed.

[post-review.json](tests/terminal-setup/post-review.json) records final artifact hashes, native summaries, code-file SHA-256 values, canonical owner revisions and commitlint evidence. It retains deterministic compressed XML for those runs and the final gate log. Earlier evidence above remains the historical pre-review record. Code changes were audited against the approved matrix; all five original tasks and all acceptance criteria remain satisfied within the stated source/package capability boundaries.
