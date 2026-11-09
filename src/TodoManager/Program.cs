using System.Text.Json.Serialization;
using TodoManager;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddOpenApi();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<TaskPriority>(allowIntegerValues: false));
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<TaskRecurrence>(allowIntegerValues: false));
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TaskDatabase>();
builder.Services.AddScoped<ITaskService, TaskService>();

var app = builder.Build();
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
app.UseDefaultFiles();
app.UseStaticFiles();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
app.MapTasks();
app.Run();

public partial class Program;
