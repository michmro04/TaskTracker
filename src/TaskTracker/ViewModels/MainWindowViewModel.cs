using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using TaskTracker.Models;
using TaskTracker.Services;
using System.ComponentModel;

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
    [NotifyPropertyChangedFor(nameof(CurrentFileName))]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private string? currentFilePath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private bool hasUnsavedChanges;

    public string CurrentFileName
    {
        get
        {
            if (CurrentFilePath == null)
            {
                return "Nowy plik";
            }
            return Path.GetFileName(CurrentFilePath);
        }
    }

    public string WindowTitle
    {
        get
        {
            if (HasUnsavedChanges)
            {
                return $"TaskTracker - {CurrentFileName} *";
            }
            return $"TaskTracker - {CurrentFileName}";
        }
    }

    [ObservableProperty]
    private ViewModelBase _currentPage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSmartActive))]
    [NotifyPropertyChangedFor(nameof(IsDashboardActive))]
    [NotifyPropertyChangedFor(nameof(IsAllTasksActive))]
    private int _activeTabIndex = 1; // 0 = Smart, 1 = Dashboard, 2 = AllTasks

    public IReadOnlyList<AccentOption> AccentOptions => ThemeService.AccentOptions; //themes
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThemeButtonLabel))]
    private bool _isDarkMode = true;

    [ObservableProperty]
    private AccentOption _selectedAccent = ThemeService.AccentOptions[0];

    public string ThemeButtonLabel => IsDarkMode ? "Tryb: Ciemny" : "Tryb: Jasny";

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
    }

    partial void OnIsDarkModeChanged(bool value) => ThemeService.ApplyTheme(SelectedAccent, value);

    partial void OnSelectedAccentChanged(AccentOption value) => ThemeService.ApplyTheme(value, IsDarkMode);

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
        ThemeService.ApplyTheme(_selectedAccent, _isDarkMode);
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

            ClearTasks();
            foreach (var item in document.Zadania)
            {
                Tasks.Add(item);
                StartTracking(item);
            }

            CurrentFilePath = filePath;
            HasUnsavedChanges = false;
            StatusMessage = $"Wczytano {Tasks.Count} zadań z: {CurrentFileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd wczytywania: {ex.Message}";
        }
    }

    public async Task<bool> SaveToFileAsync(string filePath)
    {
        try
        {
            var document = new TaskDocument
            {
                Wersja = _documentVersion,
                Zadania = [.. Tasks]
            };

            await _storageService.SaveAsync(filePath, document);

            CurrentFilePath = filePath;
            HasUnsavedChanges = false;
            StatusMessage = $"Zapisano {Tasks.Count} zadań do: {CurrentFileName}";
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd zapisu: {ex.Message}";
            return false;
        }
    }


    public int GetNextId()
    {
        int maxId = 0;
        foreach (var task in Tasks)
        {
            if (task.Id > maxId)
            {
                maxId = task.Id;
            }
        }
        return maxId + 1;
    }

    public void AddTask(TaskItem task)
    {
        task.Id = GetNextId();
        Tasks.Add(task);
        StartTracking(task);
        HasUnsavedChanges = true;
        StatusMessage = $"Dodano zadanie: {task.Tytul}";
    }

    public void UpdateTask(TaskItem original, TaskItem edited)
    {
        original.Tytul = edited.Tytul;
        original.Opis = edited.Opis;
        original.Termin = edited.Termin;
        original.Priorytet = edited.Priorytet;
        original.Wykonane = edited.Wykonane;
        original.Tagi = new List<string>(edited.Tagi);
        StatusMessage = $"Zapisano zmiany w zadaniu: {original.Tytul}";
    }

    public void DeleteTask(TaskItem task)
    {
        if (Tasks.Remove(task))
        {
            StopTracking(task);
            HasUnsavedChanges = true;
            StatusMessage = $"Usunięto zadanie: {task.Tytul}";
        }
    }

    public void NewDocument()
    {
        ClearTasks();
        _documentVersion = 1;
        CurrentFilePath = null;
        HasUnsavedChanges = false;
        StatusMessage = "Utworzono nowy, pusty plik zadań.";
    }


    private void StartTracking(TaskItem task)
    {
        task.PropertyChanged += OnTaskPropertyChanged;
    }

    private void StopTracking(TaskItem task)
    {
        task.PropertyChanged -= OnTaskPropertyChanged;
    }

    private void OnTaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        HasUnsavedChanges = true;
    }

    private void ClearTasks()
    {
        foreach (var task in Tasks)
        {
            StopTracking(task);
        }
        Tasks.Clear();
    }
}