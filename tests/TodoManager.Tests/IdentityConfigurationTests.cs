using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TodoManager;
using Xunit;

namespace TodoManager.Tests;

public sealed class IdentityConfigurationTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "IdentityConfigurationTests", Guid.NewGuid().ToString("N"));
    private string IdentityPath => Path.Combine(directory, "identity.db");
    private string TaskPath => Path.Combine(directory, "tasks.db");

    private WebApplicationFactory<Program> CreateApp() => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder
            .UseEnvironment("Development")
            .UseSetting("FeatureFlags:Login", "false")
            .UseSetting("TaskDatabase:Path", TaskPath)
            .UseSetting("IdentityDatabase:Path", IdentityPath));

    [Fact]
    public void IdentityServicesResolveFromScope()
    {
        using var app = CreateApp();
        using var scope = app.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>());
    }

    [Fact]
    public void ContextUsesConfiguredSeparateSqliteDatabase()
    {
        using var app = CreateApp();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.True(context.Database.IsSqlite());
        Assert.Equal(IdentityPath, context.Database.GetDbConnection().DataSource);
        Assert.NotEqual(TaskPath, context.Database.GetDbConnection().DataSource);
    }

    [Fact]
    public void DefaultIdentityPathIsUnderContentRoot()
    {
        using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseEnvironment("Development")
            .UseSetting("FeatureFlags:Login", "false")
            .UseSetting("TaskDatabase:Path", TaskPath));
        using var scope = app.Services.CreateScope();
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(Path.Combine(environment.ContentRootPath, "App_Data", "identity.db"),
            context.Database.GetDbConnection().DataSource);
    }

    [Fact]
    public async Task StartingDisabledAppDoesNotCreateIdentityDatabase()
    {
        using var app = CreateApp();
        using var client = app.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/tasks")).StatusCode);
        Assert.False(File.Exists(IdentityPath));
    }

    [Fact]
    public async Task ProductionAppDoesNotExposeTestAuthenticationEndpoints()
    {
        using var app = CreateApp().WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = app.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/auth-test/sign-in", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/auth-test/user")).StatusCode);
        Assert.False(File.Exists(IdentityPath));
    }

    [Fact]
    public async Task DefaultSchemesUseIdentityApplicationCookie()
    {
        using var app = CreateApp();
        var schemes = app.Services.GetRequiredService<IAuthenticationSchemeProvider>();

        Assert.Equal(IdentityConstants.ApplicationScheme, (await schemes.GetDefaultAuthenticateSchemeAsync())?.Name);
        Assert.Equal(IdentityConstants.ApplicationScheme, (await schemes.GetDefaultChallengeSchemeAsync())?.Name);
        Assert.Equal(IdentityConstants.ApplicationScheme, (await schemes.GetDefaultForbidSchemeAsync())?.Name);
    }

    [Fact]
    public void NewUsersHaveExplicitLockoutPolicy()
    {
        using var app = CreateApp();
        var options = app.Services.GetRequiredService<IOptions<IdentityOptions>>().Value;

        Assert.True(options.Lockout.AllowedForNewUsers);
        Assert.Equal(5, options.Lockout.MaxFailedAccessAttempts);
        Assert.Equal(TimeSpan.FromMinutes(15), options.Lockout.DefaultLockoutTimeSpan);
    }

    public void Dispose()
    {
        foreach (var path in new[] { IdentityPath, TaskPath })
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = path,
                ForeignKeys = true
            }.ToString());
            SqliteConnection.ClearPool(connection);
        }
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
