# Identity completion corrections — 2026-10-07

This packet verifies the accepted owner-local corrections in `spec-5-4-identity-completion-and-readiness-fixes.md`. The initial implementation check executed **236 passing source tests** and **31 synthetic script/path cases**. Those prereview script checks are preserved as historical evidence; the review corrections and their targeted results are recorded below. Synthetic localhost inputs and receipts establish script behavior only. They establish no dependency acceptance, available production seam, authenticated owner fixture or Live qualification.

The seven selected Parties files were copied to `pre-change/` before mutation; `pre-change-manifest.json` records their exact SHA-256 hashes. `post-change/` captures the corrected files. `correction.diff` compares those snapshots, so existing owner changes are preserved separately from these corrections. No staging, commit, push, submodule update, deployment, owner message, acceptance-field edit or original-story closure was performed.

`evidence.json` contains exact build/test command arguments and working directories, executable assembly hashes, source hashes and the historical broad Local blocker. `artifact-manifest.json` hashes the packet. No passing source lane was reused from earlier evidence.

| Lane | Classes and executed counts | Result |
| --- | --- | --- |
| Parties domain/query | Identity admission 3; identity query 81; retention configuration 2; SDK query 54 | 140 passed; zero errors/failures/skips/not-run |
| Parties client | Identity client 18; production DI 11; command client 37; query client 30 | 96 passed; zero errors/failures/skips/not-run |
| PowerShell | Both verifier scripts parsed; owning `git diff --check` passed | Passed |
| Synthetic HTTP/path | 28 HTTP/gate cases; Local relative path; Local after `Set-Location`; complete Live gate | 31 expected outcomes verified |

Both source projects were freshly built in the owning Parties repository with Debug project references and isolated `/tmp/hexalith-agents54-completion-artifacts` output, using the exact prescribed root overrides, `NuGetAudit=false`, serialized build and `MinVerVersionOverride=1.0.0`. Both final builds reported zero warnings and errors. Individual built xUnit v3 assemblies were invoked with single-dash `-class` and `-result-xml` arguments. The final authoritative logs/XML are `query-build.log`, `query-tests.log`, `query-tests.xml`, `client-build.log`, `client-tests.log`, and `client-tests.xml`. Initial build logs retain two corrected test-authoring failures; `client-tests-prebuild-failure.log` records an attempted run before the first successful client build.

Reproduce the script verification with:

```sh
python3 /home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/tests/identity-completion-2026-10-07/verify-readiness-fixtures.py
```

The harness binds only `127.0.0.1`. It creates separate synthetic registers/receipts and never changes the actual dependency register. Its real-register case checks the gate and observes zero HTTP requests. Local path probes replace only build invocation with a deliberately failing synthetic `dotnet` executable, forwarding PowerShell tool startup to the real runtime; they verify path resolution rather than claim any Local lane passed. Inputs, per-case logs, commands, exit codes and request counts are under `synthetic-verifier/`. The oversized decoded fixture is retained as compressed JSON to keep the packet small; rerunning the harness regenerates it.

| Spec scenario / review issue | Executed coverage |
| --- | --- |
| Current source and custody authority changes (V01) | `CurrentAuthorityChangesDuringAwait_DenyEvidence` suspends either source or custody, withdraws authority, then completes; no evidence is released |
| Current interval/custody exact boundary and lower-bound rollback (E02/V02) | `CurrentCompletionTimeChangesDuringCustody_DenyEvidence` covers exact interval end, exact custody expiry, rollback before source observation and rollback before binding start |
| Completion-aware current client | `DelayedReply_ValidatesCompletionTime` covers a valid reply, delayed interval/custody expiry, source/binding rollback and future observation |
| Completion-aware historical client (B03) | The same delayed-reply theory preserves a closed historical interval before custody expiry and refuses exact custody expiry/future action |
| Constructor compatibility | `AddPartiesClient_ResolvesIPartiesIdentityClient` resolves the production typed client with and without registered `TimeProvider`; `AddPartiesClient_IdentityAcceptanceUsesRegisteredClock` executes controlled replies and proves acceptance just before the registered clock reaches expiry and refusal exactly at expiry; the HttpClient-only public constructor is retained |
| Independent scope/expiry/opening basis (E04/B08/E05/V04) | Separate mutations of nested tenant/Party, actor revision, source/provenance, interval starts/ends, custody expiry/lifecycle/evidence ID, opening scope/duration and policy duration are refused; positive controls use exact 365-day custody |
| Transition interval reconstruction (B09) | Positive revoked and rebound fixtures pass; returned intervals ignoring later revocation/rebind fail |
| Checkpoint basis | Newer and older query checkpoints fail; original opening-position mismatch fails; independent/query observation IDs may differ because each source read issues a distinct observation; missing query observation fails |
| Receive limits (B10) | Declared >32 MiB refusal occurs before body receive; chunked >32 MiB refusal interrupts the server before its 40 MiB body completes; decoded >16 MiB is separately refused |
| Receipt gate (V03) | Mismatched exact-target receipt fails before HTTP; actual Uncommitted register fails before HTTP |
| Relative caller paths (B11/E03) | Wrapper LiveReadiness positive control uses relative evidence/register/artifact/Memories paths; Local probes verify absolute build evidence/artifact/Memories paths after the owner directory change and after a caller `Set-Location` |
| Complete Live gate | Missing inputs retain the full Live refusal and produce no success receipt |

The review correction run is deliberately limited to the edited client DI/helper files and readiness script/harness. `review-fix-client-build.log` records a clean focused Debug build. `review-fix-di-tests.log` / `.xml` record 13 passing DI tests, including both controlled-clock outcomes. `review-fix-synthetic.log` and `review-fix-synthetic/results.json` record 15 targeted synthetic cases: valid revoke and rebind controls, transition interval mutations, singleton-array rejection, three string-revision rejections, two malformed timestamp grammar cases, wrong/past revocation expiry, a shortened opening without any closing transition, a seven-digit UTC-ticks control and a 307 redirect refusal with zero destination requests. These cases remain synthetic script evidence only. Full final verification is performed by the parent after this correction handoff.

The original review-ID mapping above follows `lifecycle-review-2026-10-07/triage.json`: B03 is client completion expiry; E02 is the current lower time bound; V01/V02 are current authority/completion regression coverage; B08/E05/V04 are exact immutable expiry; B09 is transition proof; B10 is streaming bounds; B11/E03 are relative paths; E04 is nested binding scope; V03 is exact-target receipt coverage. The subsequent child review's Blind 1–5 and 8, Edge 1–2 and Verification 1–2 are covered by the targeted correction cases described here.

The earlier broad Local command remains distinct:

```sh
pwsh -NoProfile -File eng/verify-ext-parties-1.ps1 -Mode Local -ArtifactsDirectory /tmp/parties-story54-local-verifier-20261006/artifacts -EvidenceDirectory /tmp/parties-story54-local-verifier-20261006
```

Its historical result was 489 passes and one failure: `PartyDomainProcessorValidationTests.ProcessAsync_ProtectedHistoricalPayloadWithDestroyedKey_RedactsAndContinuesRehydration`, where the sibling strict replay path rejects `json-redacted`. Its dated source record and preserved failure log are hashed in this packet. That gate was neither changed nor rerun to claim a pass, and none of its earlier passing lanes is reused as current evidence.

The original Story 5.4 remains draft/backlog. The four owner records remain Uncommitted. B01/B02/B04/B05/B06, production custody/destruction/restore, full installed P-01–P-10, independent erasure certification and complete Live qualification remain unresolved. The accepted 365 fixed days from binding-effective-at, exclusive expiry, historical action-time intervals, original source positions and Branch B Organization identity are preserved.
