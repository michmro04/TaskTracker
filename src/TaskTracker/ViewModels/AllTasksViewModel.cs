using System.Collections.ObjectModel;
using TaskTracker.Models;

namespace TaskTracker.ViewModels;

public partial class AllTasksViewModel : ViewModelBase
{
    public ObservableCollection<TaskItem> Tasks { get; }

    public AllTasksViewModel(ObservableCollection<TaskItem> tasks)
    {
        Tasks = tasks;
    }
}