---
ai_generated: true
model: "OpenAI/unknown@unknown"
operator: "jMM-at-CODETraining"
chat_id: "refactoring-finding-issues-20261112"
prompt: |
  create issues for the findings and put them in the issues folder as markdown files
started: "2026-11-12T11:14:00.289-08:00"
ended: "2026-11-12T11:17:21.225-08:00"
task_durations:
  - task: "Document unresolved refactoring findings"
    duration: "00:03:21"
total_duration: "00:03:21"
ai_log: "../ai-logs/2026/11/12/refactoring-finding-issues-20261112/conversation.md"
source: "user; src/TodoManager/docs/refactoring-plan.md"
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

# Split task API tests by concern without losing coverage

**Finding:** Test organization

## Summary

Separate task API integration tests into focused test groups while preserving
their current behavioral coverage and shared test setup.

## Context

`TaskApiTests.cs` covers API contracts, persistence across host restarts,
validation, recurrence, concurrency, rollback, and legacy database upgrades.
The broad scenarios make failures harder to localize.

## Assessment

- **Impact:** Low - the scenarios are covered, but broad tests make failures
  harder to diagnose.
- **Effort:** Medium - reorganize tests and setup without changing behavior or
  adding a general-purpose framework.
- **Risk:** Medium - moving integration tests can weaken coverage or alter
  database and host isolation.
- **Recommended priority:** P2 - useful preparation for
  [010](010-version-task-schema-upgrades.md), but not a prerequisite.

## Expected outcome

Test files and classes have focused responsibilities and retain existing
integration coverage.

## Acceptance criteria

- [ ] Test classes/files have clear responsibility boundaries.
- [ ] Existing assertions and scenarios retain equivalent behavior coverage.
- [ ] Restart persistence, recurring-task atomicity, concurrency, and legacy
  database compatibility remain covered.
- [ ] The complete .NET test suite passes.

## Evidence

- [TaskApiTests.cs](../tests/TodoManager.Tests/TaskApiTests.cs#L34-L107)
  combines restart persistence and input validation coverage.
- The same file includes recurrence, concurrency, rollback, and legacy upgrade
  scenarios at [lines 180-356](../tests/TodoManager.Tests/TaskApiTests.cs#L180-L356).

## Dependencies and scope

- No hard prerequisite. This reorganization can help prepare tests for
  [010](010-version-task-schema-upgrades.md), but schema versioning does not
  depend on the split.
- Keep the existing isolated database and host setup. Do not add a general
  testing framework.
