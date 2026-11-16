# Ingress and consumer qualification — 2026-11-16

The current combined-source completion result is [FINAL.md](FINAL.md). The
reports and reproduction scripts below retain their original phase and source.

This report and its original receipts/results below describe **pre-integration**
source and checks. The concurrent upstream integration at
`c5c22620cafaa0af2625896cf41022e614e9b490` changed the preparation helpers and
product-hook surface; [INTEGRATION.md](INTEGRATION.md) records its separate focused
evidence and current source fingerprints. Prior gates are historical results,
not combined-source requalification. Original review rows and intent remain intact.

This batch reconciles exactly five selected ledger records: established intake directory scope, provider JSON casing, raw governance timestamps, verified platform provider queries, and executable consumer helper dependencies. It establishes local behavior only. DW-21 installed gateway/domain-host composition, owner credentials/acceptance, production availability, retained-history, migration and live-runtime qualification remain open.

The source baseline is `805b35da01efe4d3ac5f1402e611950ac282d0a1`. No dependency pin changed. [verification.json](verification.json) records the exact root-declared revisions, source hashes, native counts and consumer inventory. All disposable copies, package feeds and build outputs were created beneath `run-with-temp`'s TMPDIR, and child work completed before cleanup. No deployment, publication or nested-submodule initialization was performed.

## Boundary evidence

| Matrix row | Executed evidence |
| --- | --- |
| Foreign directory | `EstablishedForeignDirectoryScopeRemainsNotFoundWithoutEffects` covers intake and recovery, foreign tenant/Conversation, revisions 1/17/-1, and open/Closing states. Both owner snapshots and dispatch/scheduling remain unchanged. |
| Empty directory | `EmptyDirectoryObservationAllowsExactIntakeRecovery`, rejection-only delayed visibility, contention and Closing regressions preserve exact permit creation, acknowledgement, leases and scheduling with replayed owner end-state. |
| Ambiguous provider JSON | `GovernanceJsonIsUnambiguousAcrossEveryPolicyEntryPoint` covers all six commands, key order, camel/Pascal/upper-case controls and equivalent/conflicting same/mixed-case identity/enablement duplicates. Scope, source validation, coordinator digest and registered admission-intent adapters agree or refuse. Persisted event parsing is unchanged. |
| Raw governance time | `RawGovernanceTimestampRequiresExactPriorAuthorityAdmission` exercises back/future raw payloads through the real HTTP gateway, reserved admission and real aggregate Handle/Apply. The callback model produces only NotAuthorized rejection for missing authority; forged/false/wrong-key flags, wrong app/subject and proof/payload/time mismatch produce no accepted decision in the callback model. Exact prior server-stamped admission controls replay accepted state. `RawPlatformGovernanceTimestampsCannotBypassTheStorageScopeBoundary` confirms public create/update refusal of the historical system partition before dispatch. The callback event lists and Handle/Apply replay are in-memory model evidence, not durable-store transactions or a qualified private proof/admission implementation. |
| Verified provider query | `DispatcherBindsProviderQueriesToCurrentGlobalHumanAuthority` runs list and detail through the real SDK dispatcher and retains the workload principal. Forged subject/tenant/authority, missing/stripped binding, workload role without current human grant, stale/revoked/missing/foreign global evidence and a non-global envelope read no protected model. Verified context is cleared after dispatch. The real SDK registration test resolves actual authentication and query policy with no permissive scheme. These direct dispatcher tests bypass the `/query` endpoint administrator verifier. A working platform endpoint requires an owner-bound `IDomainServiceAdministratorVerifier` and authenticated HTTP positive control, retained as the pre-existing endpoint/DW-21 follow-up. |
| Consumer helper | Schema-2 receipts observe actual managed and native MSBuild/compiler loads, package ownership and bytes, separately from generated/SDK/OS provenance. Real source-generator and UsingTask fixtures execute separate managed/native helpers; isolated helper-only mutations refuse against an unchanged entry/import baseline. Actual external managed/native helper symlinks refuse. The full consumer fixture proves the task runs only once, in the observed Build. Missing/incomplete observation, an unobserved native process and unsupported platform refuse. |

## Pre-review verification retained

Before the review corrections below, the ingress matrix passed **219/219** cases. That owning Server suite passed **1,957/1,957**, and the owning UI suite passed **1,352/1,352** with zero failures/errors/skips/not-run cases. Their builds have zero warnings/errors. [Native matrix results](matrix.xml.gz), [Server results](Server.xml.gz), [UI results](UI.xml.gz) and corresponding compressed logs are retained.

The maintained Story 5.2 Release gate passed, including Contracts **2,005**, Client **7**, Domain **1,042**, Server **1,955** and UI **1,352** plus all focused/composition gates. The two additional raw-platform cases were added afterward and pass in the final 1,957-case Server run. [Story 5.2 log](story-5.2.log.gz).

The initial Story 6.1 Package run passed Contracts, Client, Domain and Server, then failed the existing UI `Pending_enablement_for_one_tenant_does_not_lock_same_model_for_another` immediate render assertion (expected reset revision 0, saw prior revision 5). The same case passed alone but failed again in a full UI suite. Its one-line correction waits for the same reset assertion after the tenant change; no product behavior or expected value changed. The final focused case and full 1,352-case UI suite pass. The original [Story 6.1 failure](story-6.1.log.gz) remains evidence. A final end-to-end maintained Story 6.1 Package rerun passed on the corrected test: Contracts **2,005**, Client **7**, Domain **1,042**, Server **1,957**, UI **1,352**, all warning-free with no failures/errors/skips/not-run cases. [Final gate log](story-6.1-final.log.gz) and [structured gate result](story-6.1-final-result.json.gz) retain that separate success.

## Source-build boundary

A bare Debug/source build initially refused the missing Conversations producer mapping. The maintained producer mapping was then prepared successfully in exact local source exports, preserving the direct dependency pins and exact additional owner roots. The mapped Debug Server build stopped on two existing `CA1822` errors in EventStore `ProductionProfileValidationResult.Authorization` and `.Qualification`, at lines 15 and 18. No audit, analyzer or warning gate was disabled and no owner API was changed. [Mapped source build](source-build.log.gz), [source preparation receipt](source-preparation.json), [preparation reproducer](verify-source.py).

Full tooling discovery ran **339** cases: **337 passed**, with **2 environment skips** and no failures. The skipped Story 5.2 and Story 5.3 real-runner compatibility cases both require a prebuilt Debug Domain assembly, which this source blocker prevents. Both unchanged real selector/summary tests are separately exercised against an actual Release/package Domain assembly in a disposable harness with an explicit path alias: **2/2 pass**, with no skips, including class/method selection and zero-test refusal. The final affected helper fixtures also pass **2/2**, and the final observer/policy unit lane passes **6/6**. The original discovery skips remain recorded rather than being relabeled as discovery passes. This is package-runner compatibility, not a claimed Debug/source build. [Fallback log](real-runner-fallback.log.gz), [explicit fallback harness](verify-real-runner.py).

## Consumer policy

The reviewed union preserves all **38** prior entries and adds **8** actual assembly observations, all matching an existing analyzer's exact package/version/path/SHA-256. The policy now has **46** union entries and still binds each individual consumer graph. No current package-owned helper outside those analyzer entries was observed; the executable fixtures prove helper behavior independently. [Final consumer receipt](consumer-receipt.json.gz) records all six individual packages and the combined consumer, their exact archives, SDK pin and observed managed/native provenance.

Observation requires Linux/glibc, `cc`, and permitted ptrace/inotify. The executable regression fixtures additionally require static libc development files; unavailable prerequisites fail explicitly before qualification and are never silently skipped. Unsupported complete-consumer entry refuses before restore or SDK builds. Other platforms, missing native traces and executable subprocesses without the corresponding managed observer refuse. OS libraries under resolved `/lib`, `/lib64` and `/usr/lib` roots and dotnet host/SDK/runtime libraries remain separate from the package policy. These local receipts do not certify a deployed runtime or package publication.

## Reproduction

Run from the module root, keeping requested evidence outside TMPDIR:

```bash
run-with-temp bash _bmad-output/implementation-artifacts/tests/ingress-consumer-2026-11-16/verify-gates.sh
run-with-temp bash _bmad-output/implementation-artifacts/tests/ingress-consumer-2026-11-16/verify-consumers.sh
run-with-temp python3 -B -m unittest discover -s tests/tooling -p '*test.py' -v
run-with-temp python3 -B _bmad-output/implementation-artifacts/tests/ingress-consumer-2026-11-16/verify-real-runner.py
```

`verify-story61.sh` repeats only the final maintained Story 6.1 Package lane. `verify-source.py` reproduces producer preparation and the recorded source-build boundary without changing checkout pins. `verify-native.sh` runs standard-layout Server/UI builds and native suites; `INGRESS_NATIVE_SUITES=UI` selects only the owning UI recheck. No debug-mode package substitution or gate weakening is hidden in these commands.

## Review correction evidence

The original full-suite, receipt and review history above is preserved and predates
these corrections. Only tests covering the edited files were rerun here; the parent
workflow owns the subsequent complete validation and delivery audit.

Kernel ptrace now records all process creation/exec independently of loader or
environment cooperation, including traced threads that launch children. Real
static and cleared-environment subprocesses successfully write their execution
markers and qualification refuses them. SIGTRAP semantics and initial/main-loop
EINTR handling have native probes. Inotify watches package directories and file
inodes across preprocessing and Build, so a changed native helper that runs and
atomically restores reviewed bytes still refuses. Exact archive/library-file
membership is required before assigning package provenance. The conditional-import
fixture requires `UseSharedCompilation=false` and verifies preprocessing uses the
same complete observed Build inputs. Actual byte-loaded and emitted assemblies
execute their helper code successfully and then refuse fileless provenance.

Tenant enablement retains required case-insensitive boolean Enabled presence;
missing/null cases refuse at every shared policy entry point, while false remains
valid. Duplicate/malformed JSON maps to the existing nonretryable HTTP 400
client-validation response before admission or aggregate/model effects. Intake
rechecks cancellation after the directory await before its scope refusal.

[Focused correction results](review-corrections.json) record **213/213** native cases, **4/4** executable helper/restore tests, **8/8** observer unit/probe cases, and **1/1** independent exact-membership follow-up, with no failures or skips. The compressed [native results](review-native-results.xml.gz), [helper fixtures](review-helper-fixtures.log.gz), [observer probes](review-observer-units.log.gz) and [membership control](review-exact-membership.log.gz) retain the current focused outputs and source fingerprints. Earlier
full verification counts are not attributed to these corrected bytes.

### SIGSTOP follow-up

The tracer now seizes the pipe-held initial child and distinguishes synthetic
`PTRACE_EVENT_STOP` traps from real signal-delivery stops. Genuine group stops use
`PTRACE_LISTEN`, so the tracee's real parent retains its `waitpid(WUNTRACED)` and
SIGCONT behavior. The observer does not request WCONTINUED notifications. The exact
fork/raise(SIGSTOP)/waitpid(WUNTRACED)/SIGCONT probe exits **0 directly and traced**.
The two focused kernel tests also retain detached double-fork waiting, ordinary
SIGTRAP termination and injected interrupted waits. The one focused adversarial
test retains all five subcases, including actual changed native execution followed
by atomic restoration and refusal. Both focused runs pass without skips:
[kernel probes](review-stop-probes.log.gz), [restoration/adversary regression](review-stop-restoration.log.gz).
No broader verification was rerun for this signal-only correction; the parent
workflow retains that work. Previous correction rows and evidence are preserved.
