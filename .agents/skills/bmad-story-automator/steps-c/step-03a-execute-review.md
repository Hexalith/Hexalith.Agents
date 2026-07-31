---
name: 'step-03a-execute-review'
description: 'Autonomous execution loop - automate and code review'
nextStep: './step-03b-execute-finish.md'
scriptsDir: '../scripts/story-automator'
outputFile: '{output_folder}/story-automator/orchestration-{epic_id}-{timestamp}.md'
retryStrategy: '../data/retry-fallback-strategy.md'
reviewLoop: '../data/code-review-loop.md'
---

# Step 3a: Execute Review Phase

**Goal:** Run automate (guardrails) and code review loop for the current story.
**Interaction mode:** Deterministic autonomous execution.

---

## Prerequisites

- Step 3 completed (create-story and dev-story done)
- State document updated with current story progress

Set: `scripts="{scriptsDir}"`

---

## Story Loop (Continue from Step 3)

### C. Automate (Guardrails)
*Skip if `overrides.skipAutomate`*

**Apply retry/fallback pattern from `{retryStrategy}`:** Non-blocking, but still retry on failure.

```bash
# --command required (see Spawn Pattern in step-03)
resolve_agent_for_task "auto" "$state_file" "{story_id}"
if should_apply_primary_model "$current_agent"; then
  built_cmd=$("$scripts" tmux-wrapper build-cmd auto {story_id} --agent "$current_agent" --model "$primary_model" --state-file "$state_file")
else
  built_cmd=$("$scripts" tmux-wrapper build-cmd auto {story_id} --agent "$current_agent" --state-file "$state_file")
fi
session=$("$scripts" tmux-wrapper spawn auto {epic} {story_id} \
  --agent "$current_agent" \
  --command "$built_cmd")
result=$("$scripts" monitor-session "$session" --json --agent "$current_agent")
"$scripts" tmux-wrapper kill "$session"
```

- SUCCESS:
  ```bash
  # Update Story Progress: mark automate done
  tmp_state=$(mktemp)
  sed "s/^| ${story_id} |.*$/| ${story_id} | done | done | done | - | - | in-progress |/" "{outputFile}" > "$tmp_state" && mv "$tmp_state" "{outputFile}"
  ```
  Display: `[story {N}/{total}] automate -> done`
  → proceed to D
- FAILURE → retry up to 3 attempts (non-blocking, so fewer retries), then log warning:
  ```bash
  # Update Story Progress: mark automate skipped
  tmp_state=$(mktemp)
  sed "s/^| ${story_id} |.*$/| ${story_id} | done | done | skip | - | - | in-progress |/" "{outputFile}" > "$tmp_state" && mv "$tmp_state" "{outputFile}"
  ```
  Display: `[story {N}/{total}] automate -> skip (non-blocking)`
  → proceed to D

### D. Dev Agent Record Readiness Gate

Run this gate after Automate completes or is skipped, including when Automate
exhausted its non-blocking retries. Do not initialize or increment a review
cycle until the gate passes.

Resolve exactly one story through the existing create-story success contract;
the helper can return exit 0 with `verified=false`, so inspect its JSON result:

```bash
resolution=$("$scripts" validate-story-creation check "{story_id}" --state-file "{outputFile}")
resolution_ok=$(printf '%s' "$resolution" | jq -r '.verified and ((.matches // []) | length == 1)')

if [ "$resolution_ok" = "true" ]; then
  story_file=$(printf '%s' "$resolution" | jq -r '.matches[0]')
  if gate_output=$(python3 "{project-root}/tools/check-story-review-readiness.py" "$story_file" 2>&1); then
    gate_rc=0
  else
    gate_rc=$?
  fi
else
  story_file=""
  gate_rc=1
  gate_output="Story resolution failed: $(printf '%s' "$resolution" | jq -r '.reason // "expected exactly one story file"')"
fi
```

**If `gate_rc == 0`:**

```bash
gate_summary=$(printf '%s\n' "$gate_output" | tail -n 1)
echo "- **[$(date -u +%Y-%m-%dT%H:%M:%SZ)]** RECORD_READINESS_PASSED: $gate_summary" >> "{outputFile}"
```

Display: `[story {N}/{total}] record-readiness -> done`
→ proceed to E

**If `gate_rc != 0`:**

1. Do not spawn a review session, consume a review-cycle retry, or change the
   progress table's review cell.
2. Preserve the story and sprint statuses so the Developer can correct the
   reported record drift.
3. Pause on this resumable step and remove the stop marker through the installed
   helper rather than hard-coding its path.

```bash
timestamp=$(date -u +%Y-%m-%dT%H:%M:%SZ)
{
  echo "- **[$timestamp]** RECORD_READINESS_BLOCKED (exit $gate_rc)"
  printf '%s\n' "$gate_output" | sed 's/^/  /'
} >> "{outputFile}"

if "$scripts" orchestrator-helper state-update "{outputFile}" \
  --set status=PAUSED \
  --set currentStep=step-03a-execute-review \
  --set lastUpdated="$timestamp"; then
  "$scripts" orchestrator-helper marker remove
else
  echo "CRITICAL: Readiness failed and PAUSED state could not be persisted; stop marker retained." >&2
fi
```

Display the concise remediation output and offer Developer correction followed
by resume, or a manual pause. If the pause-state update fails, retain the stop
marker and escalate the critical diagnostic. **HALT.** Never substitute a checklist assertion
or reviewer judgment for a passing command.

### E. Code Review Loop

**See `{reviewLoop}` for complete script-based review cycle with v2.3 per-task agent configuration.**

**MANDATORY log-summary contract (every review cycle):**
- Run a single grep/regex pass over review output first.
- Return only compact fields to parent flow: `next_action`, `confidence`, `error_class`, `issues_count`, `top_issues`.
- Do not carry full log payloads forward unless escalation requires raw evidence.

```bash
review_log=$(echo "$result" | jq -r '.output_file')
review_focus=$(grep -nE "SUCCESS|FAIL|ERROR|CRITICAL|WARN|RETRY|ESCALATE|ISSUE" "$review_log" | head -n 120)
if [ -z "$review_focus" ]; then
  review_focus=$(tail -n 120 "$review_log")
fi

# Compact subprocess-style summary contract for parent flow
review_summary=$("$scripts" orchestrator-helper parse-output "$review_log" review --state-file "$state_file" | jq -c '
  {
    next_action: (.next_action // "retry"),
    confidence: (.confidence // 0),
    error_class: (.error_class // "unknown"),
    issues_count: ((.issues // []) | length),
    top_issues: ((.issues // [])[:3])
  }
')
```

Key points:
- Up to 5 cycles using `story-automator tmux-wrapper spawn review` + `story-automator monitor-session`
- **Agent:** Uses per-task config from state document (`resolve_agent_for_task "review"`)
- **Verification:** Uses `--workflow review --story-key` for sprint-status verification
- **States:** `completed` (verified):
  ```bash
  # Update Story Progress: mark code-review done
  tmp_state=$(mktemp)
  sed "s/^| ${story_id} |.*$/| ${story_id} | done | done | done | done | - | in-progress |/" "{outputFile}" > "$tmp_state" && mv "$tmp_state" "{outputFile}"
  ```
  Display: `[story {N}/{total}] review -> done`
  → Auto-Proceed to Finalization | `incomplete` → count as failed attempt, retry until maxCycles, then CRITICAL escalate (Trigger #8)
- Exit loop when sprint-status shows "done"
- If `review_summary.next_action` is ambiguous, ask one clarifying question before escalating.

---

## Auto-Proceed to Finalization

Display: "**Code review complete. Proceeding to finalize commits and status checks...**"

```bash
"$scripts" orchestrator-helper state-update "{outputFile}" \
  --set currentStep=step-03b-execute-finish \
  --set lastUpdated="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
echo "- **[$(date -u +%Y-%m-%dT%H:%M:%SZ)]** Code review complete, proceeding to finalization" >> "{outputFile}"
```

---

## Then
→ Immediately load and execute `{nextStep}`
