using TodoApp;
using Xunit;

namespace TodoApp.Tests;

public sealed class TodoStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "TodoApp.Tests", Guid.NewGuid().ToString("N"));
    private string DataFile => Path.Combine(_directory, "todos.json");

    [Fact]
    public void Add_AssignsIncreasingIds_AndPersistsTodos()
    {
        var store = new TodoStore(DataFile);

        var first = store.Add("First");
        var second = store.Add("Second");

        Assert.Equal(1, first.Id);
        Assert.Equal(2, second.Id);
        Assert.Equal(new[] { "First", "Second" }, store.GetAll().Select(todo => todo.Text));

        var reloaded = new TodoStore(DataFile);
        Assert.Equal(new[] { "First", "Second" }, reloaded.GetAll().Select(todo => todo.Text));
        Assert.Equal(3, reloaded.Add("Third").Id);
    }

    [Fact]
    public void Update_ChangesOnlyProvidedFields_AndReturnsNullForMissingTodo()
    {
        var store = new TodoStore(DataFile);
        var todo = store.Add("Original");

        var completed = store.Update(todo.Id, null, true);
        Assert.NotNull(completed);
        Assert.Equal("Original", completed.Text);
        Assert.True(completed.Done);

        var renamed = store.Update(todo.Id, "Renamed", null);
        Assert.NotNull(renamed);
        Assert.Equal("Renamed", renamed.Text);
        Assert.True(renamed.Done);
        Assert.Null(store.Update(999, "Missing", true));
    }

    [Fact]
    public void AddAndUpdate_HandlePriorityDueDateAndNotes_AndPersistThem()
    {
        var store = new TodoStore(DataFile);
        var todo = store.Add("Task");
        Assert.Equal(Priorities.Medium, todo.Priority);
        Assert.Null(todo.DueDate);
        Assert.Equal("", todo.Notes);

        var due = new DateOnly(2026, 12, 1);
        var created = store.Add("Detailed", Priorities.High, due, "Some notes");
        Assert.Equal(Priorities.High, created.Priority);
        Assert.Equal(due, created.DueDate);

        var updated = store.Update(created.Id, null, null, Priorities.Low, "", "Changed");
        Assert.NotNull(updated);
        Assert.Equal(Priorities.Low, updated.Priority);
        Assert.Null(updated.DueDate);
        Assert.Equal("Changed", updated.Notes);

        var reloaded = new TodoStore(DataFile).GetAll().Single(t => t.Id == created.Id);
        Assert.Equal(Priorities.Low, reloaded.Priority);
        Assert.Null(reloaded.DueDate);
        Assert.Equal("Changed", reloaded.Notes);
    }

    [Fact]
    public void Delete_ReturnsWhetherTodoExisted_AndPersistsRemoval()
    {
        var store = new TodoStore(DataFile);
        var todo = store.Add("Remove me");

        Assert.False(store.Delete(999));
        Assert.True(store.Delete(todo.Id));
        Assert.Empty(new TodoStore(DataFile).GetAll());
    }

    [Fact]
    public void ClearCompleted_RemovesOnlyCompletedTodos_AndPersistsChanges()
    {
        var store = new TodoStore(DataFile);
        var completed = store.Add("Done");
        store.Add("Still active");
        store.Update(completed.Id, null, true);

        store.ClearCompleted();

        var remaining = new TodoStore(DataFile).GetAll();
        var onlyRemaining = Assert.Single(remaining);
        Assert.Equal("Still active", onlyRemaining.Text);
        Assert.False(onlyRemaining.Done);
    }

    [Fact]
    public void Constructor_TreatsMalformedDataAsEmpty()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(DataFile, "not valid JSON");

        var store = new TodoStore(DataFile);

        Assert.Empty(store.GetAll());
        Assert.Equal(1, store.Add("First").Id);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
