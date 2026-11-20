using System;
using System.IO;
using TaskTracker.Models;
using TaskTracker.Services;
using Xunit;

namespace TaskTracker.Tests;

public class TaskTagsAndSettingsTests
{
    [Fact]
    public void Collect_ReturnsDistinctSortedTags_IgnoringCaseAndBlanks()
    {
        var tasks = new[]
        {
            new TaskItem { Tagi = ["studia", "dom"] },
            new TaskItem { Tagi = ["Studia", " ", "zdrowie"] },
            new TaskItem { Tagi = [] },
        };

        var tags = TaskTags.Collect(tasks);

        Assert.Equal(["dom", "studia", "zdrowie"], tags);
    }

    [Fact]
    public void HasTag_IsCaseInsensitive()
    {
        var task = new TaskItem { Tagi = ["Projekt"] };

        Assert.True(TaskTags.HasTag(task, "projekt"));
        Assert.False(TaskTags.HasTag(task, "dom"));
    }

    [Fact]
    public void Settings_RememberLastFile_RoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tasktracker_settings_{Guid.NewGuid()}", "settings.json");
        try
        {
            var service = new AppSettingsService(path);
            Assert.Null(service.Load().LastFilePath);

            service.RememberLastFile(@"C:\dane\zadania.json");

            Assert.Equal(@"C:\dane\zadania.json", new AppSettingsService(path).Load().LastFilePath);
        }
        finally
        {
            var dir = Path.GetDirectoryName(path)!;
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public void Settings_CorruptedFile_ReturnsDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tasktracker_settings_bad_{Guid.NewGuid()}.json");
        try
        {
            File.WriteAllText(path, "{ to nie jest json");

            Assert.Null(new AppSettingsService(path).Load().LastFilePath);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
