using Microsoft.AspNetCore.Http.HttpResults;

namespace TodoManager;

public static class TaskEndpoints
{
    public static void MapTasks(this WebApplication app, bool loginEnabled)
    {
        var group = app.MapGroup("/api/tasks").WithTags("Tasks");
        if (loginEnabled)
            group.RequireAuthorization();
        group.MapGet("/", async (ITaskService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetAllAsync(ct)))
            .WithName("ListTasks").WithSummary("List all shared tasks.");
        group.MapGet("/{id:long}", async Task<Results<Ok<TaskResponse>, NotFound>> (
            long id, ITaskService service, CancellationToken ct) =>
            await service.GetAsync(id, ct) is { } task ? TypedResults.Ok(task) : TypedResults.NotFound())
            .WithName("GetTask").WithSummary("Get a task by ID.");
        group.MapPost("/", async (SaveTaskRequest request, ITaskService service, CancellationToken ct) =>
        {
            var task = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/tasks/{task.Id}", task);
        }).WithName("CreateTask").WithSummary("Create a task.");
        group.MapPut("/{id:long}", async Task<Results<Ok<TaskResponse>, NotFound>> (
            long id, SaveTaskRequest request, ITaskService service, CancellationToken ct) =>
            await service.UpdateAsync(id, request, ct) is { } task ? TypedResults.Ok(task) : TypedResults.NotFound())
            .WithName("UpdateTask").WithSummary("Replace a task, including completion status.");
        group.MapDelete("/{id:long}", async Task<Results<NoContent, NotFound>> (
            long id, ITaskService service, CancellationToken ct) =>
            await service.DeleteAsync(id, ct) ? TypedResults.NoContent() : TypedResults.NotFound())
            .WithName("DeleteTask").WithSummary("Permanently delete a task.");
    }
}
