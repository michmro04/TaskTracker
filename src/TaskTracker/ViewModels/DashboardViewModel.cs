using System.Collections.ObjectModel;
using TaskTracker.Models;

namespace TaskTracker.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    public ObservableCollection<TaskItem> Tasks { get; }

    public DashboardViewModel(ObservableCollection<TaskItem> tasks)
    {
        Tasks = tasks;
    }
}