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

# Allow task database initialization to be cancelled

**Audit rule:** RULE-007

## Summary

Propagate cancellation through task database initialization and its supported
asynchronous SQLite operations.

## Context

`TaskDatabase.InitializeAsync` performs asynchronous database operations without
accepting a cancellation token. Application startup currently calls it without
a token.

## Assessment

- **Impact:** Medium - cancellation cannot currently interrupt asynchronous
  task database initialization.
- **Effort:** Medium - thread a token through initialization and supported
  SQLite async APIs, then test cancellation behavior.
- **Risk:** Medium - cancellation during schema setup can affect startup and
  needs reliable coverage.
- **Recommended priority:** P2 - medium impact and effort; coordinate with
  [010](010-version-task-schema-upgrades.md) because both change initialization.

## Expected outcome

Task database initialization observes the application lifecycle's cancellation
token wherever the SQLite API supports it.

## Acceptance criteria

- [ ] Initialization accepts a cancellation token and passes it through
  supported SQLite async APIs.
- [ ] The application lifecycle supplies the appropriate token where available.
- [ ] Existing initialization and schema-upgrade tests pass.
- [ ] Cancellation behavior is tested where it can be verified reliably.

## Evidence

- [TaskDatabase.InitializeAsync](../src/TodoManager/TaskDatabase.cs#L16-L59)
  calls `OpenAsync`, `ExecuteNonQueryAsync`, `ReadAsync`, and `CommitAsync`
  without passing cancellation.
- [Program.cs](../src/TodoManager/Program.cs#L99) invokes initialization without
  an application stopping token.
- The [C# development guidance](../.github/instructions/csharp-development.instructions.md#L51-L54)
  requires cancellation to be propagated through supported asynchronous I/O.

## Dependencies and scope

- No hard prerequisite. Coordinate with [010](010-version-task-schema-upgrades.md)
  because both changes affect initialization and schema setup.
- Preserve initialization behavior when no cancellation is requested.
