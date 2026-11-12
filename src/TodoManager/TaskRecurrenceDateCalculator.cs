namespace TodoManager;

internal static class TaskRecurrenceDateCalculator
{
    public static DateOnly GetNextDueDate(DateOnly completedOn, TaskRecurrence recurrence) =>
        recurrence switch
        {
            TaskRecurrence.Daily => completedOn.AddDays(1),
            TaskRecurrence.Weekly => completedOn.AddDays(7),
            TaskRecurrence.Monthly => completedOn.AddMonths(1),
            _ => throw new InvalidOperationException("Unsupported recurrence.")
        };
}
