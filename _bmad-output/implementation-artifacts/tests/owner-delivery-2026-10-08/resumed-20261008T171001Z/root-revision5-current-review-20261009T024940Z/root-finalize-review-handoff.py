"""Finalize review evidence, then record the mandatory sixth-iteration halt."""
import datetime
import hashlib
import json
from pathlib import Path
import re
import shutil

review = Path(__file__).resolve().parent
workspace = review.parents[5]
base = review.parents[1]
spec = workspace / '_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md'
triage_path = review / 'current-review-triage.json'
triage = json.loads(triage_path.read_text())
assert triage['individualVerdictsBeforeGrouping'] and len(triage['findings']) == 13
assert not triage['groups']
assert b'review_loop_iteration: 5' in spec.read_bytes()
assert not (review / 'review-halt.json').exists()
ledger = workspace / '_bmad-output/implementation-artifacts/deferred-work.md'
ledger_before = hashlib.sha256(ledger.read_bytes()).hexdigest()

# Only now group survivors. Different omitted dependency calls are distinct defects,
# even where their proposed corrections reuse the same deadline helper.
groups = []
for row in triage['findings']:
    if row['proposedRoute'].startswith('reject'):
        continue
    group = dict(id='R5-' + row['id'], members=[row['id']], highestVerdict=row['verdict'],
                 route=row['proposedRoute'], rootCause=row['evidence'])
    if row['proposedRoute'] == 'defer':
        group['severityIfVerified'] = 'medium'
        group['processing'] = 'moot while bad_spec loopback is required; no ledger entry appended'
    else:
        group['processing'] = 'unapplied; mandatory loop-limit halt precedes amendments or engineer re-engagement'
        group['reviewableCorrection'] = row['reviewableCorrection']
    groups.append(group)
bad_specs = [g for g in groups if g['route'] == 'bad_spec']
patches = [g for g in groups if g['route'] == 'patch']
assert len(bad_specs) == 5 and len(patches) == 4 and len(groups) == 10
now = datetime.datetime.now(datetime.timezone.utc).isoformat()
triage.update(groups=groups, groupedUtc=now, verdictsRenderedBeforeGrouping=True,
              status='HALTED: required bad_spec loopback increments review_loop_iteration from 5 to 6',
              reviewLoopIterationAfterRequiredIncrement=6,
              survivorCounts=dict(bad_spec=5, patch=4, unverified_defer=1, rejected_carried=3),
              cascadingDisposition='bad_spec entries trigger loopback; patches/defer remain moot. No source patch, spec requirement amendment, rollback or new deferred entry performed.')
triage_path.write_text(json.dumps(triage, indent=2) + '\n')

shape = json.loads((review / 'root-full-matrix-shape-and-drift-audit.json').read_text())
xml_audit = json.loads((review / 'root-first-matrix-completed-xml-protected-audit.json').read_text())
assert not shape['errors'] and not xml_audit['errors']
assert shape['passingTestExecutions'] == 4607
supplement = json.loads((review / 'supplemental-verification/command-index.json').read_text())
assert len(supplement) == 2 and supplement[0]['exitCode'] == 0 and supplement[1]['exitCode'] == 1
keep = ('Preserve every positive revision-1 through revision-5 correction and all logged constraints: '
        'exact independently anchored admitted-original recovery, immutable unknown/terminal outcomes and signing/dispatch receipts, '
        'current private authority, no compromised-key new effect, same-owner reservation ordering, exact source/outbox transactions, '
        'finite complete namespaces and authenticated resumable reconciliation/acknowledgement prefixes, '
        'single-use abandoned cleanup and detached immediate history-buffer ownership, protection v2 bytes and cumulative carrier limits, '
        'retired-epoch refusal, final aggregate registration fence, spool recorder/capacity readiness, and pre-sign terminal denial after lost trust. '
        'Keep Branch B, accepted 365-day effective-at/exclusive expiry/no renewal, L/S/O/H/R, dedicated Conversations service Party, '
        'accepted catalogue predicate and named approvals. Keep normal Debug/analyzers/XML/local project references and disabled runtime defaults. '
        'Preserve all user/concurrent edits, protected original Story5.4/sprint/register/policy/frozen intent, and prior rejected findings. '
        'No blanket rollback, source reset, Git mutation, deployment, external message or unavailable consuming live seam.')
proposal = '# Revision 5 review handoff — 2026-10-09\n\n'
proposal += ('The nine verified source findings below remain unapplied. Five require a coherent technical revision; four are direct corrections. '
             'The three repeat findings retain their earlier low-complexity rejections. The verification reviewer reported no gaps. '
             'The operational-diagnostics claim is unverified and lower-priority processing is moot during the required loopback.\n\n')
proposal += 'Proposed next revision, pending the review-limit decision:\n\n'
for row in triage['findings']:
    if row['proposedRoute'] in ('bad_spec', 'patch'):
        proposal += f"- **{row['id']} ({row['verdict']}, {row['proposedRoute']})** — {row['reviewableCorrection']} See `{row['location']}`.\n"
proposal += ('\nAfter any authorized revision: retain exact before/current source and genuine red/green evidence, update the four complete owner packets, '
             'obtain all three fresh independent final reviews, then run the complete required Local matrix against stable source. '
             'Do not reask accepted policy/role approvals or the runtime configuration location the user does not know. '
             'Worker/signer enrollment, trust and actual backend qualification remain unestablished.\n\nKEEP: ' + keep + '\n\n')
proposal += ('Verification evidence:\n\n'
             '- First full root matrix: 13 command exits zero; 21 actual full-suite XML files; **4,607 passing executions** with zero failed/errors/skipped/not-run; all 44 required owner test classes executed. These executions overlap and are not unique coverage. Parties1598, Conversations1624, Custody576, SDK Contracts12/Client239/Server219/Payload339.\n'
             '- The original Host wrapper suppressed the normal build summary. A supplemental normal Debug Host build retained identical warning/error and Aspire properties and produced zero warnings/errors; the independently audited historical index references that diagnostic evidence explicitly. The first failed summary audit remains archived.\n'
             '- All 395 frozen owned source/copy representations and 125 capture inputs match. Seven intentionally absent source paths retain the frozen empty-byte representation; the initial absence-screen error and corrected audit are preserved.\n'
             '- Before/after scans detected five concurrent unowned logical-snapshot source/test changes during the first matrix, and four further changes during the refresh. Their union includes the new snapshot-rewitness tests. Every concurrent edit is preserved.\n'
             '- The fresh affected SDK Server build at 03:12:26–03:12:36 UTC exited1: CS0246 for IReadOnlyPayload and IBoundedPayloadWriter at DaprLogicalSnapshotRewitnessTests.cs:606/611. No subsequent Server test execution occurred. This failed current refresh is not replaced by earlier passing evidence; current full-workspace verification is incomplete.\n'
             '- Revision-5 focused engineering evidence remains separately audited: 426 distinct selected current passes, 38 executed command records, 13 historical nonpassing attempts and 25 genuine failed-before cases. Do not sum it with the overlapping full matrix.\n\n'
             'Links: [individual triage](current-review-triage.md), [complete JSON triage](current-review-triage.json), '
             '[full-matrix shape/drift audit](root-full-matrix-shape-and-drift-audit.json), '
             '[XML/protected-state audit](root-first-matrix-completed-xml-protected-audit.json), '
             '[frozen-source audit](root-post-full-matrix-source-audit.json), '
             '[supplemental exact commands](supplemental-verification/command-index.json), '
             '[failed Server build](supplemental-verification/2.log).\n\n')
rule = 'If it exceeds 5, HALT and escalate to the human.'
step = workspace / '_bmad/render/bmad-build/agents-6d6e04778d0d/f6d1ba87af5ae4d1989a/step-04-review.md'
skill = workspace / '.agents/skills/bmad-build/SKILL.md'
proposal += ('BMad processing disposition: the required bad_spec loopback increments the persisted review counter from5 to6. '
             f'The invoked [bmad-build skill]({skill}) directs the rendered [review step]({step}), which says: "{rule}" '
             'Work halts before re-derivation, requirement amendments, rollback or patch dispatch. '
             'This records a workflow checkpoint, not completion or a new runtime authorization. '
             'Approve one further implementation-and-review revision to address the concrete findings above.\n')
(review / 'README.md').write_text(proposal)

append = ('\n\n## Revision 5 full Local verification and mandatory review-limit checkpoint — 2026-10-09\n\n'
          'The first mandatory full Local matrix completed 13 zero-exit commands and 4,607 passing executions in21 XML files; '
          'all44 required owner classes executed, with22 clean normal build logs after supplemental Host diagnostics. '
          'Exact commands, XML, source hashes and protected-state audits are archived in '
          '[the root review handoff](tests/owner-delivery-2026-10-08/resumed-20261008T171001Z/root-revision5-current-review-20261009T024940Z/README.md). '
          'This is historical source evidence: concurrent unowned snapshot changes occurred during verification, and the later Server refresh '
          'failed with two CS0246 errors in the newly added rewitness tests. Current complete workspace verification is incomplete.\n\n'
          'All13 findings have individual verdicts before grouping. Five bad_spec entries (B2/B4/B5/B8/E1) and four direct corrections '
          '(B1/B3/B7/B9) survive; B6/B10/E2 carry prior rejections, and B11 remains maybe-false pending actual host observability evidence. '
          'Bad-spec loopback takes precedence, so no lower patch or deferred-ledger mutation is performed. '
          'The required counter increment is5→6. The requested bmad-build review step says "' + rule + '" '
          'HALT before source changes, spec requirement amendments or engineer re-engagement; obtain the human review-limit decision. '
          'The reviewable correction proposal and positive preservation instructions are saved in the handoff.\n\n'
          'KEEP: ' + keep + '\n\n'
          'The full parent remains accepted/in-progress under the existing override. Original Story5.4 remains draft/backlog; '
          'the four owner dependencies remain Uncommitted, complete targets and accepted commands remain TBD, and live worker/signer/backend '
          'qualification is unestablished. No source/runtime/owner acceptance is inferred from tests. '
          'The user does not know the runtime configuration location; no repeat request is required.\n')
before_increment = spec.read_bytes()
shutil.copyfile(spec, review / 'spec-before-review-limit-increment.md')
newline = '\r\n' if b'\r\n' in before_increment else '\n'
after = before_increment.replace(b'review_loop_iteration: 5', b'review_loop_iteration: 6', 1) + append.replace('\n', newline).encode()

# Everything concrete is prepared before recording the HALT; no engineering work follows.
spec.write_bytes(after)
errors = []
initial = json.loads((base / 'initial-state.json').read_text())
date = json.loads((base / 'authorized-date-update.json').read_text())
for relative, expected in initial['protected'].items():
    if relative.endswith('spec-5-4-owner-prerequisites.md'):
        continue
    if relative.endswith('external-dependency-register.md'):
        expected = date['register_after_sha256']
    actual = hashlib.sha256((workspace / relative).read_bytes()).hexdigest()
    if actual != expected:
        errors.append('Protected state changed: ' + relative)
frozen = re.search(rb'<frozen-after-approval.*?</frozen-after-approval>', after, re.S).group()
if hashlib.sha256(frozen).hexdigest() != initial['frozen_parent_sha256']:
    errors.append('Frozen intent changed')
for required in [b"status: 'in-progress'", b"human_approval: 'accepted'", b"baseline_commit: 'b252fcfde51bd04317ce5843a0a42bfe4dce5170'", b'review_loop_iteration: 6']:
    if required not in after.split(b'---', 2)[1]:
        errors.append('Required parent frontmatter lost')
if hashlib.sha256(ledger.read_bytes()).hexdigest() != ledger_before:
    errors.append('Deferred ledger changed during moot processing')
manifest = json.loads((review / 'review-input.json').read_text())
for item in manifest['files']:
    for key in ['sourcePath', 'reviewCopy']:
        path = Path(item[key])
        actual = hashlib.sha256(path.read_bytes() if path.is_file() else b'').hexdigest()
        if actual != item['currentSha256']:
            errors.append('Frozen source/copy changed: ' + item[key])
for item in manifest['inputs']:
    if hashlib.sha256(Path(item['path']).read_bytes()).hexdigest() != item['sha256']:
        errors.append('Capture changed: ' + item['path'])
report = dict(capturedUtc=now, status='HALTED awaiting human review-limit decision',
              counterBefore=5, counterAfter=6, triggeringFindings=[g['members'][0] for g in bad_specs],
              remainingVerifiedSourceFindings=9, unverifiedOperationalClaim='B11',
              requestedDecision='Approve one further implementation-and-review revision for the nine concrete fixes.',
              exactInvokedSkill=str(skill), exactRenderedStep=str(step), exactRule=rule,
              sourceCodeEditedAfterFinalEngineeringHandoff=False,
              sourceRequirementAmendedAfterCounterExceeded=False,
              deferredLedgerUnchanged=True, fullParentStatus='in-progress',
              originalStoryAndSprintAndPolicyAndRegisterUnchanged=True,
              frozenIntentSha256=hashlib.sha256(frozen).hexdigest(),
              finalParentSha256=hashlib.sha256(after).hexdigest(),
              frozenSourceAndCopiesChecked=len(manifest['files']), captureInputsChecked=len(manifest['inputs']),
              currentWorkspaceVerification='incomplete: latest affected Server refresh failed amid concurrent unowned edits',
              errors=errors)
(review / 'review-halt.json').write_text(json.dumps(report, indent=2) + '\n')
progress_path = review / 'root-review-progress.json'
progress = json.loads(progress_path.read_text())
progress.update(status=report['status'], currentReviewLoopIteration=6,
                fullParentStatus='accepted in-progress override retained', reviewDisposition='13 rows; 5bad_spec/4patch/3carried-reject/1unverified-defer; no subsequent engineering',
                rootFullLocalMatrix=dict(phase='root-revision5-full-local-20261009t030010z', commandExitsZero=13,
                                        passingExecutions=4607, xmlFiles=21, cleanNormalBuildLogs=22,
                                        requiredOwnedClassesExecuted=44, scope='historical first matrix; later Server refresh failed'),
                updatedUtc=now)
progress_path.write_text(json.dumps(progress, indent=2) + '\n')
print(json.dumps(report, indent=2))
raise SystemExit(bool(errors))
