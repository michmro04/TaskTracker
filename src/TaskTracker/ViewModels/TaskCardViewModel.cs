using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskTracker.Models;

namespace TaskTracker.ViewModels;

public partial class TaskCardViewModel : ViewModelBase
{
    private readonly MainWindowViewModel owner;

    public TaskItem Item { get; }

    [ObservableProperty]
    private bool isConfirmingDelete;

    public TaskCardViewModel(MainWindowViewModel owner, TaskItem item)
    {
        this.owner = owner;
        Item = item;
    }

    public bool IsOverdue
    {
        get
        {
            if (Item.Wykonane || Item.Termin == null)
            {
                return false;
            }
            return Item.Termin.Value < DateOnly.FromDateTime(DateTime.Today);
        }
    }

    public string DeadlineText
    {
        get
        {
            if (Item.Termin == null)
            {
                return "Brak terminu";
            }

            string date = Item.Termin.Value.ToString("dd.MM.yyyy");
            if (IsOverdue)
            {
                return $"Termin: {date} - po terminie!";
            }
            return $"Termin: {date}";
        }
    }

    public string PriorityText
    {
        get
        {
            if (Item.Priorytet == Priority.Wysoki)
            {
                return "Wysoki";
            }
            if (Item.Priorytet == Priority.Niski)
            {
                return "Niski";
            }
            return "Średni";
        }
    }

    public IBrush PriorityBrush
    {
        get
        {
            if (Item.Priorytet == Priority.Wysoki)
            {
                return Brush.Parse("#EF4444");
            }
            if (Item.Priorytet == Priority.Niski)
            {
                return Brush.Parse("#22C55E");
            }
            return Brush.Parse("#F59E0B");
        }
    }

    public bool HasTags => Item.Tagi.Count > 0;

    public bool HasDescription => !string.IsNullOrWhiteSpace(Item.Opis);

    [RelayCommand]
    private void Edit()
    {
        owner.OpenEditor(Item);
    }

    [RelayCommand]
    private void AskDelete()
    {
        IsConfirmingDelete = true;
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsConfirmingDelete = false;
    }

    [RelayCommand]
    private void ConfirmDelete()
    {
        owner.DeleteTask(Item);
    }
}
