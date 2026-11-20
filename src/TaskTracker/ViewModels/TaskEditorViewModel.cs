using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskTracker.Models;

namespace TaskTracker.ViewModels;

public partial class TaskEditorViewModel : ViewModelBase
{
    private readonly MainWindowViewModel owner;
    private readonly TaskItem? original; // null = dodajemy nowe zadanie

    public Priority[] Priorities { get; } = [Priority.Niski, Priority.Sredni, Priority.Wysoki];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TitleError))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string tytul = string.Empty;

    [ObservableProperty]
    private string opis = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DateError))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private DateTime? termin;

    [ObservableProperty]
    private Priority priorytet = Priority.Sredni;

    [ObservableProperty]
    private string tagiText = string.Empty;

    [ObservableProperty]
    private bool wykonane;

    public bool IsNew => original == null;

    public string HeaderText => IsNew ? "Nowe zadanie" : "Edycja zadania";

    // ===== walidacja =====

    public string? TitleError
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Tytul))
            {
                return "Tytuł nie może być pusty.";
            }
            return null;
        }
    }

    public string? DateError
    {
        get
        {
            // data z przeszłości blokuje tylko NOWE zadanie
            if (IsNew && Termin != null && Termin.Value.Date < DateTime.Today)
            {
                return "Termin nowego zadania nie może być w przeszłości.";
            }
            return null;
        }
    }

    public TaskEditorViewModel(MainWindowViewModel owner, TaskItem? original)
    {
        this.owner = owner;
        this.original = original;

        if (original != null)
        {
            Tytul = original.Tytul;
            Opis = original.Opis;
            Priorytet = original.Priorytet;
            Wykonane = original.Wykonane;
            TagiText = string.Join(", ", original.Tagi);

            if (original.Termin.HasValue)
            {
                Termin = original.Termin.Value.ToDateTime(TimeOnly.MinValue);
            }
        }
    }

    // ===== przyciski =====

    private bool CanSave()
    {
        return TitleError == null && DateError == null;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        DateOnly? deadline = null;
        if (Termin.HasValue)
        {
            deadline = DateOnly.FromDateTime(Termin.Value);
        }

        var result = new TaskItem
        {
            Tytul = Tytul.Trim(),
            Opis = (Opis ?? string.Empty).Trim(),
            Termin = deadline,
            Priorytet = Priorytet,
            Wykonane = Wykonane,
            Tagi = ParseTags()
        };

        if (original == null)
        {
            owner.AddTask(result);
        }
        else
        {
            owner.UpdateTask(original, result);
        }

        owner.CloseEditor();
    }

    [RelayCommand]
    private void Cancel()
    {
        owner.CloseEditor();
    }

    // "studia, Projekt ,, dom" -> ["studia", "projekt", "dom"]
    private List<string> ParseTags()
    {
        var result = new List<string>();

        if (string.IsNullOrWhiteSpace(TagiText))
        {
            return result;
        }

        string[] parts = TagiText.Split(',');
        foreach (string part in parts)
        {
            string tag = part.Trim().ToLower();
            if (tag.Length > 0 && !result.Contains(tag))
            {
                result.Add(tag);
            }
        }
        return result;
    }
}