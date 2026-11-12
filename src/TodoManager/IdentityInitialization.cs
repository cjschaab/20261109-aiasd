using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TodoManager;

public static class IdentityInitialization
{
    public static async Task InitializeIdentityAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var path = context.Database.GetDbConnection().DataSource;
        var taskDatabasePath = Path.GetFullPath(app.Configuration["TaskDatabase:Path"]
            ?? Path.Combine(app.Environment.ContentRootPath, "App_Data", "tasks.db"));
        if (string.Equals(path, taskDatabasePath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("IdentityDatabase:Path must differ from TaskDatabase:Path.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Identity database path needs a parent directory."));
        await context.Database.MigrateAsync();
        if (await context.Users.AnyAsync())
            return;

        var userName = app.Configuration["IdentityBootstrap:UserName"];
        var password = app.Configuration["IdentityBootstrap:Password"];
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(
                "Login is enabled but no users exist. Supply IdentityBootstrap:UserName and IdentityBootstrap:Password through a secret provider or environment variables.");
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var result = await users.CreateAsync(new ApplicationUser { UserName = userName }, password);
        if (!result.Succeeded)
            throw new InvalidOperationException("Initial account provisioning failed: "
                + string.Join(", ", result.Errors.Select(error => error.Code)));
        app.Logger.LogInformation("The initial Identity account was provisioned. Remove bootstrap credentials from configuration.");
    }
}
