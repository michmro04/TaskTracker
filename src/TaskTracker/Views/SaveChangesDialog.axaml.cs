using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TaskTracker.Views;

public enum SaveChoice
{
    Cancel,
    Save,
    SaveAs,
    DontSave
}

public partial class SaveChangesDialog : Window
{
    public SaveChangesDialog()
    {
        InitializeComponent();
    }

    public SaveChangesDialog(string fileName) : this()
    {
        MessageText.Text = $"Masz niezapisane zmiany w pliku „{fileName}”. Czy chcesz je zapisać?";
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        Close(SaveChoice.Save);
    }

    private void OnSaveAsClick(object? sender, RoutedEventArgs e)
    {
        Close(SaveChoice.SaveAs);
    }

    private void OnDontSaveClick(object? sender, RoutedEventArgs e)
    {
        Close(SaveChoice.DontSave);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(SaveChoice.Cancel);
    }
}