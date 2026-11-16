# Workflow hardening evidence — 2026-11-16

The local implementation completes exactly the five selected Story 6.1 patches. Generation keeps one protected result inside one activity while acquiring, observing and committing its exact ProposalMutation lease, then recording the result. Other checkpoints observe one command's exact owner fact with bounded cancellation-aware pacing. The workflow retries 100 times with a one-second initial interval, coefficient 2 and one-minute cap. Both terminal Provider refusals replay as public `Blocked`. Activity diagnostics contain only the exception type and a validated interaction ID, and a failing diagnostic sink cannot bypass the safe Dapr exception.

The final coordinator Story 6.1 Release/package gate passes **6,465/6,465**: Contracts **2,005**, Client **7**, Domain **1,047**, Server **2,044**, UI **1,362**. All five builds have zero warnings; native XML has zero failures, errors, skips and not-run cases. The full results include all **73** named workflow controls and all **38** affected review cases. The [manifest](verification.json) binds the exact **16** compiled code/test inputs, native results and executed reproduction command. It preserves the earlier 6,449-case gate and 57-case focused run under `preReviewEvidence`.

Review-fix verification passed **38/38** affected cases (checkpoint **22**, generation **14**, Confirmation Closing **2**), with zero warnings, failures, errors, skips or not-run cases. The final strengthened Confirmation replay assertions also passed a separate **2/2** selection. Retained focused XML and compiled input hashes are under `review-focused-02/` and `review-confirmation-03/` in `/home/administrator/verification/agents-workflow-hardening-20261116/`. No broad gate was run for these review edits; the coordinator owns final full verification.

| Required behavior | Executed controls |
| --- | --- |
| Generation and Closing | `GeneratedProposalMutationLeaseTests` covers absent/reserved/committed mutation leases, one generation seam call per uninterrupted activity, and Closing before acquire, before commit and after a winning commit. Existing Closing and failed-lookup tests retain persisted lease/result assertions. |
| Checkpoint agreement | `AutomaticInteractionCheckpointAgreementTests` covers delayed interaction and directory facts, unrelated revisions, rejection/exhaustion, one-command limits, cancellation during a wait and at the exact matching observation, and settled/replayed terminal refusals. |
| Restart | `AutomaticInteractionRestartIntegrationTests` injects faults before and after every command and compares complete replayed owner state and immutable attempt/version/post identities. This establishes no exactly-once external transport claim. |
| Retry, public status and diagnostics | Actual `WorkflowTaskOptions` properties are inspected; `AutomaticInteractionPublicStatusTests` covers audit visibility, serialized replay, later dispositions and phase-pinned safety mappings; `WorkflowActivityFailureSafetyTests` snapshots structured logs synchronously and covers poisoned errors, cancellation, invalid IDs and failing log sinks. |

The documented reproduction completed with exit 0 against the final reviewed inputs. Its retained logs, XML and copy/source bindings are under `artifacts/workflow-hardening-verification.Bmbm3J9Q/`; its disposable workspace and children exited before cleanup. Three independent review layers completed. Divergent facts, delayed generation transitions, cancellation/restart, complete protected results, bounded setup, exact command sequences and the Confirmation Closing race all have passing final controls. The retained `final-06/` run is historical pre-review evidence.

Reproduce the maintained package gate from the repository root using this self-contained command. It preserves complete tracked inputs, overlays current edits, uses only local Git copies, and retains output outside disposable scratch:

```bash
set -euo pipefail
WORKFLOW_ROOT=$PWD
mkdir -p "$WORKFLOW_ROOT/artifacts"
WORKFLOW_EVIDENCE=$(mktemp -d "$WORKFLOW_ROOT/artifacts/workflow-hardening-verification.XXXXXXXX")
export WORKFLOW_ROOT WORKFLOW_EVIDENCE
run-with-temp bash <<'JOB'
set -euo pipefail
export MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0
export DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 UseSharedCompilation=false
git clone --quiet --shared --no-recurse-submodules "$WORKFLOW_ROOT" "$TMPDIR/repo"
git clone --quiet --shared --no-recurse-submodules "$WORKFLOW_ROOT/references/Hexalith.Builds" \
    "$TMPDIR/repo/references/Hexalith.Builds"
rsync -a --delete --exclude=.git --exclude=references --exclude=/artifacts \
    --exclude=bin --exclude=obj --exclude=node_modules "$WORKFLOW_ROOT/" "$TMPDIR/repo/"
rsync -a --delete --exclude=.git --exclude=references --exclude=bin --exclude=obj \
    --exclude=node_modules "$WORKFLOW_ROOT/references/Hexalith.Builds/" "$TMPDIR/repo/references/Hexalith.Builds/"
cd "$TMPDIR/repo"
python3 - <<'BIND'
from pathlib import Path
import hashlib, json, os, subprocess
source = Path(os.environ['WORKFLOW_ROOT'])
def files(*args):
    return subprocess.check_output(['git', '-C', str(source), *args], text=True).splitlines()
paths = set(files('diff', 'HEAD', '--name-only', '--', 'src', 'test'))
paths.update(files('ls-files', '--others', '--exclude-standard', '--', 'src', 'test'))
bindings = {}
for name in sorted(paths):
    expected = hashlib.sha256((source / name).read_bytes()).hexdigest()
    actual = hashlib.sha256(Path(name).read_bytes()).hexdigest()
    if expected != actual:
        raise SystemExit('Source copy mismatch: ' + name)
    bindings[name] = expected
Path(os.environ['WORKFLOW_EVIDENCE'], 'source-bindings.json').write_text(json.dumps(bindings, indent=2) + '\n')
BIND
pwsh -NoLogo -NoProfile -File eng/verify-story-6.1.ps1 -DependencyMode Package \
    -ArtifactsPath "$WORKFLOW_EVIDENCE/package" > "$WORKFLOW_EVIDENCE/package-gate.log" 2>&1
python3 - <<'STABLE'
from pathlib import Path
import hashlib, json, os
source = Path(os.environ['WORKFLOW_ROOT'])
bindings = json.loads(Path(os.environ['WORKFLOW_EVIDENCE'], 'source-bindings.json').read_text())
for name, expected in bindings.items():
    if hashlib.sha256((source / name).read_bytes()).hexdigest() != expected:
        raise SystemExit('Source changed during verification: ' + name)
STABLE
JOB
printf 'Retained verification: %s\n' "$WORKFLOW_EVIDENCE"
```

No nested submodule is initialized. The maintained verifier builds individual projects and runs their native suites; its logs, XML and result summary remain under the printed retained directory after scratch cleanup.

Debug/source remains blocked before Agents compilation by the unchanged EventStore `ProductionProfileValidationResult.Authorization`/`.Qualification` CA1822 diagnostics and Conversations' missing Platform tool-selected source mapping. The coordinator reproduced both failures against all 16 final code/test inputs in `/home/administrator/verification/agents-workflow-hardening-20261116/coordinator-source-01/`; no source test pass or suppressed gate is claimed.

Earlier compilation/log-capture attempts remain historical. The broad stale-submission failure correctly required a safe retry after rejection; its fixture now preserves the original revision-scoped cached-receipt recovery assertions. Another broad attempt exposed a cancellation timer firing before dispatch; the timer now starts at the accepted-dispatch boundary. Obsolete broad runs `final-02` through `final-05` were interrupted at the coordinator's request after retaining their failures; each `interruption.json` records exact owned PIDs/groups, exit 143 and zero remaining live owned processes. `final-06` supplies pre-review package success evidence.

Live Dapr, production restart/RTO, migration and external owner acceptance remain outside this local scope. Polling bounds completed observations and pacing; noncooperative host reads still require a qualified operation deadline, recorded in the deferred ledger. All in-scope review corrections are verified. Root main divergence, clean submodules, remote availability and Conventional Commit checks precede the authorized Git delivery.

The baseline [CI run](https://github.com/Hexalith/Hexalith.Agents/actions/runs/43950635060) already fails the policy step `Require the pinned Builds workflow source`; its product build/unit-test job succeeds. The workflow expects Builds `264e17d3185c87b72b0757dd66a13040793756bb`, while the recorded checkout is `fdda17c0a4538b0f7c99f813c20ca24b49006530`. The policy job also references absent `package.json`, `eng/verify-story.ps1` and `eng/verify-story-5.2.ps1`. No CI policy or dependency pin is changed here, and no complete hosted-CI success is claimed.
