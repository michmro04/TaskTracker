using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker.ViewModels;

/// <summary>
/// Archiwum: zadania oznaczone jako wykonane. Odznaczenie "wykonane" na kafelku
/// cofa zadanie z powrotem do "Do zrobienia".
/// </summary>
public partial class AllTasksViewModel : ViewModelBase
{
    private readonly MainWindowViewModel owner;

    public ObservableCollection<TaskItem> Tasks { get; }

    public ObservableCollection<TaskCardViewModel> Cards { get; } = [];

    [ObservableProperty]
    private string summaryText = string.Empty;

    [ObservableProperty]
    private bool isEmpty = true;

    public AllTasksViewModel(MainWindowViewModel owner, ObservableCollection<TaskItem> tasks)
    {
        this.owner = owner;
        Tasks = tasks;
        Refresh();
    }

    public void Refresh()
    {
        var done = new List<TaskItem>();
        foreach (var task in Tasks)
        {
            if (task.Wykonane)
            {
                done.Add(task);
            }
        }

        // najnowsze terminy na górze
        done.Sort((a, b) => TaskSorting.ByDeadline(b, a));

        Cards.Clear();
        foreach (var task in done)
        {
            Cards.Add(new TaskCardViewModel(owner, task));
        }

        IsEmpty = Cards.Count == 0;
        SummaryText = $"Wykonane: {Cards.Count} z {Tasks.Count} · odznacz zadanie, aby przywrócić je do \"Do zrobienia\"";
    }



    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTileView))]
    private bool isListView;

    public bool IsTileView => !IsListView;

    [RelayCommand]
    private void SetTileView() => IsListView = false;

    [RelayCommand]
    private void SetListView() => IsListView = true;
}
