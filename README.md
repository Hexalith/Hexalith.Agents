# Hexalith.Agents

The Hexalith Agents module.

Packages target .NET 11 and require a .NET 11 compatible application. Building from source requires
the .NET SDK selected by `global.json` (11.0.100).

Provider-catalog command status is unavailable through the current default SDK reader.
Provider-catalog commands use reserved `system` scope, which EventStore 5.1.1 rejects on its
workload command-status route. An approved platform status route or host-provided scoped reader
is required before those commands can obtain terminal status through this integration.

See [CONTRIBUTING.md](https://github.com/Hexalith/Hexalith.Agents/blob/main/CONTRIBUTING.md) for setup, build and test commands,
commit conventions, release policy, and coordinated dependency updates.
