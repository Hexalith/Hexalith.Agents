---
title: Sprint Change Proposal - EventStore Security Initialization In Agents Aspire Host
status: draft
created: 2026-06-26
updated: 2026-06-26
mode: Batch
change_scope: minor
recommended_path: direct-adjustment
owner: Administrator
approval_required: true
---

# Sprint Change Proposal: EventStore Security Initialization In Agents Aspire Host

## 1. Issue Summary

The Agents Aspire host is still the Story 1.1 buildable shell. It creates an empty distributed application and does not initialize the shared EventStore security service. The live-binding/AppHost topology follow-through now needs the Agents host to reuse `HexalithEventStoreSecurityExtensions.AddHexalithEventStoreSecurity()` instead of duplicating Keycloak, JWT bearer, OIDC, or service credential wiring.

Trigger: `HexalithEventStoreSecurityExtensions` must initialize the security service in the Agents Aspire host.

Evidence:

- `src/Hexalith.Agents.AppHost/Program.cs` currently only creates the builder and runs an empty distributed application.
- `src/Hexalith.Agents.AppHost/Hexalith.Agents.AppHost.csproj` references only Agents Server and UI, both with `IsAspireProjectResource="false"`.
- `Hexalith.EventStore.Aspire` already exposes `AddHexalithEventStoreSecurity()`, `WithJwtBearerSecurity()`, `WithEventStoreClientCredentials()`, and `WithOpenIdConnectSecurity()`.
- `Hexalith.EventStore.AppHost` already uses `builder.AddHexalithEventStoreSecurity()` and applies the returned security resources to EventStore, Tenants, Admin Server, Admin UI, and sample UI resources.
- Architecture AD-16 requires Hexalith.Agents to own its own AppHost/local orchestration.
- `sprint-status.yaml` has open live-binding/AppHost topology follow-through instead of a canonical implementation story.

This is a topology and implementation-readiness correction. It does not change PRD scope, UX behavior, domain contracts, or MVP requirements.

## 2. Impact Analysis

### Epic Impact

No existing product epic is invalidated. Epics 1-4 are marked done in sprint status. The change belongs to the open live-binding/AppHost topology follow-through, effectively the planned production/live-binding slice after Epic 4.

Primary impacted item:

- Epic 4 action item: author the production/live-binding plan, including AppHost topology.

Secondary impacted items:

- Epic 1 action item: record static-vs-live readiness and AppHost topology seams.
- Epic 2/Epic 4 live-binding backlog: bind deferred seams behind a runnable local topology.

No epic resequencing is required if this is handled before the first live topology story depends on EventStore-authenticated resources.

### Story Impact

There is no current canonical story that explicitly names the EventStore security extension. The next AppHost/live-binding topology story should add acceptance criteria for:

- Initializing security through `builder.AddHexalithEventStoreSecurity()`.
- Referencing `Hexalith.EventStore.Aspire` from the AppHost only.
- Applying `WithJwtBearerSecurity`, `WithEventStoreClientCredentials`, or `WithOpenIdConnectSecurity` when EventStore-authenticated resources are introduced.
- Preserving `EnableKeycloak=false` local fallback behavior.
- Guarding against regression with a static topology test and AppHost build.

### PRD Impact

No PRD edit is required. The PRD already requires tenant isolation, authorization before side effects, fail-closed dependency handling, provider secret safety, and safe status/audit behavior. This change supplies local orchestration support for those requirements.

### Architecture Impact

Small clarification recommended for AD-16. AD-16 already says Agents owns AppHost/local orchestration; it should also state that EventStore-authenticated local topology uses the shared EventStore Aspire security helper rather than copy-pasted security wiring.

### UX Impact

No UX design or screen-flow edit is required. Existing readiness/status surfaces remain valid. Until the live topology is complete, unavailable/blocked states should remain visible and fail closed.

### Technical Impact

Implementation should:

- Add an AppHost-only project reference to `$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Aspire\Hexalith.EventStore.Aspire.csproj` with `IsAspireProjectResource="false"`.
- Add `using Hexalith.EventStore.Aspire;` in `src/Hexalith.Agents.AppHost/Program.cs`.
- Initialize the security resource with `builder.AddHexalithEventStoreSecurity()`.
- Use the returned resources when concrete EventStore, Agents service, UI, Tenants, Conversations, or Parties resources are added to the topology.
- Avoid any duplicated Keycloak/OIDC/JWT/service-credential environment logic in Agents.
- Add a static topology guard to the existing test lane.
- Build the AppHost after the change.

## 3. Checklist Findings

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 | Done | Trigger is the Agents Aspire host missing explicit initialization of the shared EventStore security service. |
| 1.2 | Done | Issue type: technical limitation discovered during live-binding/AppHost topology planning. |
| 1.3 | Done | Evidence exists in Agents AppHost, EventStore Aspire extension, EventStore AppHost usage, AD-16, and sprint-status live-binding actions. |
| 2.1 | Done | Current epic plan remains valid; the live-binding/AppHost follow-through needs explicit security acceptance criteria. |
| 2.2 | Done | No new product epic required; add or amend the AppHost topology implementation slice. |
| 2.3 | Done | Future runtime/live topology work depends on this before authenticated local orchestration is meaningful. |
| 2.4 | Done | No planned epic is obsolete. |
| 2.5 | Done | No resequencing required if the AppHost security slice lands before live resource wiring. |
| 3.1 | Done | PRD remains aligned; no PRD edit required. |
| 3.2 | Done | Architecture should clarify AD-16 with the shared EventStore security initialization invariant. |
| 3.3 | N/A | No direct UI/UX artifact edit required. |
| 3.4 | Done | Secondary artifacts: AppHost code, AppHost csproj, static topology guard, sprint-status action item after approval. |
| 4.1 | Viable | Direct adjustment is low effort and low to medium risk. |
| 4.2 | Not viable | Rollback is unnecessary; no completed behavior must be reverted. |
| 4.3 | Not viable | MVP review is unnecessary; product scope is unaffected. |
| 4.4 | Done | Recommended path: Direct Adjustment. |
| 5.1 | Done | Issue summary included. |
| 5.2 | Done | Epic, story, architecture, UX, and technical impacts documented. |
| 5.3 | Done | Direct adjustment selected because the shared EventStore API already exists. |
| 5.4 | Done | MVP unaffected; implementation plan defined below. |
| 5.5 | Done | Handoff: Developer for code/tests; Architect/PO only for backlog wording. |
| 6.1 | Done | Applicable checklist sections addressed. |
| 6.2 | Done | Proposal reviewed against current docs and source. |
| 6.3 | Action-needed | Explicit user approval is required before implementation. |
| 6.4 | Action-needed | Update sprint status only after approval. |
| 6.5 | Action-needed | Final handoff confirmation depends on approval. |

## 4. Recommended Approach

Use Direct Adjustment.

Rationale:

- The missing behavior is AppHost/security composition, not product scope.
- EventStore already owns the correct reusable Aspire security extension.
- Reusing the extension avoids duplicated Keycloak, JWT bearer, OIDC, and client credential wiring.
- The current Agents AppHost is intentionally empty, so the blast radius is narrow.
- Static topology tests plus an AppHost build can catch regression without requiring a full live Dapr topology yet.

Effort estimate: low.

Risk level: low to medium. The risk is mostly AppHost composition and cross-module source reference hygiene.

Timeline impact: one focused implementation slice plus verification. No sprint replan is required.

## 5. Detailed Change Proposals

### Architecture Change

Artifact: `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md`

Section: `AD-16 - Module-Local Operational Topology`

OLD:

```markdown
Hexalith.Agents owns its own AppHost/local orchestration and deployable workloads. The root `agents` workspace remains a coordination/super-repo. Agents AppHost composes Agents service/UI with existing EventStore, Conversations, Parties, Tenants, and provider adapter dependencies for local/dev/test.
```

NEW:

```markdown
Hexalith.Agents owns its own AppHost/local orchestration and deployable workloads. The root `agents` workspace remains a coordination/super-repo. Agents AppHost composes Agents service/UI with existing EventStore, Conversations, Parties, Tenants, and provider adapter dependencies for local/dev/test. When the local topology includes EventStore-authenticated resources, the AppHost initializes security through `HexalithEventStoreSecurityExtensions.AddHexalithEventStoreSecurity()` and applies the returned resources through the shared EventStore Aspire helpers instead of duplicating Keycloak, JWT bearer, OIDC, or service-credential wiring.
```

Rationale: AD-16 already owns topology. The new sentence fixes the security wiring invariant for local/dev/test composition.

### Backlog / Story Change

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml` or the next live-binding/AppHost topology story.

OLD:

```yaml
action: "Author the production / live-binding plan (de-facto Epic 5): convert the Story 4.5 report sections 5/6 enumeration into a real epic+story plan (durable runtime owner AD-18, read-model projections, Conversations AddParticipant seam AD-6/7, provider-SDK adapter, content-safety engine, AppHost topology), each gated on its prerequisite governance decision."
```

NEW:

```yaml
action: "Author the production / live-binding plan (de-facto Epic 5): convert the Story 4.5 report sections 5/6 enumeration into a real epic+story plan (durable runtime owner AD-18, read-model projections, Conversations AddParticipant seam AD-6/7, provider-SDK adapter, content-safety engine, AppHost topology), each gated on its prerequisite governance decision. The AppHost topology story must initialize shared EventStore security with builder.AddHexalithEventStoreSecurity() and apply EventStore Aspire security helpers to authenticated resources."
```

Rationale: The current action names AppHost topology but does not make the shared security service a tracked acceptance gate.

Proposed acceptance criteria for the implementation story:

```markdown
**Given** the Agents AppHost composes local/dev/test resources
**When** the AppHost starts
**Then** it initializes shared EventStore security through `builder.AddHexalithEventStoreSecurity()`
**And** it references `Hexalith.EventStore.Aspire` only from AppHost/topology code
**And** EventStore-authenticated resources use `WithJwtBearerSecurity`, `WithEventStoreClientCredentials`, or `WithOpenIdConnectSecurity` as appropriate
**And** `EnableKeycloak=false` remains a supported local fallback path
**And** tests fail if the AppHost no longer initializes the shared security service.
```

### AppHost Project Change

Artifact: `src/Hexalith.Agents.AppHost/Hexalith.Agents.AppHost.csproj`

OLD:

```xml
<ItemGroup>
  <ProjectReference Include="..\Hexalith.Agents.Server\Hexalith.Agents.Server.csproj" IsAspireProjectResource="false" />
  <ProjectReference Include="..\Hexalith.Agents.UI\Hexalith.Agents.UI.csproj" IsAspireProjectResource="false" />
</ItemGroup>
```

NEW:

```xml
<ItemGroup>
  <ProjectReference Include="..\Hexalith.Agents.Server\Hexalith.Agents.Server.csproj" IsAspireProjectResource="false" />
  <ProjectReference Include="..\Hexalith.Agents.UI\Hexalith.Agents.UI.csproj" IsAspireProjectResource="false" />
  <ProjectReference Include="$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Aspire\Hexalith.EventStore.Aspire.csproj" IsAspireProjectResource="false" />
</ItemGroup>
```

Rationale: This mirrors EventStore AppHost's own reference to `Hexalith.EventStore.Aspire` and keeps the shared hosting helpers out of contracts/domain/UI projects.

### AppHost Program Change

Artifact: `src/Hexalith.Agents.AppHost/Program.cs`

OLD:

```csharp
IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.Build().Run();
```

NEW:

```csharp
using Hexalith.EventStore.Aspire;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

_ = builder.AddHexalithEventStoreSecurity();

builder.Build().Run();
```

Future resource wiring when concrete resources exist:

```csharp
HexalithEventStoreSecurityResources? security = builder.AddHexalithEventStoreSecurity();
if (security is not null)
{
    _ = eventStore.WithJwtBearerSecurity(security);
    _ = agentsServer.WithEventStoreClientCredentials(security);
    _ = agentsUi.WithOpenIdConnectSecurity(security, clientId: "...", clientSecret: "...");
}
```

Rationale: The current minimal AppHost can initialize the shared security resource immediately. Actual helper application belongs with the resource graph once the live topology adds concrete resources.

### Test / Verification Change

Artifact: `test/Hexalith.Agents.Server.Tests` or a future AppHost-specific test project.

NEW:

```markdown
Add a static topology guard that verifies:

- `Hexalith.Agents.AppHost.csproj` references `Hexalith.EventStore.Aspire` with `IsAspireProjectResource="false"`.
- `Program.cs` calls `AddHexalithEventStoreSecurity()`.
- No Agents contracts, domain, client, UI, or server project references `Hexalith.EventStore.Aspire`.
- AppHost code does not duplicate Keycloak/OIDC/JWT/service credential environment wiring that belongs to EventStore Aspire helpers.
```

Rationale: This catches regression before the broader runnable topology exists.

## 6. Implementation Handoff

Change scope: Minor.

Route to: Developer agent for direct implementation after approval.

Developer responsibilities:

- Update `src/Hexalith.Agents.AppHost/Hexalith.Agents.AppHost.csproj`.
- Update `src/Hexalith.Agents.AppHost/Program.cs`.
- Add static topology guard coverage in the existing test lane.
- Build the AppHost and run the focused test project.

Architect/Product Owner responsibilities:

- If backlog artifacts are being updated, amend the live-binding/AppHost topology story or sprint action item with the acceptance criteria above.
- No PRD/MVP approval is needed unless the live-binding plan is otherwise reorganized.

Success criteria:

- Agents AppHost initializes shared EventStore security.
- EventStore Aspire security wiring is referenced only from AppHost/topology code.
- No duplicated Keycloak/OIDC/JWT/service credential configuration appears in Agents.
- `EnableKeycloak=false` remains a supported local fallback through the shared EventStore helper.
- Static guard tests fail if the security initialization is removed.
- AppHost builds cleanly.

Recommended verification:

```bash
dotnet restore Hexalith.Agents.slnx
dotnet build src/Hexalith.Agents.AppHost/Hexalith.Agents.AppHost.csproj -c Release --no-restore
DiffEngine_Disabled=true dotnet test test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj -c Release --no-build
```

## 7. Approval Needed

This proposal is ready for review.

Review complete proposal. Continue [c] to approve routing to Developer for implementation, or Edit [e] to revise the proposal.
