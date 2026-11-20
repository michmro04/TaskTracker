using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    public const string AllTags = "Wszystkie tagi";

    private readonly MainWindowViewModel owner;
    private bool updatingTags;

    public ObservableCollection<TaskItem> Tasks { get; }

    public ObservableCollection<TaskCardViewModel> Cards { get; } = [];

    public string[] SortOptions { get; } = ["Termin", "Priorytet", "Tag"];

    /// <summary>Opcje filtra: "Wszystkie tagi" + tagi z niewykonanych zadań.</summary>
    public ObservableCollection<string> TagOptions { get; } = [AllTags];

    [ObservableProperty]
    private string selectedSort = "Termin";

    [ObservableProperty]
    private string? selectedTag = AllTags;

    [ObservableProperty]
    private string summaryText = string.Empty;

    [ObservableProperty]
    private bool isEmpty = true;

    public DashboardViewModel(MainWindowViewModel owner, ObservableCollection<TaskItem> tasks)
    {
        this.owner = owner;
        Tasks = tasks;
        Refresh();
    }

    partial void OnSelectedSortChanged(string value)
    {
        Refresh();
    }

    partial void OnSelectedTagChanged(string? value)
    {
        if (!updatingTags)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        var todo = new List<TaskItem>();
        foreach (var task in Tasks)
        {
            if (!task.Wykonane)
            {
                todo.Add(task);
            }
        }

        UpdateTagOptions(todo);

        var filterActive = SelectedTag != null && SelectedTag != AllTags;
        if (filterActive)
        {
            todo = todo.Where(t => TaskTags.HasTag(t, SelectedTag!)).ToList();
        }

        if (SelectedSort == "Priorytet")
        {
            todo.Sort(TaskSorting.ByPriority);
        }
        else if (SelectedSort == "Tag")
        {
            todo.Sort(TaskSorting.ByTag);
        }
        else
        {
            todo.Sort(TaskSorting.ByDeadline);
        }

        Cards.Clear();
        foreach (var task in todo)
        {
            Cards.Add(new TaskCardViewModel(owner, task));
        }

        IsEmpty = Cards.Count == 0;
        SummaryText = filterActive
            ? $"Do zrobienia z tagiem \"{SelectedTag}\": {Cards.Count}"
            : $"Do zrobienia: {Cards.Count} z {Tasks.Count}";
    }

    /// <summary>Odświeża listę tagów w filtrze, zachowując wybór (jeśli tag nadal istnieje).</summary>
    private void UpdateTagOptions(List<TaskItem> todo)
    {
        var wanted = new List<string> { AllTags };
        wanted.AddRange(TaskTags.Collect(todo));

        if (wanted.SequenceEqual(TagOptions))
        {
            return;
        }

        var previous = SelectedTag;
        updatingTags = true;
        try
        {
            TagOptions.Clear();
            foreach (var tag in wanted)
            {
                TagOptions.Add(tag);
            }
            SelectedTag = previous != null && wanted.Contains(previous) ? previous : AllTags;
        }
        finally
        {
            updatingTags = false;
        }
    }

    [RelayCommand]
    private void AddNewTask()
    {
        owner.OpenEditor(null);
    }
}
