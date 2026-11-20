using System;
using TaskTracker.Models;

namespace TaskTracker.Services;

public static class TaskSorting
{
    public static int ByDeadline(TaskItem a, TaskItem b)
    {
        if (a.Termin == null && b.Termin == null)
        {
            return b.Priorytet.CompareTo(a.Priorytet);
        }
        if (a.Termin == null)
        {
            return 1;
        }
        if (b.Termin == null)
        {
            return -1;
        }

        int result = a.Termin.Value.CompareTo(b.Termin.Value);
        if (result != 0)
        {
            return result;
        }
        return b.Priorytet.CompareTo(a.Priorytet);
    }

    public static int ByPriority(TaskItem a, TaskItem b)
    {
        int result = b.Priorytet.CompareTo(a.Priorytet);
        if (result != 0)
        {
            return result;
        }
        return ByDeadline(a, b);
    }

    public static int ByTag(TaskItem a, TaskItem b)
    {
        string tagA = GetFirstTag(a);
        string tagB = GetFirstTag(b);

        if (tagA == "" && tagB == "")
        {
            return ByDeadline(a, b);
        }
        if (tagA == "")
        {
            return 1;
        }
        if (tagB == "")
        {
            return -1;
        }

        int result = string.Compare(tagA, tagB, StringComparison.CurrentCultureIgnoreCase);
        if (result != 0)
        {
            return result;
        }
        return ByDeadline(a, b);
    }

    private static string GetFirstTag(TaskItem task)
    {
        string first = "";
        foreach (string tag in task.Tagi)
        {
            if (first == "" || string.Compare(tag, first, StringComparison.CurrentCultureIgnoreCase) < 0)
            {
                first = tag;
            }
        }
        return first;
    }
}
