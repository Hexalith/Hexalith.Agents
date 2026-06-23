# Rubric Review

Verdict: Pass after fixes.

Scope checked: `ARCHITECTURE-SPINE.md` against the good-spine checklist.

Findings:

- Resolved: Source paths initially pointed one directory too shallow. Fixed to `../../briefs`, `../../prds`, and `../../ux-designs`.
- Resolved: Stack rows for sibling source modules initially said only "sibling source module". Fixed with local submodule commits.
- Resolved: Conversations membership seam was too implicit. Tightened AD-6/AD-7 and added a Deferred prerequisite for public `AddParticipant` client/API exposure if absent.
- Resolved: Sensitive content protection was too conditional. Tightened AD-14 so content-bearing workflows stay disabled unless EventStore payload protection/redaction conventions are available.

Residual risks:

- The product term "conversation owner" remains mapped to `ParticipantRole.Facilitator` for V1. This is explicit in AD-8 and Deferred; product or Conversations can still replace it with a first-class owner resolver.
- The concrete provider SDK is intentionally deferred. AD-9/AD-10 constrain the adapter and metadata floor enough for implementation to start without choosing a provider package in the spine.

## 2026-06-23 Dapr Runtime Amendment

Verdict: Pass.

Checklist results:

- AD-18 fixes a real divergence point: different builders could otherwise choose custom hosted services, direct provider loops, Dapr Workflow, or another workflow engine.
- The rule is enforceable: V1 agent workflow execution must use Dapr Workflow and mutate durable domain state only through `AgentInteraction` commands/events.
- The brownfield stack remains consistent: Dapr `1.18.4` is already a local package baseline and Dapr Workflow/AI packages are present in sibling modules where used.
- Named technology was current-checked against official Dapr docs before binding.
- The Deferred entry for Dapr Agents worker packaging does not reopen the substrate decision; it only defers whether the adapter is in-process .NET or a Python worker.

Residual risks:

- No provider component or Dapr AI component configuration is selected yet. This remains implementation/provider selection work under AD-9, AD-10, and AD-18.
