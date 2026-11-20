using System;
using System.Collections.Generic;
using System.Linq;
using TaskTracker.Models;

namespace TaskTracker.Services;

/// <summary>Ustawienia planera.</summary>
/// <param name="MinSessionMinutes">Minimalna długość sesji (min).</param>
/// <param name="MaxSessionMinutes">Maksymalna długość sesji (min).</param>
/// <param name="DailyMinutes">Dostępny czas na kolejne dni (indeks 0 = dziś).</param>
/// <param name="WorkLow">Szacowany łączny czas pracy dla priorytetu niskiego (min).</param>
/// <param name="WorkMedium">Szacowany łączny czas pracy dla priorytetu średniego (min).</param>
/// <param name="WorkHigh">Szacowany łączny czas pracy dla priorytetu wysokiego (min).</param>
public sealed record PlannerSettings(
    int MinSessionMinutes,
    int MaxSessionMinutes,
    IReadOnlyList<int> DailyMinutes,
    int WorkLow = 60,
    int WorkMedium = 120,
    int WorkHigh = 240);

/// <summary>Jedna sesja w planie. Task == null oznacza przerwę.</summary>
public sealed record PlannedSession(int StartMinute, int LengthMinutes, TaskItem? Task)
{
    public bool IsFree => Task is null;
    public int EndMinute => StartMinute + LengthMinutes;
}

public sealed record PlannedDay(DateOnly Date, int AvailableMinutes, IReadOnlyList<PlannedSession> Sessions);

/// <summary>Zadanie, którego nie da się skończyć przed terminem przy obecnych ustawieniach.</summary>
public sealed record PlanWarning(TaskItem Task, int MissingMinutes, bool IsOverdue);

public sealed record SmartPlan(IReadOnlyList<PlannedDay> Days, IReadOnlyList<PlanWarning> Warnings);

/// <summary>
/// Dzieli niewykonane zadania na sesje o długości od Min do Max minut i rozkłada je na kolejne dni.
/// Reguły:
///  - najpierw zadania z najbliższym terminem (EDF), przy remisie wyższy priorytet,
///  - sesja trwa tyle, ile zostało pracy nad zadaniem, ale nie dłużej niż Max i nie krócej niż Min
///    (końcówka zadania krótsza niż Min jest zaokrąglana w górę do Min),
///  - zadanie nie jest planowane po swoim terminie (zadania po terminie traktowane są jak "na dziś"),
///  - to samo zadanie nie występuje dwa razy z rzędu w ciągu dnia; jeśli nie ma innego
///    kandydata, wstawiana jest przerwa o długości Min,
///  - zadania bez terminu wypełniają pozostały czas.
/// </summary>
public static class SmartPlanner
{
    private sealed class Work(TaskItem task, int remaining, DateOnly? deadline, bool overdue)
    {
        public TaskItem Task { get; } = task;
        public int Remaining { get; set; } = remaining;
        public DateOnly? Deadline { get; } = deadline;
        public bool Overdue { get; } = overdue;
    }

    public static SmartPlan Build(IEnumerable<TaskItem> tasks, DateOnly today, PlannerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.MinSessionMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(settings), "Minimalna długość sesji musi być większa od zera.");
        }

        var min = settings.MinSessionMinutes;
        var max = Math.Max(min, settings.MaxSessionMinutes);

        var work = tasks
            .Where(t => !t.Wykonane)
            .Select(t =>
            {
                var overdue = t.Termin is { } d && d < today;
                var deadline = overdue ? today : t.Termin;
                return new Work(t, WorkFor(t.Priorytet, settings), deadline, overdue);
            })
            .Where(w => w.Remaining > 0)
            // stała kolejność pilności: termin -> priorytet -> id
            .OrderBy(w => w.Deadline ?? DateOnly.MaxValue)
            .ThenByDescending(w => w.Task.Priorytet)
            .ThenBy(w => w.Task.Id)
            .ToList();

        var days = new List<PlannedDay>();

        for (var dayIndex = 0; dayIndex < settings.DailyMinutes.Count; dayIndex++)
        {
            var date = today.AddDays(dayIndex);
            var available = Math.Max(0, settings.DailyMinutes[dayIndex]);
            var sessions = new List<PlannedSession>();
            var time = 0;
            TaskItem? previous = null; // nowy dzień = brak ograniczenia "z rzędu"

            while (available - time >= min)
            {
                var dayLeft = available - time;
                var candidates = work
                    .Where(w => w.Remaining > 0 && (w.Deadline is null || w.Deadline >= date))
                    .ToList();

                if (candidates.Count == 0)
                {
                    break; // nie ma już nic do zrobienia tego dnia
                }

                var pick = candidates.FirstOrDefault(w => !ReferenceEquals(w.Task, previous));
                if (pick is null)
                {
                    // zostało tylko zadanie z poprzedniej sesji -> przerwa
                    sessions.Add(new PlannedSession(time, min, null));
                    time += min;
                    previous = null;
                    continue;
                }

                var length = Math.Min(max, Math.Min(pick.Remaining, dayLeft));
                length = Math.Max(length, min); // dayLeft >= min, więc się zmieści

                pick.Remaining = Math.Max(0, pick.Remaining - length);
                sessions.Add(new PlannedSession(time, length, pick.Task));
                time += length;
                previous = pick.Task;
            }

            // przerwy na końcu dnia nie mają sensu
            while (sessions.Count > 0 && sessions[^1].IsFree)
            {
                sessions.RemoveAt(sessions.Count - 1);
            }

            days.Add(new PlannedDay(date, available, sessions));
        }

        var horizonEnd = today.AddDays(Math.Max(0, settings.DailyMinutes.Count - 1));
        var warnings = work
            .Where(w => w.Remaining > 0 && w.Deadline is { } d && d <= horizonEnd)
            .Select(w => new PlanWarning(w.Task, w.Remaining, w.Overdue))
            .ToList();

        return new SmartPlan(days, warnings);
    }

    public static int WorkFor(Priority priority, PlannerSettings settings) => priority switch
    {
        Priority.Niski => settings.WorkLow,
        Priority.Wysoki => settings.WorkHigh,
        _ => settings.WorkMedium
    };
}
