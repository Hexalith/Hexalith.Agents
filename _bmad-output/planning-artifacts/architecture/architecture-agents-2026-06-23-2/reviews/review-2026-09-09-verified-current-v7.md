---
name: Hexalith Agents Architecture Spine — Web-Verification Review (v7)
type: architecture-spine-review
target: architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
date: 2026-09-09
lens: verified-current v7
---

# Reviewer Gate — Web-Verification Pass (v7)

**Verdict:** PASS WITH FINDINGS — no claim in the Stack table is factually
wrong, but one already-tracked deviation (`ARCH-A-4`, the `.NET 10.0.3xx`
servicing lapse) is materially under-stated: the exact release that
dropped the band also shipped fixes for six disclosed CVEs (one CVSS 8.8
RCE) that the 10.0.3xx band cannot receive, and a second, previously
"no action" item (MediatR's commercial license) turns out to already be
load-bearing through the mandated `EXT-HOST-1`/EventStore SDK host, not
merely a dormant catalog entry.

**Method:** live NuGet.org / NuGet v3 flat-container fetches, live
`dotnet/core` raw GitHub release-notes fetches, a live fetch of the
official .NET Blog September 2026 servicing post, targeted web searches
for the CVEs it names, and direct repository inspection (`git ls-tree`,
`git log -p`, `git merge-base --is-ancestor`, and `grep` over
`Directory.Packages.props` and the `references/Hexalith.EventStore`
source tree) — all performed today, 2026-09-09.

Per the brief, `ARCH-A-4` (SDK band / xUnit / NSubstitute / Test.Sdk /
xunit.runner.visualstudio catalog deviations), `ARCH-A-8` (Fluent UI
Blazor RC not GA), and bunit's plain version lag are **not** re-litigated
below except where new evidence changes their framing.

---

## High — `ARCH-A-4`'s own escalation trigger has already fired, uncited

**Spine claim (Stack table, `.NET SDK` row):** "the `10.0.3xx` feature
band's last patch (`10.0.303`) shipped in the 2026-08-11 monthly bundle
(`10.0.11`), and the 2026-09-08 bundle (`10.0.12`) shipped with no
`10.0.3xx` member, so the band is now out of active servicing — a
build-supportability risk (no further security patches), not only
test-stack debt." `ARCH-A-4` (Architecture Assumptions table) mirrors
this: "...escalating this row's target sooner than Story 5.6 on any
security advisory against `10.0.3xx`."

Both statements frame the security risk as **forward-looking /
hypothetical** ("no further security patches" as a future condition,
escalation "on any advisory" as a not-yet-triggered clause).

**Evidence — the triggering advisory already exists, in the exact
release that ended the band:**

- Fetched `https://raw.githubusercontent.com/dotnet/core/main/release-notes/10.0/10.0.12/10.0.12.md`
  directly. Its "Notable Changes" section states: "`.NET 10.0.12` release
  carries security and non-security fixes" and lists six CVEs by MSRC
  link: `CVE-2026-69439`, `CVE-2026-71328`, `CVE-2026-69522`,
  `CVE-2026-69304`, `CVE-2026-58649`, `CVE-2026-69806`. This is the same
  bundle the spine itself cites as the one with "no `10.0.3xx` member"
  (SDKs `10.0.401` / `10.0.112` only).
- `https://devblogs.microsoft.com/dotnet/dotnet-and-dotnet-framework-september-2026-servicing-updates/`
  confirms these (plus `CVE-2026-69805`) affect ".NET 10.0, 9.0, 8.0."
  Web search of Patch-Tuesday roundups (`senserva.com`, `zecurit.com`)
  independently corroborates `CVE-2026-69522` as a **CVSS 8.8 remote
  code execution** vulnerability affecting .NET 10/9/8 (and .NET
  Framework 4.6.2–4.8.1).
- Repo check: the pinned `global.json` (`10.0.301`,
  `rollForward: latestPatch`) can resolve at most to `10.0.303` — the
  highest `10.0.3xx` SDK ever shipped, paired with the **10.0.11**
  runtime (per the 10.0.11 release notes cited in v6/this spine).
  `10.0.12`'s runtime fixes are only reachable through `10.0.401` or
  `10.0.112`, neither of which `rollForward: latestPatch` on a
  `10.0.301` pin can select.
- Net effect: a build using this repo's own `global.json` today is,
  concretely, unpatched against six disclosed September-2026 CVEs
  (including one RCE-class, CVSS 8.8), with **no available fix inside
  the pinned band** — not a hypothetical future state the escalation
  clause is waiting for, but the state it already describes.

**Why it matters:** `ARCH-A-4`'s own text makes "any security advisory
against `10.0.3xx`" the trigger for escalating past Story 5.6. That
trigger is not merely plausible-someday — it is independently
verifiable, today, against the primary source the spine itself cites
(`dotnet/core` release notes). Leaving the Stack table and `ARCH-A-4` in
present-hypothetical language after this round's `updated:` date invites
a reader (or `RQ-1`) to treat the escalation as not-yet-armed when the
named condition has already occurred.

**Fix:** cite `CVE-2026-69439`, `CVE-2026-71328`, `CVE-2026-69522`,
`CVE-2026-69304`, `CVE-2026-58649`, and `CVE-2026-69806` by number in the
Stack table row and in `ARCH-A-4`, state that the escalation condition is
**already met** as of the 2026-09-08 bundle, and record the actual target
date decision (escalate now vs. accept residual risk to Story 5.6) rather
than leaving `TargetRetirementDate` as "Story 5.6, or immediately on
advisory" with the advisory unnamed. Add the two `10.0.11.md` /
`10.0.12.md` URLs to frontmatter `sources` (this closes the Low
traceability item v5/v6 already carried).

---

## Medium — MediatR's "not referenced by Agents projects" is true at the project-reference level but understates the actual dependency

**Spine claim (Stack table):** "MediatR | `14.2.0` from imported workspace
catalog (catalog-pinned; not referenced by Agents projects)." Prior
rounds (v4–v6) treated this as settled: "Not a live risk since Agents
doesn't consume it... no new action needed."

**Evidence:**

- `grep -rl "MediatR" src/**/*.csproj` in this repo: zero hits. No
  `Hexalith.Agents.*` project has a direct `PackageReference` to
  `MediatR` — the literal Stack-table claim is correct.
- But `AD-16` commits Agents to "expose[] the reusable Agents domain
  service through the shared EventStore DomainService SDK host," and
  `references/Hexalith.EventStore/src/Hexalith.EventStore/` — the
  package that host is built from — has `PackageReference
  Include="MediatR"` in three `.csproj` files
  (`Hexalith.EventStore`, `Hexalith.EventStore.Gateway`,
  `Hexalith.EventStore.Server`) and wires `IMediator` through its
  command/query dispatch pipeline: `Controllers/CommandsController.cs`,
  `Controllers/QueriesController.cs`, `Controllers/ReplayController.cs`,
  and pipeline behaviors `ValidationBehavior.cs`, `LoggingBehavior.cs`,
  `AuthorizationBehavior.cs`.
- MediatR's own NuGet page (fetched live today,
  `https://www.nuget.org/packages/MediatR`) states a license key is
  required and explicitly carves out client apps: "The license key does
  not need to be set on client applications (such as Blazor WASM)" —
  implying it **is** needed for server-side hosts, which is exactly what
  `Hexalith.Agents.Server` (running atop the EventStore SDK host) is.
- `Hexalith.Tenants` and `Hexalith.Parties` — two of Agents' own named
  adapter dependencies — also directly reference `MediatR`, for the same
  reason (they too sit on the EventStore SDK host).

**Why it matters:** the Stack table's parenthetical reads as "MediatR is
dormant for Agents, revisit only if a future story adds a reference."
That framing is only true for Agents' own source; the deployed
`Hexalith.Agents.Server` process already executes MediatR's licensed
pipeline transitively through the mandated shared host, so MediatR's
commercial-license requirement is operationally live today, not deferred
to a future decision point. Nothing in the spine's `EXT-HOST-1` /
`EXT-SECRETS-1` inventory names a MediatR license key as a secret the
platform must provision — it may already be handled at the platform
level (consistent with `AD-16`'s "Hosting package versions are the
platform's, not the spine's"), but the spine gives no evidence either
way.

**Fix:** either (a) confirm with the platform/`EXT-HOST-1` owner that a
MediatR license key is already provisioned for every EventStore-hosted
domain service (Agents included) and add one line to the Stack table or
`EXT-HOST-1` prerequisites saying so, or (b) reword the Stack table row
from "not referenced by Agents projects" to something like "no direct
Agents project reference; transitively required at runtime by the
mandated EventStore SDK host (`AD-16`) — license provisioning is
`EXT-HOST-1`'s responsibility, not re-verified here."

---

## Low (carried forward, unchanged) — bunit is now two patches behind the pin

Re-verified live via the NuGet v3 flat-container index
(`api.nuget.org/v3-flatcontainer/bunit/index.json`): the version list
ends `[..., '2.9.0', '2.10.3']` — latest stable is `2.10.3`, one more
patch beyond what v6 already flagged (`2.10.3` was released 2026-09-08,
the day before the spine's own `updated:` stamp — v6 already caught it
same-day). The spine still pins `2.9.0`. Not an `ARCH-A-4`-tracked
deviation; consistent with v4–v6's framing as low-risk, non-blocking
continuity. No new action.

---

## Confirmed accurate — re-verified live/against the repo today, no finding

| Item | Spine claim | Verified value (2026-09-09) | Source |
| --- | --- | --- | --- |
| `Hexalith.EventStore` gitlink correction | `e302432c` at spine `updated` date | Exact match: `git ls-tree HEAD -- references/` → `e302432ca6daf3aa0436c3c0011f7baa551bb449`; submodule's own `HEAD` is the same commit, dated `2026-09-09 11:43:41 +0200` (same day as the spine's `updated:` stamp); `12d2dfc1` (the hash v6 recommended) is a **strict ancestor** of `e302432c` (`git merge-base --is-ancestor` confirms), so the correction moved *forward* past v6's own suggestion, not sideways or backward | `git ls-tree`, `git log -p -- references/Hexalith.EventStore`, `git merge-base --is-ancestor` |
| Other 4 gitlinks | `Conversations 73bcee6f`, `Parties fa423985`, `Tenants 54fc4040`, `FrontComposer 053b2008` | All four exact matches against `git ls-tree HEAD` today | `git ls-tree` |
| Shouldly | `4.3.0` | Confirmed latest stable (`4.3.0`, 2025-01-23); only newer entries are `5.0.0-preview.1`/`.2`, not GA | NuGet v3 flat-container index; `nuget.org/packages/Shouldly` |
| Dapr .NET packages / Dapr Workflow | `1.18.5` | Confirmed latest stable, published 2026-07-25; `1.19.0-preview.2` exists but is prerelease | `nuget.org/packages/Dapr.Workflow` |
| FluentValidation | `12.1.1` | Confirmed latest stable | `nuget.org/packages/FluentValidation` |
| OpenTelemetry | `1.18.0` | Confirmed latest stable, published 2026-08-21 | `nuget.org/packages/OpenTelemetry` |
| Fluent UI Blazor v5, no GA (`ARCH-A-8`) | `5.0.0-rc.5-26219.1`, RC | Confirmed: latest stable is still `4.14.4` (2026-07-30); latest prerelease `5.0.0-rc.5-26219.1` (2026-08-09); no v5 GA | `nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components` |
| xUnit v3 / NSubstitute / xunit.runner.visualstudio catalog values | `4.0.0` / `6.2.0` / `4.0.0` | All three confirmed as current latest stable, and all three match `references/Hexalith.Builds/Props/Directory.Packages.props` exactly | NuGet v3 flat-container index; repo file |
| Root overrides (`Microsoft.NET.Test.Sdk` `18.6.0`, `xunit.runner.visualstudio` `3.1.5`) | Root `Directory.Packages.props` | Exact match, read directly | repo file |

---

## Minor continuity note (not a finding)

Since v6's check (same day, earlier), NuGet has published
`Microsoft.NET.Test.Sdk 18.10.0` (2026-09-09, confirmed real via the
v3 flat-container index, not a scraping artifact — `[..., '18.9.0',
'18.10.0']`). The workspace catalog (`Hexalith.Builds`) still pins
`18.9.0`, one release behind. The Stack table doesn't state a version
number for this row (only "root override diverging from the workspace
catalog... same deviation as xUnit" — still true, per v6's Low finding,
still open), so nothing the spine asserts is now wrong; flagged only so
whoever eventually fills in the version-pair detail (v6's Low fix) uses
`18.10.0`, not `18.9.0`, as the catalog's "current" reference point.
