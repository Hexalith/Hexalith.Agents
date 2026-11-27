---
title: 'Determine the NuGet publication race for the six Agents packages'
type: 'chore'
created: '2026-11-27'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="user requested the next eligible deferred items">

## Intent

Determine whether a separate credential can publish an Agents package version between the absence check and publication. Verify whether NuGet.org offers an atomic reservation for the six-package release. Use the publisher authority records and the documented NuGet.org contract.

</frozen-after-approval>

## Implementation Notes

**Publisher authority.** CiCd's authenticated NuGet.org readback on 2026-11-22 at 14:29:00 UTC records nine active policies belonging to other repositories under the `Hexalith` package owner. Each policy had the `*` glob and direct scope to push new packages and package versions. On 2026-11-27, each of the six IDs in [the Agents release manifest](../../eng/release-packages.json) showed `Hexalith` as the owner in NuGet Gallery. The policy owner and the wildcard cover all six IDs. NuGet issues short-lived API keys to matching workflows, so a separate publisher could obtain a credential authorized to publish those same versions under the observed configuration. [Authenticated readback at CiCd commit `052fe731f4ffb2d1f3c139e8562634d540ccd2ec`](https://github.com/Hexalith/Hexalith.CiCd/blob/052fe731f4ffb2d1f3c139e8562634d540ccd2ec/_bmad-output/specs/spec-cicd/evidence/11/ad-7-ad-21-maintainer-repair-20261122.json), [CiCd's scope interpretation](https://github.com/Hexalith/Hexalith.CiCd/blob/b00126c0a8d6159e7cb527282f9811eda9b852f4/_bmad-output/specs/spec-cicd/stories/11-pilot-migration-commons-and-polymorphicserializations.md#maintainer-acceptance--2026-11-22), [NuGet policy scope and ownership](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing#select-scopes)

| Policy ID | Repository | Workflow | Environment |
| --- | --- | --- | --- |
| `16915` | `Hexalith.EventStore` | `release.yml` | `production` |
| `18269` | `Hexalith.FrontComposer` | `release-publish.yml` | `production` |
| `18601` | `Hexalith.Parties` | `release.yml` | `production` |
| `18928` | `Hexalith.Conversations` | `release.yml` | `production` |
| `20362` | `Hexalith.Memories` | `release.yml` | `production` |
| `20456` | `Hexalith.Platform` | `release.yml` | `production` |
| `20857` | `Hexalith.Projects` | `release.yml` | `production` |
| `20891` | `Hexalith.Timesheets` | `release.yml` | `production` |
| `22255` | `Hexalith.Chatbot` | `release.yml` | None configured |

The 2026-11-21 Agents adoption verified its narrowed policy `28307` for `release.yml`, `hexalith-release`, and `Hexalith.Agents*`. It separately verified removal of the legacy Agents wildcard policy `26964`. CiCd `origin/main` at `b00126c0a8d6159e7cb527282f9811eda9b852f4` still carries the November 22 authenticated inventory and the open scope-remediation item DW-44. That inventory is the latest authenticated readback found in that history. [Agents policy verification](https://github.com/Hexalith/Hexalith.CiCd/blob/62dc50b18888be88d9ce27415d22f612c55c50af/_bmad-output/specs/spec-cicd/evidence/14/adoption-20261121/nuget-policy-verification.json), [legacy policy removal](https://github.com/Hexalith/Hexalith.CiCd/blob/fc696e17a52e8d4bc055d397e57aae1e23bb9b8f/_bmad-output/specs/spec-cicd/evidence/14/adoption-20261121/nuget-legacy-cleanup.json), [CiCd DW-44](https://github.com/Hexalith/Hexalith.CiCd/blob/b00126c0a8d6159e7cb527282f9811eda9b852f4/_bmad-output/implementation-artifacts/deferred-work.md#dw-44-resolve-nuget-trusted-publishing-scopes-that-overlap-the-pilot-package-ids)

A matching workflow identity and environment are required for each exchange. For example, the November 27 EventStore source still has a `release.yml` job in `production` that requests a NuGet key, matching the fields recorded for policy `16915`. The inspected Conversations job uses `hexalith-release`, so it would fail the recorded `production` condition of policy `18928`. The dated readback establishes overlapping authorization. A current token exchange or an actual competing push would require separate evidence. [EventStore job](https://github.com/Hexalith/Hexalith.EventStore/blob/0a224fb7536472439b6647e282061f2a3a352c71/.github/workflows/release.yml#L82-L100), [EventStore token exchange](https://github.com/Hexalith/Hexalith.EventStore/blob/0a224fb7536472439b6647e282061f2a3a352c71/.github/workflows/release.yml#L223-L228), [Conversations job](https://github.com/Hexalith/Hexalith.Conversations/blob/f8d81ca843c855cd5b87677617600e97bdcdee2f/.github/workflows/release.yml#L27-L37)

**Release behavior.** E6 referred to an older publisher's separate absence check. The Agents workflow checked on 2026-11-27 delegates to Builds `v6.9.1`, which pushes the NuGet files sequentially with `--skip-duplicate`, then compares the public packages' identities and payloads against the original release artifacts before finalizing GitHub. A competing publisher can still occupy an ID and version before the Agents push. If its payload differs, readback stops finalization even when other packages from the same release have already been published. [Original E6 finding](spec-implement-agents-ci-cd.md#review-triage-log), [Agents release at `7270a4a3e6edf3fa99664c45788a097eaad84468`](https://github.com/Hexalith/Hexalith.Agents/blob/7270a4a3e6edf3fa99664c45788a097eaad84468/.github/workflows/release.yml#L27-L47), [Builds push and readback](https://github.com/Hexalith/Hexalith.Builds/blob/91cb7cba990ffdf82930f9bf8cb68aef41baba76/Github/lib/release_publish.py#L278-L367), [Builds finalization order](https://github.com/Hexalith/Hexalith.Builds/blob/91cb7cba990ffdf82930f9bf8cb68aef41baba76/Github/lib/release_publish.py#L466-L478)

**Reservation.** The documented `PackagePublish` API accepts one package per push and returns HTTP `409` when that ID and version already exists. An absence probe leaves the destination available to a later accepted push. NuGet.org documents no conditional reservation of all six versions before ordinary push. Prefix reservation restricts eligibility by package name and owner. With private staging, a maintainer can approve a group and each package independently enters public distribution. NuGet documents non-atomic group promotion and gives no guarantee that an individually staged version excludes an ordinary push. [PackagePublish API](https://learn.microsoft.com/en-us/nuget/api/package-publish-resource#push-a-package), [ID prefix reservation](https://learn.microsoft.com/en-us/nuget/nuget-org/id-prefix-reservation#id-prefix-reservation-details), [NuGet.org staging and promotion](https://devblogs.microsoft.com/dotnet/nuget-staged-publishing/#review-and-promote)

**Package ownership checked on 2026-11-27.** [Hexalith.Agents](https://www.nuget.org/packages/Hexalith.Agents#owners-body), [Hexalith.Agents.Client](https://www.nuget.org/packages/Hexalith.Agents.Client#owners-body), [Hexalith.Agents.Contracts](https://www.nuget.org/packages/Hexalith.Agents.Contracts#owners-body), [Hexalith.Agents.EventStore](https://www.nuget.org/packages/Hexalith.Agents.EventStore#owners-body), [Hexalith.Agents.Testing](https://www.nuget.org/packages/Hexalith.Agents.Testing#owners-body), [Hexalith.Agents.UI](https://www.nuget.org/packages/Hexalith.Agents.UI#owners-body)

## Review Triage Log

- Medium, patched. The current publisher supersedes the historical absence-check premise.
- Medium, patched. Duplicate handling and payload readback explain how a conflict can leave some packages published while GitHub finalization stops.
- Medium, patched. Policy matching and two checked workflow environments delimit what the dated inventory establishes.
- Low, patched. The staging contract establishes independent group distribution and leaves single-package collision behavior unverified.
