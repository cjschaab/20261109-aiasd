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

# Repair stale links in the instruction-authoring guide

**Audit rule:** RULE-003

## Summary

Make internal references in the instruction-authoring guide resolve to current
repository content.

## Context

The guide links to instruction and skill files that are absent from this
workspace and to a heading that does not exist in the target document. These
links can misdirect contributors following the authoring workflow.

## Assessment

- **Impact:** Medium - broken references obstruct the instruction-authoring
  workflow.
- **Effort:** Low - verify each reference, then update it or remove it.
- **Risk:** Low - documentation-only, provided replacements are confirmed before
  linking.
- **Recommended priority:** P2 - medium impact and low effort; resolving these
  links also helps [002](002-missing-provenance-logs.md) and
  [005](005-overdue-instruction-review.md).

## Expected outcome

Every retained internal link in the instruction-authoring guide resolves to the
intended current file or section.

## Acceptance criteria

- [ ] Every active internal link in the guide resolves.
- [ ] Removed or migrated content points to a verified replacement, or the stale
  reference is removed.
- [ ] The post-creation link points to an existing section with the intended
  scope.

## Evidence

- The guide links to absent `github-cli.instructions.md` and
  `business-rules-to-slices.instructions.md` files in its
  [related-documentation section](../.github/instructions/instruction-files.instructions.md#L60-L64).
- The guide links to the absent instruction-authoring skill at
  [line 360](../.github/instructions/instruction-files.instructions.md#L358-L360).
- Its [post-creation reference](../.github/instructions/instruction-files.instructions.md#L557-L563)
  uses a heading anchor that does not exist in the
  [AI-output instructions](../.github/instructions/ai-assisted-output.instructions.md).

## Dependencies and scope

- No hard prerequisite. Coordinate with [005](005-overdue-instruction-review.md)
  because both affect the same guide.
- Verify replacement targets before linking. Do not restore or create referenced
  material as part of this issue unless separately approved.
