using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TaskTracker.Models
{
    public enum  Priority
    {
        Niski  =1,
        Sredni =2,
        Wysoki =3
    }


    public class TaskItem
    {
        [JsonPropertyName("id")] 
        public int Id { get; set; }
        
        [JsonPropertyName("tytul")] 
        public string Tytul { get; set; } = String.Empty;  //może być pusty
        
        [JsonPropertyName("opis")]
        public string Opis { get; set; } = String.Empty;   //może być pusty
        
        [JsonPropertyName("termin")]
        public DateOnly? Termin { get; set; }              //może być null
        
        [JsonPropertyName("priorytet")]
        public Priority Priorytet { get; set; } = Priority.Sredni;
        
        [JsonPropertyName("wykonane")]
        public bool Wykonane { get; set; }
        
        [JsonPropertyName("tagi")]
        public List<string> Tagi { get; set; } = [];       //domyślnie pusta


        //funkcja pomocnicza - aby user podczas edycji działa na kopii zadania
        //  i gdy kliknie 'anuluj' oryginał nie będzie zmieniony
        public TaskItem Clone() => new()
        {
            Id = Id,
            Tytul = Tytul,
            Opis = Opis,
            Termin = Termin,
            Priorytet = Priorytet,
            Wykonane = Wykonane,
            Tagi = [.. Tagi]
        };

        public override string ToString()
        {
            var status = Wykonane ? "WYKONANE" : " ----- ";
            var date = Termin?.ToString("yyyy-MM-dd") ?? "brak terminu";
            return $"#{Id} {Tytul} - {status} - ({Priorytet}, {date})";
        }
    }
}
