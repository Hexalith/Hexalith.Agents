# Epic 5 Context: Live Governed Setup And Honest Readiness

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Enable Agent Administrators to configure `hexa` through live, authorized public operations and receive one authoritative tenant-scoped explanation of lifecycle, setup validity, Provider state, blockers, and callability. This epic replaces inferred or host-local readiness with durable, replayable evidence so later runtime work can depend on an honest platform boundary and fail-closed activation decision.

## Stories

- Story 5.1: Establish Build Package Boundary And Basic CI Gates
- Story 5.2: Configure hexa Through Live EventStore Operations
- Story 5.3: Govern Provider Models And Pricing Through Live Operations
- Story 5.4: Prove Tenant Party And Approver Readiness
- Story 5.5: Publish Authoritative Readiness And Provider-State Contracts
- Story 5.6: Compose Agents In The Platform-Owned Production-Like Host
- Story 5.7: Activate hexa Only When Setup Gates Pass

## Requirements & Constraints

- Authorized administrators must configure Agent identity references, display metadata, versioned instructions, lifecycle, response mode, approver policy, Provider/model selection, and safe Provider catalog state through stable API/client and UI operations. Accepted changes must be EventStore-persisted, replayable, auditable without sensitive prior/new values leaking, and visible through current versioned projections. Response-mode and selection changes affect future interactions only.
- Every command and query must enforce current tenant and Party authorization before mutation, lookup-dependent disclosure, secret resolution, or downstream side effects. Missing, stale, gapped, ambiguous, disabled, revoked, unavailable, regressed, unknown, or indeterminate dependency state fails closed. Cross-tenant responses, counts, diagnostics, timing text, accessible output, logs, and audit summaries must not reveal record existence or sensitive instructions.
- One current tenant-scoped Party identity and every configured approver basis must be resolvable from authoritative dependency evidence. Store stable Party references only, never Party PII; historical roles, JWT claims alone, or UI state cannot establish current authority.
- Provider catalog truth must include enabled/configured state, text-generation capability, positive context/output/timeout limits, secret reference, current versioned pricing, and a non-reusable monotonic capability version. Secret values and Provider SDK types never cross public, durable, telemetry, audit, browser, or accessible-name boundaries.
- Provider readiness accepts only `Ready/Callable/None`, `Degraded/Callable/NonBlockingOperationalWarning`, or `Blocked/Blocked/<defined blocker>`. The API and UI must consume the authoritative result without inferring callability. Lifecycle `active` is necessary but not sufficient; missing, blocked, stale, or insufficient setup evidence remains non-callable.
- The clean-checkout baseline uses `.slnx`, central package versions, warnings-as-errors, root-declared submodules only, and named source, package-consumer, public-API, boundary, and basic test gates. Debug development may use intentional project references; Release consumer validation must use produced public packages and detect reverse or implementation-type leakage.
- Public contracts are structured for automation, additive-first within V1, and expose no EventStore stream, aggregate, projection, hosting, workflow, or Provider SDK internals. Deterministic fixtures may prove decision logic but cannot claim live evidence, production enablement, or release qualification.

## Technical Decisions

- Agents is an EventStore-backed domain module. `Agent` owns setup and policy; `ProviderCatalog` owns safe Provider/model capability and pricing truth; `LaunchReadinessGate` is the sole readiness-record writer. Aggregate handlers are pure and emit events; dependency reads and other effects execute in application adapters and return through commands.
- The module ships `Contracts`, `Client`, `Server`, domain/UI assets, `Testing`, and focused test projects. It must not contain or package module-owned `AppHost`, `Aspire`, or `ServiceDefaults` projects. `EXT-HOST-1` owns production-like composition of the reusable DomainService and FrontComposer UI with EventStore, Conversations, Parties, Tenants, Dapr Workflow, readiness, capacity, telemetry, identity, health, secrets, and evidence ingress.
- Provider integration remains behind an Agents-owned adapter committed through `EXT-PROVIDER-1`; credentials resolve only through `EXT-SECRETS-1`. Uncommitted dependencies permit neither inferred compatibility nor live execution.
- Readiness observations are immutable and versioned by EventStore revision. The projection selects the greatest committed revision, never falls back to an older pass, and evaluates freshness using each record's `ObservedAt` and exclusive `ValidUntil`. API, BFF, UI, workflow, and projections use the same versioned operation-gate matrix and one consistent registry checkpoint; missing families, unknown versions, missing records, or checkpoint changes block or retry.

## UX & Interaction Patterns

Use FrontComposer and Fluent UI Blazor V5 for the Agents overview, configuration, Provider catalog, and readiness surfaces. Keep lifecycle and callability visually and semantically separate; reserve Success for current authoritative callable proof, render callable Provider degradation as Warning, and show safe blockers with a recovery owner. Accepted mutations follow `submitted -> authoritative pending -> projection-confirmed terminal`; pending or optimistic state is never Success. All status and denial text must be whole-string localized with English/French parity, keyboard and live-region accessible, responsive, and free of secrets or cross-tenant disclosure.

## Cross-Story Dependencies

Story 5.1 establishes the boundary baseline; Stories 5.2 and 5.3 then add independently usable Agent and Provider operations; Story 5.4 binds current tenant, Party, and approver evidence; Story 5.5 publishes shared readiness truth; Story 5.6 proves platform-hosted composition; Story 5.7 consumes Stories 5.2–5.6 to gate activation. `EXT-HOST-1` must be at least Committed for Story 5.1; `EXT-PROVIDER-1` must be at least Committed for Stories 5.3 and 5.5; and `EXT-HOST-1`, `EXT-SECRETS-1`, and `EXT-TOPOLOGY-1` must be Available with passing compatibility commands for Story 5.6 live execution. Release qualification remains a separate gate and requires current qualifying evidence plus Available consumed external dependencies.
