using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TodoManager;

public interface ITaskService
{
    Task<IReadOnlyList<TaskResponse>> GetAllAsync(CancellationToken ct);
    Task<TaskResponse?> GetAsync(long id, CancellationToken ct);
    Task<TaskResponse> CreateAsync(SaveTaskRequest request, CancellationToken ct);
    Task<TaskResponse?> UpdateAsync(long id, SaveTaskRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(long id, CancellationToken ct);
}

public sealed class TaskService(TaskDatabase database) : ITaskService
{
    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(CancellationToken ct)
    {
        await using var connection = database.OpenConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Tasks ORDER BY Id DESC";
        await using var reader = await command.ExecuteReaderAsync(ct);
        var tasks = new List<TaskResponse>();
        while (await reader.ReadAsync(ct))
            tasks.Add(Read(reader));
        return tasks;
    }

    public async Task<TaskResponse?> GetAsync(long id, CancellationToken ct)
    {
        await using var connection = database.OpenConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Tasks WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Read(reader) : null;
    }

    public async Task<TaskResponse> CreateAsync(SaveTaskRequest request, CancellationToken ct)
    {
        await using var connection = database.OpenConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Tasks (Title, Notes, Priority, DueDate, IsCompleted, CreatedAt)
            VALUES ($title, $notes, $priority, $due, $completed, $created)
            RETURNING *;
            """;
        AddFields(command, request);
        command.Parameters.AddWithValue("$created", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return Read(reader);
    }

    public async Task<TaskResponse?> UpdateAsync(long id, SaveTaskRequest request, CancellationToken ct)
    {
        await using var connection = database.OpenConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Tasks SET Title=$title, Notes=$notes, Priority=$priority,
                DueDate=$due, IsCompleted=$completed WHERE Id=$id RETURNING *;
            """;
        AddFields(command, request);
        command.Parameters.AddWithValue("$id", id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Read(reader) : null;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct)
    {
        await using var connection = database.OpenConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Tasks WHERE Id=$id";
        command.Parameters.AddWithValue("$id", id);
        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    private static void AddFields(SqliteCommand command, SaveTaskRequest request)
    {
        command.Parameters.AddWithValue("$title", request.Title.Trim());
        command.Parameters.AddWithValue("$notes", request.Notes?.Trim() ?? "");
        command.Parameters.AddWithValue("$priority", request.Priority.ToString());
        command.Parameters.AddWithValue("$due", (object?)request.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue("$completed", request.IsCompleted);
    }

    private static TaskResponse Read(SqliteDataReader reader) => new(
        reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
        Enum.Parse<TaskPriority>(reader.GetString(3)),
        reader.IsDBNull(4) ? null : DateOnly.ParseExact(reader.GetString(4), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        reader.GetBoolean(5),
        DateTimeOffset.Parse(reader.GetString(6), CultureInfo.InvariantCulture));
}
