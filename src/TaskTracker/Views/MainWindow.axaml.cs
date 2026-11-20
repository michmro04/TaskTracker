using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TaskTracker.Models;
using TaskTracker.ViewModels;

namespace TaskTracker.Views;

public partial class MainWindow : Window
{
    private static readonly FilePickerFileType JsonFileType = new("Pliki JSON")
    {
        Patterns = ["*.json"]
    };

    private bool closingConfirmed;

    public MainWindow()
    {
        InitializeComponent();
    }

    // po otwarciu okna wczytujemy ostatnio używany plik JSON
    protected override async void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is MainWindowViewModel vm)
        {
            await vm.LoadLastFileAsync();
        }
    }

    private async void OnNewClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (await ConfirmUnsavedChangesAsync(vm))
        {
            vm.NewDocument();
        }
    }

    private async void OnLoadClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (!await ConfirmUnsavedChangesAsync(vm))
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Wybierz plik z zadaniami (JSON)",
            AllowMultiple = false,
            FileTypeFilter = [JsonFileType]
        });

        if (files.Count == 0)
        {
            return;
        }

        var path = files[0].TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(path))
        {
            await vm.LoadFromFileAsync(path);
        }
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await SaveAsync(vm);
        }
    }

    private async void OnSaveAsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await SaveAsAsync(vm);
        }
    }

    private void OnAddTestTaskClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.AddTask(new TaskItem { Tytul = "Zadanie testowe" });
        }
    }


    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (closingConfirmed)
        {
            return;
        }

        if (DataContext is not MainWindowViewModel vm || !vm.HasUnsavedChanges)
        {
            return;
        }

        e.Cancel = true;

        if (await ConfirmUnsavedChangesAsync(vm))
        {
            closingConfirmed = true;
            Close();
        }
    }

    private async Task<bool> ConfirmUnsavedChangesAsync(MainWindowViewModel vm)
    {
        if (!vm.HasUnsavedChanges)
        {
            return true;
        }

        var dialog = new SaveChangesDialog(vm.CurrentFileName);
        SaveChoice choice = await dialog.ShowDialog<SaveChoice>(this);

        if (choice == SaveChoice.Save)
        {
            return await SaveAsync(vm);
        }
        if (choice == SaveChoice.SaveAs)
        {
            return await SaveAsAsync(vm);
        }
        if (choice == SaveChoice.DontSave)
        {
            return true;
        }
        return false;
    }

    private async Task<bool> SaveAsync(MainWindowViewModel vm)
    {
        if (vm.CurrentFilePath == null)
        {
            return await SaveAsAsync(vm);
        }
        return await vm.SaveToFileAsync(vm.CurrentFilePath);
    }

    private async Task<bool> SaveAsAsync(MainWindowViewModel vm)
    {
        string suggestedName = "zadania.json";
        if (vm.CurrentFilePath != null)
        {
            suggestedName = vm.CurrentFileName;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Zapisz zadania do pliku JSON",
            DefaultExtension = "json",
            SuggestedFileName = suggestedName,
            FileTypeChoices = [JsonFileType]
        });

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }
        return await vm.SaveToFileAsync(path);
    }
}