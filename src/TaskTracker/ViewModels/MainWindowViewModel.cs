using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ITaskStorageService _storageService;
    private int _documentVersion = 1;

    public ObservableCollection<TaskItem> Tasks { get; } = [];

    public SmartViewModel SmartVm { get; }
    public DashboardViewModel DashboardVm { get; }
    public AllTasksViewModel AllTasksVm { get; }

    [ObservableProperty]
    private ViewModelBase _currentPage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSmartActive))]
    [NotifyPropertyChangedFor(nameof(IsDashboardActive))]
    [NotifyPropertyChangedFor(nameof(IsAllTasksActive))]
    private int _activeTabIndex = 1; // 0 = Smart, 1 = Dashboard, 2 = AllTasks

    [ObservableProperty]
    private bool _isSlideReversed;

    [ObservableProperty]
    private string _statusMessage = "Gotowy. Wczytaj plik JSON, aby rozpocząć.";

    public bool IsSmartActive => ActiveTabIndex == 0;
    public bool IsDashboardActive => ActiveTabIndex == 1;
    public bool IsAllTasksActive => ActiveTabIndex == 2;

    public MainWindowViewModel() : this(new JsonTaskStorageService())
    {
    }

    public MainWindowViewModel(ITaskStorageService storageService)
    {
        _storageService = storageService;

        SmartVm = new SmartViewModel(Tasks);
        DashboardVm = new DashboardViewModel(Tasks);
        AllTasksVm = new AllTasksViewModel(Tasks);

        _currentPage = DashboardVm;
    }

    [RelayCommand]
    private void ShowSmart() => NavigateTo(0);

    [RelayCommand]
    private void ShowDashboard() => NavigateTo(1);

    [RelayCommand]
    private void ShowAllTasks() => NavigateTo(2);

    private void NavigateTo(int targetIndex)
    {
        if (targetIndex == ActiveTabIndex)
        {
            return;
        }

        IsSlideReversed = targetIndex < ActiveTabIndex;
        ActiveTabIndex = targetIndex;

        CurrentPage = targetIndex switch
        {
            0 => SmartVm,
            1 => DashboardVm,
            2 => AllTasksVm,
            _ => DashboardVm
        };
    }

    public async Task LoadFromFileAsync(string filePath)
    {
        try
        {
            var document = await _storageService.LoadAsync(filePath);
            _documentVersion = document.Wersja > 0 ? document.Wersja : 1;

            Tasks.Clear();
            foreach (var item in document.Zadania)
            {
                Tasks.Add(item);
            }

            StatusMessage = $"Wczytano {Tasks.Count} zadań z: {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd wczytywania: {ex.Message}";
        }
    }

    public async Task SaveToFileAsync(string filePath)
    {
        try
        {
            var document = new TaskDocument
            {
                Wersja = _documentVersion,
                Zadania = [.. Tasks]
            };

            await _storageService.SaveAsync(filePath, document);
            StatusMessage = $"Zapisano {Tasks.Count} zadań do: {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd zapisu: {ex.Message}";
        }
    }
}