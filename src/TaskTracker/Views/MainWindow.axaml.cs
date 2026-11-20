using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TaskTracker.ViewModels;

namespace TaskTracker.Views;

public partial class MainWindow : Window
{
    private static readonly FilePickerFileType JsonFileType = new("Pliki JSON")
    {
        Patterns = ["*.json"]
    };

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnLoadJsonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
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

    private async void OnSaveJsonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Zapisz zadania do pliku JSON",
            DefaultExtension = "json",
            SuggestedFileName = "zadania.json",
            FileTypeChoices = [JsonFileType]
        });

        var path = file?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(path))
        {
            await vm.SaveToFileAsync(path);
        }
    }
}