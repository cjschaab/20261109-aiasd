---
ai_generated: true
model: "OpenAI/unknown@unknown"
operator: "jMM-at-CODETraining"
chat_id: "instruction-compliance-issues-20261112"
prompt: |
  create issues for the findings and put them in the issues folder as markdown files
started: "2026-11-12T11:10:00.322-08:00"
ended: "2026-11-12T11:15:11.457-08:00"
task_durations:
  - task: "Create Markdown issues from instruction compliance audit findings"
    duration: "00:05:11"
total_duration: "00:05:11"
ai_log: "../ai-logs/2026/11/12/instruction-compliance-issues-20261112/conversation.md"
source: "user"
revisions:
  - chat_id: "prioritize-local-issues-20261112"
    model: "OpenAI/unknown@unknown"
    date: "2026-11-12"
    change: "Add impact, effort, risk, and recommended priority assessment."
    ai_log: "../ai-logs/2026/11/12/prioritize-local-issues-20261112/conversation.md"
  - chat_id: "issues-conformance-20261112"
    model: "OpenAI/unknown@unknown"
    date: "2026-11-12"
    change: "Restructure the local issue draft to follow GitHub issue authoring guidance and add verified evidence."
    ai_log: "../ai-logs/2026/11/12/issues-conformance-20261112/conversation.md"
---

# Make the provenance-field count consistent in the authoring guide

**Audit finding:** Missing/ambiguous instructions

## Summary

Reconcile the number of required provenance fields stated in the
instruction-authoring guide with its actual field list.

## Context

The guide calls the provenance requirements "12 fields," while its checklist
refers to 11 required fields. This leaves unclear whether the list or the count
is authoritative.

## Assessment

- **Impact:** Low - the mismatch is ambiguous, although the field list and
  examples are present.
- **Effort:** Low - verify the canonical list and make the count consistent.
- **Risk:** Low - focused documentation correction; verify every count
  reference.
- **Recommended priority:** P3 - low impact and effort, but resolve before or
  alongside [001](001-instruction-metadata.md).

## Expected outcome

The authoring guide and its examples state one consistent number of required
provenance fields.

## Acceptance criteria

- [ ] The canonical provenance fields are enumerated and counted consistently.
- [ ] Examples and checklists match the canonical list.

## Evidence

- The guide says there are 12 required fields at
  [line 183](../.github/instructions/instruction-files.instructions.md#L181-L185).
- Its [technical checklist](../.github/instructions/instruction-files.instructions.md#L386-L390)
  and [post-creation checklist](../.github/instructions/instruction-files.instructions.md#L557-L563)
  refer to 11 fields.
- The [canonical AI-output policy](../.github/instructions/ai-assisted-output.instructions.md#L67-L81)
  enumerates the required provenance fields.

## Dependencies and scope

- No hard prerequisite. Resolve before or alongside
  [001](001-instruction-metadata.md), which audits instruction metadata.
- Limit the change to the authoring guide's count, field list, and matching
  examples/checklists.
