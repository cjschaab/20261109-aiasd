using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TodoManager;
using Xunit;

namespace TodoManager.Tests;

public sealed class ApplicationDbContextTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly ApplicationDbContext context;

    public ApplicationDbContextTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        context = new ApplicationDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        await context.Database.EnsureCreatedAsync();
    }

    [Fact]
    public void ContextUsesSqlite()
    {
        Assert.True(context.Database.IsSqlite());
    }

    [Fact]
    public async Task IdentitySchemaCanBeCreated()
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
        await using var reader = await command.ExecuteReaderAsync();
        var tables = new HashSet<string>();
        while (await reader.ReadAsync())
            tables.Add(reader.GetString(0));

        foreach (var table in new[]
        {
            "AspNetUsers", "AspNetRoles", "AspNetUserRoles", "AspNetUserClaims",
            "AspNetUserLogins", "AspNetUserTokens", "AspNetRoleClaims"
        })
            Assert.Contains(table, tables);
    }

    [Fact]
    public async Task UserRoundTripsThroughStorage()
    {
        var user = new ApplicationUser
        {
            UserName = "reader",
            NormalizedUserName = "READER",
            Email = "reader@example.test",
            NormalizedEmail = "READER@EXAMPLE.TEST"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var stored = await context.Users.SingleAsync(u => u.Id == user.Id);

        Assert.False(string.IsNullOrWhiteSpace(stored.Id));
        Assert.Equal(user.Id, stored.Id);
        Assert.Equal(user.UserName, stored.UserName);
        Assert.Equal(user.NormalizedUserName, stored.NormalizedUserName);
        Assert.Equal(user.Email, stored.Email);
        Assert.Equal(user.NormalizedEmail, stored.NormalizedEmail);
    }

    [Fact]
    public async Task DuplicateNormalizedUsernamesAreRejected()
    {
        context.Users.Add(new ApplicationUser { UserName = "reader", NormalizedUserName = "READER" });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        context.Users.Add(new ApplicationUser { UserName = "Reader", NormalizedUserName = "READER" });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        var sqliteException = Assert.IsType<SqliteException>(exception.InnerException);
        Assert.Equal(19, sqliteException.SqliteErrorCode);
        Assert.Equal(2067, sqliteException.SqliteExtendedErrorCode);
    }

    [Fact]
    public async Task UserRoleRelationshipPersists()
    {
        var user = new ApplicationUser { UserName = "reader", NormalizedUserName = "READER" };
        var role = new IdentityRole { Name = "Reader", NormalizedName = "READER" };
        context.Users.Add(user);
        context.Roles.Add(role);
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = role.Id });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var roleNames = await (
            from membership in context.UserRoles
            join storedRole in context.Roles on membership.RoleId equals storedRole.Id
            join storedUser in context.Users on membership.UserId equals storedUser.Id
            where storedUser.Id == user.Id
            select storedRole.Name).ToListAsync();

        Assert.Equal("Reader", Assert.Single(roleNames));
    }

    [Fact]
    public async Task MissingUserReturnsNoResult()
    {
        Assert.Null(await context.Users.FindAsync("missing-user"));
    }

    public async Task DisposeAsync()
    {
        await context.DisposeAsync();
        await connection.DisposeAsync();
    }
}
