---
title: Sprint Change Proposal - EventStore Security Initialization In Agents Aspire Host
status: draft
created: 2026-06-26
change_scope: minor
recommended_path: direct-adjustment
owner: Administrator
---

# Sprint Change Proposal: EventStore Security Initialization In Agents Aspire Host

## 1. Issue Summary

The Agents AppHost remains a minimal buildable shell and does not initialize the shared EventStore security service. Runtime/live-binding work for Hexalith.Agents now depends on a real Aspire topology, so the AppHost story must explicitly require use of `HexalithEventStoreSecurityExtensions.AddHexalithEventStoreSecurity()` and the related `WithJwtBearerSecurity`, `WithEventStoreClientCredentials`, or `WithOpenIdConnectSecurity` helpers where applicable.

### Trigger

- Triggering issue: `HexalithEventStoreSecurityExtensions` must initialize the security service in the Agents Aspire host.
- Triggering area: deferred AppHost/local topology work tracked from Epic 4 live-binding follow-through.
- Evidence:
  - `Hexalith.EventStore.Aspire` already exposes `AddHexalithEventStoreSecurity()` and security resource wiring helpers.
  - `Hexalith.EventStore.AppHost` already calls `builder.AddHexalithEventStoreSecurity()` and wires security into EventStore, Tenants, Admin Server, Admin UI, and sample UI resources.
  - `Hexalith.Agents.AppHost` currently builds an empty distributed application and explicitly defers the real topology.
  - `epics.md` already says Hexalith.Agents owns its AppHost/local orchestration, but no story currently names the EventStore security service requirement.
  - `sprint-status.yaml` tracks AppHost topology as part of the live-binding plan, but without this explicit security-service acceptance criterion.

## 2. Impact Analysis

### Epic Impact

Epic 4 is the primary impact because the current sprint status tracks production/live-binding work as the follow-through from Story 4.5. The change does not alter the product MVP. It tightens the AppHost topology acceptance criteria so local/dev/test orchestration starts with the same security resource model used by EventStore.

Epic 2 is indirectly affected because safe invocation, context loading, posting, and fail-closed dependency gates depend on authenticated EventStore, Conversations, Parties, and Tenants paths.

Epic 1 remains conceptually aligned: setup readiness still depends on identity, provider, response policy, approver policy, and content safety. This change adds infrastructure preconditions for executing those workflows locally, not a new product feature.

### Story Impact

The change should amend the deferred AppHost/local topology story or the planned live-binding Epic 5 story, not create a broad new epic.

Most directly affected story candidate:

- Story 1.1a from the prior sprint change proposal: "Establish AppHost, Local Topology, And CI Build Gate."

If Story 1.1a has not been merged into the canonical epics, apply the same acceptance criteria to the live-binding/AppHost topology story currently planned from Epic 4 action item #2.

### Artifact Conflicts

PRD impact: none. The PRD already requires strict tenant isolation, authorization, fail-closed dependency handling, and safe API/client contracts.

Architecture impact: small clarification to AD-16 or the AppHost/local topology implementation notes. AD-16 already requires Agents AppHost/local orchestration; it should explicitly say the Agents AppHost uses the shared EventStore security extension rather than reimplementing Keycloak/OIDC wiring.

UX impact: none for screen layout. UX readiness/status states remain valid. If the topology is absent or security is disabled, readiness/status surfaces should continue to show blocked/unavailable states rather than success.

Sprint status impact: after approval, add an action item or update the existing AppHost topology action so EventStore security initialization is tracked as a required acceptance gate.

### Technical Impact

Implementation should:

- Add a reference from `Hexalith.Agents.AppHost` to `Hexalith.EventStore.Aspire` using the existing `$(HexalithEventStoreRoot)` MSBuild property.
- Initialize security with `builder.AddHexalithEventStoreSecurity()` in `Hexalith.Agents.AppHost/Program.cs`.
- Wire security into the EventStore resource and any Agents, UI, or dependent domain resources once those resources exist.
- Preserve `EnableKeycloak=false` fallback behavior.
- Avoid duplicating Keycloak, JWT bearer, OIDC, service credential, or realm URL configuration.
- Add focused topology/build tests that detect missing security initialization without requiring production secrets.
- Document that AppHost changes require restarting `aspire run`.

## 3. Checklist Findings

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 | Done | Trigger is the Agents Aspire host missing explicit initialization of the shared EventStore security service. |
| 1.2 | Done | Issue type: technical limitation discovered during implementation/live-binding planning. |
| 1.3 | Done | Evidence exists in EventStore security extension, EventStore AppHost usage, Agents empty AppHost shell, epics AD-16 requirement, and sprint-status live-binding action. |
| 2.1 | Done | Current epic plan can continue if AppHost topology acceptance criteria are amended. |
| 2.2 | Done | No new epic required; modify the AppHost/local topology story or live-binding plan. |
| 2.3 | Done | Future runtime stories depend on this for authenticated local topology. |
| 2.4 | Done | No planned epic becomes obsolete. |
| 2.5 | Done | No resequencing required if the AppHost topology story is completed before live runtime binding. |
| 3.1 | Done | PRD remains aligned; no PRD edits required. |
| 3.2 | Done | Architecture should clarify AD-16 implementation note around shared EventStore security. |
| 3.3 | N/A | No direct UI/UX artifact edit is required. |
| 3.4 | Done | Secondary artifacts: AppHost code, AppHost csproj, topology/build tests, sprint-status action item. |
| 4.1 | Viable | Direct adjustment is low effort and low risk. |
| 4.2 | Not viable | Rollback is unnecessary; no completed behavior must be reverted. |
| 4.3 | Not viable | MVP scope is unaffected. |
| 4.4 | Done | Recommended path: Direct Adjustment. |
| 5.1 | Done | Issue summary included in this proposal. |
| 5.2 | Done | Epic, story, architecture, and technical impacts documented. |
| 5.3 | Done | Direct adjustment selected because it preserves scope and reuses existing EventStore APIs. |
| 5.4 | Done | MVP unaffected; implementation plan defined below. |
| 5.5 | Done | Handoff: Developer for code/tests, Architect for acceptance wording if canonical epics are updated. |
| 6.1 | Done | Applicable checklist sections completed. |
| 6.2 | Done | Proposal is specific and actionable. |
| 6.3 | Action-needed | Explicit user approval is still required before implementation or sprint-status changes. |
| 6.4 | Action-needed | Update `sprint-status.yaml` only after approval. |
| 6.5 | Action-needed | Confirm implementation handoff after approval. |

## 4. Recommended Approach

Use Direct Adjustment.

Rationale:

- The missing behavior is topology wiring, not a change in product scope.
- EventStore already owns and exposes the correct Aspire security extension.
- Reusing `HexalithEventStoreSecurityExtensions` avoids duplicate Keycloak/OIDC environment logic.
- The risk is mainly local/runtime readiness; acceptance tests can catch it.
- No rollback or MVP review is justified.

Effort estimate: low.

Risk level: low to medium. The risk is cross-module AppHost composition and dependency-reference hygiene, not domain behavior.

Timeline impact: one focused implementation slice plus build/topology verification.

## 5. Detailed Change Proposals

### Architecture Change

Artifact: `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md`

Section: AD-16 - Module-Local Operational Topology

OLD:

```markdown
Hexalith.Agents owns its own AppHost/local orchestration and deployable workloads. The root `agents` workspace remains a coordination/super-repo. Agents AppHost composes Agents service/UI with existing EventStore, Conversations, Parties, Tenants, and provider adapter dependencies for local/dev/test.
```

NEW:

```markdown
Hexalith.Agents owns its own AppHost/local orchestration and deployable workloads. The root `agents` workspace remains a coordination/super-repo. Agents AppHost composes Agents service/UI with existing EventStore, Conversations, Parties, Tenants, and provider adapter dependencies for local/dev/test. When the local topology includes EventStore-authenticated resources, the AppHost initializes security through `HexalithEventStoreSecurityExtensions.AddHexalithEventStoreSecurity()` and applies the returned resources through the shared EventStore Aspire helpers instead of duplicating Keycloak, JWT bearer, OIDC, or service-credential wiring.
```

Rationale: AD-16 already owns topology; this clarifies the shared security initialization invariant.

### Story Change

Artifact: `_bmad-output/planning-artifacts/epics.md` or the live-binding/AppHost topology story derived from sprint-status.

Section: Story 1.1a or equivalent AppHost topology story.

OLD:

```markdown
**Given** the Agents module shell exists
**When** local orchestration is inspected
**Then** `Hexalith.Agents.AppHost` composes the Agents service/UI with EventStore, Conversations, Parties, Tenants, and provider-adapter placeholders needed for local/dev/test
**And** the root `agents` workspace remains only a coordination/super-repo.
```

NEW:

```markdown
**Given** the Agents module shell exists
**When** local orchestration is inspected
**Then** `Hexalith.Agents.AppHost` composes the Agents service/UI with EventStore, Conversations, Parties, Tenants, and provider-adapter placeholders needed for local/dev/test
**And** the AppHost initializes the shared security resource through `HexalithEventStoreSecurityExtensions.AddHexalithEventStoreSecurity()`
**And** EventStore-authenticated project resources use the returned security resources through `WithJwtBearerSecurity`, `WithEventStoreClientCredentials`, or `WithOpenIdConnectSecurity` as appropriate
**And** `EnableKeycloak=false` remains a supported local fallback path
**And** the root `agents` workspace remains only a coordination/super-repo.
```

Rationale: The story currently requires local topology but does not name the security service needed for authenticated EventStore and dependent resources.

### Test/Verification Change

Artifact: `Hexalith.Agents/tests/Hexalith.Agents.Server.Tests` or a future AppHost/topology test project.

NEW:

```markdown
Add a topology/build guard that verifies `Hexalith.Agents.AppHost`:

- References `Hexalith.EventStore.Aspire` only from AppHost or Aspire composition code, not from contracts/domain/UI.
- Calls `AddHexalithEventStoreSecurity()` when composing the local topology.
- Wires security helper methods to EventStore-authenticated resources when those resources are present.
- Preserves `EnableKeycloak=false` as a local fallback.
- Does not require production secrets for build or static topology tests.
```

Rationale: A test prevents this from regressing back to a build-only empty host while live-binding work proceeds.

### Implementation Handoff

Code targets:

- `src/Hexalith.Agents.AppHost/Hexalith.Agents.AppHost.csproj`
- `src/Hexalith.Agents.AppHost/Program.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Server.Tests` or a dedicated topology test project

Likely implementation tasks:

1. Add a guarded project reference:

```xml
<ProjectReference Include="$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Aspire\Hexalith.EventStore.Aspire.csproj" />
```

2. Import `Hexalith.EventStore.Aspire` in the AppHost.

3. Call:

```csharp
HexalithEventStoreSecurityResources? security = builder.AddHexalithEventStoreSecurity();
```

4. When Agents AppHost resources are added, apply:

```csharp
if (security is not null)
{
    _ = eventStore.WithJwtBearerSecurity(security);
    // Add client/OIDC helper calls for Agents UI or service resources as their concrete resources are introduced.
}
```

5. Add static or Aspire testing coverage that proves the initialization path exists without launching production dependencies.

6. Validate with a narrow build/test lane. Do not run solution-level `dotnet test` for EventStore-owned tests; follow the Agents repo's current test conventions for the touched test project.

## 6. Implementation Handoff

Change scope: Minor.

Route to: Developer agent for direct implementation, with Architect review only if the canonical epics/architecture text is updated.

Responsibilities:

- Developer: implement AppHost security initialization and tests.
- Architect: approve AD-16/story wording if planning artifacts are updated.
- Product Owner: no PRD/MVP approval needed unless the live-binding plan is reorganized.

Success criteria:

- Agents AppHost uses `HexalithEventStoreSecurityExtensions` for security initialization.
- No duplicate Keycloak/OIDC/security environment implementation appears in Agents.
- Security can be disabled with `EnableKeycloak=false` for local fallback.
- Tests or static conformance checks fail if the AppHost no longer initializes the shared security service.
- `aspire run` restart is documented or called out for AppHost model changes.

## 7. Approval Needed

This proposal is ready for review.

Approve to route to Developer for direct implementation. After approval, update `sprint-status.yaml` to add or amend the AppHost topology action with the explicit EventStore security initialization acceptance gate.
