---
ai_generated: true
model: "unknown/unknown@unknown"
operator: "jMM-at-CODETraining"
chat_id: "testability-recommendations-20261110"
prompt: |
  write these recommendations to a markdown file in the docs folder
started: "2026-11-10T12:51:55.0174229-08:00"
ended: "2026-11-10T12:52:05.4774332-08:00"
task_durations:
  - task: "Prepare testability recommendations for documentation"
    duration: "00:00:10"
total_duration: "00:00:10"
ai_log: "ai-logs/2026/11/10/testability-recommendations-20261110/conversation.md"
source: "user"
---

# Testability Recommendations

These recommendations come from a code inspection of Daybook and its existing
tests. They are proposed improvements, not implemented changes. No tests were
run as part of the review.

## Existing strengths

The application already has useful testing seams:

- An injected `TimeProvider` makes server-side time controllable.
- `ITaskService` separates endpoint handlers from the service implementation.
- Configurable database paths allow isolated temporary SQLite databases.
- Integration tests cover persistence, migrations, concurrent completion, and
  transaction rollback.

Preserve these strengths while making individual rules easier to exercise.

## Prioritized improvements

### 1. Extract pure recurrence-date calculation

In [TaskService.cs](../TaskService.cs), move recurrence-date calculation into a
pure function accepting the completion date and recurrence value.

Test daily, weekly, and monthly rules, leap years, and month-end boundaries
without starting an HTTP host or opening SQLite. Keep integration tests against
real SQLite for atomic completion and successor creation, concurrency, and
rollback. Extracting date arithmetic should not change transaction boundaries
or the use of the server's local completion date.

### 2. Separate UI rules from DOM rendering

In [app.js](../wwwroot/app.js), separate filtering, searching, sorting, and
overdue calculation from DOM rendering. Pass tasks, selection settings, and
today's date explicitly rather than reading global state and controls inside
each calculation.

This makes most UI rules suitable for fast, deterministic unit tests while
browser tests continue to validate rendered interactions. No frontend framework
is necessary.

### 3. Make browser and server time deterministic

The browser's `today()` function reads the system clock, while recurring-task
completion uses the server clock.

- Freeze browser time for overdue and date-display tests.
- Provide a controllable server clock in the test host when asserting exact
  successor dates.
- Test both sides of local midnight and cases where UTC and local dates differ.

Preserve the distinction between browser-local overdue status and server-local
recurrence dates; controlling time in one does not control the other.

### 4. Add focused HTTP contract tests

Use the existing `ITaskService` dependency in
[TaskEndpoints.cs](../TaskEndpoints.cs) to substitute a stub service in focused
HTTP tests. Verify status codes, the creation `Location` header, serialization,
and exception-to-Problem-Details behavior independently of database operations.

[Program.cs](../Program.cs) still initializes SQLite at startup even when the
service is replaced. If database-free host tests become worthwhile, isolate
initialization behind an explicit test-host seam while keeping normal startup
initialization intact.

Do not introduce a repository abstraction solely for mocking. Real SQLite tests
remain valuable for persistence behavior.

### 5. Split broad test scenarios

In [TaskApiTests.cs](../../../tests/TodoManager.Tests/TaskApiTests.cs), separate
tests that combine unrelated concerns:

- Separate individual CRUD behavior from restart persistence, retaining one
  end-to-end persistence scenario.
- Separate missing-resource responses from OpenAPI operation coverage.

Focused tests identify the broken behavior directly and prevent an early
assertion failure from hiding unrelated checks.

### 6. Strengthen test-resource isolation

[Browser setup](../../../tests/browser/tasks.spec.ts) deletes every task in one
shared test database. The suite currently runs serially, as configured in
[playwright.config.ts](../../../tests/browser/playwright.config.ts).
Before enabling parallel execution, provide worker-isolated databases and
servers so one worker cannot erase another worker's data.

[API test cleanup](../../../tests/TodoManager.Tests/TaskApiTests.cs) calls
`SqliteConnection.ClearAllPools()`, which affects all SQLite pools in the test
process. Prefer cleanup scoped to the test's own connection pool where needed.
After host disposal and connection release, clean up the temporary database and
its SQLite sidecar files. Ensure browser test teardown also removes its
temporary database files.

## High-value assertions to add

- Assert the exact UTC creation timestamp supplied by the fixed clock, not just
  the presence of `createdAt`.
- Verify that 200-character titles and 2,000-character notes are accepted,
  alongside existing oversize rejection cases.
- Validate error-body fields rather than checking only status and content type.
- Extend browser coverage with delayed requests, initial-load failures, delete
  cancellation, and due-date sorting ties.

## Suggested starting point

Start with pure recurrence and UI functions. They provide the largest improvement
in test speed and failure diagnosis without requiring a broad architectural
rewrite. Preserve existing integration and browser coverage as safeguards
against behavior changes.
