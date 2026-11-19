using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskTracker.Models;
using Xunit;

namespace TaskTracker.Tests;

public class TaskModelTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Fact]
    public void NewTaskItem_HasSafeDefaultValues()
    {
        var task = new TaskItem();

        Assert.Equal(string.Empty, task.Tytul);
        Assert.Equal(string.Empty, task.Opis);
        Assert.Null(task.Termin);
        Assert.Equal(Priority.Sredni, task.Priorytet);
        Assert.False(task.Wykonane);
        Assert.NotNull(task.Tagi);
        Assert.Empty(task.Tagi);
    }

    [Fact]
    public void Deserialize_ValidJsonFromSpec_MapsAllFieldsCorrectly()
    {
        const string json = """
        {
          "wersja": 1,
          "zadania": [
            {
              "id": 1,
              "tytul": "Kolokwium z sieci komputerowych",
              "opis": "Rozdziały 3-5",
              "termin": "2026-11-18",
              "priorytet": "wysoki",
              "wykonane": false,
              "tagi": ["studia"]
            }
          ]
        }
        """;

        var doc = JsonSerializer.Deserialize<TaskDocument>(json, JsonOptions);

        Assert.NotNull(doc);
        Assert.Equal(1, doc.Wersja);

        var task = Assert.Single(doc.Zadania);
        Assert.Equal(1, task.Id);
        Assert.Equal("Kolokwium z sieci komputerowych", task.Tytul);
        Assert.Equal("Rozdziały 3-5", task.Opis);
        Assert.Equal(new DateOnly(2026, 11, 18), task.Termin);
        Assert.Equal(Priority.Wysoki, task.Priorytet);
        Assert.False(task.Wykonane);
        Assert.Equal(["studia"], task.Tagi);
    }

    [Fact]
    public void Deserialize_EdgeCaseFields_HandlesNullTerminAndEmptyFields()
    {
        const string json = """
        {
          "wersja": 1,
          "zadania": [
            {
              "id": 2,
              "tytul": "Kupić zeszyt",
              "opis": "",
              "termin": null,
              "priorytet": "sredni",
              "wykonane": true,
              "tagi": []
            }
          ]
        }
        """;

        var doc = JsonSerializer.Deserialize<TaskDocument>(json, JsonOptions);

        Assert.NotNull(doc);
        var task = Assert.Single(doc.Zadania);
        Assert.Equal(string.Empty, task.Opis);
        Assert.Null(task.Termin);
        Assert.Equal(Priority.Sredni, task.Priorytet);
        Assert.True(task.Wykonane);
        Assert.Empty(task.Tagi);
    }

    [Fact]
    public void Serialize_TaskDocument_WritesLowercasePriorityName()
    {
        var doc = new TaskDocument
        {
            Wersja = 1,
            Zadania =
            [
                new TaskItem
                {
                    Id = 3,
                    Tytul = "Przeczytać artykuł",
                    Termin = new DateOnly(2026, 11, 22),
                    Priorytet = Priority.Niski
                }
            ]
        };

        var json = JsonSerializer.Serialize(doc, JsonOptions);

        Assert.Contains("\"priorytet\":\"niski\"", json);
        Assert.Contains("\"termin\":\"2026-11-22\"", json);
    }

    [Fact]
    public void Clone_CreatesIndependentDeepCopy()
    {
        var original = new TaskItem
        {
            Id = 5,
            Tytul = "Projekt z baz danych",
            Opis = "Diagram ERD",
            Termin = new DateOnly(2026, 11, 25),
            Priorytet = Priority.Wysoki,
            Wykonane = false,
            Tagi = ["studia", "bazy"]
        };

        var copy = original.Clone();
        copy.Tytul = "Zmieniony tytuł";
        copy.Wykonane = true;
        copy.Tagi.Add("nowy-tag");

        Assert.Equal("Projekt z baz danych", original.Tytul);
        Assert.False(original.Wykonane);
        Assert.Equal(["studia", "bazy"], original.Tagi);
    }
}