// Minimal buildable host for the Hexalith Agents module shell (Story 1.1).
//
// Scope guardrail: this story delivers a buildable boundary, not a runnable topology. The canonical
// Hexalith shape is the EventStore domain-service host:
//
//     builder.AddEventStoreDomainService(typeof(AgentsAssemblyMarker).Assembly, ...);
//     ... app.UseEventStoreDomainService();
//
// That wiring (plus AddDaprClient / tenant-access registrations) lands with Story 1.2, once the Agent
// aggregate and contracts exist for the SDK assembly-scan to discover. Until then this two-line host keeps
// Hexalith.Agents.Server buildable without pulling EventStore server internals into the shell.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

WebApplication app = builder.Build();

app.Run();
