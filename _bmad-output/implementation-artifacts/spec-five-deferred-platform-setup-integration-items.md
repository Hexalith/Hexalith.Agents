---
title: 'Complete five deferred Platform setup integration items'
type: 'feature'
created: '2026-10-26'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context: []
baseline_commit: 50258a358d47edf58d667727a485dab6a3d7a131
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Agents setup has working local seams but lacks a runnable Platform UI composition and gateway registration. Existing HTTP tests substitute routing, command status and domain results, so they cannot establish persisted replay/projection behavior. Package builds also disable status credentials despite the selected SDK now supporting them.

**Approach:** Deliver one authenticated Platform setup path, resolving exactly five ledger entries: DW-1 (UI host), DW-21 (gateway registration), DW-19 (status reachability), the unnumbered production-like setup result replay/projection entry, and the unnumbered published exact-domain status capability follow-up. Verify the complete path and push reviewed changes on main in each owning repository.

## Boundaries & Constraints

**Always:** Keep executable hosts in Platform. Reuse FrontComposer, Fluent UI V5, public Agents operations, EventStore transport, durable replay admission and owner credentials. Bind proof to the exact authenticated caller, tenant, domain, payload and request. Preserve host overrides, cancellation and accepted-write uncertainty. Use the Builds catalog exclusively for package versions.

**Never:** Treat raw flags or a client proof string as authority; register replay inside `HasAdmittedProof`; bypass authorization; substitute a router or persisted backend in topology evidence; infer success from HTTP alone; alter DW-6's Completed policy; or promote broader dependency records from a setup-only run. Existing owner acceptance and execution restrictions remain binding. Prepare any changed qualification target/command for explicit owner acceptance before live execution.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected behavior | Failure handling |
| --- | --- | --- | --- |
| Accepted write | Verified caller and exact setup intent | One append; exact receipt; persisted projected version/lifecycle | Retain pending until proved |
| Replay | Same command identity and payload after restart | Original receipt/result; no second append | Conflicting payload refused |
| Rejection | Exact authenticated rejected command | Reachable terminal rejection; fresh retry identity | Missing/mismatched status remains unverifiable |
| Isolation | Foreign tenant/domain, missing proof or credentials | No unauthorized write or disclosure | Existing typed refusal |
| Unconfigured | Missing target or owner binding | Start no ungrounded mutation | Existing unavailable/dependency surface |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Platform/src/Hexalith.Platform.Composition/AgentsHostComposition.cs`: currently launches the UI library and bare EventStore host; fixture projects mask this.
- Platform `src/Hexalith.Platform.EventStoreHost/Program.cs` and its project: no Agents integration registration/reference.
- `src/Hexalith.Agents.EventStore/IAgentsTrustedCommandProofReader.cs`: verifier-owned exact-request admission contract; default absence refuses reserved extensions.
- `src/Hexalith.Agents.Client/AgentsClient.cs`: operation facade without HTTP transport. `src/Hexalith.Agents.Server/Api/AgentsOperationEndpoints.cs` supplies existing HTTP routes.
- `src/Hexalith.Agents.UI/Services/Gateways/AgentsUiServiceCollectionExtensions.cs`: reuse `AddAgentsUiSetup` and existing pages.
- Server and Server.Tests project files enable `HEXALITH_EVENTSTORE_DOMAIN_ASSERTIONS` only for source. Published Client/DomainService 3.128.0 support exact-domain status assertions.
- `test/Hexalith.Agents.Server.Tests/AgentsEventStoreGatewayIntegrationTests.cs`: retain controller/admission coverage; its substituted backend does not prove topology persistence.

## Tasks & Acceptance

**Execution:**
- [ ] Server and Server.Tests project files, `eng/verify-story-5.2.ps1`, `AgentSetupCompositionTests.cs` — enable package status only behind a verified compatible SDK floor; exercise actual issuer and composed transport with exact tenant/domain grants.
- [ ] Client `AgentsHttpClientServiceCollectionExtensions.cs`, `HttpAgentAdministrationOperations.cs`, `HttpProviderCatalogOperations.cs` and corresponding Client.Tests — add authenticated transport for existing setup operations; preserve public contracts and uncertainty.
- [ ] Platform `src/Hexalith.Platform.AgentsUiHost/` (new project, entry point and Blazor root), solution and `AgentsHostComposition.cs` — host existing Agents pages with FrontComposer, live client/auth configuration and `AddAgentsUiSetup`; replace library-as-executable fixtures.
- [ ] Platform EventStoreHost files above and new `AgentsTrustedCommandProofReader.cs` — register `AddAgentsEventStore` with the exact allow-listed app id and verifier-owned request admission; launch this host in the setup topology.
- [ ] Platform `tests/Hexalith.Platform.Composition.Tests/AgentsSetupTopologyTests.cs`, `eng/verify-agents-setup.sh` and qualification documentation — add real Dapr/backend setup, restart, rejection and replay evidence; prepare immutable owner acceptance inputs before restricted execution.
- [ ] `CONTRIBUTING.md`, root deferred ledger and dated verification report — remove stale 3.127 guidance and close only the five entries supported by passing evidence; preserve remaining owner qualifications.

**Acceptance Criteria:**
- Given a configured Platform setup host, when an authorized administrator submits and reloads setup, then actual gateway/domain execution persists the matching projected state.
- Given a completed or rejected command, when status is requested through source or compatible packages, then exact terminal evidence is reachable and foreign bindings refuse.
- Given a restart and exact replay, when the same intent is submitted, then the original identity/result survives and persisted state proves one append.
- Given missing required owner evidence, when qualification runs, then it refuses without fabricated closures or dependency promotion.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

Run affected test projects individually: Debug/source for development and Release/packages for consumer compatibility. Run maintained Story 5.2 gates and the accepted exact-target setup topology command, retaining native nonzero/no-skip results, full revisions and persisted-state assertions. Inspect authorization and replay independently. Before each push, fetch divergence, validate Conventional Commits with commitlint, commit owner changes first and update parent gitlinks separately. Initialize no nested submodules.
