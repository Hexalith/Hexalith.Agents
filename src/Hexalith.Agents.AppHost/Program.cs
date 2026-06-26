// Minimal buildable Aspire AppHost for the Hexalith Agents module shell (Story 1.1).
//
// Scope guardrail: a *functioning* AppHost/topology is still deferred (AD-16; readiness-report Concern #5).
// The host initializes the shared EventStore security resource now so later topology work does not duplicate
// Keycloak/OIDC wiring; the real resource graph (Server + UI + DAPR state-store / pub-sub + provider
// components) is wired by the operational-topology stories.
using Hexalith.EventStore.Aspire;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

_ = builder.AddHexalithEventStoreSecurity();

builder.Build().Run();
