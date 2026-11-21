# TaskTracker

Desktopowa aplikacja do zarządzania zadaniami napisana w C# (.NET 10) i Avalonia UI.

## Wymagania

- **Gotowy plik `.exe` (Windows x64):** brak dodatkowych wymagań - nie trzeba instalować .NET ani żadnych bibliotek.
- **Uruchomienie z kodu źródłowego / testy:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (Windows, Linux lub macOS).

## Uruchomienie aplikacji

### Opcja 1: Gotowy plik wykonywalny (Windows – najszybsza)

1. Pobierz pliki `TaskTracker.exe` oraz przykładowy plik z danymi `zadania.json` z zakładki [Releases](https://github.com/michmro04/TaskTracker/releases/latest).
2. Uruchom `TaskTracker.exe` dwuklikiem *(jeśli Windows SmartScreen wyświetli ostrzeżenie dla niepodpisanego pliku, kliknij **Więcej informacji → Uruchom mimo to**)*.
3. W aplikacji kliknij **Plik ▾ → Wczytaj...** i wybierz pobrany plik `zadania.json`.

### Opcja 2: Z terminala (CLI)

```bash
git clone https://github.com/michmro04/TaskTracker.git
cd TaskTracker
dotnet run --project src/TaskTracker
```

### Opcja 3: Z IDE (Visual Studio / JetBrains Rider)

1. Otwórz plik rozwiązania `TaskTracker.slnx`.
2. Ustaw projekt `TaskTracker` jako startowy i uruchom aplikację (`F5`).

### Przykładowe dane testowe

Gotowy plik z zestawem zadań znajduje się w repozytorium w `data/zadania.json` oraz w załącznikach w zakładce [Releases](https://github.com/michmro04/TaskTracker/releases/latest). Można go wczytać w dowolnym momencie przez **Plik ▾ → Wczytaj...**.

## Uruchomienie testów

Będąc w głównym katalogu repozytorium:

```bash
dotnet test
```

---

## Funkcje aplikacji

### Trzy główne widoki
- **Do zrobienia** - lista aktywnych zadań z filtrowaniem po tagach, sortowaniem (`Termin`, `Priorytet`, `Tag`) w obu kierunkach (`↑` / `↓`) oraz przełącznikiem układu **Kafelki / Lista**.
- **Archiwum** - lista zadań wykonanych z możliwością przywrócenia zadania do panelu „Do zrobienia” przez odznaczenie checkboxa oraz z przełącznikiem **Kafelki / Lista**.
- **Smart** - panel planowania zadań z osią czasu, ułatwiający ustalenie kolejności pracy na najbliższe dni.

### Zarządzanie zadaniami
- **Dodawanie i edycja** - dedykowany formularz z walidacją pól (wymagany tytuł; blokada terminu z przeszłości przy tworzeniu nowego zadania, z zachowaniem możliwości edycji zadań zaległych).
- **Karty zadań** - oznaczenia priorytetów (`Wysoki`, `Średni`, `Niski`), wyróżnianie zadań po terminie, dwustopniowe potwierdzanie usunięcia bezpośrednio na karcie oraz adaptacyjny podgląd opisu (strzałka rozwijania pojawia się tylko wtedy, gdy tekst nie mieści się w jednej linii).

### Praca z plikami i motywami
- **Pliki JSON** - pełna obsługa w menu `Plik` (`Nowy`, `Wczytaj...`, `Zapisz`, `Zapisz jako...`), oznaczanie niezapisanych zmian (`*` w tytule okna + okno potwierdzenia przed zamknięciem) oraz automatyczne wczytywanie ostatnio otwartego pliku przy ponownym starcie.
- **Motywy** - 12 wariantów wyglądu (tryb Jasny / Ciemny oraz 6 palet akcentu) przełączanych bez restartu aplikacji.

## Jak korzystać

1. Uruchom aplikację i wczytaj istniejący plik JSON (`Plik ▾ → Wczytaj...`) lub utwórz nowy (`Plik ▾ → Nowy`).
2. Przełączaj się między panelami **Smart**, **Do zrobienia** i **Archiwum** z górnego paska nawigacji.
3. Kliknij **+ Dodaj nowe zadanie**, aby utworzyć wpis, albo **Edytuj** / **Usuń** na wybranej karcie.
4. Korzystaj z filtrów tagów, sortowania ze strzałką kierunku oraz przełącznika **Kafelki / Lista**, aby dopasować widok do liczby zadań.
5. Zapisz zmiany przez `Plik ▾ → Zapisz` (lub `Zapisz jako...`).

## Format pliku JSON

Aplikacja przechowuje zadania w kodowaniu UTF-8 w następującej strukturze:

```json
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
```

- `termin` – format `YYYY-MM-DD` lub `null` (brak terminu).
- `priorytet` – dozwolone wartości: `"niski"`, `"sredni"`, `"wysoki"`.
- `tagi` – lista tekstowa (może być pusta `[]`).

## Architektura i struktura projektu

Projekt wykorzystuje wzorzec **MVVM** (`CommunityToolkit.Mvvm`), interfejs w **Avalonia UI** oraz testy jednostkowe **xUnit**.

```text
TaskTracker/
├── data/
│   └── zadania.json              # Przykładowy zestaw zadań do testów
├── src/TaskTracker/
│   ├── Controls/                 # Własne kontrolki UI (TimelinePanel)
│   ├── Models/                   # Modele danych (TaskItem, TaskDocument)
│   ├── Services/                 # Zapis JSON, motywy, ustawienia, sortowanie, SmartPlanner
│   ├── ViewModels/               # Logika widoków (MVVM)
│   └── Views/                    # Widoki XAML (Avalonia)
└── tests/TaskTracker.Tests/      # Testy jednostkowe xUnit
```