using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskTracker.Services;

public class AppSettings
{
    /// <summary>Ostatnio wczytany lub zapisany plik z zadaniami.</summary>
    [JsonPropertyName("ostatniPlik")]
    public string? LastFilePath { get; set; }
}

/// <summary>
/// Zapamiętuje ustawienia aplikacji między uruchomieniami
/// (domyślnie w %AppData%\TaskTracker\settings.json).
/// </summary>
public class AppSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string SettingsPath { get; }

    public AppSettingsService() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TaskTracker",
        "settings.json"))
    {
    }

    public AppSettingsService(string settingsPath)
    {
        SettingsPath = settingsPath;
    }

    /// <summary>Wczytuje ustawienia; przy braku lub uszkodzeniu pliku zwraca domyślne.</summary>
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception)
        {
            return new AppSettings(); // uszkodzone ustawienia nie mogą blokować startu aplikacji
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
        }
        catch (Exception)
        {
            // brak możliwości zapisu ustawień nie jest błędem krytycznym
        }
    }

    public void RememberLastFile(string? filePath)
    {
        var settings = Load();
        settings.LastFilePath = filePath;
        Save(settings);
    }
}
