# Hexalith.Agents

The Hexalith Agents module.

Packages target .NET 11 and require a .NET 11 compatible application. Building from source requires
the .NET SDK selected by `global.json` (11.0.100).

Provider-catalog command status is unavailable through the current default SDK reader.
Provider-catalog commands use reserved `system` scope, which EventStore 5.1.1 rejects on its
workload command-status route. An approved platform status route or host-provided scoped reader
is required before those commands can obtain terminal status through this integration. Platform catalog
writes authorize first and return `Unavailable` without dispatch unless the registered
`IAgentCommandStatusReader.SupportsScope("system", "provider-catalog")` explicitly returns true.
Hosts must provide exact tenant/domain/message/correlation terminal evidence; capability does not
replace authorization. The SDK and deferred readers do not claim this platform capability.

Provider governance pages persist a prepared canonical message/correlation identity before submitting
catalog, tenant-enablement or terms writes. Prepared intentions do not claim acceptance. Failed initial
storage prevents submission; ambiguous responses and receipt-save failures retain the original resource
lock across refresh until exact terminal evidence. Session records contain only safe identities and
confirmation expectations, are scoped to subject/tenant, and refuse corrupt storage or authentication
transitions without clearing unresolved locks.

See [CONTRIBUTING.md](https://github.com/Hexalith/Hexalith.Agents/blob/main/CONTRIBUTING.md) for setup, build and test commands,
commit conventions, release policy, and coordinated dependency updates.
