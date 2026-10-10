# Story 5.4 owner prerequisites — revision 12 exact-target gate

Observed 2026-10-10T12:14Z. The [machine-readable results](pinned-command-results.json) record the four commands pinned by the [dependency register](../../../../planning-artifacts/external-dependency-register.md), their clean detached owner commits, exits and output. Each exact-HEAD and clean-worktree preflight passed before its verifier ran. No root-declared or nested submodule was initialized in these temporary checkouts. All four Live/Full commands exited 1 at their intentional closed gates; none produced passing Level 4/5 evidence or makes its record Available.

| Record | Exact target | Result |
| --- | --- | --- |
| EXT-PARTIES-1 | `56cb4401c4179d431385ad3d931fefa402e4fc5a` | Twelve named live inputs missing before P-01–P-10. |
| EXT-CONV-AI-1 | `8469c5c6891d12b43d2082136b1ca7ddfac52c3b` | Candidate catalogue/feed and delivery lack installed current authority, worker enrollment, authenticated receiver acknowledgement and production source qualification. |
| EXT-SECRETS-1 | `f448db29493c1b30b0f18380188fe99b8b7606fe` | S1–S4/v23 production custody/profile, independent authority and persisted installation/qualification unavailable. |
| EXT-HOST-1 | `f448db29493c1b30b0f18380188fe99b8b7606fe` | H1–H4 composition, providers and persisted qualification unavailable. |

## Retained spool inventory

The source key is `system/security-observations/{InstallationEpoch}` in the separately provisioned Dapr `ComponentName`. The current [snapshot type](../../../../../../platform/src/Hexalith.Platform.Custody/SecuritySpoolSnapshot.cs) has `ReservedArchiveDigest` and `ArchiveReservationCount`; a pre-revision-11 serialized head would lack those properties. A restored head's digest is authenticated against the independent state anchor after deserialization, so raw pre-change bytes and anchor values are needed to decide whether compatibility logic is required. Synthetic test fixtures are not retained production state.

Read-only Kubernetes inspection at 2026-10-10T12:10Z listed Dapr components only in `hexalith-memories`: `access-telemetry-config`, `access-telemetry-secrets`, `access-telemetry-store`, `llm-openai`, `pubsub`, `secretstore`, and `statestore`. A name-only scan of Deployments, StatefulSets, DaemonSets, Jobs, CronJobs, Pods and Services found no Agents, Parties, Conversations or security-spool-named workload; an image-name scan at 12:24Z also found no Hexalith Agents/Parties/Conversations image. This does not exclude differently named services or storage outside Kubernetes. No installed Agents spool component, installation epoch, independent anchor, or retained head/archive bytes were identified, so the older-byte question remains unverified. No compatibility migration was introduced without those bytes.

The Hexalith.Platform tree was checked after the user identified it as the manifest location. Its tracked `apphost.cs` refuses `Platform:Agents:Enabled=true` before composition, `apphost.run.json` is a localhost Development profile, and `DaprComponents/statestore.yaml` is a local Redis development component scoped to `eventstore`, `works`, `eventstore-admin` and `eventstore-operations`. The tracked files and non-ignored working tree expose no Agents/Parties/Conversations deployment manifest, application credential-reference map, or qualified replicated spool target. The local Redis component is not an Agents spool binding.

The runtime-reference inventory remains in [owner-inputs.md](../owner-inputs.md). Accepted Branch B, retention, timing and service-Party choices were unchanged.

[Remaining engineering and runtime gaps](remaining-gaps.md) maps each record to its closed source gate and the exact missing installation evidence.
