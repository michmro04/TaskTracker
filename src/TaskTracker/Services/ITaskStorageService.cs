using System.Threading.Tasks;
using TaskTracker.Models;

namespace TaskTracker.Services;

public interface ITaskStorageService
{
    Task<TaskDocument> LoadAsync(string filePath);
    Task SaveAsync(string filePath, TaskDocument document);
}

