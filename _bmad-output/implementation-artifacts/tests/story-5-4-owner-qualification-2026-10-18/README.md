# Story 5.4 owner qualification run — 2026-10-18

These runs follow the owner acceptance recorded in Agents commit `24b771d481083d04971a820238be18a638d5d889`. Each of the four register-pinned owner commands was run exactly once.

- **Command source:** each command text was taken verbatim from the register committed at that commit.
- **Working directory:** a clean clone of that commit, used as the Agents repository root.
- **Owner checkouts:** clean `../` clones at the exact accepted targets ([layout-heads.txt](layout-heads.txt)).
- **Submodules:** only Platform's root-declared `references/Hexalith.Builds` and `references/Hexalith.EventStore` were initialized, at their gitlinks, non-recursively.
- **Isolation:** the user's working owner checkouts were not touched.

**No record became `Available`.** Every command exited non-zero, which matches the closed gates and missing installed inputs documented in the [reconciliation](../../story-5-4-dependency-reconciliation-2026-10-18.md).

## Results

| Record | Target | Exit | Outcome | Evidence |
| --- | --- | --- | --- | --- |
| EXT-PARTIES-1 | `56cb4401c4179d431385ad3d931fefa402e4fc5a` | 1 | Live gate: twelve missing Live inputs, starting with `EXT_PARTIES_GATEWAY_URL`. The verifier at this target has no pass path. | [EXT-PARTIES-1/](EXT-PARTIES-1/) |
| EXT-CONV-AI-1 | `87e8b4a6ec3224b20a560b0e83de857805925572` | 1 | `BLOCKED [LIVE_MANIFEST_MISSING]`: `/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/live-manifest.json` is not installed. The runner blocks before its own target and register checks; only the wrapper's exact-HEAD and clean preflight ran. Zero Live calls. | [EXT-CONV-AI-1/](EXT-CONV-AI-1/) |
| EXT-SECRETS-1 | `abbec4093715c1db854433119b32d4e16df20d08` | 1 (qualify exit 2) | `DependencyNotAvailable`, 67 missing inputs, `effectsStarted: false`, `qualificationEffects: 0`. The register target, exact command, `RuntimeQualificationBinding`, configuration and plan pins all matched. Only `PinnedPublicAnchorSha256` (three null anchors) and the installed-input or implementation items remained. No recovery journal was created, because preflight stops before journal creation. | [EXT-SECRETS-1/](EXT-SECRETS-1/) |
| EXT-HOST-1 | `abbec4093715c1db854433119b32d4e16df20d08` | 1 | `DependencyNotAvailable: EXT-HOST-1`. The Platform pins and the 13 eng Python tests passed. The custody suite passed 129/129 and `GovernanceScopeGuardTests` passed 71/71, both exit 0. Blocked gaps: `partiesBinding`, `conversationsBinding`, `protectionEngine`, `protectionObserver`. `effects: 0`, `liveEffects: 0`. The verifier at this target returns 1 on every path, and these suites are unit/component evidence, not live Level 4. | [EXT-HOST-1/](EXT-HOST-1/) |

Each `result.json` records:

- the register commit and its SHA-256;
- the accepted status at run time;
- the target;
- the exact command and working directory;
- UTC start and finish times;
- the exit code.

`stdout.log` and `stderr.log` are the raw streams. `tmp-output/` holds copies of what each command wrote under `/tmp`. Pre-existing `/tmp/ext-parties-1-live`, `/tmp/ext-host-1-full` and `/tmp/ext-conv-ai-1-live` contents were copied to [preexisting-tmp/](preexisting-tmp/) before the run, with hashes in [preexisting-tmp-sha256.txt](preexisting-tmp-sha256.txt). The fixed command paths overwrite those `/tmp` locations.

The EXT-SECRETS-1 detached packet was regenerated at `abbec40` from the owner configuration, with `runtime.acceptedReplacementCommit` set to the accepted target. The canonical copy is under `/home/administrator/hexalith-secrets-evidence/ext-secrets-1/acceptance-abbec40-20261018/`; [EXT-SECRETS-1/detached-packet/](EXT-SECRETS-1/detached-packet/) mirrors it. It holds public references only.

## Not run

No `OQ-5.4-1` cohort run was attempted, because its prerequisites are unmet:

- `EXT-PROTECTION-1` is `Uncommitted`.
- No host verifier has a pass path or a pinned profile.
- The installed inputs and trust anchors are absent.

No consuming Agents runtime, test or qualification path ran.
