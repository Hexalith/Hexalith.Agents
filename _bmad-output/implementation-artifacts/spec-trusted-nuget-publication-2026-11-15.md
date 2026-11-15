---
title: 'Require NuGet trusted publishing for release and recovery'
type: 'bugfix'
created: '2026-11-15'
status: 'done'
baseline_commit: '23ce124203d14874c6c60196ec2023e86b9db30a'
route: 'dispatch'
review_loop_iteration: 0
context:
  - /home/administrator/projects/hexalith/agents/AGENTS.md
  - /home/administrator/projects/hexalith/agents/references/Hexalith.AI.Tools/hexalith-llm-instructions.md
  - /home/administrator/projects/hexalith/agents/references/Hexalith.AI.Tools/hexalith-git-instructions.md
---

<frozen-after-approval reason="user explicitly requires trusted connections and previously authorized pushing and release without human validation">

## Intent

**Problem:** The maintenance change added a stored-key fallback and left recovery dependent on a stored secret. NuGet rejected that credential; the user explicitly requires trusted publishing instead.

**Approach:** Make release and recovery obtain their publication credential exclusively through the pinned NuGet/login OIDC action. Preserve the existing automated source, freeze, environment, provenance, archive and publication checks. Then push the correction, wait for green CI, attempt a trusted release, and verify NuGet publication.

## Boundaries & Constraints

**Always:** Use the configured NUGET_TRUSTED_PUBLISHING_USER and production environment, request id-token: write on each publication job, and pass only the NuGet/login step's short-lived output to existing publisher scripts. Keep source-check, original-tag, retained-archive and six-package integrity gates intact. Preserve user changes and already-pushed maintenance work. Use run-with-temp for disposable verification and retain useful logs outside TMPDIR.

**Never:** Read a stored NUGET_API_KEY secret in release/recovery, select authentication from its presence, add a fallback, bypass a failed OIDC exchange, expose credentials, delete organization secrets, rewrite tags/history, or loosen original-source recovery requirements.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Release enabled | Current main has green CI and matching NuGet trust policy | OIDC login supplies the only publication credential | Login/push failures block publication |
| Recovery enabled | Original source remains current green main and retained draft passes all checks | Recovery uses its own OIDC login and original archives | Reject mismatched source, tag, archives or policy |
| Legacy secret exists | Organization still shares an obsolete NUGET_API_KEY | Workflow ignores it completely | No secret-dependent branch or fallback |
| Publication frozen | Publication flag is not exactly true | Existing publication freeze remains effective | Never request credentials for a disabled publication job |

</frozen-after-approval>

## Code Map

- `.github/workflows/release.yml`: remove Select NuGet authentication; keep pinned NuGet/login at 8d196754b4036150537f80ac539e15c2f1028841 and its prepare-output guard; Semantic Release takes only steps.nuget-login.outputs.NUGET_API_KEY.
- `.github/workflows/recover-release.yml`: add id-token: write to the recover job, add the same pinned login before recovery, and pass its output to the existing command. Preserve exact main/ref/source and production conditions.
- `tests/tooling/release_authentication_test.py`: replace stored-key-selection tests with regression coverage for both trusted-only publication paths, permission/ordering, guarded execution, and absence of secret/fallback references.
- `test/Hexalith.Agents.Server.Tests/PackageInventoryTests.cs`: update the existing coupled release contract assertion from the rejected secret-fallback expression to the NuGet/login output only, and assert the stored-secret route is absent.
- `CONTRIBUTING.md`: document required policy owner variable and separate matching workflow policies for release.yml and recover-release.yml, environment production, and all six manifest package identities.
- `scripts/recover-release-packages.py`, `scripts/verify-release-source.py`: existing immutable recovery/source protections remain intact; final presence verification shares the bounded indexing defaults with the normal publisher.

## Tasks & Acceptance

**Execution:**
- [x] `.github/workflows/release.yml` -- restore trusted-only normal release -- remove the rejected credential route.
- [x] `.github/workflows/recover-release.yml` -- use OIDC for retained-archive recovery -- eliminate stored credentials from both supported publication paths.
- [x] `tests/tooling/release_authentication_test.py` -- verify trusted-only wiring, permissions and existing gates -- catch reintroduction of a secret route or unguarded publication.
- [x] `test/Hexalith.Agents.Server.Tests/PackageInventoryTests.cs` -- align the coupled release contract with trusted-only authentication -- keep the maintained C# CI gate consistent with the workflows.
- [x] `CONTRIBUTING.md` -- describe trusted setup and credentials without stored keys -- make the workflow's required identity explicit.
- [x] GitHub/NuGet -- commit, push, run CI and attempt trusted publication -- verify the actual service exchange and package availability.

**Acceptance Criteria:**
- Given an inherited legacy secret, when either workflow is inspected/executed, then no step reads it and publication requires the OIDC action output.
- Given a configured matching trusted policy, when the exact green source release runs, then all six published packages are verified against their original release archives.
- Given a missing/mismatched policy, when login runs, then report the exact matching-policy prerequisite without requesting an API key or bypassing authentication.

## Implementation Notes

- The user already authorized these changes, pushes and release; skip human approval checkpoints.
- The retained 2.0.0 draft is bound to source 23ce124203d14874c6c60196ec2023e86b9db30a, and all six NuGet versions are absent. Changing main makes its existing recovery guard refuse that older source. Preserve this draft unchanged; a fix commit on new green main should plan 2.0.1. Do not add an older-source exception.
- NUGET_TRUSTED_PUBLISHING_USER currently equals jpiquot; production has no approval rules. Test the actual trusted exchange after the change before treating an older policy failure as current.
- Implementation handoff changes local content and runs focused disposable checks only. The coordinator handles review, commits, pushes, CI, release dispatch, policy blockers and verification.

## Spec Change Log

- The successful trusted upload exposed the existing two-minute NuGet indexing budget: all six uploads were accepted, but the twelve-attempt presence check timed out. Extend the shared bounded presence budget to ninety ten-second waits (fifteen minutes), use it for recovery as well, and exercise visibility after twelve missing probes plus the existing exhaustion and integrity cases. Preserve the exact source, original archives, authentication and content checks.

- Implementation discovered the maintained C# package-inventory contract also asserted the rejected fallback expression. Added this existing coupled assertion to the Code Map and tasks; preserve all other source, environment, manifest and artifact assertions. This is a narrow consistency correction within the user's trusted-publishing instruction.

## Review Triage Log


| Finding | Verdict | Evidence and route |
|---|---|---|
| BH-01 | medium | GitHub string comparisons ignore case, so recovery's job condition admits True/TRUE. The Python exact flag rejection occurred after the new login. Patch an exact shell flag check before credentials and execute negative flag cases. |
| BH-02 | medium | Invalid recovery source arguments reach OIDC login before the existing source comparisons. Patch pre-login source equality and reuse the existing exact green main verifier; keep final recovery proofs unchanged. |
| BH-03 | low | The pinned shared preparation action describes and requires the individual NuGet policy creator; organization package ownership does not name that profile. Patch setup wording to distinguish the two. |
| BH-04 | low | The no-step-if assertion overconstrains future valid guarded execution and does not establish the actual credential prerequisite. Replace it with executable preflight and ordering assertions. |
| BH-05 | false | The Verification section lists required future checks, and the remote task is explicitly unchecked. It does not claim a successful exchange or publication. Run identifiers and results will be recorded when executed; reject a finding whose fix is editing this build's spec. |
| ECH-01 | medium | Same exact-string recovery freeze defect as BH-01, independently traced to GitHub case-insensitive expression comparison. Patch before login, not after credential issuance. |
| VG | false | The verification reviewer found no gaps in the implementation's focused validation. |

## Verification

- Focused authentication, release-source, recovery, semantic-planner and publisher tooling tests pass; no full .NET rebuild is needed for these workflow/documentation changes before CI.
- All active release/recovery workflow references to secrets.NUGET_API_KEY and authentication fallback selectors are absent; both publisher steps consume only their NuGet/login output.
- Existing main-only, source-SHA, production, freeze and archive checks remain present and covered by existing tests.
- After validated Conventional Commit and push, current CI/CodeQL/commitlint pass; normal Release confirms OIDC exchange and checks every package's remote content. Record any account-side blocker precisely.

- Review patch instruction: preflight recovery flag and exact green main through the existing verifier before NuGet/login, retain every subsequent source/archive gate, and clarify that NUGET_TRUSTED_PUBLISHING_USER names the individual policy creator. These directly enforce the existing frozen requirements without adding a public input or recovery exception.

- Local verification completed: 125 focused tooling tests; serialized Server Release/package build with zero warnings/errors; seven PackageInventoryTests without skips; both workflows passed actionlint. Review patches then passed coordinator reruns of eight authentication, fourteen source-proof and thirteen recovery tests. No product/runtime source or recovery script changed. At that local verification stage remote CI, trusted exchange and package publication remained pending; completed results are recorded below.

## Publication Evidence

- Trusted-only source: `4aa69ee30d30bddd643bdd2221c4b7e0cff7c302`. [CI](https://github.com/Hexalith/Hexalith.Agents/actions/runs/43761372107), CodeQL `43761372152`, and Commitlint `43761372053` succeeded at that exact source.
- After the user configured the connection, [Release](https://github.com/Hexalith/Hexalith.Agents/actions/runs/43763217914) attempt 2 authenticated through NuGet/login and NuGet accepted all six version `2.0.1` uploads. Its presence wait exhausted while NuGet indexed those accepted uploads; no package was repacked, overwritten or assigned another version.
- Once indexed, `recover-release-packages.py --verify-only` proved the original draft, both source tags, current green main, all seven GitHub assets, original provenance and every remote package's ordinary archive contents. The verified existing draft was then finalized as [v2.0.1](https://github.com/Hexalith/Hexalith.Agents/releases/tag/v2.0.1).
- Independent public-release verification passed again: exact source/tag, uploaded asset metadata/digests, original provenance and all six downloaded NuGet package contents. Evidence: `artifacts/trusted-nuget-2026-11-15/recovery-verify-only.log` and `independent-publication-verification.log`. The older v2.0.0 draft is retained unchanged.
- The indexing adjustment passed 25 publication-verifier tests and 13 recovery tests. This includes a package appearing on probe thirteen, bounded exhaustion, collisions, transient transport failures and archive mismatches. Release attempt 3 completed successfully, including the existing workflow's independent post-publication checks.

- Completion follow-up: all 12 root-declared submodules are clean, and each reports zero commits outside its origin remote refs. Release workflow attempt 3 completed successfully with all four jobs green, including exact release and NuGet content verification. The bounded indexing configuration and this evidence are recorded separately from the immutable published source.
