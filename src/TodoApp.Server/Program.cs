using System.Text.Json;
using TodoApp;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(new TodoStore(Path.Combine(builder.Environment.ContentRootPath, "data", "todos.json")));

var app = builder.Build();
app.MapStaticAssets();

var api = app.MapGroup("/api/todos");

api.MapGet("/", (TodoStore store) => store.GetAll());

api.MapPost("/", (CreateTodo req, TodoStore store) =>
{
    var text = req.Text?.Trim();
    if (string.IsNullOrEmpty(text)) return Results.BadRequest(new { error = "text is required" });
    if (req.Priority is not null && !Priorities.IsValid(req.Priority)) return Results.BadRequest(new { error = "invalid priority" });
    if (req.DueDate is not null && !DateOnly.TryParse(req.DueDate, out _)) return Results.BadRequest(new { error = "invalid dueDate" });
    var todo = store.Add(text, req.Priority ?? Priorities.Medium, ParseDate(req.DueDate), req.Notes?.Trim() ?? "");
    return Results.Created($"/api/todos/{todo.Id}", todo);
});

// Null fields are left unchanged; an empty dueDate or notes string clears the value.
api.MapPatch("/{id:long}", (long id, UpdateTodo req, TodoStore store) =>
{
    if (req.Priority is not null && !Priorities.IsValid(req.Priority)) return Results.BadRequest(new { error = "invalid priority" });
    if (!string.IsNullOrEmpty(req.DueDate) && !DateOnly.TryParse(req.DueDate, out _)) return Results.BadRequest(new { error = "invalid dueDate" });
    var todo = store.Update(id, req.Text?.Trim(), req.Done, req.Priority, req.DueDate, req.Notes?.Trim());
    return todo is null ? Results.NotFound() : Results.Ok(todo);
});

api.MapDelete("/{id:long}", (long id, TodoStore store) =>
    store.Delete(id) ? Results.NoContent() : Results.NotFound());

api.MapDelete("/", (bool? completed, TodoStore store) =>
{
    if (completed != true) return Results.BadRequest(new { error = "use ?completed=true" });
    store.ClearCompleted();
    return Results.NoContent();
});

app.MapFallbackToFile("index.html");

static DateOnly? ParseDate(string? s) =>
    !string.IsNullOrEmpty(s) && DateOnly.TryParse(s, out var d) ? d : null;

app.Run();

namespace TodoApp
{
    public static class Priorities
    {
        public const string Low = "Low", Medium = "Medium", High = "High";
        public static bool IsValid(string p) => p is Low or Medium or High;
    }

    public class Todo
    {
        public long Id { get; set; }
        public string Text { get; set; } = "";
        public bool Done { get; set; }
        public string Priority { get; set; } = Priorities.Medium;
        public DateOnly? DueDate { get; set; }
        public string Notes { get; set; } = "";
    }

    public record CreateTodo(string? Text, string? Priority, string? DueDate, string? Notes);
    public record UpdateTodo(string? Text, bool? Done, string? Priority, string? DueDate, string? Notes);

    public class TodoStore
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
        private readonly string _path;
        private readonly object _lock = new();
        private readonly List<Todo> _todos;
        private long _nextId;

        public TodoStore(string path)
        {
            _path = path;
            try
            {
                _todos = File.Exists(path)
                    ? JsonSerializer.Deserialize<List<Todo>>(File.ReadAllText(path), Options) ?? []
                    : [];
            }
            catch (JsonException)
            {
                _todos = [];
            }
            _nextId = _todos.Count == 0 ? 1 : _todos.Max(t => t.Id) + 1;
        }

        public List<Todo> GetAll() { lock (_lock) return [.. _todos]; }

        public Todo Add(string text, string priority = Priorities.Medium, DateOnly? dueDate = null, string notes = "")
        {
            lock (_lock)
            {
                var todo = new Todo { Id = _nextId++, Text = text, Priority = priority, DueDate = dueDate, Notes = notes };
                _todos.Add(todo);
                Save();
                return todo;
            }
        }

        public Todo? Update(long id, string? text, bool? done, string? priority = null, string? dueDate = null, string? notes = null)
        {
            lock (_lock)
            {
                var todo = _todos.Find(t => t.Id == id);
                if (todo is null) return null;
                if (!string.IsNullOrEmpty(text)) todo.Text = text;
                if (done is not null) todo.Done = done.Value;
                if (priority is not null) todo.Priority = priority;
                if (dueDate is not null) todo.DueDate = DateOnly.TryParse(dueDate, out var d) ? d : null;
                if (notes is not null) todo.Notes = notes;
                Save();
                return todo;
            }
        }

        public bool Delete(long id)
        {
            lock (_lock)
            {
                var removed = _todos.RemoveAll(t => t.Id == id) > 0;
                if (removed) Save();
                return removed;
            }
        }

        public void ClearCompleted()
        {
            lock (_lock)
            {
                _todos.RemoveAll(t => t.Done);
                Save();
            }
        }

        private void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(_todos, Options));
        }
    }
}
