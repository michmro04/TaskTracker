using System.Collections.ObjectModel;
using TaskTracker.Models;

namespace TaskTracker.ViewModels
{
    public partial class SmartViewModel : ViewModelBase
    {
        public ObservableCollection<TaskItem> Tasks { get; }

        public SmartViewModel(ObservableCollection<TaskItem> tasks)
        {
            Tasks = tasks;
        }

    }
}
