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

# Add actionable security checks to the TDD instructions

**Audit rule:** RULE-004

## Summary

Ensure the test-driven development guide includes or links to applicable
security checks for behavior changes.

## Context

The instruction-authoring guide requires code-generation instructions to
include security requirements. The TDD guide covers implementation and bug fixes
but does not currently mention security checks.

## Assessment

- **Impact:** Medium - behavior changes developed using the TDD guide may omit
  security-specific validation.
- **Effort:** Low - add concise checks or link to verified canonical guidance.
- **Risk:** Low - documentation-only if the guidance matches existing policy.
- **Recommended priority:** P2 - medium impact, low effort, and independent of
  the other listed work.

## Expected outcome

Contributors following the TDD guide are directed to apply relevant security
checks without duplicating or contradicting canonical security guidance.

## Acceptance criteria

- [ ] The TDD guide includes actionable security checks or a direct link to
  canonical security guidance.
- [ ] Guidance addresses input validation and safe handling of secrets or other
  security-sensitive behavior.
- [ ] No unrelated security policy is duplicated.

## Evidence

- The [instruction-authoring guide](../.github/instructions/instruction-files.instructions.md#L517-L545)
  requires security requirements in code-generation instructions.
- The [TDD guide](../.github/instructions/test-driven-development.instructions.md#L27-L123)
  covers behavior changes, testing, and validation but has no security section
  or security-specific checks.

## Dependencies and scope

- No prerequisite. Check for current canonical security guidance before adding
  new rules; keep this change limited to the TDD guide or its direct links.
