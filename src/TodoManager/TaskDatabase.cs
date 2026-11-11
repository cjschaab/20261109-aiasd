using Microsoft.Data.Sqlite;

namespace TodoManager;

public sealed class TaskDatabase(IConfiguration configuration, IWebHostEnvironment environment)
{
    private readonly string path = Path.GetFullPath(
        configuration["TaskDatabase:Path"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "tasks.db"));

    public SqliteConnection OpenConnection() => new(new SqliteConnectionStringBuilder
    {
        DataSource = path,
        ForeignKeys = true
    }.ToString());

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS Tasks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL CHECK(length(trim(Title)) BETWEEN 1 AND 200),
                Notes TEXT NOT NULL CHECK(length(Notes) <= 2000),
                Priority TEXT NOT NULL CHECK(Priority IN ('Low', 'Medium', 'High')),
                DueDate TEXT,
                IsCompleted INTEGER NOT NULL CHECK(IsCompleted IN (0, 1)),
                CreatedAt TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync();

        using var transaction = connection.BeginTransaction();
        command.Transaction = transaction;
        command.CommandText = "PRAGMA table_info(Tasks)";
        var columns = new HashSet<string>();
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                columns.Add(reader.GetString(1));
        }
        if (!columns.Contains("Recurrence"))
        {
            command.CommandText = """
                ALTER TABLE Tasks ADD COLUMN Recurrence TEXT NOT NULL DEFAULT 'None'
                    CHECK(Recurrence IN ('None', 'Daily', 'Weekly', 'Monthly'));
                """;
            await command.ExecuteNonQueryAsync();
        }
        if (!columns.Contains("HasGeneratedNext"))
        {
            command.CommandText = """
                ALTER TABLE Tasks ADD COLUMN HasGeneratedNext INTEGER NOT NULL DEFAULT 0
                    CHECK(HasGeneratedNext IN (0, 1));
                """;
            await command.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
    }
}
