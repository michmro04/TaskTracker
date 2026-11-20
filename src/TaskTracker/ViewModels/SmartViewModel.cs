using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker.ViewModels;

public partial class SmartViewModel : ViewModelBase
{
    private const int DaysAhead = 5;
    private const int DefaultDailyMinutes = 120;

    /// <summary>Skala osi czasu: ile pikseli przypada na minutę.</summary>
    public const double PixelsPerMinute = 2.0;

    public ObservableCollection<TaskItem> Tasks { get; }

    /// <summary>Plan na kolejne 5 dni (dziś + 4).</summary>
    public ObservableCollection<DayPlanViewModel> Days { get; } = [];

    /// <summary>Zadania, których nie da się skończyć przed terminem.</summary>
    public ObservableCollection<string> Warnings { get; } = [];

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

        for (var i = 0; i < DaysAhead; i++)
        {
            Days.Add(new DayPlanViewModel(DefaultDailyMinutes, Recalculate));
        }

        // każde wczytanie / dodanie / usunięcie zadania przelicza plan
        Tasks.CollectionChanged += (_, _) => Recalculate();

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

    public void Recalculate()
    {
        // konstruktor jeszcze nie dodał wszystkich dni albo ustawienia są niepoprawne
        if (Days.Count < DaysAhead || MinSessionMinutes <= 0)
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

    /// <summary>Ile minut mam danego dnia – ustawiane przez użytkownika.</summary>
    [ObservableProperty]
    private int _availableMinutes;

    [ObservableProperty]
    private string _header = string.Empty;

    [ObservableProperty]
    private string _capacityText = string.Empty;

    /// <summary>Szerokość tła osi czasu (cały dostępny czas dnia) w pikselach.</summary>
    [ObservableProperty]
    private double _trackWidth;

    public ObservableCollection<SessionRow> Sessions { get; } = [];

    partial void OnAvailableMinutesChanged(int value) => _onChanged();

    public void Apply(PlannedDay day, DateOnly today)
    {
        var name = day.Date.ToString("dddd, dd.MM", Pl);
        name = char.ToUpper(name[0], Pl) + name[1..];
        var offset = day.Date.DayNumber - today.DayNumber;
        Header = offset switch
        {
            0 => $"Dziś · {name}",
            1 => $"Jutro · {name}",
            _ => name
        };

        var work = day.Sessions.Where(s => !s.IsFree).ToList();
        CapacityText = $"{work.Count} sesji · {SmartViewModel.FormatMinutes(work.Sum(s => s.LengthMinutes))} pracy";

        TrackWidth = day.AvailableMinutes * SmartViewModel.PixelsPerMinute;

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
    double Width,
    bool IsFree,
    bool IsDueToday)
{
    public static SessionRow From(PlannedSession s, DateOnly date)
    {
        var time = $"{Clock(s.StartMinute)}–{Clock(s.EndMinute)}";
        var width = s.LengthMinutes * SmartViewModel.PixelsPerMinute;

        if (s.Task is null)
        {
            return new SessionRow(time, "Przerwa", $"{s.LengthMinutes} min", $"Przerwa · {time}", width, true, false);
        }

        var t = s.Task;
        var deadline = t.Termin is { } d ? $"termin {d:dd.MM}" : "bez terminu";
        var dueToday = t.Termin is { } dd && dd <= date;
        var details = $"{s.LengthMinutes} min · {t.Priorytet}";
        var tip = $"{t.Tytul}\n{time} ({s.LengthMinutes} min)\n{t.Priorytet} · {deadline}";
        return new SessionRow(time, t.Tytul, details, tip, width, false, dueToday);
    }

    // czas od początku dnia pracy, np. 1:25
    private static string Clock(int minutes) => $"{minutes / 60}:{minutes % 60:00}";
}
