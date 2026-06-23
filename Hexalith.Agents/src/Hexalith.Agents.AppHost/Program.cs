// Minimal buildable Aspire AppHost for the Hexalith Agents module shell (Story 1.1).
//
// Scope guardrail: a *functioning* AppHost/topology is explicitly out of scope for the foundation story
// (AD-16; readiness-report Concern #5). This host creates an empty distributed-application builder so the
// project compiles and the solution is buildable; the real resource graph (Server + UI + DAPR state-store /
// pub-sub + provider components) is wired by the operational-topology stories.
IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.Build().Run();
