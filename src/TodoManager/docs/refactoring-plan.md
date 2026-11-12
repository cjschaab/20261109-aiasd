---
ai_generated: true
model: "unknown/unknown@unknown"
operator: "jMM-at-CODETraining"
chat_id: "save-refactoring-plan-20261112"
prompt: |
  save this refatoring plan
started: "2026-11-12T09:18:28.907-08:00"
ended: "2026-11-12T09:20:23.800-08:00"
task_durations:
  - task: "Save the refactoring analysis as a developer document"
    duration: "00:01:54"
total_duration: "00:01:54"
ai_log: "ai-logs/2026/11/12/save-refactoring-plan-20261112/conversation.md"
source: "user; refactoring analysis from this chat"
revisions:
  - chat_id: "ebbe8f67-e720-4db7-a51d-c2a9d581eccd"
    model: "OpenAI/unknown@unknown"
    date: "2026-11-12"
    change: "Implemented and verified recurrence and browser list-rule extraction; marked completed work."
    ai_log: "ai-logs/2026/11/12/ebbe8f67-e720-4db7-a51d-c2a9d581eccd/conversation.md"
  - chat_id: "phase1-sqlite-pool-cleanup-20261112"
    model: "OpenAI/unknown@unknown"
    date: "2026-11-12"
    change: "Replaced process-wide test database pool cleanup with test-scoped cleanup and verified the Phase 1 .NET tests."
    ai_log: "ai-logs/2026/11/12/phase1-sqlite-pool-cleanup-20261112/conversation.md"
---

# Daybook Refactoring Plan

This plan records evidence-backed opportunities from a codebase review. It is
an incremental roadmap, not a proposal for a broad rewrite. No application code
was changed as part of the analysis.

## Executive summary

Daybook already has useful seams: `ITaskService`, injected `TimeProvider`,
configurable SQLite paths, and real integration tests. The main opportunities
are to make recurrence and browser list rules independently testable, keep
task-database schema upgrades easier to extend, and reduce coupling in the
largest integration-test class.

## Hotspots inventory

- [app.js](../wwwroot/app.js#L62) - 302 lines combining task/auth state, DOM
  rendering, filtering, sorting, and request handling.
- [TaskApiTests.cs](../../../tests/TodoManager.Tests/TaskApiTests.cs#L34) -
  372 lines covering CRUD, persistence, validation, recurrence, concurrency,
  and schema upgrades.
- [LoginFlowTests.cs](../../../tests/TodoManager.Tests/LoginFlowTests.cs#L21)
  and
  [IdentityCookieTests.cs](../../../tests/TodoManager.Tests/IdentityCookieTests.cs#L21)
  - 326 and 304 lines respectively, including host and Identity setup.
- [TaskService.cs](../TaskService.cs#L45) - 181 lines combining CRUD, SQL
  mapping, transactions, and recurring-task generation.
- [Program.cs](../Program.cs#L10) - 146 lines of service configuration and an
  ordering-sensitive middleware pipeline.
- [TaskDatabase.cs](../TaskDatabase.cs#L16) - 63 lines of task-table creation
  and hand-managed schema upgrades.

## Findings

| Category | File/target | Evidence | Impact | Refactor | Effort | Risk |
|---|---|---|---|---|---|---|
| Testing friction | [TaskService.cs](../TaskService.cs#L103), lines 103-150 | `UpdateAsync` combines persistence, transaction handling, local-date acquisition, and recurrence-date calculation. Month-end rules are exercised through API tests in [TaskApiTests.cs](../../../tests/TodoManager.Tests/TaskApiTests.cs#L342). | Date-rule changes require database/host setup, making edge cases slower to isolate. | Extract a pure recurrence-date calculator; leave clock selection and successor insertion within the service transaction. | S | Low |
| UI/logic coupling | [app.js](../wwwroot/app.js#L46), lines 46-80 and 91-303 | Date rules, filtering, sorting, counts, rendering, global state, and event handlers share one module. Playwright coverage is in [tasks.spec.ts](../../../tests/browser/tasks.spec.ts#L1). | UI-rule changes are harder to test independently and can entangle rendering with selection behavior. | Extract pure date and task-selection functions that accept tasks, filter/sort settings, and today's date; keep browser tests for rendered interactions. | M | Med |
| Schema evolution | [TaskDatabase.cs](../TaskDatabase.cs#L24), lines 24-61 | Startup creates the table, inspects columns with `PRAGMA table_info`, and conditionally runs `ALTER TABLE` statements. | Each schema change adds another imperative upgrade path to maintain and verify against existing databases. | Incrementally introduce an explicit, versioned task-schema upgrade mechanism; preserve the legacy-data test in [TaskApiTests.cs](../../../tests/TodoManager.Tests/TaskApiTests.cs#L306). | M | Med |
| Test organization | [TaskApiTests.cs](../../../tests/TodoManager.Tests/TaskApiTests.cs#L34), lines 34-150 and 162-368 | One class spans API contracts, restart persistence, recurrence/concurrency, and migrations. `CrudAndPersistenceWorkAcrossHostRestarts` also combines multiple CRUD operations. | Failures can obscure which behavior regressed, and the large test class is harder to navigate. | Split by concern while retaining a focused end-to-end persistence test. Avoid a generalized test framework unless setup duplication justifies it. | S | Low |
| Test isolation | [TaskApiTests.cs](../../../tests/TodoManager.Tests/TaskApiTests.cs#L368); [playwright.config.ts](../../../tests/browser/playwright.config.ts#L17) | API-test disposal calls process-wide `SqliteConnection.ClearAllPools()`; browser tests share a database and delete its tasks in `beforeEach` ([tasks.spec.ts](../../../tests/browser/tasks.spec.ts#L3)). Playwright currently uses one worker. | Global pool clearing may interfere with other active tests; the shared browser database prevents safe parallel runs. | Scope SQLite cleanup to the test's connection pool. Keep browser execution serial unless worker-specific databases and servers are introduced. | S-M | Med |

## Prioritized proposals

### Quick wins

- [x] Extract recurrence-date calculation and add focused tests for daily,
  weekly, monthly, leap-year, and month-end behavior.
- [ ] Split broad API tests by contract, persistence, recurrence, and migration
  concern.
- [x] Assert the fixed-clock creation timestamp and acceptance of maximum-length
  task fields.

### Phase 1 - Test enablers

- [x] Separate browser task-selection/date rules from DOM rendering; pure
  rule tests and the existing Playwright integration suite pass.
- [x] Replace process-wide SQLite pool cleanup with test-scoped cleanup for
  both connection strings used by the API fixture; verify tests remain isolated.

### Phase 2 - Structural

- Introduce explicit task-schema versioning in small steps, preserving upgrades
  from existing databases and the legacy-data test.
- Keep transaction boundaries and real-SQLite coverage around recurring-task
  completion, concurrency, and rollback.

### Phase 3 - Conditional

- Add worker-specific browser databases only if parallel Playwright execution
  becomes a goal.
- Extract startup configuration or middleware from [Program.cs](../Program.cs)
  only if it grows; the composition root is currently straightforward and
  middleware order matters.

## Safety net and test strategy

- [x] Preserve real-SQLite coverage for persistence, concurrency, rollback, and
  legacy upgrades.
- [x] Add pure tests for extracted recurrence and browser-selection rules;
  continue using controlled server and browser clocks.
- [x] Run the focused .NET tests, browser unit tests, and Playwright suite for
  these changes. No tests were run for the analysis itself.

## Verification

- `dotnet test tests/TodoManager.Tests/TodoManager.Tests.csproj --no-restore --filter "FullyQualifiedName~TaskRecurrenceDateCalculatorTests|FullyQualifiedName~TaskApiTests"` - 31 passed.
- `npm run test:unit --prefix tests/browser` - 3 passed.
- `npm test --prefix tests/browser` - 17 passed.
- `dotnet test .\tests\TodoManager.Tests --no-restore --filter "FullyQualifiedName~TaskApiTests"` - 25 passed.
- `dotnet test .\tests\TodoManager.Tests --no-restore` - 93 passed.

## Sources scanned

[TaskService.cs](../TaskService.cs),
[TaskDatabase.cs](../TaskDatabase.cs),
[Program.cs](../Program.cs),
[app.js](../wwwroot/app.js),
[TaskApiTests.cs](../../../tests/TodoManager.Tests/TaskApiTests.cs),
[LoginFlowTests.cs](../../../tests/TodoManager.Tests/LoginFlowTests.cs),
[IdentityCookieTests.cs](../../../tests/TodoManager.Tests/IdentityCookieTests.cs),
[tasks.spec.ts](../../../tests/browser/tasks.spec.ts),
[playwright.config.ts](../../../tests/browser/playwright.config.ts), and
[Testability Recommendations](./testability-recommendations.md).

## Analysis durations

- Inventory: 00:00:35
- Source and test review: 00:01:05
- Prioritization and synthesis: 00:00:24
- Total: 00:02:04
