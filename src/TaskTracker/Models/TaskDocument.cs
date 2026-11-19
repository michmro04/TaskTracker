using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace TaskTracker.Models
{
    public class TaskDocument
    {
        [JsonPropertyName("wersja")] private int wersja = 1;
        [JsonPropertyName("zadania")] private List<TaskItem> tasksItems = [];
    }
}
