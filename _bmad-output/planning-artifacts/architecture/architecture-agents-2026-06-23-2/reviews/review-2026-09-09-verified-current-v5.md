# Reviewer Gate — Web-Verification Pass (v5)

**Verdict: PASS WITH FINDINGS**

**Reviewer role:** verify every committed decision in `ARCHITECTURE-SPINE.md`
(amended again 2026-09-09, status: final) was web-researched or
reality-checked rather than asserted from training data — current
library/framework versions, that each named technology still exists and
fits — and flag anything that could be out of date and wasn't confirmed
against the web, the existing project, or the current starter.

**Scope of this pass:** this is a delta review against
`reviews/review-2026-09-09-verified-current-v4.md`. Per the memlog
(`.memlog.md`, entries tagged "verified-current H-5" and "Stack table
deviation tracking completed"), this Update round took v4's High finding
(H1, the `.NET SDK 10.0.3xx` feature-band lapse) and v4's Medium finding
(M1, missing `Microsoft.NET.Test.Sdk`/`xunit.runner.visualstudio` rows) and
edited the Stack table and `ARCH-A-4` in response. This pass re-verifies
those two specific edits against the web rather than re-deriving everything
v4 already checked; items v4 confirmed accurate and that were **not**
touched in this round (Dapr Workflow, MediatR, FluentValidation,
OpenTelemetry, xUnit v3/NSubstitute catalog pins, bunit, Aspire) are spot-
checked for drift only, not re-argued in full.

**Method:** live NuGet.org and `dotnet/core` GitHub release notes, fetched
today (2026-09-09) via `WebFetch`/`WebSearch`, independently of v4's own
citations. All URLs used are cited inline.

---

## Findings

### Critical

None.

### High

#### H1 (new, v5) — The Stack table's new `.NET SDK` claim gets its own date wrong by about a month, and carries no citable source

- **Spine claim (Stack table, line 472):** "the `10.0.3xx` feature band's
  last monthly servicing bundle shipped 2026-09-08 with no successor, so it
  is a build-supportability risk (no further security patches to an
  out-of-support band)."
- **What I checked (live, today):**
  - [`10.0.12.md`](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md) — released **2026-09-08**. SDKs included: `10.0.401`, `10.0.112`. No `10.0.3xx` entry. This is the bundle where the `3xx` band's *absence* first becomes visible.
  - [`10.0.11.md`](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.11/10.0.11.md) — released **2026-08-11**. SDKs included: `10.0.400`, `10.0.303`, `10.0.111`. This is the last bundle that actually shipped a `10.0.3xx` patch (`10.0.303`).
  - [`10.0` release-notes `README.md`](https://github.com/dotnet/core/blob/main/release-notes/10.0/README.md) — confirms the monthly cadence and both dates above.
- **The gap:** the sentence as written says `10.0.3xx`'s "last monthly
  servicing bundle shipped 2026-09-08" — but the band's actual last shipped
  patch (`10.0.303`) was in the **2026-08-11** bundle. 2026-09-08 is the
  date the *next* bundle shipped **without** a `10.0.3xx` member — i.e. the
  date the gap became observable, not the date the band itself last
  shipped. Read literally, the spine's sentence is off by about a month and
  attributes the wrong event to the wrong date. The directional claim
  (band `3xx` is now absent from the most recent monthly bundle, one month
  after last appearing) is real and independently reconfirmed here — this
  is a precision error in a specific, checkable date claim inside a
  `status: final` document, not a fabrication of the underlying fact.
  This is exactly the failure mode this lens exists to catch: a specific
  date got asserted without being checked against the source it's
  implicitly claiming to summarize.
  - Secondary, softer point (unchanged from v4's own "confidence caveat,"
    still unresolved): neither `NETSDK1240`'s reference page nor the
    release notes constitute an explicit Microsoft announcement that
    `10.0.3xx` is permanently retired — "absent from one monthly bundle"
    is strong circumstantial evidence of a band transition, consistent
    with `NETSDK1240`'s general description ("when the recommended
    servicing path moves to a different feature band, the older band
    stops receiving updates"), but is still an inference, not a
    documented end-of-service date. The spine states it as flat fact
    ("out-of-support band," "no further security patches") with no
    hedge.
  - The frontmatter `sources` list (lines 16-49) was **not** updated in
    this round to cite any of the `dotnet/core` release-notes URLs or
    `NETSDK1240` that this claim rests on — every other version claim in
    the Stack table traces to something in `sources` (NuGet package pages,
    Dapr docs, MS Learn), but this new, specific, risk-elevating claim
    does not. A future reader/auditor has no way to check where
    "2026-09-08... with no successor" came from without re-deriving it
    from scratch, which is exactly what this pass had to do.
- **Fix:** Reword to "`10.0.3xx`'s last shipped patch was `10.0.303`
  (2026-08-11); the following monthly bundle (`10.0.12`, 2026-09-08)
  shipped no `10.0.3xx` successor" (or equivalent), and add the two
  `dotnet/core` release-notes URLs (and/or the `NETSDK1240` reference) to
  the frontmatter `sources` list so the claim is traceable in the document
  itself rather than only in a review file.

### Medium

#### M1 (new, v5) — The Stack table's added urgency language for the SDK band isn't mirrored in the tracked `ARCH-A-4` assumption row, so the two co-located statements of the same fact already drift

- **What I checked:** Stack table line 472 (quoted above) now says the SDK
  band issue is "targeted no later than Story 5.6, **sooner on any
  security advisory** against `10.0.3xx`" — i.e. it has an escalation
  trigger distinct from the flat Story-5.6 target. `ARCH-A-4`'s own table
  row (Architecture Assumptions, line 752) still reads: "Root test-stack
  overrides (xunit.v3 3.2.2, NSubstitute 5.3.0, Microsoft.NET.Test.Sdk,
  xunit.runner.visualstudio, SDK 10.0.301) are aligned with the workspace
  catalog... by Story 5.6," with a "Target retirement" column value of
  flat `Story 5.6` — no escalation clause, and the SDK pin is still bundled
  with the test-runner deviations as one assumption with one retirement
  condition.
- **Why it matters:** `ARCH-A-4` (not the Stack table prose) is the row
  `RQ-1`'s `UnretiredAssumption` mechanism actually evaluates against
  (per AD-17's own index-versioning rule quoted just above the Architecture
  Assumptions table: "Adding or retiring a row... increments
  `architecture_assumption_index_version`... in the same change"). A
  reader or automated `RQ-1` check consulting `ARCH-A-4` alone — the
  governance-facing surface — would not see the "sooner on any security
  advisory" trigger the Stack table narrative promises; only the prose
  paragraph carries it. This is the same "two normative sources drifting"
  failure the spine's own `H-13` convention was written to prevent (at the
  spine/register level); here it recurs one level down, between the
  Stack table's free-text cell and the Architecture Assumptions table's
  structured row for the same fact, both edited in this same pass.
- **Fix:** Either add the security-advisory escalation as an explicit
  alternate retirement trigger in `ARCH-A-4`'s "Retired when" / "Target
  retirement" columns, or split the SDK-band risk into its own assumption
  row (as v4's H1 originally suggested) so it can carry a different
  urgency profile than the xUnit/NSubstitute/Test.Sdk/runner.visualstudio
  test-stack cluster it's currently bundled with.

### Low

None new this pass. `bunit` (still `2.9.0`, unchanged this round), MediatR's
dual-license status, and the Aspire host-vs-catalog patch gap are carried
forward unchanged from `review-2026-09-09-verified-current-v4.md` (its L1,
L2, L3) — none of the three inputs that changed were touched by this
Update round, so they were not re-fetched here; nothing suggests they need
to be.

---

## Confirmed accurate — re-verified live today, no finding

| Item | Spine claim | Live-checked value (today, 2026-09-09) | Source |
| --- | --- | --- | --- |
| Stack table completeness (v4's M1) | `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio` now listed as root overrides, same `ARCH-A-4` reason and Story 5.6 target | Both rows now present in the Stack table (lines 492-493) and in `ARCH-A-4`'s parenthetical package list (line 752) — v4's M1 is closed | Direct read of `ARCHITECTURE-SPINE.md` |
| `10.0.3xx` absence from the latest monthly SDK bundle (directional fact under H1) | Implied by the Stack table's risk framing | Reconfirmed: `10.0.12` (2026-09-08) ships `10.0.401`/`10.0.112` only, no `10.0.3xx` member; `10.0.11` (2026-08-11) was the last bundle carrying one (`10.0.303`) | [`10.0.12.md`](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md), [`10.0.11.md`](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.11/10.0.11.md) |
| Dapr Workflow package | `1.18.5` from imported workspace catalog | Latest stable still **1.18.5** (2026-07-25); newest item overall is a `1.19.0-preview.2` prerelease, doesn't affect the stable pin | [NuGet: Dapr.Workflow](https://www.nuget.org/packages/Dapr.Workflow) |
| Fluent UI Blazor v5 pin | `5.0.0-rc.5-26219.1`, matching catalog | Confirmed still the latest v5 prerelease (2026-08-09); latest **stable** is `4.14.4` (2026-07-30) — still no v5 GA, `ARCH-A-8`'s framing still accurate | [NuGet: Microsoft.FluentUI.AspNetCore.Components](https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components) |
| Dapr Agents doc URL (frontmatter `sources`) | `https://docs.dapr.io/developing-ai/dapr-agents/` | Live, loads, describes Dapr Agents (Python framework) as expected | [Dapr docs](https://docs.dapr.io/developing-ai/dapr-agents/) |

---

## What this pass did not re-derive from scratch

Every Stack table row and frontmatter URL v4 already re-fetched and
confirmed accurate on 2026-09-09 (MediatR, FluentValidation, OpenTelemetry,
xUnit v3/NSubstitute catalog pins, bunit, Aspire host-vs-catalog gap,
Microsoft Agent Framework GA, Dapr Agents GA, Dapr Workflow instance-id
semantics, C#14/net10.0 pairing) was left as v4 found it, since none of
those inputs changed in this Update round per the memlog. This pass's
effort went entirely into the two items the task named as new/modified:
the `.NET SDK` Stack-table row's added risk claim (H1 above) and the
Stack-table completeness fix (confirmed closed, no finding).

---

## Verdict Summary

**PASS WITH FINDINGS.** No Critical issues. This round's edits closed v4's
Medium finding (M1: missing `Microsoft.NET.Test.Sdk`/
`xunit.runner.visualstudio` rows) cleanly — confirmed. v4's High finding
(H1) was addressed with new text but that new text introduces its own
problem: the specific date it asserts (`10.0.3xx`'s "last monthly
servicing bundle shipped 2026-09-08") is checkable and wrong by about a
month — the band's actual last patch shipped 2026-08-11; 2026-09-08 is
when the *next* bundle shipped without it — and the claim carries no
citation in the frontmatter `sources` list despite being new, specific,
and used to justify an elevated risk framing (H1, this pass). A second,
Medium-severity gap follows directly from the same edit: the urgency
language added to the Stack table prose ("sooner on any security
advisory") isn't mirrored in `ARCH-A-4`'s own tracked row, so the
governance-facing assumption and the narrative describing it already
disagree on retirement urgency within the same edit pass (M1, this pass).
The underlying directional fact — that the `10.0.3xx` band is genuinely
absent from the most recent monthly SDK bundle — is real and was
independently reconfirmed live today, so this is a precision/traceability
problem with how the claim is worded and sourced, not evidence that the
claim was invented from training data.
