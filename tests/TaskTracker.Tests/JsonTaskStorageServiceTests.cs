using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TaskTracker.Models;
using TaskTracker.Services;
using Xunit;

namespace TaskTracker.Tests;

public class JsonTaskStorageServiceTests
{
    private readonly JsonTaskStorageService _service = new();

    [Fact]
    public async Task SaveAndLoadAsync_PreservesJsonFormatAndEdgeCases()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"tasktracker_{Guid.NewGuid()}.json");

        try
        {
            var original = new TaskDocument
            {
                Wersja = 1,
                Zadania =
                [
                    new TaskItem
                    {
                        Id = 1,
                        Tytul = "Kolokwium z sieci komputerowych",
                        Opis = "Rozdziały 3-5",
                        Termin = new DateOnly(2026, 11, 18),
                        Priorytet = Priority.Wysoki,
                        Wykonane = false,
                        Tagi = ["studia"]
                    },
                    new TaskItem
                    {
                        Id = 2,
                        Tytul = "Zrobić zakupy",
                        Opis = "",
                        Termin = null,
                        Priorytet = Priority.Sredni,
                        Wykonane = true,
                        Tagi = []
                    }
                ]
            };

            await _service.SaveAsync(tempPath, original);
            var rawJson = await File.ReadAllTextAsync(tempPath);
            var loaded = await _service.LoadAsync(tempPath);

            Assert.Contains("\"wersja\": 1", rawJson);
            Assert.Contains("\"priorytet\": \"sredni\"", rawJson);
            Assert.Contains("\"termin\": null", rawJson);
            Assert.Contains("Rozdziały 3-5", rawJson);

            Assert.Equal(1, loaded.Wersja);
            Assert.Equal(2, loaded.Zadania.Count);
            Assert.Equal(new DateOnly(2026, 11, 18), loaded.Zadania[0].Termin);
            Assert.Null(loaded.Zadania[1].Termin);
            Assert.Empty(loaded.Zadania[1].Tagi);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_NullFieldsInJson_NormalizesToSafeDefaults()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"tasktracker_nulls_{Guid.NewGuid()}.json");

        try
        {
            const string jsonWithNulls = """
            {
              "wersja": 1,
              "zadania": [
                {
                  "id": 10,
                  "tytul": null,
                  "opis": null,
                  "termin": null,
                  "priorytet": "niski",
                  "wykonane": false,
                  "tagi": null
                }
              ]
            }
            """;

            await File.WriteAllTextAsync(tempPath, jsonWithNulls);
            var loaded = await _service.LoadAsync(tempPath);

            var task = Assert.Single(loaded.Zadania);
            Assert.Equal(string.Empty, task.Tytul);
            Assert.Equal(string.Empty, task.Opis);
            Assert.NotNull(task.Tagi);
            Assert.Empty(task.Tagi);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_MissingFile_ThrowsFileNotFoundException()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"nie_istnieje_{Guid.NewGuid()}.json");

        await Assert.ThrowsAsync<FileNotFoundException>(() => _service.LoadAsync(missingPath));
    }

    [Fact]
    public async Task LoadAsync_BrokenJson_ThrowsJsonException()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"broken_{Guid.NewGuid()}.json");

        try
        {
            await File.WriteAllTextAsync(tempPath, "{ to nie jest poprawny json");
            await Assert.ThrowsAsync<JsonException>(() => _service.LoadAsync(tempPath));
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}