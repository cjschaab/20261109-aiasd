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

# Review and update instruction-authoring guide review metadata

**Audit rule:** RULE-005

## Summary

Review the instruction-authoring guide and record accurate review metadata.

## Context

The guide's `nextReview` date is 2026-05-11, which has passed. Its template
describes a three-month review interval.

## Assessment

- **Impact:** Low - the overdue date does not prevent use, but leaves current
  accuracy unconfirmed.
- **Effort:** Low - review the guide and update its review dates.
- **Risk:** Low - documentation-only; verify the review and links before
  recording completion.
- **Recommended priority:** P3 - low impact and effort; address with
  [003](003-stale-instruction-references.md).

## Expected outcome

The guide has been reviewed for accuracy and its review metadata reflects the
completed review and documented cadence.

## Acceptance criteria

- [ ] The guide is reviewed for accuracy and working links.
- [ ] `reviewedDate` reflects the actual review date.
- [ ] `nextReview` is set according to the guide's stated review interval.

## Evidence

- The guide's front matter sets `nextReview` to `2026-05-11`
  ([metadata](../.github/instructions/instruction-files.instructions.md#L25-L33)).
- Its metadata template specifies a review date and a next review three months
  later ([governance fields](../.github/instructions/instruction-files.instructions.md#L215-L228)).

## Dependencies and scope

- Coordinate with [003](003-stale-instruction-references.md) so known broken
  links in the same guide are fixed before review completion is recorded.
- Do not change review metadata until the review has actually been completed.
