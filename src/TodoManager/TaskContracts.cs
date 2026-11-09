using System.ComponentModel.DataAnnotations;

namespace TodoManager;

public enum TaskPriority { Low, Medium, High }

/// <summary>Fields used to create or replace a task. DueDate is a calendar date, not a timestamp.</summary>
public sealed record SaveTaskRequest
{
    [Required, StringLength(200)]
    public required string Title { get; init; }

    [StringLength(2000)]
    public string? Notes { get; init; }

    [EnumDataType(typeof(TaskPriority))]
    public TaskPriority Priority { get; init; } = TaskPriority.Medium;
    public DateOnly? DueDate { get; init; }
    public bool IsCompleted { get; init; }
}

/// <summary>A persisted task, including its creation timestamp and optional calendar due date.</summary>
public sealed record TaskResponse(
    long Id, string Title, string Notes, TaskPriority Priority,
    DateOnly? DueDate, bool IsCompleted, DateTimeOffset CreatedAt);
