using System;
using System.Linq;
using TaskTracker.Models;
using TaskTracker.Services;
using Xunit;

namespace TaskTracker.Tests;

public class SmartPlannerTests
{
    private static readonly DateOnly Today = new(2026, 11, 20);

    // domyślnie: sesje 30-60 min, praca: niski 60, średni 120, wysoki 240 min
    private static PlannerSettings Settings(int min = 30, int max = 60, params int[] daily) =>
        new(min, max, daily.Length > 0 ? daily : [120, 120, 120, 120, 120]);

    [Fact]
    public void SameTaskNeverAppearsTwiceInARow()
    {
        var tasks = new[]
        {
            new TaskItem { Id = 1, Tytul = "A", Priorytet = Priority.Wysoki, Termin = Today.AddDays(1) },
            new TaskItem { Id = 2, Tytul = "B", Priorytet = Priority.Wysoki, Termin = Today.AddDays(3) },
        };

        var plan = SmartPlanner.Build(tasks, Today, Settings(30, 60, 300, 300, 300, 300, 300));

        foreach (var day in plan.Days)
        {
            for (var i = 1; i < day.Sessions.Count; i++)
            {
                var prev = day.Sessions[i - 1].Task;
                var cur = day.Sessions[i].Task;
                Assert.False(cur is not null && ReferenceEquals(prev, cur), $"{day.Date}: powtórzenie w sesji {i + 1}");
            }
        }
    }

    [Fact]
    public void SessionLengthsStayBetweenMinAndMax()
    {
        var tasks = new[]
        {
            new TaskItem { Id = 1, Tytul = "A", Priorytet = Priority.Wysoki, Termin = Today.AddDays(2) },
            new TaskItem { Id = 2, Tytul = "B", Priorytet = Priority.Niski, Termin = Today.AddDays(1) },
            new TaskItem { Id = 3, Tytul = "C", Priorytet = Priority.Sredni },
        };

        var plan = SmartPlanner.Build(tasks, Today, Settings(25, 50, 200, 130, 95, 300, 60));

        var all = plan.Days.SelectMany(d => d.Sessions).ToList();
        Assert.NotEmpty(all);
        Assert.All(all, s => Assert.InRange(s.LengthMinutes, 25, 50));
    }

    [Fact]
    public void SessionsNeverExceedAvailableDayTime()
    {
        var tasks = new[]
        {
            new TaskItem { Id = 1, Tytul = "A", Priorytet = Priority.Wysoki, Termin = Today.AddDays(4) },
            new TaskItem { Id = 2, Tytul = "B", Priorytet = Priority.Wysoki, Termin = Today.AddDays(4) },
        };

        var plan = SmartPlanner.Build(tasks, Today, Settings(30, 60, 100, 45, 170, 29, 240));

        Assert.All(plan.Days, d =>
        {
            var end = d.Sessions.Count == 0 ? 0 : d.Sessions[^1].EndMinute;
            Assert.True(end <= d.AvailableMinutes, $"{d.Date}: {end} > {d.AvailableMinutes}");
        });
    }

    [Fact]
    public void HigherPriorityGoesFirst()
    {
        var high = new TaskItem { Id = 1, Tytul = "Wysoki", Priorytet = Priority.Wysoki, Termin = Today.AddDays(4) };
        var low = new TaskItem { Id = 2, Tytul = "Niski", Priorytet = Priority.Niski, Termin = Today };

        var plan = SmartPlanner.Build([low, high], Today, Settings());

        Assert.Same(high, plan.Days[0].Sessions[0].Task);
    }

    [Fact]
    public void SamePriority_EarlierDeadlineGoesFirst()
    {
        var later = new TaskItem { Id = 1, Tytul = "Później", Priorytet = Priority.Sredni, Termin = Today.AddDays(4) };
        var sooner = new TaskItem { Id = 2, Tytul = "Wcześniej", Priorytet = Priority.Sredni, Termin = Today.AddDays(1) };

        var plan = SmartPlanner.Build([later, sooner], Today, Settings());

        Assert.Same(sooner, plan.Days[0].Sessions[0].Task);
    }

    [Fact]
    public void TaskIsNotScheduledAfterItsDeadline()
    {
        // wysoki = 240 min pracy, a dziś są tylko 2 godziny
        var task = new TaskItem { Id = 1, Tytul = "Na dziś", Priorytet = Priority.Wysoki, Termin = Today };

        var plan = SmartPlanner.Build([task], Today, Settings(30, 60, 120, 600, 600, 600, 600));

        Assert.All(plan.Days.Skip(1), d => Assert.DoesNotContain(d.Sessions, s => s.Task == task));
        var warning = Assert.Single(plan.Warnings);
        Assert.Same(task, warning.Task);
        Assert.True(warning.MissingMinutes > 0);
    }

    [Fact]
    public void DoneTasksAreIgnored()
    {
        var done = new TaskItem { Id = 1, Tytul = "Zrobione", Wykonane = true, Termin = Today };

        var plan = SmartPlanner.Build([done], Today, Settings());

        Assert.All(plan.Days, d => Assert.Empty(d.Sessions));
        Assert.Empty(plan.Warnings);
    }

    [Fact]
    public void SingleTaskAlternatesWithBreaks()
    {
        // średni = 120 min, sesje 30-60 -> 60 pracy, 30 przerwy, 60 pracy
        var task = new TaskItem { Id = 1, Tytul = "Jedyne", Priorytet = Priority.Sredni, Termin = Today.AddDays(2) };

        var plan = SmartPlanner.Build([task], Today, Settings(30, 60, 180, 0, 0, 0, 0));

        var day = plan.Days[0];
        Assert.Equal(3, day.Sessions.Count);
        Assert.Same(task, day.Sessions[0].Task);
        Assert.Equal(60, day.Sessions[0].LengthMinutes);
        Assert.True(day.Sessions[1].IsFree);
        Assert.Equal(30, day.Sessions[1].LengthMinutes);
        Assert.Same(task, day.Sessions[2].Task);
        Assert.Equal(90, day.Sessions[2].StartMinute);
    }

    [Fact]
    public void OverdueTaskIsScheduledTodayAndFlagged()
    {
        var overdue = new TaskItem { Id = 1, Tytul = "Zaległe", Priorytet = Priority.Wysoki, Termin = Today.AddDays(-2) };

        var plan = SmartPlanner.Build([overdue], Today, Settings(30, 60, 60, 600, 600, 600, 600));

        Assert.Same(overdue, plan.Days[0].Sessions[0].Task);
        Assert.True(Assert.Single(plan.Warnings).IsOverdue);
    }
}
