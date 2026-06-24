# Story 3.6 Test Summary

Date: 2026-06-25T00:04:53+02:00

## Build

- `dotnet msbuild Hexalith.Agents.slnx /t:Build /p:Configuration=Release /m:1 /v:minimal /nologo`
  - Result: pass, 0 errors.
- `dotnet build Hexalith.Agents.slnx --configuration Release -m:1 --no-restore --nologo`
  - Result: pass, 0 warnings, 0 errors.

## Restore

- `dotnet restore Hexalith.Agents.slnx --nologo`
- Result: failed in this sandbox with exit code 1 and no console output.
- Fallback used: `dotnet msbuild src/Hexalith.Agents.Contracts/Hexalith.Agents.Contracts.csproj /t:Restore /v:diag /nologo`
- Fallback result: pass; restore reported all projects up-to-date for the sampled project graph.

## Tests

VSTest (`dotnet test`) was blocked by sandbox socket permissions (`System.Net.Sockets.SocketException (13): Permission denied`), so the built xUnit v3 executables were run directly.

- `tests/Hexalith.Agents.Contracts.Tests/bin/Release/net10.0/Hexalith.Agents.Contracts.Tests`
  - Total: 282, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0
- `tests/Hexalith.Agents.Tests/bin/Release/net10.0/Hexalith.Agents.Tests`
  - Total: 648, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0
- `tests/Hexalith.Agents.Server.Tests/bin/Release/net10.0/Hexalith.Agents.Server.Tests`
  - Total: 334, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0
- `tests/Hexalith.Agents.UI.Tests/bin/Release/net10.0/Hexalith.Agents.UI.Tests`
  - Total: 525, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0

## Coverage Added

- Contracts: terminal state/status ordinals, terminal command/event/rejection marker interfaces, safe result/evidence serialization.
- Domain: reject/abandon/expire terminal transitions, fail-closed authorization decisions, terminal idempotency, post-terminal approve/edit/regenerate guards, no-drift policy decisions.
- Server: reject/abandon authorization and fail-closed dispatch, structural no-dispatch guards, deterministic expiry dispatch/no-dispatch paths, cancellation propagation.
- UI: terminal queue historical reads, reject/abandon controls, deferred gateway defaults, callback/status behavior, Escape cancel path.
