using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker.ViewModels;

public partial class SmartViewModel : ViewModelBase
{
    public const int MinDays = 1;
    public const int MaxDays = 14;
    private const int DefaultDailyMinutes = 120;

    public ObservableCollection<TaskItem> Tasks { get; }

    /// <summary>Plan na kolejne dni (dziś + następne), tyle ile ustawiono w <see cref="DaysAhead"/>.</summary>
    public ObservableCollection<DayPlanViewModel> Days { get; } = [];

    /// <summary>Zadania, których nie da się skończyć przed terminem.</summary>
    public ObservableCollection<string> Warnings { get; } = [];

    /// <summary>Ile dni pokazuje plan.</summary>
    [ObservableProperty]
    private int _daysAhead = 5;

    [ObservableProperty]
    private int _minSessionMinutes = 25;

    [ObservableProperty]
    private int _maxSessionMinutes = 60;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWarnings))]
    private int _warningCount;

    [ObservableProperty]
    private string _summary = string.Empty;

    public bool HasWarnings => WarningCount > 0;

    public SmartViewModel(ObservableCollection<TaskItem> tasks)
    {
        Tasks = tasks;

        SyncDayCount();

        // każde wczytanie / dodanie / usunięcie zadania przelicza plan
        Tasks.CollectionChanged += (_, _) => Recalculate();

        Recalculate();
    }

    partial void OnDaysAheadChanged(int value)
    {
        var clamped = Math.Clamp(value, MinDays, MaxDays);
        if (clamped != value)
        {
            DaysAhead = clamped; // wywoła tę metodę ponownie z poprawną wartością
            return;
        }

        SyncDayCount();
        Recalculate();
    }

    partial void OnMinSessionMinutesChanged(int value)
    {
        // min nie może przekroczyć max - podciągamy max
        if (MaxSessionMinutes < value)
        {
            MaxSessionMinutes = value;
        }
        Recalculate();
    }

    partial void OnMaxSessionMinutesChanged(int value)
    {
        // max nie może być mniejsze od min - obniżamy min
        if (MinSessionMinutes > value)
        {
            MinSessionMinutes = value;
        }
        Recalculate();
    }

    /// <summary>Dodaje / usuwa dni tak, żeby było ich DaysAhead. Istniejące dni zachowują ustawiony czas.</summary>
    private void SyncDayCount()
    {
        while (Days.Count < DaysAhead)
        {
            Days.Add(new DayPlanViewModel(DefaultDailyMinutes, Recalculate));
        }
        while (Days.Count > DaysAhead)
        {
            Days.RemoveAt(Days.Count - 1);
        }
    }

    public void Recalculate()
    {
        // dni jeszcze nie zsynchronizowane albo ustawienia są niepoprawne
        if (Days.Count != DaysAhead || MinSessionMinutes <= 0)
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var settings = new PlannerSettings(
            MinSessionMinutes,
            MaxSessionMinutes,
            Days.Select(d => d.AvailableMinutes).ToList());

        var plan = SmartPlanner.Build(Tasks, today, settings);

        for (var i = 0; i < Days.Count; i++)
        {
            Days[i].Apply(plan.Days[i], today);
        }

        Warnings.Clear();
        foreach (var w in plan.Warnings)
        {
            var when = w.IsOverdue
                ? $"PO TERMINIE ({w.Task.Termin:yyyy-MM-dd})"
                : $"termin {w.Task.Termin:yyyy-MM-dd}";
            Warnings.Add($"⚠ {w.Task.Tytul} – brakuje {FormatMinutes(w.MissingMinutes)}, {when}");
        }
        WarningCount = Warnings.Count;

        var pending = Tasks.Count(t => !t.Wykonane);
        var plannedMinutes = plan.Days.Sum(d => d.Sessions.Where(s => !s.IsFree).Sum(s => s.LengthMinutes));
        Summary = pending == 0
            ? "Brak niewykonanych zadań – wczytaj plik JSON albo dodaj zadania."
            : $"{pending} niewykonanych zadań · zaplanowano {FormatMinutes(plannedMinutes)} pracy";
    }

    internal static string FormatMinutes(int minutes) =>
        minutes >= 60 ? $"{minutes / 60} h {minutes % 60:00} min" : $"{minutes} min";
}

/// <summary>Jeden dzień w planie (wiersz osi czasu w widoku Smart).</summary>
public partial class DayPlanViewModel : ObservableObject
{
    private static readonly CultureInfo Pl = new("pl-PL");
    private readonly Action _onChanged;

    public DayPlanViewModel(int availableMinutes, Action onChanged)
    {
        _availableMinutes = availableMinutes;
        _onChanged = onChanged;
    }

    /// <summary>Ile minut mam danego dnia – ustawiane przez użytkownika. Pełna szerokość osi czasu = ten czas.</summary>
    [ObservableProperty]
    private int _availableMinutes;

    [ObservableProperty]
    private string _header = string.Empty;

    [ObservableProperty]
    private string _capacityText = string.Empty;

    public ObservableCollection<SessionRow> Sessions { get; } = [];

    partial void OnAvailableMinutesChanged(int value) => _onChanged();

    public void Apply(PlannedDay day, DateOnly today)
    {
        var name = day.Date.ToString("ddd, dd.MM", Pl);
        name = char.ToUpper(name[0], Pl) + name[1..];
        var offset = day.Date.DayNumber - today.DayNumber;
        Header = offset switch
        {
            0 => $"Dziś · {name}",
            1 => $"Jutro · {name}",
            _ => name
        };

        var work = day.Sessions.Where(s => !s.IsFree).ToList();
        CapacityText = $"{work.Count} sesji · {SmartViewModel.FormatMinutes(work.Sum(s => s.LengthMinutes))}";

        Sessions.Clear();
        foreach (var s in day.Sessions)
        {
            Sessions.Add(SessionRow.From(s, day.Date));
        }
    }
}

/// <summary>Blok sesji na osi czasu.</summary>
public sealed record SessionRow(
    string TimeText,
    string Title,
    string Details,
    string ToolTip,
    double LengthMinutes,
    IBrush? PriorityBrush,
    bool IsFree,
    bool IsDueToday)
{
    // te same kolory co na kafelkach w "Do zrobienia"
    private static readonly IBrush High = Brush.Parse("#EF4444");
    private static readonly IBrush Medium = Brush.Parse("#F59E0B");
    private static readonly IBrush Low = Brush.Parse("#22C55E");

    public static IBrush BrushFor(Priority priority) => priority switch
    {
        Priority.Wysoki => High,
        Priority.Niski => Low,
        _ => Medium
    };

    public static SessionRow From(PlannedSession s, DateOnly date)
    {
        var time = $"{Clock(s.StartMinute)}–{Clock(s.EndMinute)}";

        if (s.Task is null)
        {
            return new SessionRow(time, "Przerwa", $"{s.LengthMinutes} min", $"Przerwa · {time}",
                s.LengthMinutes, null, true, false);
        }

        var t = s.Task;
        var deadline = t.Termin is { } d ? $"termin {d:dd.MM}" : "bez terminu";
        var dueToday = t.Termin is { } dd && dd <= date;
        var details = $"{s.LengthMinutes} min · {deadline}";
        var tip = $"{t.Tytul}\n{time} ({s.LengthMinutes} min)\n{t.Priorytet} · {deadline}";
        return new SessionRow(time, t.Tytul, details, tip, s.LengthMinutes, BrushFor(t.Priorytet), false, dueToday);
    }

    // czas od początku dnia pracy, np. 1:25
    private static string Clock(int minutes) => $"{minutes / 60}:{minutes % 60:00}";
}
