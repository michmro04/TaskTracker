using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly MainWindowViewModel owner;

    public ObservableCollection<TaskItem> Tasks { get; }

    public ObservableCollection<TaskCardViewModel> Cards { get; } = [];

    public string[] SortOptions { get; } = ["Termin", "Priorytet", "Tag"];

    [ObservableProperty]
    private string selectedSort = "Termin";

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
        SummaryText = $"Do zrobienia: {Cards.Count} z {Tasks.Count}";
    }

    [RelayCommand]
    private void AddNewTask()
    {
        owner.OpenEditor(null);
    }
}
