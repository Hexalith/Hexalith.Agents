# Independent findings on initial HTTP continuation

All three context-free layers completed before triage. The initial reviewed diff is preserved separately. IDs below retain each individual finding before grouping.

| ID | Layer | Finding |
| --- | --- | --- |
| B1 | Blind | Fixture sets symmetric SigningKey without clearing inherited Authority or fully isolating workload/array authentication options. |
| B2 | Blind | caller-header and channel-only generate identical requests, leaving both intended credential-only vectors unisolated. |
| B3 | Blind | human-bearer is a workload assertion plus a valid assertion header and proves credential conflict rather than valid human-only denial. |
| B4 | Blind | No correctly signed assertion names a disallowed workload; foreign signature and unsigned caller conflict do not exercise the allow-list. |
| B5 | Blind | Actor/pubsub tests observe the command processor instead of their dispatch dependencies and lack valid-channel delivery controls. |
| B6 | Blind | Inventory validation only inspects existing endpoints; a removed untested catalog route can escape that test. |
| B7 | Blind | Changed receipt/revision alter mocked decoded evidence rather than independent lifecycle authority over unchanged restored evidence. |
| E1 | Edge | /healthz/ routes to actor health but misses the degradation middleware's exact infrastructure exemption. |
| V1 | Verification | Removed Program comments are required by a normal-CI architecture test; the scoped matrix excludes that test. Its /tmp binary cannot locate repository source, so that attempt did not reproduce the assertion itself. |

Blind floor: 38,278 bytes / 1,000 = 38.278 kB; min(floor(sqrt(38.278) + 1), 10) = 7. Edge returned one finding. Verification returned one Other finding and no preverified gap finding.
