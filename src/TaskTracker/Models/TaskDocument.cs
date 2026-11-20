using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TaskTracker.Models
{
    public class TaskDocument
    {
        [JsonPropertyName("wersja")] 
        public int Wersja { get; set; } = 1;
        
        [JsonPropertyName("zadania")] 
        public List<TaskItem> Zadania { get; set; } = [];
    }
}
