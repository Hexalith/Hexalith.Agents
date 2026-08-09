# Deferred Work

- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-enforce-complete-launch-readiness-before-callability.md`
  summary: Platform/FrontComposer host must call AddAgentsUiSetup so the live AgentsClientSetupGateway replaces the deferred UI gateway in a runnable composition.
  evidence: Story 5.2 ships AddAgentsUiSetup and composition tests, but no in-repo host invokes it; platform-owned hosting is Story 5.6 / EXT-HOST-1.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-enforce-complete-launch-readiness-before-callability.md`
  summary: Align setup-query authorization with live tenant-access evidence instead of the deferred ITenantAccessReader alongside HTTP Agents.Administrator role checks.
  evidence: Write/read HTTP path uses HttpAgentAdministrationContextProvider while IDomainQueryHandler setup queries still authorize via ITenantAccessReader; live tenant/Party binding is Story 5.4.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-enforce-complete-launch-readiness-before-callability.md`
  summary: Optional post-write projection polling so AuthoritativePending can advance to ProjectionConfirmed without a manual reload.
  evidence: Spec accepts a single post-write re-read; a lagging projection keeps Submitted/pending until the administrator reloads or acts again.
