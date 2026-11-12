using Xunit;

namespace TodoManager.Tests;

public sealed class TaskRecurrenceDateCalculatorTests
{
    [Theory]
    [InlineData(2028, 1, 31, TaskRecurrence.Daily, 2028, 2, 1)]
    [InlineData(2027, 12, 28, TaskRecurrence.Weekly, 2028, 1, 4)]
    [InlineData(2028, 1, 31, TaskRecurrence.Monthly, 2028, 2, 29)]
    [InlineData(2027, 1, 31, TaskRecurrence.Monthly, 2027, 2, 28)]
    [InlineData(2028, 2, 29, TaskRecurrence.Monthly, 2028, 3, 29)]
    public void CalculatesNextDate(
        int year, int month, int day, TaskRecurrence recurrence,
        int expectedYear, int expectedMonth, int expectedDay)
    {
        var actual = TaskRecurrenceDateCalculator.GetNextDueDate(
            new DateOnly(year, month, day), recurrence);

        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), actual);
    }

    [Fact]
    public void RejectsNonRecurringValue()
    {
        Assert.Throws<InvalidOperationException>(() =>
            TaskRecurrenceDateCalculator.GetNextDueDate(
                new DateOnly(2028, 1, 31), TaskRecurrence.None));
    }
}
