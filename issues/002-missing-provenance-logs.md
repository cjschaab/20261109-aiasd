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

# Resolve missing instruction provenance logs without inventing history

**Audit rule:** RULE-002

## Summary

Repair or accurately document missing conversation-log references in active
instruction files.

## Context

Several instruction files point to conversation logs that are absent from the
workspace. The provenance policy also requires a summary for each chat log.
Historical content that cannot be recovered must not be reconstructed as fact.

## Assessment

- **Impact:** Medium - broken references reduce traceability for affected
  instruction files.
- **Effort:** High - locating historical conversations may take substantial
  effort, and some may not be recoverable.
- **Risk:** High - fabricating historical content would create misleading
  provenance.
- **Recommended priority:** P2 - address after the metadata requirements and
  stale references are clarified.

## Expected outcome

Each active instruction file either references an existing, complete chat log or
clearly records the limits of unrecoverable provenance.

## Acceptance criteria

- [ ] Every retained `ai_log` reference resolves to a `conversation.md`.
- [ ] Each referenced chat folder contains its required `summary.md`.
- [ ] Unrecoverable logs are handled by correcting the reference or explicitly
  documenting the provenance limitation.
- [ ] No historical prompt, model, or conversation content is invented.

## Evidence

- [ai-assisted-output.instructions.md](../.github/instructions/ai-assisted-output.instructions.md#L18)
  references `ai-logs/2026/01/20/generate-ai-output-policy-20260120/conversation.md`,
  which is absent from the workspace.
- [instruction-files.instructions.md](../.github/instructions/instruction-files.instructions.md#L23)
  references `ai-logs/2026/02/11/submit-create-instruction-files-20260211/conversation.md`,
  which is absent from the workspace.
- [custom-agents.instructions.md](../.github/instructions/custom-agents.instructions.md#L17)
  references `ai-logs/2026/02/12/update-custom-agents-instructions-20260212/conversation.md`,
  which is absent from the workspace.
- The [AI-output policy](../.github/instructions/ai-assisted-output.instructions.md#L106-L121)
  requires both `conversation.md` and `summary.md`.

## Dependencies and scope

- No implementation prerequisite is mandatory. Resolve or coordinate with
  [001](001-instruction-metadata.md) and
  [003](003-stale-instruction-references.md) so the final metadata and source
  references are settled before repairing logs.
- Limit the work to active instruction files and provenance references they
  actually contain.
