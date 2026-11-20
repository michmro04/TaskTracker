using System;
using System.Collections.Generic;
using TaskTracker.Models;

namespace TaskTracker.Services;

public static class TaskTags
{
    /// <summary>Wszystkie różne tagi z podanych zadań, posortowane alfabetycznie (bez rozróżniania wielkości liter).</summary>
    public static List<string> Collect(IEnumerable<TaskItem> tasks)
    {
        var unique = new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);
        foreach (var task in tasks)
        {
            foreach (var tag in task.Tagi)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    unique.Add(tag.Trim());
                }
            }
        }
        return [.. unique];
    }

    /// <summary>Czy zadanie ma dany tag (bez rozróżniania wielkości liter).</summary>
    public static bool HasTag(TaskItem task, string tag)
    {
        foreach (var t in task.Tagi)
        {
            if (string.Equals(t.Trim(), tag, StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
