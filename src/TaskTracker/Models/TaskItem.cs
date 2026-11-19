using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TaskTracker.Models
{
    public enum  Priority
    {
        niski  =1,
        sredni =2,
        wysoki =3
    }


    public class TaskItem
    {
        [JsonPropertyName("id")] 
        public int Id { get; set; }
        
        [JsonPropertyName("tytul")] 
        private string Tytul { get; set; } = String.Empty;  //może być pusty
        
        [JsonPropertyName("opis")] 
        private string Opis { get; set; } = String.Empty;   //może być pusty
        
        [JsonPropertyName("termin")] 
        private DateOnly? Termin { get; set; }              //może być null
        
        [JsonPropertyName("priorytet")] 
        private Priority Priorytet { get; set; } = Priority.sredni;
        
        [JsonPropertyName("wykonane")] 
        private bool Wykonane { get; set; }
        
        [JsonPropertyName("tagi")] 
        private List<string> Tagi { get; set; } = [];       //domyślnie pusta


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
            var status = Wykonane ? "wykonane" : " --- ";
            var date = Termin?.ToString("yyyy-MM-dd") ?? "brak terminu";
            return $"#{Id} {Tytul} - {status} - ({Priorytet}, {date})";
        }
    }
}
