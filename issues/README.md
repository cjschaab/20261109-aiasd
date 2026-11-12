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
  - chat_id: "refactoring-finding-issues-20261112"
    model: "OpenAI/unknown@unknown"
    date: "2026-11-12"
    change: "Add an index for unresolved refactoring findings."
    ai_log: "../ai-logs/2026/11/12/refactoring-finding-issues-20261112/conversation.md"
  - chat_id: "prioritize-local-issues-20261112"
    model: "OpenAI/unknown@unknown"
    date: "2026-11-12"
    change: "Add impact, effort, risk, and a prioritized execution sequence."
    ai_log: "../ai-logs/2026/11/12/prioritize-local-issues-20261112/conversation.md"
  - chat_id: "issues-conformance-20261112"
    model: "OpenAI/unknown@unknown"
    date: "2026-11-12"
    change: "Align local issue drafts and prioritization with GitHub issue authoring guidance."
    ai_log: "../ai-logs/2026/11/12/issues-conformance-20261112/conversation.md"
---

# Local issues

These Markdown issue descriptions track actionable findings from repository
reviews. They are local drafts and have not been published as GitHub issues.

## Recommended prioritization

Priority tiers reflect impact, effort, and dependency value; they are not a
strict implementation order. P1 is the most consequential work, P2 covers
medium-impact work and important prerequisites, and P3 covers lower-impact
cleanup. A P3 issue may still need to happen early when it unblocks another
issue.

| Recommended tier | Issue | Rationale |
| --- | --- | --- |
| P1 | [Ensure task schema upgrades are versioned and preserve existing data](010-version-task-schema-upgrades.md) | High impact and high risk to existing data; schedule early, implement after test preparation and database-change coordination. |
| P2 | [Repair stale links in the instruction-authoring guide](003-stale-instruction-references.md) | Medium impact, low effort, and unblocks provenance-log recovery and the instruction review. |
| P2 | [Add actionable security checks to the TDD instructions](004-tdd-security-guidance.md) | Medium impact, low effort, and independent. |
| P2 | [Make instruction-file provenance and governance metadata consistent](001-instruction-metadata.md) | Medium impact; resolve the field-count ambiguity first and complete before provenance-log recovery. |
| P2 | [Allow task database initialization to be cancelled](007-database-initialization-cancellation.md) | Medium impact; coordinate with schema initialization changes. |
| P2 | [Split task API tests by concern without losing coverage](009-split-task-api-tests.md) | Low direct impact, but useful test preparation for the high-impact schema upgrade. |
| P2 | [Resolve missing instruction provenance logs without inventing history](002-missing-provenance-logs.md) | Medium impact, but high effort; follow metadata and reference cleanup. |
| P3 | [Make the provenance-field count consistent in the authoring guide](008-metadata-field-count.md) | Low impact and effort; do early because it unblocks issue 001. |
| P3 | [Handle nullable database paths and API test results explicitly](006-null-forgiving-operators.md) | Low impact and effort; coordinate with related database initialization work. |
| P3 | [Review and update instruction-authoring guide review metadata](005-overdue-instruction-review.md) | Low impact and effort; complete with issue 003's link repair. |

### Suggested execution sequence

1. Coordinate issues 003, 008, and 005 because they all update the
   instruction-authoring guide. Issue 004 is independent.
2. Resolve 001 after the provenance-field count is settled; then address 002
   after the relevant metadata and source references are clear.
3. For database work, use 009 to prepare focused test boundaries and coordinate
   006, 007, and 010 because they share database or test files. Issue 010 remains
   the top scheduling priority because of its data-retention impact; this is a
   priority recommendation, not a claim that the work can proceed in parallel.

## Instruction compliance findings

1. [Make instruction-file provenance and governance metadata consistent](001-instruction-metadata.md)
2. [Resolve missing instruction provenance logs without inventing history](002-missing-provenance-logs.md)
3. [Repair stale links in the instruction-authoring guide](003-stale-instruction-references.md)
4. [Add actionable security checks to the TDD instructions](004-tdd-security-guidance.md)
5. [Review and update instruction-authoring guide review metadata](005-overdue-instruction-review.md)
6. [Handle nullable database paths and API test results explicitly](006-null-forgiving-operators.md)
7. [Allow task database initialization to be cancelled](007-database-initialization-cancellation.md)
8. [Make the provenance-field count consistent in the authoring guide](008-metadata-field-count.md)

## Refactoring findings

These issues track unresolved, actionable findings from the
[Daybook Refactoring Plan](../src/TodoManager/docs/refactoring-plan.md).
Findings already addressed in the plan are not duplicated here.

1. [Split task API tests by concern without losing coverage](009-split-task-api-tests.md)
2. [Ensure task schema upgrades are versioned and preserve existing data](010-version-task-schema-upgrades.md)
