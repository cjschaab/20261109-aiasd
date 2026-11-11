---
ai_generated: true
model: "unknown/unknown@unknown"
operator: "jMM-at-CODETraining"
chat_id: "add-review-solid-20261110"
prompt: |
  /skill add
  skill=review the codebase for SOLID violations, scope=This repository
started: "2026-11-10T11:52:09.5997879-08:00"
ended: "2026-11-10T11:52:25.4054404-08:00"
task_durations:
  - task: "Design repository-local SOLID review workflow"
    duration: "00:00:15"
total_duration: "00:00:15"
ai_log: "ai-logs/2026/11/10/add-review-solid-20261110/conversation.md"
source: "user"
revisions:
  - chat_id: "testability-recommendations-20261110"
    model: "unknown/unknown@unknown"
    date: "2026-11-10"
    change: "Link the testability recommendations."
    ai_log: "ai-logs/2026/11/10/testability-recommendations-20261110/conversation.md"
  - chat_id: "update-readme-current-state-20261110"
    model: "unknown/unknown@unknown"
    date: "2026-11-10"
    change: "Document current app capabilities, security headers, limits, migrations, API behavior, and test coverage."
    ai_log: "ai-logs/2026/11/10/update-readme-current-state-20261110/conversation.md"
  - chat_id: "add-review-solid-20261110"
    model: "unknown/unknown@unknown"
    date: "2026-11-10"
    change: "Document repository-local SOLID review skill."
    ai_log: "ai-logs/2026/11/10/add-review-solid-20261110/conversation.md"
  - chat_id: "timestamp-review-solid-20261110"
    model: "unknown/unknown@unknown"
    date: "2026-11-10"
    change: "Document timestamped SOLID review reports."
    ai_log: "ai-logs/2026/11/10/timestamp-review-solid-20261110/conversation.md"
  - chat_id: "test-workflow-20261111"
    model: "unknown/unknown@unknown"
    date: "2026-11-11"
    change: "Document automated test checks."
    ai_log: "ai-logs/2026/11/11/test-workflow-20261111/conversation.md"
  - chat_id: "tdd-instructions-20261111"
    model: "unknown/unknown@unknown"
    date: "2026-11-11"
    change: "Add test-driven development instructions."
    ai_log: "ai-logs/2026/11/11/tdd-instructions-20261111/conversation.md"
  - chat_id: "fix-pending-save-race-20261111"
    model: "OpenAI/gpt-6.1-sol@unknown"
    date: "2026-11-11"
    change: "Document pending-save form locking and deterministic browser regression coverage."
    ai_log: "ai-logs/2026/11/11/fix-pending-save-race-20261111/conversation.md"
---

# Daybook

A responsive todo manager built with ASP.NET Core (.NET 10), SQLite, and plain
HTML/CSS/JavaScript. Create, edit, complete, reopen, and delete tasks; add notes,
priorities, and due dates; search, filter, and sort your list.

## Current project state

Daybook is a working, single-server task manager. The ASP.NET Core minimal API
serves both the JSON task API and the browser UI; task data is persisted in one
SQLite database. The UI provides task counts, search, status filters, sorting,
editing, and confirmation before deletion. API input is validated and invalid
requests return Problem Details. Responses include a Content Security Policy and
`X-Content-Type-Options: nosniff`; priority and recurrence values use JSON strings.

While a request is pending, the editor fields and task action controls are
disabled. A successful save clears the form and returns focus to the title;
a failed save preserves the entered values and re-enables the form for retry.

The app does not currently include authentication, multi-instance storage,
real-time updates between browsers, or background task scheduling. Changes from
another browser appear after Refresh. Concurrent edits replace the whole task,
so the last save wins.

## Recurring tasks

Choose **Daily**, **Weekly**, or **Monthly** in the Repeat field when adding or
editing a task. On completion, the task stays in history and a new incomplete task
is created with the same title, notes, priority, and recurrence. Its due date is
one day, seven days, or one calendar month after the completion date, using the
server's local timezone, regardless of the previous due date. An initial due date
is optional. Monthly dates clamp to the last valid day of the next month.

Each occurrence generates at most one successor, even if completion is retried
or the task is reopened and completed again. Deleting a task does not delete its
successor. To stop repeating, set the active occurrence's Repeat field to
**Does not repeat**. No tasks are generated in the background.
Existing databases are automatically upgraded on startup to add recurrence and
successor-tracking fields; existing task data is retained and old tasks default
to no recurrence.

## Run

Install the .NET 10 SDK, then from the repository root:

```powershell
dotnet run --project .\src\TodoManager
```

Open http://localhost:5050. Tasks persist across restarts in
`src\TodoManager\App_Data\tasks.db`; the database starts empty.
All browsers connecting to the same server share the same list. Use Refresh to
load changes made on another device; updates are not pushed automatically.
Editing saves the entire task, so the last save wins when people edit concurrently.

This app deliberately has **no authentication**. The default listener is
localhost-only. To use it on a trusted network, bind explicitly:

```powershell
dotnet run --project .\src\TodoManager -- --urls http://0.0.0.0:5050
```

Browse to the server's LAN address on port 5050 from another device. Limit firewall
access to your trusted network. Anyone who can reach the app can view and change
every task. Do not expose it to the public internet; add authentication and HTTPS
before any public hosting. No Azure resources are required or provisioned.

## Test

For behavior changes and bug fixes, follow the
[Test-Driven Development instructions](.github/instructions/test-driven-development.instructions.md).

```powershell
dotnet test .\tests\TodoManager.Tests
```

Integration tests use isolated temporary SQLite databases. Coverage includes
CRUD, validation, persistence across host restarts, serving browser assets,
OpenAPI operations, recurring task dates and month-end clamping, duplicate and
concurrent completion, transaction rollback when successor creation fails, and
upgrading a legacy database.

Collect .NET code coverage in Cobertura format with:

```powershell
dotnet test .\tests\TodoManager.Tests --collect:"XPlat Code Coverage"
```

The report is written under `tests\TodoManager.Tests\TestResults`.

Browser tests exercise the rendered UI and its interactions against the real app:

```powershell
cd .\tests\browser
npm install
npx playwright install chromium
npm test
```

The Playwright web server uses an isolated temporary SQLite database. Browser
tests run serially because they share that test server.

The [browser regression tests](tests/browser/tasks.spec.ts) hold save responses
open to verify form locking and recovery after success or failure. Multi-task
setup waits for each new card before entering the next task. The
[TDD verification record](ai-logs/2026/11/11/fix-pending-save-race-20261111/summary.md)
records the failing tests and five consecutive passing suite runs.

The [Tests workflow](.github/workflows/tests.yml) runs both suites on every
push and pull request. Push a branch and check the results before opening a PR;
the checks also run when the PR is updated. The workflow does not prevent
someone from opening a PR, and blocking merges requires configuring both jobs
as required status checks in GitHub branch protection or rulesets.

See [Testability Recommendations](src/TodoManager/docs/testability-recommendations.md)
for prioritized improvements to test seams, deterministic time, assertions, and
test-resource isolation.

## API

| Method | Route | Behavior |
|---|---|---|
| GET | `/api/tasks` | List tasks, newest first |
| GET | `/api/tasks/{id}` | Get a task |
| POST | `/api/tasks` | Create; returns 201 and Location |
| PUT | `/api/tasks/{id}` | Replace fields, including completion |
| DELETE | `/api/tasks/{id}` | Delete; returns 204 |

Titles are required (maximum 200 characters); notes allow 2,000 characters.
Priority is `Low`, `Medium` (default), or `High`.
`recurrence` is `None` (default), `Daily`, `Weekly`, or `Monthly`.
`dueDate` is an optional
`YYYY-MM-DD` calendar date, intentionally not a timezone-dependent timestamp.
`createdAt` is a UTC timestamp. Overdue means an incomplete task due before the
browser's current calendar date.

Invalid request bodies return `400` Problem Details responses. Unknown task IDs
return `404`; successful deletion returns `204`.

Development OpenAPI: http://localhost:5050/openapi/v1.json.
Example requests: [TodoManager.http](src/TodoManager/TodoManager.http).
Override the database location with the `TaskDatabase__Path` environment variable
(use an absolute path outside the public web root). Keep the database on local
disk and use SQLite-aware backups while the app runs, or stop the app before
copying its database files. Schema creation and the recurrence migration are
automatic. This is a single-server app, not a multi-instance service.

For setup, project structure, configuration, recurrence behavior, and a detailed
endpoint and payload reference, see the [Developer Guide](src/TodoManager/docs/developers-guide.md).

## Review skill

[Review SOLID](.github/skills/review-solid/SKILL.md) provides a read-only,
evidence-backed review of Single Responsibility, Open/Closed, Liskov Substitution,
Interface Segregation, and Dependency Inversion. Ask to "review the codebase for
SOLID violations" to use it. It reports concrete impacts, focused recommendations,
and validation suggestions without changing application code. Each report includes
the run's start date and time with its timezone.
