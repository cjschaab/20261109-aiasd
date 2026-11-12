using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using TodoManager;
using Xunit;

namespace TodoManager.Tests;

public sealed class IdentityCookieTests : IAsyncLifetime
{
    private const string UserId = "cookie-test-user";
    private const string CookieName = "Daybook.Identity";
    private readonly string directory = Path.Combine(Path.GetTempPath(), "IdentityCookieTests", Guid.NewGuid().ToString("N"));
    private readonly TestClock clock = new();
    private readonly List<WebApplicationFactory<Program>> apps = [];
    private string IdentityPath => Path.Combine(directory, "identity.db");
    private string TaskPath => Path.Combine(directory, "tasks.db");
    private readonly WebApplicationFactory<Program> app;

    public IdentityCookieTests()
    {
        app = CreateApp();
    }

    private WebApplicationFactory<Program> CreateApp(string environment = "Development")
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment)
                .UseSetting("FeatureFlags:Login", "false")
                .UseSetting("TaskDatabase:Path", TaskPath)
                .UseSetting("IdentityDatabase:Path", IdentityPath);
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
                services.AddSingleton<IStartupFilter, TestEndpoints>();
                services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme,
                    options => options.TimeProvider = clock);
                services.Configure<SecurityStampValidatorOptions>(options => options.TimeProvider = clock);
            });
        });
        apps.Add(factory);
        return factory;
    }

    public async Task InitializeAsync()
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var result = await manager.CreateAsync(new ApplicationUser { Id = UserId, UserName = "cookie-user" });
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Description)));
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory, string scheme = "https") =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri($"{scheme}://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = false
        });

    private static async Task<SetCookieHeaderValue> SignInAsync(HttpClient client)
    {
        using var response = await client.PostAsync("/auth-test/sign-in", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var cookie = ReadCookie(response);
        client.DefaultRequestHeaders.Remove(HeaderNames.Cookie);
        client.DefaultRequestHeaders.Add(HeaderNames.Cookie, $"{cookie.Name}={cookie.Value}");
        return cookie;
    }

    private static SetCookieHeaderValue ReadCookie(HttpResponseMessage response) =>
        Assert.Single(SetCookieHeaderValue.ParseList(response.Headers.GetValues(HeaderNames.SetCookie).ToList()),
            cookie => cookie.Name == CookieName);

    [Fact]
    public async Task AnonymousProtectedApiReturns401WithoutRedirect()
    {
        using var client = CreateClient(app);
        using var response = await client.GetAsync("/api/auth-test/user");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task AuthenticatedUserWithoutRoleGets403WithoutRedirect()
    {
        using var client = CreateClient(app);
        await SignInAsync(client);
        using var response = await client.GetAsync("/api/auth-test/admin");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task CookieRoundTripAuthenticatesUser(string environment)
    {
        var factory = environment == "Development" ? app : CreateApp(environment);
        using var client = CreateClient(factory);
        await SignInAsync(client);
        using var response = await client.GetAsync("/api/auth-test/user");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(UserId, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("Development", "http", false)]
    [InlineData("Development", "https", true)]
    [InlineData("Production", "https", true)]
    public async Task CookieHasExpectedFlagsAndIsNotPersistent(string environment, string scheme, bool secure)
    {
        var factory = environment == "Development" ? app : CreateApp(environment);
        using var client = CreateClient(factory, scheme);
        var cookie = await SignInAsync(client);

        Assert.True(cookie.HttpOnly);
        Assert.Equal(Microsoft.Net.Http.Headers.SameSiteMode.Lax, cookie.SameSite);
        Assert.Equal("/", cookie.Path.ToString());
        Assert.Equal(secure, cookie.Secure);
        Assert.Null(cookie.Expires);
        Assert.Null(cookie.MaxAge);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void NonDevelopmentEnvironmentsRequireSecureCookies(string environment)
    {
        var factory = CreateApp(environment);
        var options = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);

        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
    }

    [Fact]
    public async Task DevelopmentSupportsHttpCookieAuthentication()
    {
        using var client = CreateClient(app, "http");
        await SignInAsync(client);

        Assert.Equal(UserId, await client.GetStringAsync("/api/auth-test/user"));
    }

    [Fact]
    public async Task InvalidCookieReturns401()
    {
        using var client = CreateClient(app);
        client.DefaultRequestHeaders.Add(HeaderNames.Cookie, $"{CookieName}=invalid-ticket");
        using var response = await client.GetAsync("/api/auth-test/user");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task CookieExpiresAfterThirtyMinutesWithoutActivity()
    {
        using var client = CreateClient(app);
        await SignInAsync(client);
        clock.Advance(TimeSpan.FromMinutes(30) + TimeSpan.FromSeconds(1));
        using var response = await client.GetAsync("/api/auth-test/user");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task CookieRenewsAfterHalfItsLifetimeAndExtendsAuthentication()
    {
        using var client = CreateClient(app);
        await SignInAsync(client);
        clock.Advance(TimeSpan.FromMinutes(14));
        using var beforeHalfway = await client.GetAsync("/api/auth-test/user");
        Assert.Equal(HttpStatusCode.OK, beforeHalfway.StatusCode);
        Assert.False(beforeHalfway.Headers.Contains(HeaderNames.SetCookie));

        clock.Advance(TimeSpan.FromMinutes(2));
        using var afterHalfway = await client.GetAsync("/api/auth-test/user");
        Assert.Equal(HttpStatusCode.OK, afterHalfway.StatusCode);
        var renewed = ReadCookie(afterHalfway);
        client.DefaultRequestHeaders.Remove(HeaderNames.Cookie);
        client.DefaultRequestHeaders.Add(HeaderNames.Cookie, $"{renewed.Name}={renewed.Value}");

        clock.Advance(TimeSpan.FromMinutes(15));
        using var afterOriginalExpiry = await client.GetAsync("/api/auth-test/user");
        Assert.Equal(HttpStatusCode.OK, afterOriginalExpiry.StatusCode);
    }

    [Fact]
    public async Task SignOutClearsApplicationCookie()
    {
        using var client = CreateClient(app);
        await SignInAsync(client);
        using var response = await client.PostAsync("/auth-test/sign-out", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var cookie = ReadCookie(response);
        Assert.Equal("", cookie.Value.ToString());
        Assert.True(cookie.Expires < clock.GetUtcNow());
        client.DefaultRequestHeaders.Remove(HeaderNames.Cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth-test/user")).StatusCode);
    }

    [Fact]
    public async Task BrowserChallengesRedirectToConfiguredPages()
    {
        using var client = CreateClient(app);
        using var anonymous = await client.GetAsync("/auth-test/browser");
        Assert.Equal(HttpStatusCode.Redirect, anonymous.StatusCode);
        Assert.Equal("/login", anonymous.Headers.Location?.AbsolutePath);
        Assert.Contains("ReturnUrl=", anonymous.Headers.Location?.Query);

        await SignInAsync(client);
        using var forbidden = await client.GetAsync("/auth-test/browser-admin");
        Assert.Equal(HttpStatusCode.Redirect, forbidden.StatusCode);
        Assert.Equal("/access-denied", forbidden.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task ExistingAssetsAndTaskApiRemainAnonymous()
    {
        using var client = CreateClient(app);
        foreach (var path in new[] { "/", "/app.js", "/styles.css", "/api/tasks" })
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
    }

    public async Task DisposeAsync()
    {
        foreach (var factory in apps)
            await factory.DisposeAsync();
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

    private sealed class TestClock : TimeProvider
    {
        private DateTimeOffset now = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan duration) => now += duration;
    }

    private sealed class TestEndpoints : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => builder =>
        {
            next(builder);
            builder.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/auth-test/sign-in", async (UserManager<ApplicationUser> users,
                    SignInManager<ApplicationUser> signIn) =>
                {
                    var user = await users.FindByIdAsync(UserId)
                        ?? throw new InvalidOperationException("The test user has not been seeded.");
                    await signIn.SignInAsync(user, isPersistent: false);
                    return TypedResults.NoContent();
                });
                endpoints.MapPost("/auth-test/sign-out", async (SignInManager<ApplicationUser> signIn) =>
                {
                    await signIn.SignOutAsync();
                    return TypedResults.NoContent();
                }).RequireAuthorization();
                endpoints.MapGet("/api/auth-test/user", async (HttpContext context) =>
                    await context.Response.WriteAsync(context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? throw new InvalidOperationException("The authenticated user has no ID.")))
                    .RequireAuthorization();
                endpoints.MapGet("/api/auth-test/admin", () => TypedResults.NoContent())
                    .RequireAuthorization(policy => policy.RequireRole("Admin"));
                endpoints.MapGet("/auth-test/browser", (HttpContext context) => context.Response.WriteAsync("private"))
                    .RequireAuthorization();
                endpoints.MapGet("/auth-test/browser-admin", (HttpContext context) => context.Response.WriteAsync("private"))
                    .RequireAuthorization(policy => policy.RequireRole("Admin"));
            });
        };
    }
}
