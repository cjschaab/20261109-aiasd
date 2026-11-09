# Daybook

A responsive todo manager built with ASP.NET Core (.NET 10), SQLite, and plain
HTML/CSS/JavaScript. Create, edit, complete, reopen, and delete tasks; add notes,
priorities, and due dates; search, filter, and sort your list.

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
Existing databases are automatically upgraded on startup; existing tasks keep
their data and default to no recurrence.

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

```powershell
dotnet test .\tests\TodoManager.Tests
```

Integration tests use isolated temporary SQLite databases and cover CRUD,
validation, persistence across host restarts, static assets, and OpenAPI.

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

Development OpenAPI: http://localhost:5050/openapi/v1.json.
Example requests: [TodoManager.http](src/TodoManager/TodoManager.http).
Override the database location with the `TaskDatabase__Path` environment variable
(use an absolute path outside the public web root). Keep the database on local
disk and use SQLite-aware backups while the app runs, or stop the app before
copying its database files. Schema creation and the recurrence migration are
automatic. This is a single-server app, not a multi-instance service.
