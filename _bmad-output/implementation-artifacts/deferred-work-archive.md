### DW-3: Optional post-write projection polling so AuthoritativePending can advance to ProjectionConfirmed without a manual reload.

origin: migrated from legacy ledger (""), 2026-09-08
location: Post-write setup-status projection refresh
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-enforce-complete-launch-readiness-before-callability.md`
reason: The spec accepts a single post-write re-read; a lagging projection keeps the submission pending until the administrator reloads or acts again.
status: done 2026-09-08
archived: 2026-09-18
resolution: resolved by sweep bundle dw-setup-projection-polling
resolution-undo: 1304c76d4ea1a612802deacd61c7dc7b6f8996c6c45f352f2ebd118674e18e36 2026-09-08 7374617475733a206f70656e

### DW-10: Canonicalize correlation identifiers echoed by setup read paths.

origin: code review round 2, 2026-09-14
location: src/Hexalith.Agents.Server/Application/Agents/EventStoreAgentAdministrationOperations.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: Read results previously echoed invalid or non-canonical caller metadata, including on denied and out-of-scope paths.
status: done 2026-09-15
archived: 2026-09-18
resolution: All setup read and out-of-scope results now echo only a canonical uppercase ULID; regression tests cover denied and cross-tenant reads.

### DW-20: Publish and consume the EventStore release that forwards no-op result payloads.

origin: product-owner decision from code review round 4, 2026-09-15
location: references/Hexalith.Builds/Props/Directory.Packages.props:8
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
severity: blocker
reason: The fix exists after EventStore 3.104.0, but package-mode Release and CI consume 3.104.0. The Story 5.2 verifier now requires 3.105.0 or later and intentionally remains red until that package is published and imported.
resolution: Hexalith.Builds catalog commit `dae84d5f96911517eb1e6f97e75eb88e759e6d8a` selects published EventStore `3.106.0`; audit commit `000abf867abc3a99cfa74d39b6e73af05c78a602` records the generated evidence and is reachable from Builds `origin/main`. Agents `origin/main` records that exact gitlink, Agents owns no local package version, and the Story 5.1 EventStore floor, Release/package, package-validation, and isolated-consumer lanes pass.
status: done 2026-09-16
archived: 2026-09-18

### DW-8: Prevent persistent MSBuild nodes from holding the readiness runner's captured output pipe.

origin: code review, 2026-09-14
location: tools/check-story-review-readiness.py
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: A persistent MSBuild child can keep the captured output pipe open until the runner's timeout; setting MSBUILDDISABLENODEREUSE inside the runner would make the workaround automatic.
status: done 2026-09-20
archived: 2026-09-21
resolution: The readiness runner now forces MSBUILDDISABLENODEREUSE=1 after applying caller overrides, with a regression test proving an inherited or supplied zero cannot defeat the safeguard.

