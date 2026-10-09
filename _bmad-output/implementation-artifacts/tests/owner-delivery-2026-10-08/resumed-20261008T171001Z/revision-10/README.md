# Owner prerequisite revision 10 focused source evidence

This packet records the revision 10 source corrections and focused Local checks. `evidence.json` contains SHA-256 and byte lengths for 19 current source/packet paths, 18 logs/XML files, the observed repository HEADs and working-tree status, and parsed XML results. `source-after/` contains the exact current files. `owner-packets/before/` and `owner-packets/before-sha256.json` preserve the four packet bytes before their revision 10 additions. The prior engineering source comparison is revision 9's `source-after/`; that historical snapshot is included in `evidence.json` where the same path exists. Some EventStore source changes were incorporated by a concurrent commit while this work was in progress. This packet does not claim a complete uncontended before capture for every source file.

## Focused execution

The exact executed build and test commands, working directories, XML destinations and log redirections are in [commands.txt](commands.txt). Each command's standard output and error are in `<name>.log`. XML contains only passing executed tests, with no failures or skips.

| Root | Exact class or method filter | Result |
| --- | --- | ---: |
| `../platform` | `tests/Hexalith.Platform.Custody.Tests/bin/Debug/net10.0/Hexalith.Platform.Custody.Tests -class Hexalith.Platform.Custody.Tests.ReplicatedSecurityObservationSpoolTests` | 53/53 |
| `../platform` | `tests/Hexalith.Platform.Custody.Tests/bin/Debug/net10.0/Hexalith.Platform.Custody.Tests -class Hexalith.Platform.Custody.Tests.Fr34ProtectionGateTests` | 34/34 |
| `../eventstore` | `tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests -class Hexalith.EventStore.Server.Tests.Security.DeletionConsumptionActorTests` | 57/57 |
| `../eventstore` | `tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests -class Hexalith.EventStore.Server.Tests.Security.GovernanceScopeGuardTests` | 71/71 |
| `../eventstore` | `tests/Hexalith.EventStore.Contracts.Tests/bin/Debug/net10.0/Hexalith.EventStore.Contracts.Tests -class Hexalith.EventStore.Contracts.Tests.Security.RecoverableAnchoredStateTests` | 14/14 |
| `../parties` | `tests/Hexalith.Parties.Tests/bin/Debug/net10.0/Hexalith.Parties.Tests -method Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CertifiedExpiredEstablishmentRetainedRevocationAndReboundResolveThroughQuery` | 5/5 |
| `../eventstore` | `tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests -class Hexalith.EventStore.Server.Tests.Security.RetainedIdentityHistorySourceReaderTests` | 78/78 |

The source history filter was rerun to check the two payload-version failures in the historical revision 9 broad matrix. Its current compiled class passes; a fresh full matrix and source-freshness gate remain for the root.

The four affected test projects were also built with `dotnet build <test-project.csproj> --configuration Debug --no-restore -m:1 -p:UseHexalithProjectReferences=true -p:NuGetAudit=false --verbosity quiet`. Platform and Parties additionally used `-p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/eventstore`. The exact project paths are the build log names, and all four build logs show zero warnings and errors. `git -c core.whitespace=cr-at-eol diff --check` returned zero in Agents, EventStore, Platform and Parties at capture.

## Completion boundary

The four owner packets map the full contract to existing source and the specific remaining runtime inputs. This revision made no Conversations or AppHost source change and did not invoke a Live seam. Complete Parties actor/certificate/custody/restore authority, Conversations service Party and worker/approval/receiver enrollment, production signing and FR34/custody providers, and Host's complete resource/credential manifest and qualified conditional replicated backend remain unestablished. The original Story 5.4 stays draft/backlog; four dependencies stay Uncommitted with complete targets and accepted compatibility commands TBD. This focused packet is not full owner acceptance or independent review.
