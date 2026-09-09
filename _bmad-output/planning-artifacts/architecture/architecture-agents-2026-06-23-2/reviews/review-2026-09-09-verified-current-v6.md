# Reviewer Gate — Web-Verification Pass (v6)

**Verdict: PASS WITH FINDINGS**

**Reviewer role:** verify every committed decision in `ARCHITECTURE-SPINE.md`
(status: final, `updated: 2026-09-09`) was web-researched or
reality-checked rather than asserted from training data — current
library/framework/SDK versions, GA status claims, specific date claims,
and (for this round) also direct cross-checks against the actual
repository state (submodule gitlinks, `global.json`, `Directory.Packages.props`),
not just against the web.

**Prior round:** `reviews/review-2026-09-09-verified-current-v5.md` flagged
H1 (the `.NET SDK` Stack-table row's servicing-lapse date was off by a
month and uncited) and M1 (`ARCH-A-4`'s row didn't mirror the Stack
table's new advisory-escalation language). Both are re-verified below —
independently, against live sources, not by trusting v5's own citations.

**Method:** live NuGet.org fetches, live `dotnet/core` GitHub release-notes
fetches, a live Microsoft Learn fetch, a live Dapr docs fetch, all
performed today (2026-09-09); plus direct inspection of this repository's
`global.json`, sibling submodules' `global.json` files, root
`Directory.Packages.props`, `references/Hexalith.Builds/Props/Directory.Packages.props`,
and `git ls-tree` / `git log` on the submodule gitlinks the Stack table
cites. All sources are cited inline.

---

## v5 carry-forward items — re-verified

### H1 (v5) — .NET SDK servicing-lapse date — CONFIRMED FIXED

Current Stack table text: "the `10.0.3xx` feature band's last patch
(`10.0.303`) shipped in the 2026-08-11 monthly bundle (`10.0.11`), and the
2026-09-08 bundle (`10.0.12`) shipped with no `10.0.3xx` member."

Independently re-fetched live today:
- [`10.0.11.md`](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.11/10.0.11.md) (2026-08-11): SDKs `10.0.400`, **`10.0.303`**, `10.0.111`.
- [`10.0.12.md`](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md) (2026-09-08): SDKs `10.0.401`, `10.0.112` — no `10.0.3xx` member.

The spine's revised sentence now matches these sources exactly. **Closed, no finding.**

### M1 (v5) — `ARCH-A-4` / Stack table drift — CONFIRMED FIXED

`ARCH-A-4`'s row now reads "...escalating this row's target sooner than
Story 5.6 on any security advisory against `10.0.3xx`" with Target
Retirement "Story 5.6, or immediately on advisory" — mirroring the Stack
table's "sooner on any security advisory" language exactly. **Closed, no
finding.**

### Residual from v5's H1 fix — traceability still incomplete (carried forward, downgraded)

v5's fix recommendation also said: "add the two `dotnet/core`
release-notes URLs (and/or the `NETSDK1240` reference) to the frontmatter
`sources` list so the claim is traceable in the document itself." Checking
the frontmatter `sources:` block (lines 16-50) today: it still carries
only the generic `https://github.com/dotnet/core/blob/main/release-notes/10.0/README.md`
link that predates this round. The Stack table row's own citation is the
vague in-prose "(see `dotnet/core` release notes; ARCH-A-4)" — no URL,
and not the two specific per-release pages the claim's dates actually rest
on. The underlying dates are now correct (see above), so this is
traceability polish, not a factual defect — kept as **Low**, downgraded
from v5's implicit "part of H1."

---

## New findings (v6)

### Medium — Stack table's `Hexalith.EventStore` gitlink is stale relative to the actual submodule state

- **Spine claim (Stack table):** "Hexalith.EventStore | parent gitlink
  `1b6f08d4` at the spine `updated` date"
- **Reality check (this repo, today):**
  - `git ls-tree HEAD -- references/Hexalith.EventStore` → committed
    gitlink is `12d2dfc1ca23de602c878b339d1c8dba192bba75`, not `1b6f08d4...`.
  - `git log -p -- references/Hexalith.EventStore` shows the bump
    `1b6f08d4... -> 12d2dfc1...` was committed in this repo at `74a9d66`
    ("feat: add Sprint Change Proposal for 2026-09-09 PRD Validation
    Follow-Through"), which predates the most recent commit (`4ec626d`,
    "Add review documents for architecture validation and web-verification
    pass") — i.e. the gitlink had already moved before v5 was written.
  - The EventStore submodule's own commit log confirms `12d2dfc1` is dated
    **2026-09-09 10:26:33 +0200** — the same day as the spine's own
    `updated:` stamp — with message "feat: add reviews for brownfield
    traceability, truth update, and rubric for EventStore PRD."
  - By contrast, the other four gitlink rows in the same table are all
    exact matches against `git ls-tree HEAD` today: Conversations
    `73bcee6f...` ✓, Parties `fa423985...` ✓, Tenants `54fc4040...` ✓,
    FrontComposer `053b2008...` ✓.
- **Why it matters:** this is exactly the failure mode this lens exists to
  catch, just against the repository instead of the web — a specific,
  checkable pointer value is asserted and is wrong, in a `status: final`
  document whose own `updated` date is today. A reader trusting the
  Stack table to reflect "parent gitlink ... at the spine `updated` date"
  would consult a commit of `Hexalith.EventStore` that is one revision
  behind what this repository actually has checked in.
- **Fix:** update the Hexalith.EventStore row to `12d2dfc1` (or re-run
  whatever gitlink-sync step produces the other four rows correctly), and
  consider adding a lint/CI check that diffs the Stack table's gitlink
  column against `git ls-tree HEAD` for the `references/*` submodules
  before a spine is marked `final`, since this is the second consecutive
  round where a Stack-table cross-reference (SDK dates in v5, a gitlink
  here) drifted from an easily-checkable ground truth between edit passes.

### Low — The two rows added to close v4's M1 still lack the version-pair detail their sibling rows carry

- **What I checked:** the Stack table's `Microsoft.NET.Test.Sdk` and
  `xunit.runner.visualstudio` rows (added last round to close v4's M1)
  read only "root override diverging from the workspace catalog... same
  deviation as xUnit" — no actual version numbers, unlike the adjacent
  `xUnit v3` row ("`3.2.2` root override; catalog `4.0.0`") and
  `NSubstitute` row ("`5.3.0` root override; catalog `6.2.0`").
- **Actual values (read directly from this repo, not asserted):**
  - Root `Directory.Packages.props`: `Microsoft.NET.Test.Sdk` = `18.6.0`;
    `xunit.runner.visualstudio` = `3.1.5`.
  - `references/Hexalith.Builds/Props/Directory.Packages.props` (the
    imported workspace catalog): `Microsoft.NET.Test.Sdk` = `18.9.0`;
    `xunit.runner.visualstudio` = `4.0.0`.
  - Both pairs are real, valid, currently-diverging values — the rows
    aren't wrong, just less specific than their siblings for no stated
    reason.
- **Fix:** state the actual override/catalog version pairs in both rows,
  matching the `xUnit v3`/`NSubstitute` rows' format, so a reader (or
  `RQ-1`/`ARCH-A-4` retirement check) can tell at a glance how far each
  override has drifted from the catalog without re-deriving it from the
  `.props` files.

### Low (carried forward, unchanged) — MediatR's license is commercial, still unremarked in the Stack table

Re-verified live today: MediatR latest is `14.2.0` (matches the spine),
and NuGet's own package page states it now requires a license key
("MediatR is validated by a license key... obtained by registering at
MediatR.io"), i.e. it is no longer a purely open-source dependency. The
Stack table's `MediatR` row still only says "catalog-pinned; not
referenced by Agents projects" with no license note. Not a live risk
since Agents doesn't consume it, but if a future story references it,
the commercial-license fact should be surfaced then. Unchanged from
v4/v5's L2 — no new action needed this round.

### Low (carried forward, unchanged) — bunit has drifted one more patch behind the pin

Re-verified live today: latest stable `bunit` is now `2.10.3` (released
2026-09-08, the day before the spine's own `updated` stamp); the spine
still pins `2.9.0`. This is not tracked as an `ARCH-A-4` deviation (only
xUnit/NSubstitute/Test.Sdk/runner.visualstudio/SDK are). Consistent with
v4/v5's L1 framing (an unremarkable, low-risk version lag) — flagged only
for continuity, not as a new problem.

---

## Confirmed accurate — re-verified live today, no finding

| Item | Spine claim | Live/repo-checked value (2026-09-09) | Source |
| --- | --- | --- | --- |
| Root `global.json` SDK pin | `10.0.301`, `rollForward: latestPatch` | Exact match, read directly from `agents/global.json` | repo file |
| Sibling `global.json` SDK pins | Siblings on `10.0.400` | Exact match across `Hexalith.Conversations`, `Hexalith.Tenants`, `Hexalith.Parties`, `Hexalith.EventStore`, `Hexalith.FrontComposer`, `Hexalith.Builds` `global.json` files | repo files |
| `.NET SDK` 10.0.3xx band absence | Last patch `10.0.303` (2026-08-11 bundle); absent from 2026-09-08 bundle | Reconfirmed live (see H1 above) | `dotnet/core` release notes |
| Dapr Workflow / Dapr .NET packages | `1.18.5` from imported catalog | Latest stable still `1.18.5` (2026-07-25); `1.19.0-preview.2` prerelease doesn't affect the pin | [NuGet: Dapr.Workflow](https://www.nuget.org/packages/Dapr.Workflow) |
| Fluent UI Blazor v5 pin, no GA yet (`ARCH-A-8`) | `5.0.0-rc.5-26219.1`; latest stable `4.14.4`, no v5 GA | Confirmed: latest stable `4.14.4` (2026-07-30), latest prerelease `5.0.0-rc.5-26219.1` (2026-08-09), still no v5 GA | [NuGet: Microsoft.FluentUI.AspNetCore.Components](https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components) |
| FluentValidation | `12.1.1` | Confirmed latest stable | [NuGet: FluentValidation](https://www.nuget.org/packages/FluentValidation) |
| OpenTelemetry | `1.18.0` | Confirmed latest stable | [NuGet: OpenTelemetry](https://www.nuget.org/packages/OpenTelemetry) |
| xUnit v3 catalog value | `4.0.0` | Confirmed latest stable, and matches `references/Hexalith.Builds/Props/Directory.Packages.props` | [NuGet: xunit.v3](https://www.nuget.org/packages/xunit.v3), repo file |
| NSubstitute catalog value | `6.2.0` | Confirmed latest stable, and matches workspace catalog file | [NuGet: NSubstitute](https://www.nuget.org/packages/NSubstitute), repo file |
| AD-9's "Microsoft Agent Framework... already GA" | Deferral justified partly on Agent Framework's .NET package being GA | `Microsoft.Agents.AI` and `Microsoft.Agents.AI.Workflows` both at stable `1.20.0` (2026-08-31), not prerelease | [NuGet: Microsoft.Agents.AI](https://www.nuget.org/packages/Microsoft.Agents.AI), [Microsoft.Agents.AI.Workflows](https://www.nuget.org/packages/Microsoft.Agents.AI.Workflows) |
| AD-9's "...and Dapr Agents [...] already GA" | Same deferral justification | Dapr docs state "Dapr Agents is v1.0 and production ready" — GA confirmed; note it is a Python-only framework, which AD-18 already correctly scopes out of V1 separately | [Dapr Agents docs](https://docs.dapr.io/developing-ai/dapr-agents/) |
| Other 4 of 5 submodule gitlinks (`Conversations`, `Parties`, `Tenants`, `FrontComposer`) | Specific short SHAs in Stack table | All four exact matches against `git ls-tree HEAD` today | repo (`git ls-tree`) |

---

## Verdict Summary

**PASS WITH FINDINGS.** No Critical issues from this lens. Both of v5's
findings (H1 the mis-dated SDK servicing claim, M1 the `ARCH-A-4`/Stack
table drift) are confirmed genuinely fixed on independent re-verification
against live sources — the specific dates in the Stack table now match
`dotnet/core`'s own release notes exactly, and `ARCH-A-4`'s row now
carries the same advisory-escalation trigger as the Stack table prose.

This round's own independent sweep — extended past v5's narrow delta
scope to include a direct cross-check of every gitlink, `global.json`,
and package-catalog value the Stack table cites against the actual
repository state, not just the web — surfaced one new Medium finding:
the `Hexalith.EventStore` gitlink (`1b6f08d4`) is stale by one submodule
commit against what this repository has actually checked in
(`12d2dfc1`, committed the same day as the spine's own `updated` stamp).
The other four gitlink rows are exact. Two Low findings round out the
pass: the two Stack-table rows added last round to close v4's M1 still
omit the version-pair detail their sibling rows carry (real values found
directly in `Directory.Packages.props`), and v5's own secondary
traceability suggestion (citing the specific `dotnet/core` release-notes
pages in frontmatter `sources`) wasn't carried out — only a generic prose
mention was added. MediatR's license status and bunit's continuing patch
lag are carried forward unchanged from v4/v5 as Low, non-blocking.

Every other version, GA-status, and date claim checked this round —
Dapr Workflow, Fluent UI Blazor (RC, not GA), FluentValidation,
OpenTelemetry, xUnit v3/NSubstitute catalog values, Microsoft Agent
Framework's `Microsoft.Agents.AI`/`Microsoft.Agents.AI.Workflows` GA
status, and Dapr Agents' GA status — was independently re-verified live
today and found accurate.
