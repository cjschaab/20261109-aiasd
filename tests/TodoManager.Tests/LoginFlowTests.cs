using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using TodoManager;

namespace TodoManager.Tests;

public sealed class LoginFlowTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "DaybookLogin", Guid.NewGuid().ToString("N"));
    private readonly string password = $"Aa1!{Guid.NewGuid():N}";
    private string IdentityPath => Path.Combine(directory, "identity.db");

    private WebApplicationFactory<Program> CreateApp(bool? enabled = null, string? bootstrapPassword = null,
        string? bootstrapUser = "first-user") =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder
            .UseEnvironment("Development")
            .UseSetting("TaskDatabase:Path", Path.Combine(directory, "tasks.db"))
            .UseSetting("IdentityDatabase:Path", IdentityPath)
            .UseSetting("IdentityBootstrap:UserName", bootstrapUser ?? "")
            .UseSetting("IdentityBootstrap:Password", bootstrapPassword ?? password);
            if (enabled is not null)
                builder.UseSetting("FeatureFlags:Login", enabled.Value.ToString());
        });

    private static HttpClient Client(WebApplicationFactory<Program> app) => app.CreateClient(
        new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    private static async Task<JsonElement> Session(HttpClient client)
    {
        var response = await client.GetAsync("/api/auth/session");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
        var session = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        if (session.TryGetProperty("csrfToken", out var token) && token.ValueKind == JsonValueKind.String)
            client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token.GetString());
        return session;
    }

    private Task<HttpResponseMessage> Login(HttpClient client, string? suppliedPassword = null) =>
        client.PostAsJsonAsync("/api/auth/login", new { userName = "first-user", password = suppliedPassword ?? password });

    [Fact]
    public async Task LoginLogoutAndSharedCrudWork()
    {
        using var app = CreateApp();
        using var client = Client(app);
        Assert.False((await Session(client)).GetProperty("isAuthenticated").GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent, (await Login(client)).StatusCode);
        var session = await Session(client);
        Assert.True(session.GetProperty("isAuthenticated").GetBoolean());
        Assert.Equal("first-user", session.GetProperty("userName").GetString());
        var created = await client.PostAsJsonAsync("/api/tasks", new { title = "Shared after login" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/tasks/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/tasks/{id}", new { title = "Updated" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/tasks/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/tasks")).StatusCode);
        Assert.False((await Session(client)).GetProperty("isAuthenticated").GetBoolean());
    }

    [Fact]
    public async Task LoginIsEnabledByDefaultAndPublicAssetsStayAvailable()
    {
        using var app = CreateApp();
        using var client = Client(app);
        Assert.True((await Session(client)).GetProperty("loginEnabled").GetBoolean());
        foreach (var path in new[] { "/", "/app.js", "/styles.css" })
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public void EmptyDatabaseRequiresExplicitBootstrapCredentials()
    {
        using var app = CreateApp(bootstrapUser: null, bootstrapPassword: "");
        var exception = Assert.Throws<InvalidOperationException>(() => app.CreateClient());
        Assert.Contains("Supply IdentityBootstrap:UserName", exception.Message);
    }

    [Fact]
    public void WeakBootstrapPasswordFailsWithoutLeakingIt()
    {
        const string weakPassword = "weak";
        using var app = CreateApp(bootstrapPassword: weakPassword);
        var exception = Assert.Throws<InvalidOperationException>(() => app.CreateClient());
        Assert.Contains("Initial account provisioning failed", exception.Message);
        Assert.DoesNotContain(weakPassword, exception.Message);
    }

    [Fact]
    public async Task LoginAttemptsAreRateLimited()
    {
        using var app = CreateApp();
        using var client = Client(app);
        await Session(client);
        for (var attempt = 0; attempt < 10; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "incorrect")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Login(client, "incorrect")).StatusCode);
    }

    [Fact]
    public async Task UnknownUserAndWrongPasswordHaveTheSameError()
    {
        using var app = CreateApp();
        using var client = Client(app);
        await Session(client);
        var unknown = await client.PostAsJsonAsync("/api/auth/login", new { userName = "missing-user", password });
        var wrong = await Login(client, "incorrect");
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(wrong.StatusCode, unknown.StatusCode);
        var unknownBody = await unknown.Content.ReadFromJsonAsync<JsonElement>();
        var wrongBody = await wrong.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(wrongBody.GetProperty("detail").GetString(), unknownBody.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("", "value")]
    [InlineData("first-user", "")]
    public async Task InvalidLoginInputReturnsValidationProblem(string userName, string suppliedPassword)
    {
        using var app = CreateApp();
        using var client = Client(app);
        await Session(client);
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password = suppliedPassword });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.False((await Session(client)).GetProperty("isAuthenticated").GetBoolean());
    }

    [Fact]
    public async Task SignedInUsersShareExistingTasks()
    {
        using var app = CreateApp();
        using var first = Client(app);
        await Session(first);
        Assert.Equal(HttpStatusCode.NoContent, (await Login(first)).StatusCode);
        await Session(first);
        var created = await first.PostAsJsonAsync("/api/tasks", new { title = "Shared list" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();
        using (var scope = app.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True((await users.CreateAsync(new ApplicationUser { UserName = "second-user" }, password)).Succeeded);
        }
        using var second = Client(app);
        await Session(second);
        Assert.Equal(HttpStatusCode.NoContent, (await second.PostAsJsonAsync("/api/auth/login",
            new { userName = "second-user", password })).StatusCode);
        await Session(second);
        Assert.Equal(HttpStatusCode.OK, (await second.GetAsync($"/api/tasks/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await second.DeleteAsync($"/api/tasks/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await first.GetAsync($"/api/tasks/{id}")).StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/tasks")]
    [InlineData("GET", "/api/tasks/1")]
    [InlineData("POST", "/api/tasks")]
    [InlineData("PUT", "/api/tasks/1")]
    [InlineData("DELETE", "/api/tasks/1")]
    public async Task EveryTaskOperationRequiresLogin(string method, string path)
    {
        using var app = CreateApp();
        using var client = Client(app);
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = JsonContent.Create(new { title = "Blocked" })
        };
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task LoginAndLogoutRequireValidAntiforgeryToken()
    {
        using var app = CreateApp();
        using var client = Client(app);
        Assert.Equal(HttpStatusCode.BadRequest, (await Login(client)).StatusCode);
        await Session(client);
        Assert.Equal(HttpStatusCode.NoContent, (await Login(client)).StatusCode);
        await Session(client);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/tasks")).StatusCode);
    }

    [Theory]
    [InlineData("/api/auth/login/")]
    [InlineData("/API/AUTH/LOGIN/")]
    public async Task LoginRouteVariantsCannotBypassCsrf(string path)
    {
        using var app = CreateApp();
        using var client = Client(app);
        var response = await client.PostAsJsonAsync(path, new { userName = "first-user", password });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False((await Session(client)).GetProperty("isAuthenticated").GetBoolean());
    }

    [Theory]
    [InlineData("POST", "/api/tasks")]
    [InlineData("PUT", "/api/tasks/1")]
    [InlineData("DELETE", "/api/tasks/1")]
    public async Task AuthenticatedTaskMutationsRejectMissingOrInvalidCsrf(string method, string path)
    {
        using var app = CreateApp();
        using var client = Client(app);
        await Session(client);
        Assert.Equal(HttpStatusCode.NoContent, (await Login(client)).StatusCode);
        foreach (var token in new[] { null, "invalid" })
        {
            client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
            if (token is not null)
                client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token);
            using var request = new HttpRequestMessage(new HttpMethod(method), path)
            {
                Content = JsonContent.Create(new { title = "Blocked" })
            };
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }
        Assert.Empty(await client.GetFromJsonAsync<JsonElement[]>("/api/tasks") ?? []);
    }

    [Fact]
    public async Task FailedLoginIsGenericAndLocksOutAfterFiveFailures()
    {
        using var app = CreateApp();
        using var client = Client(app);
        await Session(client);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var response = await Login(client, "incorrect");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("Invalid username or password.", (await response.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("detail").GetString());
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client)).StatusCode);
        Assert.False((await Session(client)).GetProperty("isAuthenticated").GetBoolean());
    }

    [Fact]
    public async Task DisabledFeaturePreservesAnonymousTasksAndDoesNotProvisionIdentity()
    {
        using var app = CreateApp(false);
        using var client = Client(app);
        Assert.False((await Session(client)).GetProperty("loginEnabled").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/tasks")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/tasks", new { title = "Anonymous" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Login(client)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.False(File.Exists(IdentityPath));
    }

    [Fact]
    public async Task EnablingLoginPreservesExistingSharedTasks()
    {
        long id;
        using (var app = CreateApp(false))
        using (var client = Client(app))
        {
            var response = await client.PostAsJsonAsync("/api/tasks", new { title = "Before login" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();
        }
        using (var app = CreateApp())
        using (var client = Client(app))
        {
            await Session(client);
            Assert.Equal(HttpStatusCode.NoContent, (await Login(client)).StatusCode);
            var task = await client.GetFromJsonAsync<JsonElement>($"/api/tasks/{id}");
            Assert.Equal("Before login", task.GetProperty("title").GetString());
            var spec = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
            var paths = spec.GetProperty("paths");
            Assert.True(paths.GetProperty("/api/auth/session").TryGetProperty("get", out _));
            Assert.True(paths.GetProperty("/api/auth/login").TryGetProperty("post", out _));
            Assert.True(paths.GetProperty("/api/auth/logout").TryGetProperty("post", out _));
        }
    }

    [Fact]
    public async Task MigrationsAndInitialAccountSurviveRestartWithoutResettingPassword()
    {
        using (var app = CreateApp())
        {
            using var client = Client(app);
            await Session(client);
            Assert.Equal(HttpStatusCode.NoContent, (await Login(client)).StatusCode);
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        }
        using (var app = CreateApp(bootstrapPassword: $"Bb2!{Guid.NewGuid():N}"))
        {
            using var client = Client(app);
            await Session(client);
            Assert.Equal(HttpStatusCode.NoContent, (await Login(client)).StatusCode);
        }
    }

    public void Dispose()
    {
        foreach (var file in Directory.Exists(directory) ? Directory.GetFiles(directory, "*.db") : [])
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = file, ForeignKeys = true }.ToString());
            SqliteConnection.ClearPool(connection);
        }
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
