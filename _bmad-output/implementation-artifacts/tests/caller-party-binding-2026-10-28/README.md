# Caller Party binding correction — local evidence

[Final reviewed results](review-1/review-fixes/FINAL-RESULTS.md): **6,001/6,001** full tests and five warning-free builds. The figures below describe the initial historical candidate.

**Superseding results:** [Final bounded caller-authority verification](review-1/review-fixes/FINAL-RESULTS.md). The figures below describe the original historical candidate; its logs and receipts are retained unchanged.

The bounded correction passes **5,947/5,947** tests across five warning-free Debug/source builds. The six focused Server suites pass **207/207**, including the executed caller-directory suite. The separate Live run exits 1 with `EXT-PARTIES-1`, `EXT-CONV-AI-1`, `EXT-SECRETS-1` and `EXT-HOST-1` unavailable and zero external calls. Full Story 5.4 and all owner deliveries remain incomplete.

`verification.json` records exact source and artifact SHA-256 hashes, counts, mapping identity and the unchanged readiness boundaries. `commands.json` preserves the exact commands and environment delta. `local/` contains all five original xUnit XML results, build logs and the verifier result; `focused.xml` retains the six-suite run. `live/` and `live.log` preserve the expected refusal.

The initial unmapped source build failed Conversations' new producer-mapping prerequisite (`unmapped-build.log`). An offline helper invoked the public Builds `SourceMappingMaterializer.WriteAsync` and its complete `Environment` using this Agents checkout and every actual root-declared `.gitmodules` path. It initialized no submodule and supplied no owner/tool qualification. The helper's exact source/project and build output are under `mapping-helper/`; generated imports and mapping are under `mapping/`.

The first full run inherited that environment into every process. Three build-contract tests correctly failed because their deliberate package/CI probes inherited source mode (`inherited-mapping-failure.log`). The validation-only launcher `dotnet-launcher.py` scopes the complete generated environment to `dotnet build` children and leaves native test processes and their probes clean. Its SHA-256 and exact invocation are recorded. The repository verifier API and its strict warning/execution gates were preserved; its only change requires the executed caller-directory suite.

The successful invocation used the temporary launcher directory prepended to `PATH`:

```bash
PATH=/tmp/agents-caller-identity-launcher:$PATH pwsh -NoProfile -File eng/verify-story-5.4.ps1 -Mode Local -ArtifactsPath /home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/tests/caller-party-binding-2026-10-28/local
pwsh -NoProfile -File eng/verify-story-5.4.ps1 -Mode Live -ArtifactsPath /tmp/agents-caller-identity-live
```

The launcher and generated mapping retain their exact temporary paths to describe the executed commands. Recreate the helper project/program at the paths in `commands.json` and copy `dotnet-launcher.py` to `/tmp/agents-caller-identity-launcher/dotnet` with executable permission before reproducing the Local invocation. The copied Live output remains byte-for-byte evidence from its separate artifact directory.

`baseline/` demonstrates two expected regression failures against the unmodified directory and gate from approved baseline `98c4dda84fe4ef5ec353bd8b92929132484a6f19`: the legacy Human read returned Valid without verified actor evidence, and bare tenant success reached Party lookup. A temporary `Baseline.targets` substituted those two `git show` copies only for compilation, selected the two regression assertions and wrote isolated output; no working source was reverted. Original source hashes, exact commands, target import, regression source, build log, failure log and XML are retained. The other Server support types remained at the implementation candidate, which is immaterial to the two baseline call paths. These fixtures prove local behavior only.
