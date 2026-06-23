# Aggregates (named extension point — AC4)

Empty in Story 1.1. This folder is the named home for the Agents domain aggregate roots, each added by the
story that needs it:

- **Agent** — the governed Agent (Hexa) aggregate (Stories 1.3-1.7).
- **ProviderCatalog** — governed provider/model catalog entries (Story 1.2).
- **AgentInteraction** — conversation-invocation interactions (Epic 2).

No aggregates, events, commands, or projections are pre-created here (Scope Guardrails: no premature
domain entities ahead of the story that needs them).
