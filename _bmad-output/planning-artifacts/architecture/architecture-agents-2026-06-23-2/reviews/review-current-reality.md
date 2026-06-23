# Current Reality And Version Review

Verdict: Pass.

Reality checks:

- Existing root workspace has no root `.slnx` or root AppHost; it is a coordination/super-repo with module-local solutions and BMad artifacts. AD-16 matches this.
- Existing modules use module-local `.slnx`, `global.json`, `Directory.Build.props`, and `Directory.Packages.props`. Structural Seed matches this pattern.
- EventStore aggregate convention is pure `Handle(command, state[, envelope]) -> DomainResult` plus replayed state. AD-3 matches this.
- Conversations has `IConversationClient.GetConversationAsync` and `AppendMessageAsync`; `AddParticipantCommand` exists in contracts/handlers but is not exposed through the inspected public client. AD-6/AD-7 now make the membership seam a prerequisite if absent.
- Conversations participant vocabulary includes `ParticipantType.AiAgent` with canonical wire value `AIAgent`; AD-7 uses that vocabulary.
- Conversations public detail exposes participant roles (`Member`, `Facilitator`, `Observer`) and no first-class owner field in inspected contracts; AD-8 captures the facilitator mapping assumption.
- FrontComposer/Tenants UI registration pattern is domain manifest plus nav entries; AD-15 matches this.

Version checks:

- Local submodule commits recorded in Stack: EventStore `c3ce4a40`, Conversations `46df0cd`, Parties `0191a6c`, Tenants `9a3567b`, FrontComposer `eee5e6b`.
- Local package baseline recorded from sibling `Directory.Packages.props`: Dapr `1.18.4`, Aspire Hosting `13.4.6`, MediatR `14.1.0`, FluentValidation `12.1.1`, OpenTelemetry `1.15.x`-`1.16.0`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, xUnit v3 `3.2.2`, Shouldly `4.3.0`.

Residual risks:

- Provider SDK is intentionally not selected, so no provider package version is pinned.
- .NET SDK patch differs across sibling modules (`10.0.300` and `10.0.301` observed). Spine records the range rather than inventing a single module baseline.

## 2026-06-23 Dapr Runtime Amendment

Verdict: Pass.

Reality checks:

- Official Dapr docs currently present v1.18 as the latest docs stream and v1.19 as preview.
- Official Dapr Agents docs describe Dapr Agents v1.0 as GA and production ready, but also describe it as a Python framework.
- Official Dapr Agents core concepts describe `DurableAgent` as workflow-based and backed by Dapr Workflows for long-running, fault-tolerant, durable execution.
- Official Dapr .NET SDK docs expose Dapr Workflow and Dapr AI pages; the local Hexalith baseline already uses `Dapr.Workflow`, `Dapr.AI`, and `Dapr.AI.Microsoft.Extensions` at `1.18.4` in sibling package props where applicable.

Residual risks:

- Exact Dapr Agents worker packaging is intentionally deferred because the core Hexalith module is .NET and Dapr Agents itself is Python. AD-18 constrains this to an adapter/worker boundary rather than letting Python SDK types leak into contracts or aggregates.

## 2026-06-23 Hybrid Runtime Research Amendment

Verdict: Pass.

Reality checks:

- Microsoft Learn currently describes Microsoft Agent Framework as supporting agents, MCP servers, graph workflows, sessions, middleware, telemetry, checkpointing, and human-in-the-loop behavior for .NET/Python.
- NuGet currently lists `Microsoft.Agents.AI` and `Microsoft.Agents.AI.Workflows` at `1.10.0`, compatible with `net10.0`.
- Dapr Agents docs currently describe Dapr Agents v1.0 as generally available/production-ready and Python-based.
- Dapr `MCPServer` docs currently describe service invocation as the default MCP path and `MCPServer` as the workflow-centric path for argument-level RBAC, audit, redaction, durable retries, and per-tool observability.
- The local Hexalith stack already uses Dapr, Aspire, OpenTelemetry, MCP, EventStore, and module-local AppHost conventions; the amendment adds Microsoft Agent Framework as a new seed dependency without changing domain ownership.

Residual risks:

- Agent Framework provider/hosting subpackages beyond the core `Microsoft.Agents.AI` and `Workflows` packages are not selected. Keep those adapter-local and pin them centrally when implementation chooses them.
- Dapr Conversation remains deferred behind the provider/model port because current docs still treat it as evolving/alpha.
