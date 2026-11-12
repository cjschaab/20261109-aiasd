using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TodoManager;

/// <summary>Provides asynchronous access to persisted tasks.</summary>
public interface ITaskService
{
    /// <summary>Gets all tasks, ordered from newest ID to oldest.</summary>
    /// <param name="ct">Token used to cancel the database operation.</param>
    /// <returns>The tasks currently stored in the database.</returns>
    Task<IReadOnlyList<TaskResponse>> GetAllAsync(CancellationToken ct);

    /// <summary>Gets a task by its ID.</summary>
    /// <param name="id">The task ID to look up.</param>
    /// <param name="ct">Token used to cancel the database operation.</param>
    /// <returns>The task, or <see langword="null"/> if no task has that ID.</returns>
    Task<TaskResponse?> GetAsync(long id, CancellationToken ct);

    /// <summary>Creates a task using the supplied fields and the current UTC time as its creation time.</summary>
    /// <param name="request">The fields for the new task.</param>
    /// <param name="ct">Token used to cancel the database operation.</param>
    /// <returns>The newly created task, including its assigned ID and creation time.</returns>
    Task<TaskResponse> CreateAsync(SaveTaskRequest request, CancellationToken ct);

    /// <summary>
    /// Replaces a task's editable fields. The first transition from incomplete to complete on a
    /// recurring task creates one next occurrence in the same transaction, dated from the current
    /// local calendar date.
    /// </summary>
    /// <param name="id">The ID of the task to replace.</param>
    /// <param name="request">The replacement task fields.</param>
    /// <param name="ct">Token used to cancel the database operation.</param>
    /// <returns>The updated task, or <see langword="null"/> if no task has that ID.</returns>
    Task<TaskResponse?> UpdateAsync(long id, SaveTaskRequest request, CancellationToken ct);

    /// <summary>Deletes a task by its ID.</summary>
    /// <param name="id">The ID of the task to delete.</param>
    /// <param name="ct">Token used to cancel the database operation.</param>
    /// <returns><see langword="true"/> if a task was deleted; otherwise, <see langword="false"/>.</returns>
    Task<bool> DeleteAsync(long id, CancellationToken ct);
}

/// <summary>Implements task persistence and recurring-task generation using SQLite.</summary>
public sealed class TaskService(TaskDatabase database, TimeProvider clock) : ITaskService
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
        return await InsertAsync(connection, null, request, ct);
    }

    /// <summary>Inserts a task and returns the persisted task, including its generated ID.</summary>
    /// <param name="connection">The open SQLite connection to use.</param>
    /// <param name="transaction">The transaction to include the insert in, or <see langword="null"/>.</param>
    /// <param name="request">The fields to insert.</param>
    /// <param name="ct">Token used to cancel the database operation.</param>
    /// <returns>The inserted task.</returns>
    private async Task<TaskResponse> InsertAsync(
        SqliteConnection connection, SqliteTransaction? transaction, SaveTaskRequest request, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO Tasks (Title, Notes, Priority, DueDate, IsCompleted, CreatedAt, Recurrence)
            VALUES ($title, $notes, $priority, $due, $completed, $created, $recurrence)
            RETURNING *;
            """;
        AddFields(command, request);
        // Store creation time as an invariant, round-trip UTC timestamp.
        command.Parameters.AddWithValue("$created", clock.GetUtcNow().ToString("O", CultureInfo.InvariantCulture));
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        // RETURNING includes the database-assigned ID and persisted field values.
        return Read(reader);
    }

    public async Task<TaskResponse?> UpdateAsync(long id, SaveTaskRequest request, CancellationToken ct)
    {
        await using var connection = database.OpenConnection();
        await connection.OpenAsync(ct);
        using var transaction = connection.BeginTransaction();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT IsCompleted, HasGeneratedNext FROM Tasks WHERE Id=$id";
        command.Parameters.AddWithValue("$id", id);
        bool generateNext;
        await using (var previous = await command.ExecuteReaderAsync(ct))
        {
            if (!await previous.ReadAsync(ct))
                return null;
            generateNext = !previous.GetBoolean(0) && !previous.GetBoolean(1)
                && request.IsCompleted && request.Recurrence != TaskRecurrence.None;
        }
        command.CommandText = """
            UPDATE Tasks SET Title=$title, Notes=$notes, Priority=$priority,
                DueDate=$due, IsCompleted=$completed, Recurrence=$recurrence,
                HasGeneratedNext=CASE WHEN $generate THEN 1 ELSE HasGeneratedNext END
                WHERE Id=$id RETURNING *;
            """;
        AddFields(command, request);
        command.Parameters.AddWithValue("$generate", generateNext);
        TaskResponse updated;
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            await reader.ReadAsync(ct);
            updated = Read(reader);
        }
        if (generateNext)
        {
            var completedOn = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
            var nextDueDate = TaskRecurrenceDateCalculator.GetNextDueDate(completedOn, request.Recurrence);
            await InsertAsync(connection, transaction, request with
            {
                DueDate = nextDueDate,
                IsCompleted = false
            }, ct);
        }
        await transaction.CommitAsync(ct);
        return updated;
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
        command.Parameters.AddWithValue("$recurrence", request.Recurrence.ToString());
    }

    private static TaskResponse Read(SqliteDataReader reader) => new(
        reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
        Enum.Parse<TaskPriority>(reader.GetString(3)),
        reader.IsDBNull(4) ? null : DateOnly.ParseExact(reader.GetString(4), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        reader.GetBoolean(5),
        DateTimeOffset.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
        Enum.Parse<TaskRecurrence>(reader.GetString(reader.GetOrdinal("Recurrence"))));
}
