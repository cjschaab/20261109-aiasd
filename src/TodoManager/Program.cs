using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TodoManager;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IFeatureFlags, ConfigurationFeatureFlags>();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<TaskPriority>(allowIntegerValues: false));
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<TaskRecurrence>(allowIntegerValues: false));
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TaskDatabase>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(
    new SqliteConnectionStringBuilder
    {
        DataSource = Path.GetFullPath(builder.Configuration["IdentityDatabase:Path"]
            ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "identity.db")),
        ForeignKeys = true
    }.ToString()));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Daybook.Identity";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Path = "/";
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        else
            context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        else
            context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "Daybook.Antiforgery";
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

var app = builder.Build();
var loginEnabled = app.Services.GetRequiredService<IFeatureFlags>().IsEnabled("Login");
app.UseExceptionHandler();
app.UseStatusCodePages();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.ContentSecurityPolicy =
        "default-src 'self'; style-src 'self'; script-src 'self'; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    await next(context);
});
await app.Services.GetRequiredService<TaskDatabase>().InitializeAsync();
if (loginEnabled)
    await app.InitializeIdentityAsync();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (loginEnabled && context.Request.Path.StartsWithSegments("/api"))
        context.Response.Headers.CacheControl = "no-store";
    await next(context);
});
app.UseAuthorization();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (loginEnabled && context.Request.Path.StartsWithSegments("/api"))
    {
        var unsafeMethod = !HttpMethods.IsGet(context.Request.Method)
            && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method);
        var authMutation = context.Request.Path.StartsWithSegments("/api/auth/login")
            || context.Request.Path.StartsWithSegments("/api/auth/logout");
        if (unsafeMethod && (authMutation ||
            (context.Request.Path.StartsWithSegments("/api/tasks") && context.User.Identity?.IsAuthenticated == true)))
        {
            try
            {
                await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                app.Logger.LogWarning("Request verification failed for {Path}.", context.Request.Path);
                await TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest,
                    detail: "Request verification failed. Refresh and try again.").ExecuteAsync(context);
                return;
            }
        }
    }
    await next(context);
});
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
app.MapAuth(loginEnabled);
app.MapTasks(loginEnabled);
app.Run();

public partial class Program;
