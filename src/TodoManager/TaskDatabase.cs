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
    }
}
