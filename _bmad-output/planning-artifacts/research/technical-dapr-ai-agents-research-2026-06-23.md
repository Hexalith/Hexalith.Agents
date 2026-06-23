---
stepsCompleted: [1, 2, 3, 4, 5, 6]
inputDocuments: []
workflowType: 'research'
lastStep: 6
research_type: 'technical'
research_topic: 'Dapr AI Agents and Microsoft Agent Framework hybrid implementation'
research_goals: 'Describe how to implement a hybrid agent architecture that uses Dapr for distributed runtime capabilities and Microsoft Agent Framework for agent abstractions, orchestration, model/tool integration, and lifecycle patterns.'
user_name: 'Administrator'
date: '2026-06-23'
web_research_enabled: true
source_verification: true
---

# Research Report: technical

**Date:** 2026-06-23
**Author:** Administrator
**Research Type:** technical

---

## Research Overview

This technical research evaluates how to implement a hybrid agent platform using **Dapr AI Agents** and **Microsoft Agent Framework**. The research scope covered technology stack selection, integration protocols, architectural patterns, implementation workflows, operational practices, and risk controls, with current source verification from Dapr, Microsoft Learn, NuGet/PyPI/GitHub, protocol specifications, and production guidance.

The central finding is that the hybrid should not try to merge the two frameworks into one abstraction. Microsoft Agent Framework should own the .NET agent and workflow application layer: typed agents, model providers, sessions, MCP/A2A integration, middleware, and graph workflows. Dapr Agents should be used as a first-class durable Python agent runtime for workloads that specifically need `DurableAgent` and Dapr-native workflow-backed autonomous execution. Dapr itself supplies the distributed runtime: state, workflow, actors, pub/sub, service invocation, resiliency, secrets, security, and observability. The full executive synthesis, roadmap, and risk assessment are in the Research Synthesis section at the end of this document.

---

## Technical Research Scope Confirmation

**Research Topic:** Dapr AI Agents and Microsoft Agent Framework hybrid implementation
**Research Goals:** Describe how to implement a hybrid agent architecture that uses Dapr for distributed runtime capabilities and Microsoft Agent Framework for agent abstractions, orchestration, model/tool integration, and lifecycle patterns.

**Technical Research Scope:**

- Architecture Analysis - design patterns, frameworks, system architecture
- Implementation Approaches - development methodologies, coding patterns
- Technology Stack - languages, frameworks, tools, platforms
- Integration Patterns - APIs, protocols, interoperability
- Performance Considerations - scalability, optimization, patterns

**Research Methodology:**

- Current web data with rigorous source verification
- Multi-source validation for critical technical claims
- Confidence level framework for uncertain information
- Comprehensive technical coverage with architecture-specific insights

**Scope Confirmed:** 2026-06-23

## Technology Stack Analysis

### Programming Languages

The hybrid should be designed as a **polyglot agent platform** where Dapr Agents is a first-class durable, workflow-backed agent runtime and Microsoft Agent Framework is the primary .NET agent/workflow abstraction. The Dapr Agents GitHub README describes built-in workflow orchestration, statefulness, security, telemetry, and workflow resilience; it also states that Dapr Agents uses Dapr's durable-execution workflow engine so agent tasks can recover state and continue after interruptions. The current Dapr Agents documentation identifies Dapr Agents as a **Python framework** for LLM-powered autonomous applications. That matters for Hexalith because the surrounding project context is .NET 10, Dapr, Aspire, EventStore, and MCP-oriented; a clean implementation should host Dapr Agents in Python worker services rather than force Python-only classes into .NET services. Dapr sidecars, Dapr Workflow, state, pub/sub, service invocation, MCP, and Conversation API become the shared runtime boundaries between the Dapr Agents and Microsoft Agent Framework parts of the system.

Microsoft Agent Framework is the natural .NET agent layer. Microsoft Learn describes Agent Framework as supporting agents and workflows in .NET and Python, with agents calling tools/MCP servers and workflows connecting agents/functions through graph-based, type-safe routing, checkpointing, and human-in-the-loop support. For the .NET services, use C#/.NET for durable APIs, tenancy, authorization, tool boundaries, and Microsoft Agent Framework agents/workflows. Use Python only for dedicated Dapr Agents workers when you want the `DurableAgent`, `AgentRunner`, Dapr Agents memory adapters, or Python ecosystem integrations directly.

_Popular Languages:_ C#/.NET for the host, APIs, MCP, Dapr client, workflows, governance, and typed contracts; Python for native Dapr Agents v1.0 durable-agent workers and Python AI ecosystem integrations.

_Emerging Languages:_ Python remains the native Dapr Agents SDK language; Agent Framework also supports Python, so a Python worker can host either Dapr Agents or Agent Framework agents if needed. Go/Java/JavaScript remain relevant for Dapr Workflow SDK support, but they are not the best fit for this Hexalith implementation unless a bounded context already uses them.

_Language Evolution:_ The practical direction is not "Dapr Agents versus Microsoft Agent Framework"; it is C#/.NET for enterprise agent hosting and Python as an optional Dapr Agents runtime behind Dapr APIs. Dapr Workflow authoring is available in multiple SDKs, including .NET, Python, JavaScript, Java, and Go.

_Performance Characteristics:_ C#/.NET gives strong typing, package governance, source generators, middleware, and easier integration with the existing Hexalith stack. Python Dapr Agents gives the direct route to Dapr's current durable workflow-backed agent runtime, but should be isolated behind HTTP/pub-sub/service-invocation/MCP boundaries so the core platform remains versionable and testable.

_Sources:_ Dapr Agents GitHub README: https://github.com/dapr/dapr-agents; Dapr Agents overview: https://docs.dapr.io/developing-ai/dapr-agents/; Dapr Agents introduction: https://docs.dapr.io/developing-ai/dapr-agents/dapr-agents-introduction/; Microsoft Agent Framework overview: https://learn.microsoft.com/en-us/agent-framework/overview/; Dapr Workflow overview: https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview/

### Development Frameworks and Libraries

The stack has two distinct framework layers:

- **Microsoft Agent Framework** should own the .NET agent abstraction: `AIAgent`, sessions, model provider integration, tool calling, MCP integration, middleware, and graph workflows. Microsoft Learn positions agents for open-ended conversational/tool-using tasks and workflows for explicit multi-step processes. The workflow API uses executors and edges, supports streaming and non-streaming execution, validates graph connectivity/type compatibility, and can wrap a workflow as an `AIAgent`.
- **Dapr** should own the distributed runtime substrate: sidecar APIs, workflow durability, state stores, pub/sub, service invocation, resiliency policies, secrets, observability, and deployment portability.
- **Dapr Agents** should be treated as the first-class Dapr-native durable agent runtime. Its `DurableAgent` is workflow-backed and recommended for production-like durable execution, while the older `Agent` class is documented as deprecated. Because the current implementation is Python, the hybrid architecture should expose Dapr Agents workers through Dapr service invocation, pub/sub, HTTP, and MCP boundaries.

For .NET, the relevant package families are `Microsoft.Agents.AI`, `Microsoft.Agents.AI.Workflows`, provider packages such as `Microsoft.Agents.AI.Foundry`/OpenAI/Azure OpenAI as needed, `Microsoft.Extensions.AI`, `Dapr.Client`, `Dapr.AspNetCore`, `Dapr.Workflow`, `Dapr.Actors`, and `ModelContextProtocol` for MCP server/client integration. NuGet currently lists `Microsoft.Agents.AI` and `Microsoft.Agents.AI.Workflows` as `1.10.0`, and Dapr's NuGet profile lists current Dapr .NET packages at `1.18.4`. This aligns with the local Hexalith project context where sibling modules already use Dapr 1.17.x/1.18.x and .NET 10.

_Major Frameworks:_ Dapr Agents Python for native durable workflow-backed `DurableAgent` workers; Microsoft Agent Framework for .NET agent/workflow orchestration; Dapr runtime and .NET SDK for distributed sidecar capabilities; ASP.NET Core for APIs/MCP hosts; Aspire for local topology orchestration.

_Micro-frameworks:_ MCP C# SDK for tool/resource boundaries; Microsoft.Extensions.AI abstractions for provider-neutral chat/model access; Dapr Conversation API client for routing LLM calls through Dapr components.

_Evolution Trends:_ Dapr Agents has moved to v1.0 GA and recommends `DurableAgent`; Dapr Conversation remains alpha in Dapr v1.18 docs, so it should be introduced behind an abstraction rather than embedded everywhere. Microsoft Agent Framework is now the successor path for Semantic Kernel/AutoGen style .NET agent work and provides the richer typed .NET developer experience.

_Ecosystem Maturity:_ Dapr building blocks are mature for distributed state/pub-sub/workflow/service invocation. Microsoft Agent Framework is production-oriented but still shows `--prerelease` installs in some Learn examples for provider packages, so pin versions centrally and expect package churn. Dapr Agents is production-ready per Dapr docs, but its Python-only nature makes it best as an isolated worker framework in this hybrid.

_Sources:_ Dapr Agents GitHub README: https://github.com/dapr/dapr-agents; Microsoft Agent Framework overview: https://learn.microsoft.com/en-us/agent-framework/overview/; workflows overview: https://learn.microsoft.com/en-us/agent-framework/workflows/; workflows as agents: https://learn.microsoft.com/en-us/agent-framework/workflows/as-agents; Dapr Agents core concepts: https://docs.dapr.io/developing-ai/dapr-agents/dapr-agents-core-concepts/; NuGet Microsoft.Agents.AI: https://www.nuget.org/packages/Microsoft.Agents.AI/; NuGet Dapr profile: https://www.nuget.org/profiles/dapr.io

### Database and Storage Technologies

Dapr should abstract storage for the hybrid architecture, but the data model needs separate stores for separate concerns:

- **Workflow state** for Dapr Workflow/Dapr Agents durable execution. Dapr Workflows are built on actors, use state stores, and require stores that support workflow use. The Dapr Agents quickstart uses a dedicated `agent-workflow` Redis state store with `actorStateStore: "true"`.
- **Conversation/session state** for agent chat history. Dapr Agents stores conversation history through `ConversationDaprStateMemory`; Microsoft Agent Framework sessions can be serialized and resumed, so a .NET implementation should persist serialized sessions in a Dapr state store or a domain-owned store.
- **Domain state and audit state** should stay in Hexalith's existing EventStore/domain model rather than being hidden in agent memory. Agents can propose commands, but command acceptance and durable business truth should remain event-sourced.
- **Vector/search memory** should be a separate retriever layer. Dapr Agents documents vector memory options such as Chroma, PostgreSQL, and Redis. Hexalith.Memories already provides a richer memory/search bounded context, so the preferred hybrid path is to expose memory retrieval as a tool/service instead of letting each agent create private vector stores.

For local development, Redis is the default practical state and pub/sub backend because Dapr local initialization and Dapr Agents quickstarts rely on Redis. For production, pick a state store by required guarantees: workflow support, ETag/transaction capability, encryption posture, backup/restore, operational support, and tenant isolation. Avoid binding agent logic to a specific backend; bind to Dapr component names and application-level repositories.

_Relational Databases:_ PostgreSQL is useful for vector extensions, audit/reporting, and strongly consistent app-owned metadata, but do not make direct SQL the workflow persistence contract if Dapr Workflow owns execution.

_NoSQL Databases:_ Azure Cosmos DB, DynamoDB, Redis, and other Dapr-supported state stores are candidates. Validate each store's workflow/actor support and payload limits before selecting it for durable agent execution.

_In-Memory Databases:_ Redis is the best local/default state, pub-sub, and memory inspection option. It is also common for sidecar-local response caching and quick iteration, but production use still needs persistence, HA, backup, and tenant isolation decisions.

_Data Warehousing:_ Not central to the runtime path. Persist agent telemetry, evaluations, and audit summaries to an analytics pipeline separately from workflow state.

_Sources:_ Dapr state management overview: https://docs.dapr.io/developing-applications/building-blocks/state-management/state-management-overview/; Dapr Workflow overview: https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview/; Dapr Workflow architecture: https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-architecture/; Dapr Agents getting started: https://docs.dapr.io/developing-ai/dapr-agents/dapr-agents-getting-started/

### Development Tools and Platforms

The local development loop should be:

1. Use Aspire AppHost to start the application topology, Redis/state stores, Dapr sidecars, and model/memory dependencies.
2. Use Dapr component YAML files for conversation components, state stores, pub/sub brokers, secrets, and resiliency specs.
3. Use central package management for .NET packages and lock Dapr/Microsoft Agent Framework package families intentionally.
4. Use MCP as the stable tool boundary between agents and Hexalith capabilities.
5. Use OpenTelemetry traces/logs/metrics across Dapr sidecars, workflow execution, agent calls, and tool invocations.

Dapr Agents quickstarts use the Dapr CLI, Docker, Python 3.11+, `uv`, Ollama by default, and component YAML for LLM/state/workflow resources. Microsoft Agent Framework .NET examples use NuGet packages, `AIProjectClient`, provider packages, `WorkflowBuilder`, `AIAgent`, and MCP C# SDK integration for tools. The repo context adds .NET 10, warnings-as-errors, `.slnx`, central package management, xUnit v3, Shouldly, NSubstitute, and Aspire.

_IDE and Editors:_ Visual Studio/Rider/VS Code are all viable; use editor settings from the repo, not framework defaults.

_Version Control:_ Git plus conventional commits. For this repo, do not write generated/planning docs into `docs/`; BMAD artifacts belong under `_bmad-output/`.

_Build Systems:_ `dotnet`/MSBuild with `.slnx` and Central Package Management for .NET; `uv`/Python virtual environments for optional Python Dapr Agents workers; Dapr CLI for local sidecar runs; Aspire for topology.

_Testing Frameworks:_ xUnit v3/Shouldly/NSubstitute for .NET unit and integration tests; Dapr/Aspire integration tests for topology; Python test stack only inside Python worker projects if those are introduced. Add deterministic workflow tests for replay/idempotency and contract tests for MCP/tool schemas.

_Sources:_ Dapr Agents getting started: https://docs.dapr.io/developing-ai/dapr-agents/dapr-agents-getting-started/; Dapr .NET SDK: https://docs.dapr.io/developing-applications/sdks/dotnet/; Microsoft Agent Framework MCP tools: https://learn.microsoft.com/en-us/agent-framework/agents/tools/local-mcp-tools; Microsoft Agent Framework workflow builder: https://learn.microsoft.com/en-us/agent-framework/workflows/workflows

### Cloud Infrastructure and Deployment

Deploy the hybrid as sidecar-enabled services rather than a monolith:

- A **.NET agent orchestration service** hosts Microsoft Agent Framework workflows, exposes REST/MCP/tool endpoints, serializes sessions, and calls Dapr APIs.
- Optional **Python Dapr Agents worker services** host `DurableAgent` implementations and expose HTTP/pub-sub triggers through `AgentRunner`.
- **Domain/tool services** expose stable business capabilities through MCP, Dapr service invocation, or internal APIs.
- **Dapr components** define state stores, pub/sub brokers, conversation providers, secret stores, and resiliency policies.
- **Kubernetes or Azure Container Apps** should run production services with Dapr sidecars; Aspire should model the local topology and produce deployment-aligned configuration where possible.

Dapr Workflow provides HTTP/gRPC management APIs for start/query/pause/resume/raise-event/terminate/purge, and Dapr v1.18 docs include workflow access policies for fine-grained control over which apps can start workflows or call activities. Use those policies to constrain cross-application agent scheduling. Dapr pub/sub publishes with at-least-once semantics, so every tool activity, pub/sub handler, and workflow activity that changes state must be idempotent.

_Major Cloud Providers:_ Azure is the most natural fit for Foundry/Azure OpenAI, Azure Container Apps/Kubernetes, Key Vault, managed identity, Application Insights/OpenTelemetry, and Cosmos DB/Redis. AWS/GCP remain possible through Dapr component abstraction if model and data requirements demand it.

_Container Technologies:_ Docker for local dependencies; Kubernetes or Azure Container Apps for production Dapr sidecars and scale-out.

_Serverless Platforms:_ Use cautiously. Agents with durable workflow state fit containerized services better than short-lived functions, although event-triggered ingress can enqueue work through pub/sub or workflow start APIs.

_CDN and Edge Computing:_ Not core to agent execution. Edge/CDN may apply to front-end delivery or low-latency read-only retrieval, not to durable agent workflows that need consistent state and auditable execution.

_Sources:_ Dapr Workflow overview: https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview/; Dapr Workflow features: https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-features-concepts/; Dapr pub/sub API: https://docs.dapr.io/reference/api/pubsub_api/; Dapr resiliency overview: https://docs.dapr.io/operations/resiliency/resiliency-overview/

### Technology Adoption Trends

The current trend is toward **agent abstractions plus durable distributed execution**, not hand-written chat loops. Dapr Agents focuses on durability, stateful workflows, event-driven communication, observability, and provider-neutral infrastructure. Microsoft Agent Framework focuses on typed agent/workflow composition, providers, MCP tools, middleware, sessions, and migration from Semantic Kernel/AutoGen patterns. Their overlap is useful, but the best hybrid implementation avoids double-orchestrating the same task.

Recommended adoption sequence:

1. Start with Microsoft Agent Framework in .NET for the primary agent/workflow API.
2. Use Dapr state, pub/sub, service invocation, resiliency, secrets, and observability from day one.
3. Persist Agent Framework sessions through a repository backed by a Dapr state store.
4. Represent every business capability as a typed tool/MCP operation with tenant and authorization gates.
5. Use Dapr Workflow for long-running, externally coordinated, replay-sensitive processes that must survive restarts.
6. Add Dapr Agents Python workers for agent tasks that need Dapr Agents' first-class durable workflow execution, automatic task recovery, built-in agent memory/state wiring, or sidecar-discovered MCP tooling.

_Migration Patterns:_ Existing Semantic Kernel/AutoGen-style .NET agents should migrate toward Microsoft Agent Framework. Existing Dapr microservices should add agent capabilities through Dapr components and sidecar APIs rather than replacing service boundaries.

_Emerging Technologies:_ MCP is the strongest common integration layer between frameworks. Dapr Conversation API is promising for provider abstraction, caching, PII scrubbing, and tool calling, but it is still documented as alpha in Dapr v1.18, so isolate it behind an adapter.

_Legacy Technology:_ Custom ad hoc agent loops, direct SDK calls scattered across services, non-durable background queues for long-running tasks, and private per-agent memory stores should be phased out.

_Community Trends:_ Both ecosystems are converging on multi-agent workflows, MCP/A2A-style interoperability, durable state, OpenTelemetry, and provider-neutral model access. The hybrid should make those explicit platform contracts rather than incidental library choices.

_Sources:_ Dapr Agents introduction: https://docs.dapr.io/developing-ai/dapr-agents/dapr-agents-introduction/; Dapr Conversation overview: https://docs.dapr.io/developing-applications/building-blocks/conversation/conversation-overview/; Microsoft Agent Framework overview: https://learn.microsoft.com/en-us/agent-framework/overview/; Microsoft Agent Framework providers: https://learn.microsoft.com/en-us/agent-framework/agents/providers/

## Integration Patterns Analysis

### API Design Patterns

The hybrid should expose **four API shapes**, each with a distinct purpose:

1. **Domain command/query APIs** for Hexalith business actions. These remain typed, tenant-aware, validated, and event-sourced. Agents call them through tools; they should not bypass domain command handlers.
2. **Agent task APIs** for starting, querying, resuming, and cancelling agent work. Dapr Agents already exposes a durable HTTP pattern where `/agent/run` schedules a workflow and returns an instance ID; Dapr Workflow also exposes lifecycle APIs for start, query, pause/resume, raise event, terminate, and purge.
3. **Tool APIs** through MCP or Agent Framework function tools. Use MCP for reusable, discoverable tool surfaces and function tools for local, strongly typed .NET business logic.
4. **Remote-agent APIs** through A2A when one agent must cross a service, team, language, or organization boundary.

For REST/HTTP, keep endpoints coarse and task-oriented: start an agent run, query an instance, submit human input, or invoke a domain command. Avoid an RPC explosion where every internal agent step becomes a public endpoint. For gRPC, use Dapr service invocation when high-throughput internal services already have protobuf contracts or when binary contracts are valuable. For GraphQL, use only as a read/query facade if clients need flexible projection retrieval; it is not the right control plane for durable agent execution.

_RESTful APIs:_ Use HTTP for agent ingress, Dapr sidecar APIs, MCP Streamable HTTP, A2A HTTP bindings, and workflow lifecycle control.

_GraphQL APIs:_ Optional for read-side projection composition. Avoid for command execution, workflow control, and tool calls because those need explicit authorization, idempotency, audit, and retry semantics.

_RPC and gRPC:_ Use Dapr service invocation for internal service-to-service calls. Dapr supports HTTP and gRPC service invocation and forwards sidecar-to-sidecar calls over gRPC.

_Webhook Patterns:_ Use Dapr pub/sub subscriptions and CloudEvents for asynchronous inbound work, not ad hoc webhooks, when both sides are Daprized. For third-party callbacks, terminate at an ingress adapter and publish normalized CloudEvents.

_Source:_ https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/; https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview/; https://docs.dapr.io/developing-ai/dapr-agents/dapr-agents-getting-started/; https://learn.microsoft.com/en-us/agent-framework/agents/providers/agent-to-agent

### Communication Protocols

Use the protocol by boundary:

- **In-process composition:** Microsoft Agent Framework function tools and agents-as-tools. This has the least overhead and best type safety, but only works when the composed agents live in the same process and release boundary.
- **Tool boundary:** MCP. Microsoft Agent Framework can call MCP tools through the official MCP C# SDK, and Dapr can govern MCP traffic through service invocation. Dapr's `MCPServer` resource is a second path for durable, policy-heavy MCP tool execution.
- **Remote-agent boundary:** A2A. Agent Framework's `A2AAgent` wraps an A2A-compliant endpoint as an `AIAgent`, supports agent card discovery, streaming, and background/continuation-token patterns.
- **Service boundary:** Dapr service invocation over HTTP/gRPC for synchronous calls and Dapr pub/sub for asynchronous events.
- **Durable orchestration boundary:** Dapr Workflow for persistent multi-step execution, including Dapr Agents `DurableAgent` and Dapr `MCPServer` tool workflows.

MCP itself uses JSON-RPC over `stdio` or Streamable HTTP in the current spec. Dapr `MCPServer` supports Streamable HTTP, legacy SSE, and stdio transports. A2A uses agent cards for discovery and Agent Framework supports HTTP+JSON and JSON-RPC protocol binding selection, with streaming over Server-Sent Events for updates.

_HTTP/HTTPS Protocols:_ Use HTTPS for public/remote agent, MCP, and domain API boundaries. Dapr sidecars commonly communicate with local app processes over localhost, while sidecar-to-sidecar traffic is secured separately by Dapr.

_WebSocket Protocols:_ Not a primary requirement. Prefer SSE/streaming responses where A2A/MCP/Agent Framework already support them, and use SignalR only for UI notification needs.

_Message Queue Protocols:_ Dapr pub/sub abstracts the broker; the app should depend on Dapr topics, not Kafka/RabbitMQ/Event Hubs APIs directly. Dapr guarantees at-least-once delivery, so consumers and workflow activities must be idempotent.

_grpc and Protocol Buffers:_ Use for internal high-performance service contracts where schema-first binary communication helps. Dapr service invocation supports gRPC proxying while keeping service discovery, mTLS, access control, resiliency, and observability.

_Source:_ https://modelcontextprotocol.io/specification/2025-06-18/basic/transports; https://docs.dapr.io/developing-ai/mcp/; https://docs.dapr.io/developing-ai/mcp/mcp-server-resource/; https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/; https://learn.microsoft.com/en-us/agent-framework/agents/providers/agent-to-agent

### Data Formats and Standards

The baseline wire format should be JSON for API ingress, agent messages, MCP payloads, A2A messages, and Dapr sidecar HTTP APIs. For pub/sub, prefer CloudEvents 1.0 with JSON payloads so trace context, source, type, topic, content type, and sender information flow consistently. Dapr automatically wraps pub/sub messages in CloudEvents by default and supports explicit CloudEvent publishing when needed.

For tool schemas, use JSON Schema-compatible contracts because MCP tools, Dapr Conversation tool calling, OpenAI-compatible function calling, and Agent Framework tools all converge on named tools with descriptions and structured argument schemas. For internal high-throughput calls, gRPC/Protobuf can be used behind Dapr service invocation, but it should not be the public agent/tool contract unless consumers need binary compatibility.

For Hexalith, keep durable domain events separate from agent transcripts. Domain events stay in EventStore contracts; agent memory/session state stays in state stores; tool call audit records should reference domain IDs and workflow IDs rather than copying large payloads or PII.

_JSON and XML:_ JSON is the default for agent/tool/workflow APIs. XML should only appear for legacy integrations and can travel inside CloudEvents with the correct content type.

_Protobuf and MessagePack:_ Protobuf is appropriate for gRPC internal APIs. MessagePack may be useful inside existing Hexalith service contracts, but the agent integration layer should expose JSON/MCP/A2A unless there is a measured need.

_CSV and Flat Files:_ Treat as ingestion artifacts handled by tools or bindings, not as direct agent control messages.

_Custom Data Formats:_ Avoid custom wire formats for agent/tool traffic. Use MCP result shapes, CloudEvents, A2A agent cards/messages, and typed domain contracts.

_Source:_ https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-cloudevents/; https://docs.dapr.io/reference/api/conversation_api/; https://docs.dapr.io/developing-ai/mcp/mcp-server-resource/; https://learn.microsoft.com/en-us/agent-framework/agents/tools/

### System Interoperability Approaches

The main interoperability rule is: **tools are MCP, agents are A2A or workflow agents, services are Dapr service invocation, events are Dapr pub/sub CloudEvents, and business mutations are domain commands.**

Use Dapr service invocation for point-to-point calls between .NET services, Python Dapr Agents workers, MCP servers, policy services, and domain services. It gives app-ID addressing, service discovery, mTLS, access control, resiliency, tracing, and metrics without every service implementing the same plumbing.

Use MCP through Dapr's service invocation path when you want off-the-shelf MCP clients and frameworks to work unchanged. Use Dapr `MCPServer` resources when you need durable MCP tool calls, argument-level RBAC, audit/redaction hooks, per-tool observability, YAML-declared credentials, and crash recovery. This is especially relevant for Dapr Agents because their README states that Dapr Agents can auto-discover Dapr `MCPServer` resources loaded into the sidecar.

Use A2A for remote agents owned by different teams, written in different languages, or deployed on separate lifecycles. In-process agents-as-tools are simpler and faster when all participants share a process and ownership boundary.

_Point-to-Point Integration:_ Dapr service invocation for app-to-app calls; Microsoft Agent Framework function tools for same-process tool calls.

_API Gateway Patterns:_ Keep external ingress behind a gateway/BFF that authenticates users, resolves tenant context, and starts agent workflows or domain commands. Do not expose every internal agent/tool service directly.

_Service Mesh:_ Dapr overlaps with service mesh concerns for invocation, mTLS, resiliency, observability, pub/sub, state, and workflow. If a mesh exists, define a clear responsibility split so retries, mTLS, and telemetry are not duplicated unpredictably.

_Enterprise Service Bus:_ Dapr pub/sub replaces most ESB-style coupling for new services. Use broker-native features only behind Dapr components or in dedicated adapters.

_Source:_ https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/; https://docs.dapr.io/developing-ai/mcp/; https://docs.dapr.io/developing-ai/mcp/mcp-server-resource/; https://github.com/dapr/dapr-agents; https://learn.microsoft.com/en-us/agent-framework/agents/tools/

### Microservices Integration Patterns

The hybrid should follow a **bounded-service integration model**:

- `AgentHost` (.NET) runs Microsoft Agent Framework agents/workflows and exposes stable APIs.
- `DaprAgentWorker` (Python, optional) runs Dapr Agents `DurableAgent` services for tasks that benefit from native Dapr Agents durable execution.
- `ToolHost` exposes MCP tools over ASP.NET Core or dedicated MCP servers.
- `PolicyHost` handles cross-agent authorization, redaction, and audit hooks, including Dapr `MCPServer` middleware workflows.
- Domain services remain command/query owners and are called through tools or service invocation.

Use Dapr access control for service invocation, and WorkflowAccessPolicy for cross-app workflow/activity scheduling because Dapr docs explicitly separate service invocation ACLs from workflow scheduling permissions. Use resiliency policies for service invocation/components/actors and workflow retry policies for durable workflow steps. For event consumers, implement dedupe/idempotency because Dapr pub/sub is at least once.

_API Gateway Pattern:_ External requests enter through one authenticated API/BFF, which starts a workflow or sends a command; internal agent workers are not public.

_Service Discovery:_ Dapr App IDs and name resolution provide service discovery. Avoid hardcoded pod URLs or direct service DNS names in agent logic.

_Circuit Breaker Pattern:_ Use Dapr resiliency specs for timeouts, retries, and circuit breakers on service/component/actor calls. For streaming calls, account for Dapr's documented retry limitations because streaming request bodies cannot be replayed.

_Saga Pattern:_ Use Dapr Workflow for multi-step agent/business processes. Keep workflow code deterministic and put I/O, model calls, and domain commands into activities/tools.

_Source:_ https://docs.dapr.io/operations/configuration/invoke-allowlist/; https://docs.dapr.io/operations/resiliency/resiliency-overview/; https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-features-concepts/; https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/

### Event-Driven Integration

Use event-driven integration for coordination, not for hidden business mutation. Agent requests, task updates, workflow lifecycle notifications, and cross-agent broadcasts can flow through Dapr pub/sub. Domain changes should still be produced by domain command handlers and published from the event-sourced source of truth.

Dapr pub/sub supports declarative, programmatic, and streaming subscriptions. For platform consistency, prefer declarative subscriptions for durable production topology and programmatic/streaming subscriptions where dynamic agent behavior is required. Use CloudEvents type/source/subject conventions that identify the bounded context, event kind, tenant, and workflow/agent run. Include workflow IDs and correlation IDs in metadata/payload so traces and audit records connect.

Dapr Agents supports pub/sub-driven multi-agent collaboration, and the core concepts docs describe Durable Agents as independent services with pub/sub configuration and an orchestrator that can coordinate specialized agents. That maps well to topic-per-agent or topic-per-team patterns, with a coordinating workflow deciding which agent receives each task.

_Publish-Subscribe Patterns:_ Use Dapr topics for task dispatch, agent notifications, and cross-agent collaboration. Consumers return success/retry/drop semantics intentionally.

_Event Sourcing:_ Use Hexalith EventStore for durable business facts. Use Dapr Workflow history for agent/process execution facts. Do not confuse the two histories.

_Message Broker Patterns:_ Let Dapr abstract Redis, Kafka, RabbitMQ, Azure Service Bus/Event Hubs, or other brokers through components. Pick broker-native capabilities only when the component supports the semantics you need.

_CQRS Patterns:_ Agents should call commands for mutations and queries/projections for reads. Memory retrieval and projection reads are tools, not backdoors into write state.

_Source:_ https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/; https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-cloudevents/; https://docs.dapr.io/developing-ai/dapr-agents/dapr-agents-core-concepts/; https://docs.dapr.io/reference/api/pubsub_api/

### Integration Security Patterns

Security must be layered because agent systems combine user input, LLM output, remote tools, workflows, and service calls:

- **User/API layer:** Authenticate users at ingress, resolve tenant/user context server-side, and pass only scoped context into agent runs.
- **Service layer:** Use Dapr mTLS and App ID identities between sidecars. Dapr Sentry issues workload identities and mTLS is enabled/configurable through Dapr configuration.
- **Invocation authorization:** Configure Dapr service invocation access control to restrict which app IDs can call which endpoints/verbs. Use WorkflowAccessPolicy for cross-app workflow/activity scheduling.
- **Tool authorization:** For regular MCP, use Dapr service invocation policies, OAuth/bearer middleware, and MCP server-side authorization. For high-risk tools, use Dapr `MCPServer` with before/after workflow hooks for argument-level RBAC, PII redaction, and audit.
- **Agent Framework safeguards:** Use tool approval/human-in-the-loop for sensitive function tools and MCP tool calls. Treat third-party MCP servers and non-Azure model providers as explicit data-sharing risks.
- **Credential handling:** Use Dapr secret stores, OAuth2 client credentials, SPIFFE workload identity, or managed identity patterns. Do not pass API keys through Dapr Conversation metadata or agent prompts.

_OAuth 2.0 and JWT:_ Use OAuth/JWT bearer middleware at ingress and for MCP servers where user-delegated or service-delegated authorization is required. Dapr `MCPServer` supports OAuth2 client credentials for outbound MCP calls.

_API Key Management:_ Prefer secret stores and managed identities. Dapr Conversation docs warn that request metadata is not for API keys; provider credentials belong in component YAML or referenced secrets.

_Mutual TLS:_ Use Dapr mTLS/SPIFFE identities for service-to-service authentication and access policies for app-level authorization.

_Data Encryption:_ Use TLS/mTLS in transit, secret stores for credentials, state-store encryption where supported, and domain-level encryption/redaction for sensitive tenant data.

_Source:_ https://docs.dapr.io/concepts/security-concept/; https://docs.dapr.io/operations/configuration/invoke-allowlist/; https://docs.dapr.io/developing-ai/mcp/; https://docs.dapr.io/developing-ai/mcp/mcp-server-resource/; https://learn.microsoft.com/en-us/agent-framework/agents/tools/local-mcp-tools; https://learn.microsoft.com/en-us/agent-framework/agents/tools/

## Architectural Patterns and Design

### System Architecture Patterns

The strongest architecture for this topic is a **hybrid polyglot agent platform**, not a single shared runtime. Dapr Agents and Microsoft Agent Framework overlap in agent/workflow terminology, but they optimize for different boundaries. Dapr Agents is a Python framework that is now v1.0 and production-ready in Dapr's documentation; its `DurableAgent` pattern is workflow-backed and uses Dapr Workflows for long-running, fault-tolerant, durable execution. Microsoft Agent Framework is the primary .NET/Python abstraction for agents, model providers, tools, sessions, middleware, graph workflows, and workflow-as-agent composition. The hybrid should keep both, but assign ownership precisely.

Recommended component pattern:

- `.NET AgentHost`: hosts Microsoft Agent Framework agents, workflow agents, APIs/BFF endpoints, tenant-aware orchestration, session persistence adapters, A2A clients, and MCP clients.
- `Python DaprAgentWorker`: hosts Dapr Agents `DurableAgent` implementations and `AgentRunner` entry points for durable autonomous tasks, pub/sub activation, and Dapr-native memory/tool integration.
- `ToolHost`: exposes Hexalith capabilities through MCP servers, Agent Framework function tools, or Dapr service-invoked APIs.
- `PolicyHost`: centralizes tool authorization, argument-level RBAC, redaction, approval, and audit hooks, especially for high-risk MCP calls.
- Domain services: keep command/query ownership, validation, EventStore persistence, tenant isolation, and business invariants outside agent memory.
- Dapr sidecars/components: provide state, workflow, actors, pub/sub, service invocation, secrets, resiliency, observability, optional Conversation API, and optional `MCPServer` resources.

Use Microsoft Agent Framework when the problem is a .NET-first agent composition problem: typed model/provider integration, multi-turn agent sessions, workflow-as-agent composition, tool approval, MCP client integration, A2A remote-agent wrapping, and explicit graph-based orchestration. Use Dapr Agents when the problem is a Dapr-native durable autonomous agent problem: long-running agent tasks, headless agents triggered by REST/pub-sub, stateful recovery, Python ecosystem integrations, and direct access to Dapr Agents memory/workflow patterns. Use Dapr Workflow directly when the problem is a deterministic long-running business process rather than an autonomous agent.

The key trade-off is double orchestration. A task should have one durable owner. If Microsoft Agent Framework owns the graph, Dapr should supply infrastructure and durable service boundaries. If Dapr Agents owns the autonomous task, Agent Framework should call it as a remote service/agent/tool and should not reimplement its internal steps.

_Source:_ https://docs.dapr.io/developing-ai/dapr-agents/; https://docs.dapr.io/developing-ai/dapr-agents/dapr-agents-core-concepts/; https://docs.dapr.io/developing-ai/dapr-agents/dapr-agents-patterns/; https://github.com/dapr/dapr-agents; https://learn.microsoft.com/en-us/agent-framework/overview/; https://learn.microsoft.com/en-us/agent-framework/workflows/as-agents

### Design Principles and Best Practices

The architecture should be governed by a small set of invariants:

- Agents do not own durable business truth. Domain events and command handlers own business state; agent sessions, memories, and workflow history are execution/supporting state.
- Every side-effecting tool call is idempotent, authorized, auditable, and correlated to tenant, user, workflow instance, and agent run.
- Workflow orchestration is deterministic. Model calls, network calls, random choices, current time reads, and domain mutations belong in workflow activities, tools, or service calls.
- Dapr pub/sub and Dapr Workflow activity execution must be treated as at-least-once. Dedupe keys and idempotency keys are part of the tool contract, not an implementation afterthought.
- Tenant and user context are resolved server-side and passed as signed/scoped execution context. Agents never infer authorization from prompt text.
- Memory is not a database. Retrieval memory may inform an agent, but accepted facts and mutations go through domain commands and event-sourced projections.
- Tool surfaces are least-privilege by default. High-risk tools require policy checks, human approval, or durable MCP guardrails.

Architecturally, use a **ports-and-adapters** shape. Agent Framework and Dapr Agents are adapters at the application boundary, not the domain model. Dapr sidecar APIs are infrastructure ports. MCP is a tool port. A2A is a remote-agent port. Domain commands/queries are application ports. This keeps Hexalith's core model stable while agent/runtime libraries continue to evolve.

The decision table should be explicit in ADRs:

- Same-process typed function: Agent Framework function tool.
- Reusable tool across processes/frameworks: MCP over Dapr service invocation.
- Durable, policy-heavy, or argument-aware tool execution: Dapr `MCPServer`.
- Remote agent owned by another service/team/language: A2A, wrapped by `A2AAgent` in Agent Framework.
- Long-running deterministic business process: Dapr Workflow or Agent Framework workflow, but only one is the owner.
- Long-running autonomous durable agent task: Dapr Agents `DurableAgent`.

_Source:_ https://learn.microsoft.com/en-us/agent-framework/overview/; https://learn.microsoft.com/en-us/agent-framework/workflows/workflows; https://learn.microsoft.com/en-us/agent-framework/agents/tools/local-mcp-tools; https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-features-concepts/; https://docs.dapr.io/developing-ai/mcp/; https://docs.dapr.io/developing-ai/mcp/mcp-server-resource/

### Scalability and Performance Patterns

Scale the system horizontally by service role, not by agent abstraction. The `.NET AgentHost`, Python `DaprAgentWorker`, MCP/tool hosts, domain services, and policy services should each scale independently. Dapr Workflows run through Dapr sidecars and use actors internally; workflow actors store state in the configured actor state store. Dapr's workflow architecture appends workflow history incrementally, so state-store selection and payload size directly affect throughput and cost.

Dapr Agents' public README claims a scale-to-zero actor-backed model, with dormant agents reclaimed while retaining state, and claims thousands of agents can run on demand on a single core under its stated conditions. Treat that as a useful architectural signal, not a capacity promise for this system. Validate with load tests using the chosen state store, broker, model provider, tool set, payload sizes, and tenancy model.

Performance patterns:

- Keep orchestration payloads small. Pass IDs and state references, not full transcripts or large documents.
- Split long-running or high-cardinality workflows with child workflows and continue-as-new style patterns where the Dapr SDK supports them, so histories do not grow without bound.
- Avoid spawning Dapr `MCPServer` child workflows for every trivial tool call. Use the service invocation MCP path for ordinary tool calls and reserve `MCPServer` for durable, audited, policy-heavy calls.
- Bound model/tool concurrency per tenant, agent type, model deployment, and tool host. Agent workloads otherwise create bursty fan-out against models and downstream systems.
- Use read models, retrieval services, and memory projections instead of pushing full business history into prompts.
- Prefer in-process Agent Framework tools only when same-process ownership is clear. Use MCP/Dapr service invocation when independent scale, versioning, observability, or governance matters.
- Measure workflow state-store write volume. Workflow history signing can improve integrity, but adds stored signature/certificate entries and larger state transactions.

Agent Framework workflows use a superstep execution model with synchronization barriers. That is useful for deterministic graph reasoning and checkpointing, but fan-out branches can block at superstep boundaries. For independent long-running parallel branches, consolidate local sequential work into executors or move independent work into separate durable workflows/tasks.

_Source:_ https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-architecture/; https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-features-concepts/; https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-history-signing/; https://github.com/dapr/dapr-agents; https://learn.microsoft.com/en-us/agent-framework/workflows/workflows; https://docs.dapr.io/developing-ai/mcp/

### Integration and Communication Patterns

Use a **boundary-specific communication pattern**:

- In-process agent/tool composition: Microsoft Agent Framework agents-as-tools and function tools.
- Cross-process tool integration: MCP through Dapr service invocation.
- Durable/policy-heavy tool integration: Dapr `MCPServer`, where each tool call is driven through Dapr Workflow.
- Cross-service synchronous calls: Dapr service invocation over HTTP/gRPC.
- Asynchronous coordination: Dapr pub/sub with CloudEvents.
- Remote-agent interoperability: A2A wrapped as Agent Framework `A2AAgent`.
- Durable task lifecycle: Dapr Workflow APIs or Dapr Agents `AgentRunner` endpoints.

The recommended flow for a complex agent request is:

1. API/BFF authenticates the user, resolves tenant/user context, and starts an agent task.
2. `.NET AgentHost` selects the orchestration owner: Agent Framework workflow, Dapr Workflow, or Dapr Agents `DurableAgent`.
3. The owner calls tools through local function tools, MCP, Dapr service invocation, or Dapr `MCPServer` based on the tool's risk/durability profile.
4. Domain mutations become commands against domain services; domain events are persisted and projected.
5. Progress/events are published as CloudEvents with workflow/run correlation IDs.
6. Human input, approval, or cancellation is raised as workflow input, Agent Framework external input, or a task API call.

A2A should not replace all internal calls. It is valuable when a remote agent is independently deployed or owned, and Agent Framework's `A2AAgent` can wrap A2A endpoints as `AIAgent` instances with streaming and background continuation-token support. For internal deterministic workflows, Dapr service invocation or direct MCP is simpler and more inspectable.

_Source:_ https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/; https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/; https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-cloudevents/; https://docs.dapr.io/developing-ai/mcp/; https://docs.dapr.io/developing-ai/mcp/mcp-server-resource/; https://learn.microsoft.com/en-us/agent-framework/agents/providers/agent-to-agent; https://learn.microsoft.com/en-us/agent-framework/workflows/as-agents

### Security Architecture Patterns

Security must be policy-first because agents turn user input into tool and service calls. The baseline security pattern is **identity at every boundary, authorization before every action, and audit after every material decision**.

Recommended layers:

- Ingress authentication: OAuth/OIDC/JWT at API/BFF or gateway.
- Tenant context: resolved by trusted server code and attached to agent runs as scoped execution context.
- Service identity: Dapr mTLS/SPIFFE identities between Daprized services.
- Service authorization: Dapr service invocation access control on called services.
- Workflow authorization: Dapr `WorkflowAccessPolicy` for cross-app workflow/activity scheduling. Dapr docs explicitly separate this from service invocation ACLs.
- Tool authorization: MCP server auth plus Dapr access control for normal MCP; Dapr `MCPServer` hooks for argument-level RBAC, redaction, rate limits, response filtering, and audit.
- Human approval: Agent Framework tool approval and workflow external-input requests for destructive, financial, legal, administrative, or cross-tenant actions.
- Secrets: Dapr secret stores, managed identity, OAuth2 client credentials, or workload identity. No secrets in prompts, Dapr Conversation metadata, or untrusted tool arguments.
- State protection: secure state stores/checkpoint stores as trusted infrastructure. Microsoft's checkpoint docs call checkpoint storage a trust boundary; Dapr workflow history signing can add integrity protection for workflow histories.

Threats to design against:

- Prompt injection causing over-broad tool use.
- Tool result injection changing future instructions.
- Cross-tenant memory leakage.
- Replay/duplicate command execution from at-least-once delivery.
- Remote MCP/A2A data exfiltration.
- Unauthorized workflow/activity scheduling.
- Tampered workflow/checkpoint state.

Fail closed on missing tenant context, missing tool authorization, unknown MCP server identity, invalid workflow policy, or unverifiable persisted execution state.

_Source:_ https://docs.dapr.io/concepts/security-concept/; https://docs.dapr.io/operations/configuration/invoke-allowlist/; https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-history-signing/; https://docs.dapr.io/developing-ai/mcp/; https://docs.dapr.io/developing-ai/mcp/mcp-server-resource/; https://learn.microsoft.com/en-us/agent-framework/agents/tools/local-mcp-tools; https://learn.microsoft.com/en-us/agent-framework/workflows/checkpoints

### Data Architecture Patterns

Keep four data planes separate:

1. **Domain data plane:** EventStore streams, domain commands, projections, tenant metadata, and business read models. This is the source of truth.
2. **Workflow data plane:** Dapr Workflow/Dapr Agents workflow history, actor state, workflow instance status, activity records, and optional signed history. This is execution truth.
3. **Agent state plane:** Agent Framework sessions, Dapr Agents memory, short-term conversation state, pending approvals, and resumed agent sessions. This is interaction continuity.
4. **Knowledge/retrieval plane:** vector indexes, documents, embeddings, semantic memories, Hexalith.Memories capabilities, and search projections. This is retrieval support.

Do not mix these stores. A workflow history entry is not a domain event. A chat transcript is not an audit log. A vector memory hit is not accepted domain truth. A tool call audit record should reference domain IDs, workflow IDs, agent run IDs, tenant IDs, and tool call IDs rather than copying sensitive payloads.

For storage choices:

- Dapr Workflow and Dapr Agents require a workflow/actor-capable state store. Validate actor support, payload limits, consistency, backup, retention, and operational maturity.
- Agent Framework checkpointing can use in-memory, file, or Cosmos DB storage depending on language/runtime and deployment needs; production distributed workloads need trusted durable storage, not process memory.
- Hexalith domain data should continue through existing event-sourced repositories and projections.
- Memory and retrieval should be exposed as a service/tool so both .NET Agent Framework and Python Dapr Agents can use the same governed memory boundary.

Use correlation metadata consistently: `tenantId`, `userId`, `agentRunId`, `workflowInstanceId`, `conversationId`, `toolCallId`, `domainCorrelationId`, and `causationId`. This makes traces, audits, dedupe, and human review possible across the hybrid.

_Source:_ https://docs.dapr.io/developing-applications/building-blocks/state-management/state-management-overview/; https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-architecture/; https://docs.dapr.io/developing-ai/dapr-agents/dapr-agents-core-concepts/; https://docs.dapr.io/developing-ai/dapr-agents/dapr-agents-getting-started/; https://learn.microsoft.com/en-us/agent-framework/workflows/as-agents; https://learn.microsoft.com/en-us/agent-framework/workflows/checkpoints

### Deployment and Operations Architecture

Deploy the hybrid as sidecar-enabled services with explicit component ownership:

- Local: Aspire AppHost starts .NET services, optional Python workers, Dapr sidecars, Redis/state stores, pub/sub, model adapters, MCP servers, and test dependencies.
- Production: Kubernetes or Azure Container Apps runs each app with a Dapr sidecar. Dapr components define state stores, pub/sub brokers, secret stores, resiliency policies, Conversation components, and MCPServer resources.
- Configuration: centralize Dapr component YAML, resiliency specs, access-control configs, WorkflowAccessPolicy resources, and MCPServer declarations by environment.
- Versioning: pin .NET packages through Central Package Management and Python worker dependencies through lock files. Treat Dapr runtime, Dapr .NET SDK, Dapr Agents, and Microsoft Agent Framework as separately versioned dependencies.
- Observability: OpenTelemetry traces, logs, and metrics should span API request, agent run, workflow instance, tool call, domain command, pub/sub message, and model call. Dapr service invocation, workflow, and pub/sub add useful correlation, but application logs still need domain IDs and tenant IDs.
- Operations: expose start/query/cancel/resume/raise-event surfaces for long-running tasks. Keep purge/terminate/admin operations behind privileged tools or administrative APIs.
- Testing: add contract tests for MCP schemas and A2A cards, replay/idempotency tests for workflows/activities, integration tests with Dapr sidecars, and load tests around state-store workflow writes and model/tool fan-out.

Operationally, Dapr Conversation API should be treated as an optional adapter because it is still documented as alpha in current Dapr docs. Dapr Agents v1.0 and Dapr Workflow can be part of the main architecture, while Conversation should sit behind an interface so provider routing, caching, PII scrubbing, and prompt middleware can be replaced without touching domain or agent orchestration code.

_Source:_ https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview/; https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-architecture/; https://docs.dapr.io/operations/resiliency/resiliency-overview/; https://docs.dapr.io/developing-applications/building-blocks/conversation/conversation-overview/; https://docs.dapr.io/developing-ai/dapr-agents/; https://learn.microsoft.com/en-us/agent-framework/overview/; https://learn.microsoft.com/en-us/agent-framework/workflows/

## Implementation Approaches and Technology Adoption

### Technology Adoption Strategies

Adopt the hybrid incrementally. The first production slice should be a .NET `AgentHost` using Microsoft Agent Framework for agent/workflow composition, with Dapr sidecars enabled from the start for state, service invocation, pub/sub, secrets, resiliency, and observability. This lets the Hexalith .NET platform establish stable agent APIs, tenant enforcement, MCP/tool boundaries, and audit behavior before adding another runtime.

The second slice should expose Hexalith capabilities as governed tools. Start with Agent Framework function tools for same-process operations and MCP over Dapr service invocation for reusable cross-process tools. Only introduce Dapr `MCPServer` resources for high-risk or policy-heavy tools that need argument-level RBAC, redaction, audit hooks, per-tool observability, or durable tool execution. Dapr docs explicitly state that the ordinary service invocation path is the default MCP path and `MCPServer` is the workflow-centric path.

The third slice should introduce Python Dapr Agents workers for workloads that specifically need `DurableAgent`: long-running autonomous tasks, headless REST/pub-sub triggered agents, durable recovery of LLM/tool steps, or Python ecosystem integrations. Do not move all agent logic into Python only because Dapr Agents exists. Keep the primary platform .NET-first and use Dapr as the runtime boundary.

Recommended adoption sequence:

1. Stand up Aspire + Dapr local topology with a .NET AgentHost and a minimal model provider.
2. Implement one low-risk Agent Framework workflow with explicit session persistence and observability.
3. Convert one domain capability into an MCP/tool contract with tenant-aware command/query handlers.
4. Add Dapr Workflow for one deterministic long-running process or Dapr Agents `DurableAgent` for one autonomous durable task.
5. Add governance: access policies, workflow policies, approval gates, evaluation datasets, and audit reports.
6. Harden production: Dapr HA control plane, resiliency specs, mTLS, secrets, backups, dashboards, and load tests.

_Source:_ https://learn.microsoft.com/en-us/agent-framework/overview/; https://docs.dapr.io/developing-ai/dapr-agents/; https://docs.dapr.io/developing-ai/dapr-agents/dapr-agents-getting-started/; https://docs.dapr.io/developing-ai/mcp/mcp-server-resource/; https://aspire.dev/integrations/frameworks/dapr/

### Development Workflows and Tooling

The development workflow should be service-topology-first. Use Aspire AppHost for the local graph, Dapr sidecars, state stores, pub/sub, model adapters, MCP servers, and diagnostics. The Aspire Dapr integration adds Dapr sidecars to Aspire resources and can wire state store, pub/sub, and component resources. It requires the Dapr CLI locally and currently documents C# AppHost usage.

For .NET code, use central package management and pin the Agent Framework, Dapr SDK, Dapr Workflow, Dapr.Testcontainers, MCP, and provider packages deliberately. For Python Dapr Agents workers, use `uv` and lock files because the Dapr Agents quickstart uses a Python virtual environment and `uv sync`. Keep Python workers in dedicated service folders with their own lifecycle, Dockerfile, tests, and dependency lock.

For tool contracts, treat MCP definitions as public API. Version tool names, input schemas, output shapes, authorization requirements, idempotency semantics, and audit fields. Tool contracts should go through review like REST/gRPC contracts because agent failures often come from ambiguous tool names, weak descriptions, under-specified schemas, or unbounded side effects.

The minimum repository additions for implementation should be:

- `AgentHost` .NET service with Microsoft Agent Framework configuration, Dapr clients, MCP/A2A adapters, observability setup, and session persistence.
- Optional `DaprAgentWorker` Python service with `DurableAgent`, `AgentRunner`, Dapr component dependencies, and health endpoints.
- `ToolHost` service or modules exposing MCP tools backed by domain commands/queries.
- Dapr `components/`, `resiliency/`, `configuration/`, `workflowaccesspolicy/`, and optional `mcpserver/` manifests.
- Integration-test infrastructure using Dapr.Testcontainers for .NET and isolated worker tests for Python.

_Source:_ https://aspire.dev/integrations/frameworks/dapr/; https://docs.dapr.io/developing-ai/dapr-agents/dapr-agents-getting-started/; https://docs.dapr.io/developing-applications/sdks/dotnet/; https://learn.microsoft.com/en-us/agent-framework/agents/tools/local-mcp-tools; https://docs.dapr.io/developing-ai/mcp/mcp-server-resource/

### Testing and Quality Assurance

Testing needs to cover conventional software quality and agent-specific behavior. Unit tests should validate domain commands, tool adapters, authorization decisions, prompt/template construction, workflow routing decisions, idempotency key creation, and error mapping. They should not call live models by default.

Integration tests should use real Dapr sidecars for state, workflow, pub/sub, jobs, distributed locks, and conversation paths. Dapr's .NET SDK documents `Dapr.Testcontainers` as a helper for writing integration tests against real Dapr runtime components using containers. It can start sidecars, placement, scheduler, and building-block infrastructure, and the package currently warns that the API is still evolving. That makes it appropriate for integration coverage, but version pinning and wrapper helpers are important.

Workflow tests must check replay and duplicate execution behavior. Dapr Workflow activities are at-least-once, so tests should prove side-effecting activities are idempotent. Agent Framework workflow tests should validate graph connectivity, superstep behavior, checkpoint persistence, checkpoint restore, external input handling, and failure paths. Dapr Agents worker tests should validate that a `DurableAgent` run survives worker restart and that tool calls resume or dedupe correctly.

Agent quality tests should include:

- Golden-path task completion tests.
- Tool-call accuracy tests.
- Prompt injection and tool-result injection tests.
- Tenant-boundary and memory-leakage tests.
- Human approval and rejection tests.
- Safety and refusal behavior tests.
- Cost and latency regression tests.

Microsoft Foundry observability/evaluation guidance frames evaluation, monitoring, and tracing as lifecycle capabilities and includes built-in evaluators for quality, RAG, safety/security, and agent-specific metrics such as tool-call accuracy and task completion. Use that as the quality-gate model even if some tests run outside Foundry.

_Source:_ https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-guidance/dotnet-guidance-testcontainers/; https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-features-concepts/; https://learn.microsoft.com/en-us/agent-framework/workflows/checkpoints; https://learn.microsoft.com/en-us/agent-framework/workflows/workflows; https://learn.microsoft.com/en-us/azure/foundry/concepts/observability

### Deployment and Operations Practices

Production should use sidecar-enabled deployment with explicit Dapr operational hardening. On Kubernetes, Dapr production guidance recommends HA mode for the control plane, creating three replicas of control-plane pods so the control plane can survive individual node failures and outages. In Azure Container Apps or Kubernetes, each agent/tool/domain service should run with its own Dapr app ID and scoped components.

Operational manifests should be environment-owned, not embedded in agent code:

- Dapr components for state, workflow/actor state, pub/sub, secrets, Conversation providers, and bindings.
- Dapr resiliency specs for timeouts, retries, and circuit breakers targeting apps, components, and actors.
- Dapr service invocation access-control configuration.
- Dapr `WorkflowAccessPolicy` for cross-app workflow/activity scheduling.
- Dapr `MCPServer` resources for durable/policy-heavy tool servers.
- OpenTelemetry collector/exporter configuration.

Observability should correlate across API request, agent session, workflow instance, model call, tool call, domain command, pub/sub event, and Dapr sidecar operation. Dapr can emit tracing through OpenTelemetry/Zipkin and propagates W3C trace context through Dapr calls. Agent Framework emits traces, logs, and metrics according to OpenTelemetry GenAI semantic conventions. These two telemetry planes need shared correlation identifiers in application logs and spans.

Runbooks should cover workflow stuck/running states, sidecar readiness, pub/sub retry storms, state-store latency, model-provider throttling, MCP server failures, approval queues, and tenant-isolation incidents. Admin operations such as workflow purge, terminate, retry, or tool-disable should require privileged access and audit logging.

_Source:_ https://docs.dapr.io/operations/hosting/kubernetes/kubernetes-production/; https://docs.dapr.io/operations/resiliency/resiliency-overview/; https://docs.dapr.io/concepts/observability-concept/; https://learn.microsoft.com/en-us/agent-framework/agents/observability; https://docs.dapr.io/concepts/security-concept/

### Team Organization and Skills

This hybrid needs clear ownership because runtime, agent behavior, domain correctness, and governance failures can look similar from the outside.

Recommended ownership model:

- Platform/runtime team: Aspire topology, Dapr runtime, sidecars, components, resiliency, deployment, secrets, observability, package governance, and local developer experience.
- Agent application team: Microsoft Agent Framework agents/workflows, Dapr Agents workers, model-provider adapters, prompt/tool selection logic, session handling, and agent UX/API flows.
- Domain/service team: command/query handlers, EventStore contracts, projections, domain validation, and tool-backed business capabilities.
- Security/governance team: tenant isolation, Dapr access policies, MCP approval policies, redaction, audit, responsible AI policies, and incident response.
- QA/evaluation team: deterministic tests, Dapr integration tests, model evaluation datasets, adversarial testing, safety regression, and release gates.

Required skills:

- .NET 10, ASP.NET Core, Microsoft Agent Framework, Microsoft.Extensions.AI, OpenTelemetry, xUnit v3, and Dapr .NET SDK.
- Dapr sidecars, state, workflow, pub/sub, service invocation, actors, resiliency, mTLS, access control, secrets, and Testcontainers.
- Python 3.11+, `uv`, Dapr Agents `DurableAgent`, `AgentRunner`, and Python service deployment for optional worker services.
- MCP and A2A protocols, including schema design, server/client hosting, authentication, and versioning.
- Responsible AI evaluation, human-in-the-loop design, prompt/tool injection defense, and production observability.

Microsoft's AI agent governance guidance recommends responsible AI policies that integrate with existing governance workflows rather than creating a parallel bureaucracy, with clear ownership, audits, incident response, and formal approvals for high-risk agents. That maps directly to this operating model.

_Source:_ https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ai-agents/responsible-ai-across-organization; https://learn.microsoft.com/en-us/agent-framework/overview/; https://docs.dapr.io/developing-ai/dapr-agents/; https://docs.dapr.io/developing-applications/sdks/dotnet/; https://docs.dapr.io/developing-ai/mcp/

### Cost Optimization and Resource Management

Cost control must be designed into the agent runtime. The expensive resources are model tokens, tool fan-out, workflow state writes, state-store reads/writes, vector retrieval, telemetry volume, and human review queues.

Practical controls:

- Keep prompts bounded with retrieval summaries, projection reads, and memory compaction.
- Track token usage, model, tool count, workflow state writes, and elapsed time per `agentRunId` and tenant.
- Apply per-tenant rate limits and budget ceilings before model/tool invocation.
- Use Dapr Conversation behind an adapter because the Conversation API is still alpha in current Dapr docs, even though it offers useful caching, response formatting, token usage, PII obfuscation, and tool-calling capabilities.
- Use Dapr Conversation response caching and prompt caching only for safe, non-sensitive, deterministic-enough interactions where cache semantics are acceptable.
- Prefer normal MCP/service invocation for low-risk tools; reserve Dapr `MCPServer` for durable or governed tools because each tool call becomes workflow-backed work.
- Store references, not large payloads, in workflow state and agent session state.
- Use cheaper models for classification, routing, summarization, and validation where quality tests prove they are sufficient.

The Dapr .NET AI client guidance recommends long-lived `DaprConversationClient` instances because they hold networking resources and are thread-safe. Apply the same principle broadly: reuse clients, connection pools, MCP transports, and model-provider clients rather than creating one per tool invocation.

_Source:_ https://docs.dapr.io/developing-applications/building-blocks/conversation/conversation-overview/; https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-ai/dotnet-ai-conversation-usage/; https://docs.dapr.io/developing-ai/mcp/mcp-server-resource/; https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-architecture/; https://learn.microsoft.com/en-us/azure/foundry/concepts/observability

### Risk Assessment and Mitigation

Primary risks and mitigations:

- **Double orchestration:** Agent Framework workflow and Dapr DurableAgent both try to own the same task. Mitigate with an ADR rule that every task has one durable owner.
- **Duplicate side effects:** Dapr Workflow activities and pub/sub handlers run under at-least-once assumptions. Mitigate with idempotency keys, dedupe stores, and command-side idempotency.
- **Cross-tenant leakage:** Agent memory, tool arguments, retrieval, or logs leak tenant data. Mitigate with server-resolved tenant context, tenant-scoped stores, tenant-filtered tools, redaction, and test cases.
- **Prompt/tool injection:** User or tool output causes unauthorized tool calls. Mitigate with least-privilege tools, structured tool schemas, approval gates, MCP hooks, policy checks, and output validation.
- **Package/runtime churn:** Agent Framework, Dapr Agents, Dapr Conversation, and Dapr `MCPServer` are evolving quickly. Mitigate with version pinning, adapter layers, small production slices, and upgrade rehearsals.
- **Checkpoint/state trust:** Agent checkpoints, workflow history, or memory are treated as trusted inputs without protection. Mitigate by securing checkpoint stores, using workflow history signing where justified, and never loading untrusted checkpoint data.
- **Operational opacity:** Agents fail with no traceable reason. Mitigate with OpenTelemetry GenAI tracing, Dapr traces, tool-call logs, model usage metrics, workflow instance IDs, and audit records.
- **Overuse of durable MCP:** Every tool call becomes a workflow, raising cost and latency. Mitigate with the decision rule: service invocation path by default, `MCPServer` only for high-risk or durable tool calls.

Microsoft's 2026 guidance on trustworthy agents emphasizes a closed loop: evaluate against policy, apply controls at failure checkpoints, re-run evaluations, observe behavior, and improve. That should be the release-management loop for this hybrid.

_Source:_ https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-features-concepts/; https://docs.dapr.io/developing-ai/mcp/mcp-server-resource/; https://learn.microsoft.com/en-us/agent-framework/agents/tools/tool-approval; https://learn.microsoft.com/en-us/agent-framework/workflows/human-in-the-loop; https://learn.microsoft.com/en-us/azure/foundry/concepts/safety-evaluations-transparency-note; https://devblogs.microsoft.com/foundry/build-2026-open-trust-stack-ai-agents/

## Technical Research Recommendations

### Implementation Roadmap

1. **Foundation:** Add Aspire+Dapr topology, Dapr components, resiliency specs, OpenTelemetry, and a .NET AgentHost.
2. **First agent:** Implement one Agent Framework workflow-as-agent with persisted session state and one read-only tool.
3. **Tool platform:** Expose domain commands/queries as MCP/function tools with tenant context, idempotency, audit, and approval metadata.
4. **Durable execution:** Add Dapr Workflow for deterministic long-running processes and one Python Dapr Agents `DurableAgent` for an autonomous durable task.
5. **Governance:** Add service invocation ACLs, WorkflowAccessPolicy, MCP approval/redaction/audit hooks, and responsible AI evaluation datasets.
6. **Production hardening:** Add Dapr HA control plane, backups, dashboards, runbooks, load tests, chaos/restart tests, and cost budgets.

### Technology Stack Recommendations

Use .NET 10 and Microsoft Agent Framework as the primary agent application layer. Use Dapr runtime, Dapr .NET SDK, Dapr Workflow, Dapr.Testcontainers, Dapr state/pub-sub/service invocation/secrets/resiliency/observability, and Aspire for local orchestration. Add Python Dapr Agents only for bounded worker services that need `DurableAgent`. Use MCP as the primary tool protocol and A2A only for remote agent boundaries. Keep EventStore/domain services as the source of business truth.

### Skill Development Requirements

Prioritize training in Dapr Workflow determinism/idempotency, Microsoft Agent Framework workflows/sessions/tools, MCP schema and security design, OpenTelemetry GenAI tracing, Dapr production operations, prompt/tool injection defense, responsible AI evaluation, and Python Dapr Agents worker operations.

### Success Metrics and KPIs

Track:

- Task completion rate and human escalation rate.
- Tool-call accuracy, approval rejection rate, and policy-block rate.
- Duplicate side-effect incidents and idempotency failures.
- Workflow recovery success after restart/failure.
- Cross-tenant leakage incidents: target zero.
- P95/P99 agent latency and workflow duration.
- Token cost, tool cost, and workflow state-store writes per successful task.
- Evaluation pass rate for safety, groundedness, relevance, and tool usage.
- Trace coverage: percentage of agent runs with correlated API, workflow, model, tool, and domain spans.
- Mean time to diagnose failed/stuck agent runs.

_Source:_ https://docs.dapr.io/developing-ai/dapr-agents/dapr-agents-core-concepts/; https://docs.dapr.io/operations/hosting/kubernetes/kubernetes-production/; https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-guidance/dotnet-guidance-testcontainers/; https://learn.microsoft.com/en-us/agent-framework/agents/observability; https://learn.microsoft.com/en-us/azure/foundry/concepts/observability

---

# Durable Agent Platforms with Dapr AI Agents and Microsoft Agent Framework: Comprehensive Technical Research

## Executive Summary

The right implementation model is a **hybrid durable agent platform**: Microsoft Agent Framework provides the .NET agent/workflow programming model, while Dapr Agents provides Dapr-native durable autonomous agents where that specific runtime is justified. Dapr is the infrastructure boundary shared by both: sidecars, workflow, actors, state, pub/sub, service invocation, resiliency, security, secrets, and observability. This division avoids the main failure mode of hybrid agent stacks: double orchestration where two frameworks both attempt to own the same long-running task.

Dapr Agents is now documented as v1.0 and production-ready, with `DurableAgent` backed by Dapr Workflows for persistent, fault-tolerant execution. Microsoft Agent Framework is documented as a production agent and multi-agent workflow framework for .NET and Python, with tools, sessions, providers, MCP, A2A, middleware, telemetry, and graph workflows. The strategic implication is clear: use Microsoft Agent Framework for the .NET application surface and use Dapr Agents as a bounded Python durable worker runtime, not as a replacement for the existing .NET platform.

For Hexalith, the implementation should start with `.NET AgentHost + Dapr sidecars + governed MCP tools`. Add Python Dapr `DurableAgent` workers only when a workload needs Dapr Agents' first-class durable autonomous execution. Keep durable business truth in Hexalith domain services and EventStore. Agent sessions, memories, workflow histories, and checkpoints are supporting execution state, not domain truth.

**Key Technical Findings:**

- Dapr Agents and Microsoft Agent Framework are complementary when each has a clear ownership boundary.
- A task should have one durable owner: Agent Framework workflow, Dapr Workflow, or Dapr Agents `DurableAgent`.
- MCP is the primary reusable tool boundary; A2A is the remote-agent boundary; Dapr service invocation is the service boundary; Dapr pub/sub CloudEvents are the event boundary.
- Dapr `MCPServer` should be reserved for durable, policy-heavy tool calls because each tool call becomes workflow-backed execution.
- Domain commands and EventStore remain the business source of truth; agent memory and workflow history must not become hidden databases.

**Technical Recommendations:**

- Build the first production slice in .NET with Microsoft Agent Framework and Dapr sidecars.
- Standardize tool contracts through MCP/function tools with tenant context, idempotency keys, authorization, and audit fields.
- Introduce Python Dapr Agents workers only for durable autonomous tasks where `DurableAgent` adds concrete value.
- Use Dapr Workflow directly for deterministic business workflows and Dapr Agents for autonomous durable agent workflows.
- Add governance early: service invocation ACLs, `WorkflowAccessPolicy`, human approval, MCP hooks, OpenTelemetry traces, and evaluation gates.

## Table of Contents

1. Technical Research Introduction and Methodology
2. Technical Landscape and Architecture Analysis
3. Implementation Approaches and Best Practices
4. Technology Stack Evolution and Current Trends
5. Integration and Interoperability Patterns
6. Performance and Scalability Analysis
7. Security and Compliance Considerations
8. Strategic Technical Recommendations
9. Implementation Roadmap and Risk Assessment
10. Future Technical Outlook and Innovation Opportunities
11. Technical Research Methodology and Source Verification
12. Technical Appendices and Reference Materials
13. Technical Research Conclusion

## 1. Technical Research Introduction and Methodology

### Technical Research Significance

Agent systems are moving from prototype chat loops to production workflows that must finish reliably, recover from failure, obey authorization boundaries, and explain what happened after the fact. Dapr Agents addresses this through Dapr's workflow-backed durable execution, state, messaging, and observability. Microsoft Agent Framework addresses the .NET/Python application programming model: agents, tools, providers, sessions, middleware, graph workflows, MCP, and A2A. A hybrid architecture matters because enterprise teams need both: durable distributed execution and a productive typed agent framework.

_Technical Importance:_ Durable agent execution is now a primary architectural requirement, not an optional enhancement. Dapr Agents' `DurableAgent` and Dapr Workflow capabilities address failure recovery, while Microsoft Agent Framework provides typed orchestration and ecosystem integration.

_Business Impact:_ The hybrid reduces platform lock-in and supports a .NET-first enterprise system while still allowing Python Dapr Agents workers where the Dapr-native agent runtime is strongest.

_Source:_ https://docs.dapr.io/developing-ai/dapr-agents/; https://docs.dapr.io/developing-ai/dapr-agents/dapr-agents-core-concepts/; https://learn.microsoft.com/en-us/agent-framework/overview/; https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0/

### Technical Research Methodology

The research used current web-verified source analysis across official Dapr documentation, Microsoft Learn, protocol specifications, package registries, GitHub repositories, production deployment guidance, and selected ecosystem announcements. Claims about fast-moving APIs were checked against primary sources where available.

- **Technical Scope:** Dapr Agents, Dapr Workflow, Dapr MCP/MCPServer, Dapr state/pub-sub/service invocation/security/resiliency/observability, Microsoft Agent Framework agents/workflows/tools/MCP/A2A/sessions/checkpoints, Aspire, testing, deployment, and governance.
- **Data Sources:** Dapr Docs, Microsoft Learn, Microsoft DevBlogs, GitHub, NuGet/PyPI, MCP/A2A documentation, Aspire docs, and Azure Foundry observability/evaluation guidance.
- **Analysis Framework:** Technology stack, integration patterns, architectural patterns, implementation adoption, risk assessment, and synthesis.
- **Time Period:** Current as of 2026-06-23.
- **Technical Depth:** Architecture and implementation guidance suitable for a .NET 10/Dapr/Aspire/EventStore platform.

### Technical Research Goals and Objectives

**Original Technical Goals:** Describe how to implement a hybrid agent architecture that uses Dapr for distributed runtime capabilities and Microsoft Agent Framework for agent abstractions, orchestration, model/tool integration, and lifecycle patterns.

**Achieved Technical Objectives:**

- Established a clear runtime ownership model for Dapr Agents, Dapr Workflow, and Microsoft Agent Framework.
- Identified MCP, A2A, Dapr service invocation, Dapr pub/sub, and domain commands as distinct integration boundaries.
- Produced a phased adoption roadmap that fits a .NET/Dapr/Aspire Hexalith architecture.
- Documented security, testing, observability, cost, scalability, and governance requirements.

## 2. Technical Landscape and Architecture Analysis

### Current Technical Architecture Patterns

The recommended architecture is a polyglot, sidecar-enabled agent platform:

- `.NET AgentHost` for Microsoft Agent Framework agents, workflows, sessions, tool orchestration, A2A, MCP clients, APIs, and tenant-aware control.
- Optional Python `DaprAgentWorker` services for Dapr Agents `DurableAgent` workloads.
- `ToolHost` services for MCP and function tool surfaces backed by domain commands/queries.
- `PolicyHost` for authorization, redaction, audit, approval, and Dapr `MCPServer` hooks.
- Dapr sidecars/components for workflow, state, pub/sub, service invocation, secrets, resiliency, mTLS, and telemetry.
- Domain services and EventStore as the durable business source of truth.

_Dominant Patterns:_ Sidecar runtime, workflow-backed durable agents, MCP tool boundary, A2A remote-agent boundary, event-driven coordination, and domain-command mutation.

_Architectural Evolution:_ The market is converging on agent frameworks plus durable execution plus open interoperability protocols. Dapr Agents emphasizes Dapr-native durability. Microsoft Agent Framework emphasizes application composition and multi-agent workflows.

_Architectural Trade-offs:_ Dapr Agents gives durable autonomous agent execution but is currently Python-centric. Microsoft Agent Framework gives stronger .NET integration but should not be treated as a replacement for Dapr's distributed runtime. Dapr `MCPServer` gives durable governed tool execution at the cost of workflow overhead.

_Source:_ https://docs.dapr.io/developing-ai/dapr-agents/dapr-agents-patterns/; https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-architecture/; https://learn.microsoft.com/en-us/agent-framework/workflows/as-agents; https://docs.dapr.io/developing-ai/mcp/mcp-server-resource/

### System Design Principles and Best Practices

The design should follow these invariants:

- One durable owner per task.
- Side-effecting tools must be idempotent, authorized, audited, and correlated.
- Workflow orchestration must stay deterministic; I/O and model calls belong in activities/tools.
- Agent memory is not business truth.
- Tenant context is resolved server-side and passed as scoped execution context.
- Tool permissions are least privilege by default.
- Evaluation, observability, and governance are release requirements, not afterthoughts.

_Design Principles:_ Ports-and-adapters, bounded contexts, explicit contracts, durable orchestration, least privilege, idempotency, and traceability.

_Best Practice Patterns:_ Use Agent Framework function tools in-process, MCP for reusable cross-process tools, Dapr `MCPServer` for durable governed tools, Dapr Workflow for deterministic long-running processes, Dapr Agents for autonomous durable agents, and A2A for remote agents.

_Architectural Quality Attributes:_ Reliability through durable workflows and idempotency; maintainability through adapter boundaries; scalability through sidecar-enabled independent services; observability through correlated traces and audits.

_Source:_ https://learn.microsoft.com/en-us/agent-framework/workflows/workflows; https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-features-concepts/; https://learn.microsoft.com/en-us/agent-framework/agents/tools/local-mcp-tools; https://docs.dapr.io/developing-ai/mcp/

## 3. Implementation Approaches and Best Practices

### Current Implementation Methodologies

The implementation should be phased rather than big-bang. Start with a minimal .NET AgentHost and Dapr sidecars, then add governed tool contracts, then durable workers and production hardening.

_Development Approaches:_ C#/.NET for platform and application orchestration; Python for optional Dapr Agents workers; Dapr YAML for deployable runtime resources; MCP schemas for tool contracts.

_Code Organization Patterns:_ Keep agent hosts, Dapr workers, tool hosts, policy hosts, and domain services separate. Avoid embedding domain mutations directly inside prompts or private agent memory.

_Quality Assurance Practices:_ Combine deterministic unit tests, MCP contract tests, Dapr.Testcontainers integration tests, workflow recovery tests, idempotency tests, and AI evaluation datasets.

_Deployment Strategies:_ Use Aspire locally; deploy Dapr-enabled services to Kubernetes or Azure Container Apps with sidecars, HA control plane, scoped components, resiliency specs, access control, and OpenTelemetry.

_Source:_ https://aspire.dev/integrations/frameworks/dapr/; https://docs.dapr.io/developing-ai/dapr-agents/dapr-agents-getting-started/; https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-guidance/dotnet-guidance-testcontainers/; https://docs.dapr.io/operations/hosting/kubernetes/kubernetes-production/

### Implementation Framework and Tooling

The implementation tooling should align with the existing Hexalith .NET/Dapr patterns:

- .NET 10, ASP.NET Core, Microsoft Agent Framework, Microsoft.Extensions.AI, Dapr.Client, Dapr.Workflow, Dapr.AspNetCore, and Dapr.Testcontainers.
- Aspire AppHost for local service topology and Dapr sidecars.
- Python 3.11+ and `uv` for optional Dapr Agents services.
- MCP C# SDK and Dapr MCP integration for tool boundaries.
- OpenTelemetry for traces, metrics, and logs.
- Foundry/Azure OpenAI/OpenAI/other providers behind Microsoft Agent Framework or Dapr Conversation adapters.

_Development Frameworks:_ Microsoft Agent Framework for .NET agent orchestration; Dapr Agents for Python durable autonomous agents; Dapr runtime for distributed building blocks.

_Tool Ecosystem:_ Aspire, Dapr CLI, Docker/Testcontainers, OpenTelemetry Collector, MCP tools, A2A clients, package lock/version management.

_Build and Deployment Systems:_ MSBuild/Central Package Management for .NET; Python lock files for Dapr Agents; CI gates for tests/evals; Kubernetes/ACA deployment manifests for Dapr components.

_Source:_ https://docs.dapr.io/developing-applications/sdks/dotnet/; https://learn.microsoft.com/en-us/agent-framework/overview/; https://github.com/dapr/dapr-agents; https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-ai/dotnet-ai-conversation-usage/

## 4. Technology Stack Evolution and Current Trends

### Current Technology Stack Landscape

Dapr Agents is GA as a Python framework for LLM-powered autonomous applications using Dapr distributed capabilities. Microsoft Agent Framework is available for .NET and Python and is positioned as the successor path that combines AutoGen-style orchestration with Semantic Kernel-style enterprise features. Dapr .NET SDK supports .NET 8, 9, and 10 in current docs.

_Programming Languages:_ C#/.NET for Hexalith platform services; Python for Dapr Agents worker services only where needed.

_Frameworks and Libraries:_ Microsoft.Agents.AI, Microsoft.Agents.AI.Workflows, Dapr .NET SDK packages, Dapr Agents, MCP SDKs, Microsoft.Extensions.AI, and Aspire Dapr integration.

_Database and Storage Technologies:_ EventStore for domain truth; Dapr actor/workflow-capable state stores for Dapr Workflow/Dapr Agents; separate session/checkpoint stores; vector/search memory behind governed services.

_API and Communication Technologies:_ HTTP/gRPC, Dapr service invocation, CloudEvents pub/sub, MCP JSON-RPC transports, A2A HTTP/JSON/JSON-RPC, and workflow APIs.

_Source:_ https://docs.dapr.io/developing-ai/dapr-agents/; https://learn.microsoft.com/en-us/agent-framework/overview/; https://docs.dapr.io/developing-applications/sdks/dotnet/; https://modelcontextprotocol.io/specification/2025-06-18/basic/transports

### Technology Adoption Patterns

Adoption should be conservative and measured. Dapr runtime capabilities are mature enough to be a platform foundation. Dapr Agents v1.0 is production-ready according to Dapr docs, but its Python-specific implementation means it should be introduced behind service boundaries. Microsoft Agent Framework is production-ready according to Microsoft announcements and docs, but package and feature churn still requires pinning and adapters.

_Adoption Trends:_ Agent frameworks increasingly standardize on tools, workflows, MCP, A2A, telemetry, and provider-neutral model clients.

_Migration Patterns:_ Migrate existing .NET agent code toward Microsoft Agent Framework. Add Dapr sidecar capabilities underneath. Introduce Dapr Agents only for bounded durable autonomous tasks.

_Emerging Technologies:_ Dapr Conversation API is promising for provider abstraction, caching, PII obfuscation, usage metrics, and tool calling, but it is currently alpha and should stay behind an adapter.

_Source:_ https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0/; https://docs.dapr.io/developing-applications/building-blocks/conversation/conversation-overview/; https://learn.microsoft.com/en-us/agent-framework/agents/providers/; https://docs.dapr.io/developing-ai/dapr-agents/

## 5. Integration and Interoperability Patterns

### Current Integration Approaches

Use boundary-specific protocols:

- Function tools for same-process Agent Framework operations.
- MCP for reusable tools.
- Dapr service invocation for service-to-service calls.
- Dapr pub/sub CloudEvents for asynchronous coordination.
- Dapr Workflow APIs for durable orchestration lifecycle.
- Dapr `MCPServer` for durable governed tool calls.
- A2A for remote agents across service/team/language boundaries.

_API Design Patterns:_ Task APIs for starting/querying/cancelling agent work; domain command/query APIs for business actions; MCP tools for reusable capabilities; A2A for remote agents.

_Service Integration:_ Dapr app IDs, mTLS, access control, retries, tracing, and metrics should replace hardcoded service URLs in agent logic.

_Data Integration:_ Use CloudEvents for event metadata and correlation. Keep domain events separate from workflow history and agent transcripts.

_Source:_ https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/; https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-cloudevents/; https://learn.microsoft.com/en-us/agent-framework/agents/providers/agent-to-agent; https://docs.dapr.io/developing-ai/mcp/

### Interoperability Standards and Protocols

MCP and A2A are the core interoperability protocols. MCP standardizes tools and data access for agents. A2A standardizes remote agent interaction. Dapr can secure and observe MCP through service invocation or turn MCP servers into deploy-time resources through `MCPServer`.

_Standards Compliance:_ Use MCP for tool schemas and transports, CloudEvents for pub/sub envelopes, OpenTelemetry for observability, and A2A for remote-agent interoperability.

_Protocol Selection:_ MCP for tools; A2A for remote agents; HTTP/gRPC for internal services; CloudEvents for events; Dapr Workflow for durable lifecycle control.

_Integration Challenges:_ Authentication propagation, schema versioning, tenant scoping, replay/idempotency, remote tool trust, and latency/cost from excessive workflow-backed tool calls.

_Source:_ https://modelcontextprotocol.io/specification/2025-06-18/basic/transports; https://learn.microsoft.com/en-us/agent-framework/agents/providers/agent-to-agent; https://docs.dapr.io/developing-ai/mcp/mcp-server-resource/

## 6. Performance and Scalability Analysis

### Performance Characteristics and Optimization

The main performance costs are model calls, prompt size, tool fan-out, workflow state writes, pub/sub retries, vector retrieval, telemetry volume, and human approval latency. Dapr Workflow uses actors and state stores, so workflow throughput depends heavily on the chosen state store and payload sizes.

_Performance Benchmarks:_ Vendor claims such as Dapr Agents' actor-backed scale-to-zero behavior are useful signals but must be validated under Hexalith workloads. The system needs load tests around workflow state writes, tool fan-out, model latency, and tenant concurrency.

_Optimization Strategies:_ Keep workflow payloads small, pass IDs not large transcripts, compact memory, summarize retrieval, use cheaper models for routing/classification, reserve `MCPServer` for high-risk tools, and reuse long-lived clients.

_Monitoring and Measurement:_ Track token usage, tool calls, workflow writes, latency percentiles, retry rates, cache hit rates, queue depth, and evaluation pass rates per tenant and agent type.

_Source:_ https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-architecture/; https://docs.dapr.io/developing-ai/mcp/mcp-server-resource/; https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-ai/dotnet-ai-conversation-usage/; https://learn.microsoft.com/en-us/azure/foundry/concepts/observability

### Scalability Patterns and Approaches

Scale by service role rather than by framework: AgentHost replicas, DaprAgentWorker replicas, ToolHost replicas, PolicyHost replicas, and domain-service replicas. Dapr sidecars give each service a consistent runtime API while preserving independent scaling.

_Scalability Patterns:_ Horizontal scaling, actor-backed workflow distribution, child workflows, bounded queues, rate limits, per-tenant budgets, and independent tool hosts.

_Capacity Planning:_ Model quotas, state-store throughput, workflow history size, pub/sub broker throughput, approval queue capacity, and telemetry storage are all first-class capacity dimensions.

_Elasticity and Auto-scaling:_ Use Kubernetes/ACA scaling signals from request rate, queue depth, workflow backlog, CPU/memory, and model-provider throttling. Treat scale-to-zero carefully for services with warm MCP/model connections.

_Source:_ https://docs.dapr.io/operations/hosting/kubernetes/kubernetes-production/; https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-features-concepts/; https://github.com/dapr/dapr-agents; https://docs.dapr.io/concepts/observability-concept/

## 7. Security and Compliance Considerations

### Security Best Practices and Frameworks

Security must be enforced outside prompt text. Use ingress auth, server-resolved tenant context, Dapr mTLS/SPIFFE, Dapr service invocation access control, `WorkflowAccessPolicy`, MCP server auth, Dapr `MCPServer` hooks, Agent Framework approval, Dapr secret stores, and audit logs.

_Security Frameworks:_ Dapr mTLS and access control, OAuth/OIDC/JWT at ingress, MCP auth, managed identity/secret stores, OpenTelemetry auditability, and responsible AI governance policies.

_Threat Landscape:_ Prompt injection, tool-result injection, cross-tenant leakage, unauthorized workflow scheduling, duplicate side effects, malicious MCP/A2A endpoints, and tampered checkpoint/workflow state.

_Secure Development Practices:_ Least-privilege tools, typed schemas, approval gates, policy checks, redaction, idempotency, state-store security, no secrets in prompts/metadata, and fail-closed tenant handling.

_Source:_ https://docs.dapr.io/concepts/security-concept/; https://docs.dapr.io/operations/configuration/invoke-allowlist/; https://learn.microsoft.com/en-us/agent-framework/agents/tools/tool-approval; https://learn.microsoft.com/en-us/agent-framework/agents/tools/local-mcp-tools

### Compliance and Regulatory Considerations

The implementation should align AI governance with existing corporate risk, security, data governance, and audit processes. Microsoft guidance recommends responsible AI standards, ownership, audit procedures, incident response, and governance checkpoints integrated into ordinary workflows.

_Industry Standards:_ Responsible AI principles, NIST AI risk patterns where applicable, OpenTelemetry, MCP, A2A, CloudEvents, and Dapr component/access policy specifications.

_Regulatory Compliance:_ Tenant data boundaries, data retention, PII handling, human review for high-risk decisions, and explainable audit trails are the main design requirements.

_Audit and Governance:_ Audit who requested an agent run, which model was used, which tools were called, which approvals were given, which domain commands were emitted, and which workflow instance produced the result.

_Source:_ https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ai-agents/responsible-ai-across-organization; https://learn.microsoft.com/en-us/azure/foundry/concepts/safety-evaluations-transparency-note; https://learn.microsoft.com/en-us/azure/foundry/concepts/observability

## 8. Strategic Technical Recommendations

### Technical Strategy and Decision Framework

Use this decision framework:

- Use Microsoft Agent Framework for .NET typed agents, sessions, providers, tool approval, MCP clients, A2A wrappers, and graph workflows.
- Use Dapr Agents `DurableAgent` for Dapr-native durable autonomous Python workers.
- Use Dapr Workflow directly for deterministic long-running business workflows.
- Use MCP for tools and A2A for remote agents.
- Use Dapr `MCPServer` only when durable, audited, argument-aware tool execution is required.
- Use EventStore/domain services for durable business truth.

_Architecture Recommendations:_ Hybrid platform with bounded services and sidecars.

_Technology Selection:_ .NET 10 + Microsoft Agent Framework + Dapr runtime/SDK + Aspire; optional Python Dapr Agents; MCP/A2A; OpenTelemetry; EventStore.

_Implementation Strategy:_ Incremental slices, adapter boundaries, central governance, integration tests, and production hardening before broad rollout.

_Source:_ https://learn.microsoft.com/en-us/agent-framework/overview/; https://docs.dapr.io/developing-ai/dapr-agents/dapr-agents-core-concepts/; https://docs.dapr.io/developing-ai/mcp/mcp-server-resource/

### Competitive Technical Advantage

The advantage of this hybrid is not novelty; it is operational clarity. Most agent systems fail at reliability, governance, observability, and cost control after the prototype. This design addresses those requirements directly by combining typed agent composition with a mature distributed runtime and explicit protocol boundaries.

_Technology Differentiation:_ Durable execution without giving up the .NET application model; governed tool boundaries; event-sourced business truth; protocol-based interoperability.

_Innovation Opportunities:_ Durable MCP tool workflows, tenant-aware agent memory, domain-specific evaluation datasets, agent run replay/audit views, and policy-driven tool approval.

_Strategic Technology Investments:_ Invest in MCP/domain tool design, Dapr production operations, OpenTelemetry trace correlation, evaluation datasets, and Dapr Agents worker templates.

_Source:_ https://devblogs.microsoft.com/foundry/build-2026-open-trust-stack-ai-agents/; https://docs.dapr.io/developing-ai/dapr-agents/; https://learn.microsoft.com/en-us/agent-framework/agents/observability

## 9. Implementation Roadmap and Risk Assessment

### Technical Implementation Framework

**Phase 1: Foundation**  
Create Aspire+Dapr topology, .NET AgentHost, baseline components, model-provider abstraction, OpenTelemetry, and one read-only tool.

**Phase 2: Tool Platform**  
Expose domain commands/queries as MCP/function tools with schemas, tenant context, idempotency, authorization, audit, and approval metadata.

**Phase 3: Durable Execution**  
Add Dapr Workflow for deterministic processes and a Python Dapr Agents `DurableAgent` for one autonomous durable task.

**Phase 4: Governance and Evaluation**  
Add Dapr ACLs, `WorkflowAccessPolicy`, MCPServer hooks for high-risk tools, human approval, evaluation datasets, and safety/security tests.

**Phase 5: Production Hardening**  
Add Dapr HA control plane, backup/restore, load tests, dashboards, alerts, runbooks, cost budgets, and incident exercises.

_Source:_ https://aspire.dev/integrations/frameworks/dapr/; https://docs.dapr.io/operations/hosting/kubernetes/kubernetes-production/; https://docs.dapr.io/operations/resiliency/resiliency-overview/

### Technical Risk Management

_Technical Risks:_ Double orchestration, duplicate side effects, state-store bottlenecks, evolving APIs, and overuse of workflow-backed tools.

_Implementation Risks:_ Unclear service ownership, weak MCP schemas, missing tenant context, insufficient integration testing, and model/provider churn.

_Business Impact Risks:_ Cross-tenant leakage, unauthorized actions, high token/tool cost, low answer quality, and poor auditability.

Mitigation is concrete: one durable owner per task, idempotent tools, tenant-scoped policy checks, explicit approval gates, adapter layers, Dapr.Testcontainers tests, AI evaluation gates, and traceable audit logs.

_Source:_ https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-features-concepts/; https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-guidance/dotnet-guidance-testcontainers/; https://learn.microsoft.com/en-us/azure/foundry/concepts/observability

## 10. Future Technical Outlook and Innovation Opportunities

### Emerging Technology Trends

_Near-term Technical Evolution:_ Agent frameworks will continue standardizing around MCP, A2A, OpenTelemetry, evaluation, and durable workflow support. Dapr Conversation and MCPServer capabilities should be watched but abstracted behind adapters until maturity is proven in the target environment.

_Medium-term Technology Trends:_ Durable agent orchestration, runtime policy controls, agent governance, and provider-neutral model routing will become ordinary platform capabilities. Evaluation will move from periodic manual review to CI/CD and production monitoring loops.

_Long-term Technical Vision:_ Agent platforms will look less like chat applications and more like distributed operating environments with durable tasks, governed tools, audit trails, policy enforcement, and multi-agent interoperability.

_Source:_ https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0/; https://devblogs.microsoft.com/foundry/build-2026-open-trust-stack-ai-agents/; https://docs.dapr.io/developing-applications/building-blocks/conversation/conversation-overview/

### Innovation and Research Opportunities

_Research Opportunities:_ Durable tool execution economics, tenant-aware memory isolation, policy-driven evaluation generation, workflow history integrity, and trace-driven agent debugging.

_Emerging Technology Adoption:_ Pilot Dapr Conversation API behind an adapter, evaluate Dapr `MCPServer` for high-risk tools, and track Agent Framework package/provider maturity.

_Innovation Framework:_ Run small production slices with explicit success metrics: completion rate, recovery rate, duplicate side-effect count, tool accuracy, policy-block rate, token cost, trace coverage, and incident diagnosis time.

_Source:_ https://docs.dapr.io/developing-ai/mcp/mcp-server-resource/; https://learn.microsoft.com/en-us/azure/foundry/concepts/safety-evaluations-transparency-note; https://learn.microsoft.com/en-us/agent-framework/agents/observability

## 11. Technical Research Methodology and Source Verification

### Comprehensive Technical Source Documentation

_Primary Technical Sources:_

- Dapr Agents documentation and GitHub repository.
- Dapr Workflow, state, pub/sub, service invocation, MCP, security, resiliency, observability, and Kubernetes production documentation.
- Microsoft Agent Framework overview, workflows, tools, A2A, MCP, HITL, observability, and checkpoint documentation.
- Microsoft Foundry observability, evaluation, and responsible AI governance documentation.
- Aspire Dapr integration documentation.
- MCP transport specification and A2A documentation.

_Secondary Technical Sources:_

- Microsoft DevBlogs announcements for Agent Framework 1.0 and trust/evaluation tooling.
- NuGet/PyPI/GitHub package and release metadata.
- Diagrid/CNCF ecosystem announcements used for adoption context, with primary docs preferred for design claims.

_Technical Web Search Queries Used:_

- "Dapr Agents DurableAgent AgentRunner Dapr Workflows core concepts"
- "Dapr MCPServer resource durable tool execution argument level RBAC audit redaction"
- "Microsoft Agent Framework workflows agents tools MCP A2A 2026"
- "Dapr Workflow architecture sidecar actors state store deterministic workflow"
- "Dapr security mTLS Sentry API token access control workflow access policy"
- "Microsoft Agent Framework observability telemetry OpenTelemetry docs"
- "Dapr Agents getting started production deployment AgentRunner DurableAgent 2026 docs"
- "Dapr production best practices deployment Kubernetes sidecar observability resiliency docs"
- "Microsoft Aspire Dapr integration documentation AddDapr pubsub state components"
- "Dapr.Testcontainers .NET SDK Testcontainers harness"

### Technical Research Quality Assurance

_Technical Source Verification:_ Current API and platform claims were checked against official Dapr Docs, Microsoft Learn, GitHub, and package registries. Non-primary ecosystem sources were used for trend context only.

_Technical Confidence Levels:_

- High: Dapr building blocks, Dapr Workflow architecture, Dapr Agents `DurableAgent`, Agent Framework core concepts, MCP/A2A integration, Dapr security/resiliency/observability.
- Medium: Dapr Conversation API production usage because it is currently documented as alpha.
- Medium: Dapr `MCPServer` production economics because the capability is current but introduces workflow-per-tool-call trade-offs requiring workload validation.

_Technical Limitations:_ This research did not run benchmarks, deploy a prototype, or validate provider-specific quotas. Capacity, cost, and latency must be measured with Hexalith workloads.

_Methodology Transparency:_ The document separates source-verified claims from architectural recommendations. Where recommendations are inferred from sources, they are stated as implementation guidance rather than vendor guarantees.

## 12. Technical Appendices and Reference Materials

### Detailed Technical Data Tables

| Decision | Recommended Choice | Rationale |
| --- | --- | --- |
| Primary .NET agent layer | Microsoft Agent Framework | Best fit for .NET agents, sessions, workflows, tools, MCP/A2A, providers |
| Durable autonomous agent runtime | Dapr Agents `DurableAgent` | Dapr-native workflow-backed Python durable agent execution |
| Deterministic business workflow | Dapr Workflow or Agent Framework workflow | Choose one durable owner; use Dapr for cross-service durable orchestration |
| Tool boundary | MCP | Reusable protocol surface across agents/frameworks |
| Remote agent boundary | A2A | Purpose-built remote-agent interoperability |
| High-risk durable tool calls | Dapr `MCPServer` | Argument-level policy hooks, durable retries, per-tool observability |
| Business source of truth | EventStore/domain services | Keeps agent state separate from domain truth |
| Local topology | Aspire + Dapr integration | Consistent local sidecar/component development |
| Integration tests | Dapr.Testcontainers | Real Dapr runtime components in tests |

### Technical Resources and References

_Technical Standards:_

- MCP specification: https://modelcontextprotocol.io/specification/2025-06-18/basic/transports
- A2A protocol: https://a2a-protocol.org/latest/
- CloudEvents: https://cloudevents.io/
- OpenTelemetry: https://opentelemetry.io/

_Open Source Projects:_

- Dapr Agents: https://github.com/dapr/dapr-agents
- Microsoft Agent Framework: https://github.com/microsoft/agent-framework
- Dapr runtime: https://github.com/dapr/dapr

_Implementation References:_

- Dapr Agents overview: https://docs.dapr.io/developing-ai/dapr-agents/
- Dapr Agents core concepts: https://docs.dapr.io/developing-ai/dapr-agents/dapr-agents-core-concepts/
- Dapr Workflow architecture: https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-architecture/
- Dapr MCPServer: https://docs.dapr.io/developing-ai/mcp/mcp-server-resource/
- Microsoft Agent Framework overview: https://learn.microsoft.com/en-us/agent-framework/overview/
- Agent Framework workflows: https://learn.microsoft.com/en-us/agent-framework/workflows/
- Agent Framework observability: https://learn.microsoft.com/en-us/agent-framework/agents/observability
- Aspire Dapr integration: https://aspire.dev/integrations/frameworks/dapr/

## 13. Technical Research Conclusion

### Summary of Key Technical Findings

The hybrid is viable and strategically sound if the architecture maintains clear boundaries. Microsoft Agent Framework should be the .NET agent application layer. Dapr Agents should be the Dapr-native durable autonomous worker layer. Dapr should be the distributed runtime and operations substrate. MCP, A2A, service invocation, pub/sub, workflow APIs, and domain commands must remain distinct boundaries.

### Strategic Technical Impact Assessment

This approach gives Hexalith a production-grade agent platform without abandoning its .NET 10, Dapr, Aspire, EventStore, and domain-driven architecture. It supports durable agent execution, protocol interoperability, observability, governance, and incremental adoption. The main engineering discipline required is resisting framework sprawl: every agent task must have one durable owner and every side effect must pass through governed tools or domain commands.

### Next Steps Technical Recommendations

1. Create the `.NET AgentHost` skeleton with Microsoft Agent Framework, Dapr clients, OpenTelemetry, and Aspire wiring.
2. Define the first MCP tool contract backed by a read-only domain query.
3. Add one side-effecting command tool with idempotency, approval, tenant checks, and audit.
4. Prototype one Dapr Agents `DurableAgent` Python worker for a bounded long-running autonomous task.
5. Add Dapr.Testcontainers integration tests and Foundry-style evaluation gates before expanding agent coverage.

---

**Technical Research Completion Date:** 2026-06-23  
**Research Period:** current comprehensive technical analysis  
**Source Verification:** All technical facts cited with current sources  
**Technical Confidence Level:** High for core architecture; medium for alpha/evolving capabilities such as Dapr Conversation API and workflow-backed MCP economics

_This comprehensive technical research document serves as an authoritative technical reference on Dapr AI Agents and Microsoft Agent Framework hybrid implementation and provides strategic technical guidance for implementation planning._
