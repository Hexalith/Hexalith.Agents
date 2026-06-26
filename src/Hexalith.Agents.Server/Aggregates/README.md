# Aggregates (structural-seed placeholder — unused)

This Server-side `Aggregates/` folder is a placeholder from the original Structural Seed. It is intentionally
**empty and unused**: the Agents domain aggregate roots are pure aggregates and live in the **domain project**
`Hexalith.Agents`, not in `Hexalith.Agents.Server` (AD-1 full EventStore domain module; AD-3 pure aggregates,
side effects outside). As of Epic 4 all three were implemented there:

- **Agent** — the governed Agent (`hexa`) aggregate (Stories 1.3–1.7) — `src/Hexalith.Agents/Agent/AgentAggregate.cs`
- **ProviderCatalog** — governed provider/model catalog entries (Story 1.2) — `src/Hexalith.Agents/ProviderCatalog/ProviderCatalogAggregate.cs`
- **AgentInteraction** — conversation-invocation interactions (Epic 2) — `src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs`

What the `Hexalith.Agents.Server` project actually hosts is the impure, server-side machinery that sits
*outside* the pure aggregates: the EventStore domain-service host (`Program.cs`), application orchestrators
and query handlers (`Application/`), dependency ports with fail-closed `Deferred*` adapters (`Ports/`), the
public API/BFF route surface (`Api/`), and the (still-deferred, `.gitkeep`-only) `Projections/` and
`Application/Workflows/` folders. This folder can be removed or repurposed; nothing references it.
