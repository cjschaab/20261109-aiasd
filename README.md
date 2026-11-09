Welcome!

# Todo List Manager

An ASP.NET Core (.NET 10) minimal API backend (`src/TodoApp.Server`) that hosts a Blazor WebAssembly frontend (`src/TodoApp.Client`). Run `dotnet run --project src/TodoApp.Server` and open http://localhost:5000.

API: `GET/POST /api/todos`, `PATCH/DELETE /api/todos/:id`, `DELETE /api/todos?completed=true`. Todos have `text`, `done`, `priority` (`Low`/`Medium`/`High`), `dueDate` (`yyyy-MM-dd`) and `notes`. On `PATCH`, omitted fields are unchanged; an empty `dueDate` or `notes` clears it.

Features: add, edit, complete, delete, priorities (color-coded red/amber/green), due dates (overdue highlighted), notes, filter (All/Active/Completed), clear completed. Data is stored server-side in `data/todos.json`.

## Tests

Run the automated tests from the repository root with:

```bash
dotnet test TodoApp.sln
```

