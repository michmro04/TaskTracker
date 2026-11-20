using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskTracker.Models;
using System.Threading.Tasks;

namespace TaskTracker.Services;

public class JsonTaskStorageService : ITaskStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<TaskDocument> LoadAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Ścieżka do pliku nie może być pusta.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Nie znaleziono pliku: {filePath}", filePath);
        }

        await using var stream = File.OpenRead(filePath);
        var document = await JsonSerializer.DeserializeAsync<TaskDocument>(stream, JsonOptions);

        if (document is null)
        {
            throw new JsonException("Plik JSON jest pusty lub ma nieprawidłowy format.");
        }

        document.Zadania ??= [];

        foreach (var item in document.Zadania)
        {
            item.Tytul ??= string.Empty;
            item.Opis ??= string.Empty;
            item.Tagi ??= [];
        }

        return document;
    }

    public async Task SaveAsync(string filePath, TaskDocument document)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Ścieżka do pliku nie może być pusta.", nameof(filePath));
        }

        ArgumentNullException.ThrowIfNull(document);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (document.Wersja <= 0)
        {
            document.Wersja = 1;
        }

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions);
    }








}
