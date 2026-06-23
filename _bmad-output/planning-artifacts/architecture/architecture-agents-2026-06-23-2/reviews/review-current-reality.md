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

