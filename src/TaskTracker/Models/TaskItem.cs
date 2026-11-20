using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTracker.Models
{
    public enum Priority
    {
        Niski = 1,
        Sredni = 2,
        Wysoki = 3
    }

    public class TaskItem : ObservableObject
    {
        private int id;
        private string tytul = string.Empty;
        private string opis = string.Empty;
        private DateOnly? termin;
        private Priority priorytet = Priority.Sredni;
        private bool wykonane;
        private List<string> tagi = [];

        [JsonPropertyName("id")]
        public int Id
        {
            get { return id; }
            set { SetProperty(ref id, value); }
        }

        [JsonPropertyName("tytul")]
        public string Tytul
        {
            get { return tytul; }
            set { SetProperty(ref tytul, value); }
        }

        [JsonPropertyName("opis")]
        public string Opis
        {
            get { return opis; }
            set { SetProperty(ref opis, value); }
        }

        [JsonPropertyName("termin")]
        public DateOnly? Termin
        {
            get { return termin; }
            set { SetProperty(ref termin, value); }
        }

        [JsonPropertyName("priorytet")]
        public Priority Priorytet
        {
            get { return priorytet; }
            set { SetProperty(ref priorytet, value); }
        }

        [JsonPropertyName("wykonane")]
        public bool Wykonane
        {
            get { return wykonane; }
            set { SetProperty(ref wykonane, value); }
        }

        [JsonPropertyName("tagi")]
        public List<string> Tagi
        {
            get { return tagi; }
            set { SetProperty(ref tagi, value); }
        }

        // kopia do edycji - "Anuluj" nie rusza oryginału
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