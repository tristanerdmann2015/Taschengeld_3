using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Taschengeld_3.Models;
using Taschengeld_3.Services;

namespace Taschengeld_3.ViewModels;

public class TimeEntryViewModel : INotifyPropertyChanged
{
    private readonly DatabaseService _databaseService;
    private ObservableCollection<TimeEntry> _timeEntries = new();
    private ObservableCollection<TaskItem> _availableTasks = new();
    private TimeEntry? _selectedEntry;
    private TaskItem? _selectedTask;
    private DateTime _entryDate = DateTime.Now;
    private TimeSpan _startTime = new(12, 0, 0);
    private double _durationInHours = 1;
    private int _count = 1;
    private string _durationInHoursText = "1,00";
    private string _countText = "1";
    private DateTime _filterStartDate = DateTime.Now.AddMonths(-3);
    private DateTime _filterEndDate = DateTime.Now;
    private bool _isFormVisible = false;

    public ObservableCollection<TimeEntry> TimeEntries
    {
        get => _timeEntries;
        set
        {
            _timeEntries = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<TaskItem> AvailableTasks
    {
        get => _availableTasks;
        set
        {
            _availableTasks = value;
            OnPropertyChanged();
        }
    }

    public TimeEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            _selectedEntry = value;
            OnPropertyChanged();
            if (value != null)
            {
                SelectedTask = value.Task;
                EntryDate = value.EntryDate;
                StartTime = value.StartTime;
                DurationInHours = value.DurationInHours;
                Count = value.Count;
            }
        }
    }

    public TaskItem? SelectedTask
    {
        get => _selectedTask;
        set
        {
            _selectedTask = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsTaskSelected));
            OnPropertyChanged(nameof(TaskSelectionHint));
        }
    }

    // Zeigt an ob eine Aufgabe ausgewählt wurde
    public bool IsTaskSelected => _selectedTask != null;

    // Zeigt "- Bitte auswählen" wenn keine Aufgabe ausgewählt
    public string TaskSelectionHint => _selectedTask == null ? " - Bitte auswählen" : "";

    public DateTime EntryDate
    {
        get => _entryDate;
        set
        {
            _entryDate = value;
            OnPropertyChanged();
        }
    }

    public TimeSpan StartTime
    {
        get => _startTime;
        set
        {
            _startTime = value;
            OnPropertyChanged();
        }
    }

    public double DurationInHours
    {
        get => _durationInHours;
        set
        {
            _durationInHours = value;
            _durationInHoursText = value.ToString("F2").Replace('.', ',');
            OnPropertyChanged();
            OnPropertyChanged(nameof(DurationInHoursText));
        }
    }

    // String-Wrapper für DurationInHours - erlaubt Backspace und Komma-Eingabe
    public string DurationInHoursText
    {
        get => _durationInHoursText;
        set
        {
            _durationInHoursText = value;
            OnPropertyChanged();
            
            if (!string.IsNullOrWhiteSpace(value))
            {
                var normalizedValue = value.Replace(',', '.');
                if (double.TryParse(normalizedValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result))
                {
                    _durationInHours = result;
                    OnPropertyChanged(nameof(DurationInHours));
                }
            }
        }
    }

    public int Count
    {
        get => _count;
        set
        {
            _count = value;
            _countText = value.ToString();
            OnPropertyChanged();
            OnPropertyChanged(nameof(CountText));
        }
    }

    // String-Wrapper für Count - erlaubt Backspace
    public string CountText
    {
        get => _countText;
        set
        {
            _countText = value;
            OnPropertyChanged();
            
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (int.TryParse(value, out int result))
                {
                    _count = result;
                    OnPropertyChanged(nameof(Count));
                }
            }
        }
    }

    public DateTime FilterStartDate
    {
        get => _filterStartDate;
        set
        {
            _filterStartDate = value;
            OnPropertyChanged();
        }
    }

    public DateTime FilterEndDate
    {
        get => _filterEndDate;
        set
        {
            _filterEndDate = value;
            OnPropertyChanged();
        }
    }

    public bool IsFormVisible
    {
        get => _isFormVisible;
        set
        {
            _isFormVisible = value;
            OnPropertyChanged();
        }
    }

    public TimeEntryViewModel()
    {
        _databaseService = ServiceHelper.GetService<DatabaseService>()!;
        // Don't load data in constructor - wait until the page actually appears
    }

    public async Task LoadData()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("LoadData: Starting...");
            
            System.Diagnostics.Debug.WriteLine("LoadData: Calling LoadTasks...");
            await LoadTasks();
            System.Diagnostics.Debug.WriteLine($"LoadData: Loaded {AvailableTasks.Count} tasks");
            
            System.Diagnostics.Debug.WriteLine("LoadData: Validating TimeEntry data consistency...");
            await ValidateAndCleanTimeEntries();
            System.Diagnostics.Debug.WriteLine("LoadData: TimeEntry validation completed");
            
            System.Diagnostics.Debug.WriteLine("LoadData: Calling ApplyFilter...");
            await ApplyFilter();
            System.Diagnostics.Debug.WriteLine($"LoadData: Loaded {TimeEntries.Count} time entries");
            
            System.Diagnostics.Debug.WriteLine("LoadData: Completed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadData Exception caught: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"LoadData Error Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"LoadData StackTrace: {ex.StackTrace}");
            // Don't rethrow - just log and continue with empty collections
        }
    }

    private async Task ValidateAndCleanTimeEntries()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("ValidateAndCleanTimeEntries: Starting validation...");
            
            if (_databaseService == null)
            {
                System.Diagnostics.Debug.WriteLine("ValidateAndCleanTimeEntries: DatabaseService is null");
                return;
            }

            // Get all available task IDs
            var validTaskIds = AvailableTasks.Select(t => t.Id).ToHashSet();
            System.Diagnostics.Debug.WriteLine($"ValidateAndCleanTimeEntries: Valid task IDs: {string.Join(", ", validTaskIds)}");

            // Get all time entries
            var allEntries = await _databaseService.GetAllTimeEntries();
            System.Diagnostics.Debug.WriteLine($"ValidateAndCleanTimeEntries: Found {allEntries.Count} total time entries");

            // Find entries with invalid task references
            var invalidEntries = allEntries.Where(e => !validTaskIds.Contains(e.TaskId)).ToList();
            System.Diagnostics.Debug.WriteLine($"ValidateAndCleanTimeEntries: Found {invalidEntries.Count} entries with invalid task references");

            if (invalidEntries.Count > 0)
            {
                // Show confirmation dialog
                var message = $"Es wurden {invalidEntries.Count} Zeiteinträge gefunden, deren verknüpfte Aufgaben nicht mehr existieren.\n\n";
                message += "Folgende Einträge sind betroffen:\n";
                foreach (var entry in invalidEntries.Take(5))
                {
                    message += $"  - Eintrag von {entry.EntryDate:dd.MM.yyyy}, Task-ID: {entry.TaskId}\n";
                }
                if (invalidEntries.Count > 5)
                {
                    message += $"  ... und {invalidEntries.Count - 5} weitere\n";
                }
                message += "\nMöchten Sie diese Einträge löschen?";

                var confirm = await DialogService.DisplayConfirmAsync(
                    "Ungueltige Zeiteintraege",
                    message,
                    "Ja, loeschen",
                    "Nein, behalten");

                if (confirm)
                {
                    System.Diagnostics.Debug.WriteLine($"ValidateAndCleanTimeEntries: User confirmed deletion of {invalidEntries.Count} entries");
                    
                    // Delete invalid entries
                    foreach (var entry in invalidEntries)
                    {
                        try
                        {
                            await _databaseService.DeleteTimeEntry(entry.Id);
                            System.Diagnostics.Debug.WriteLine($"ValidateAndCleanTimeEntries: Deleted entry {entry.Id}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"ValidateAndCleanTimeEntries: Error deleting entry {entry.Id}: {ex.Message}");
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"ValidateAndCleanTimeEntries: Deletion completed");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ValidateAndCleanTimeEntries: User declined deletion");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ValidateAndCleanTimeEntries: All entries are valid");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ValidateAndCleanTimeEntries ERROR: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ValidateAndCleanTimeEntries StackTrace: {ex.StackTrace}");
        }
    }

    private async Task LoadTasks()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("LoadTasks: Starting...");
            System.Diagnostics.Debug.WriteLine("LoadTasks: Getting database service...");
            
            if (_databaseService == null)
            {
                System.Diagnostics.Debug.WriteLine("LoadTasks: ERROR - DatabaseService is null!");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine("LoadTasks: Calling GetAllTasks...");
            var tasks = await _databaseService.GetAllTasks();
            System.Diagnostics.Debug.WriteLine($"LoadTasks: Retrieved {tasks.Count} tasks from database");
            
            System.Diagnostics.Debug.WriteLine("LoadTasks: Creating new ObservableCollection...");
            AvailableTasks = new ObservableCollection<TaskItem>(tasks);
            System.Diagnostics.Debug.WriteLine($"LoadTasks: Created collection with {AvailableTasks.Count} items");
            System.Diagnostics.Debug.WriteLine("LoadTasks: Completed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadTasks CAUGHT Exception: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"LoadTasks Error Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"LoadTasks StackTrace: {ex.StackTrace}");
            // Don't rethrow - log and continue
            AvailableTasks = new ObservableCollection<TaskItem>();
        }
    }

    public async Task ApplyFilter()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ApplyFilter: Starting... FilterStartDate={FilterStartDate}, FilterEndDate={FilterEndDate}");
            
            if (_databaseService == null)
            {
                System.Diagnostics.Debug.WriteLine("ApplyFilter: ERROR - DatabaseService is null!");
                _timeEntries.Clear();
                return;
            }
            
            System.Diagnostics.Debug.WriteLine("ApplyFilter: Calling GetTimeEntriesByDate...");
            var entries = await _databaseService.GetTimeEntriesByDate(FilterStartDate, FilterEndDate.AddDays(1));
            System.Diagnostics.Debug.WriteLine($"ApplyFilter: Retrieved {entries.Count} entries from database");
            
            System.Diagnostics.Debug.WriteLine("ApplyFilter: Clearing existing entries...");
            _timeEntries.Clear();
            System.Diagnostics.Debug.WriteLine("ApplyFilter: Entries cleared");
            
            var sortedEntries = entries.OrderByDescending(e => e.EntryDate).ThenByDescending(e => e.StartTime).ToList();
            System.Diagnostics.Debug.WriteLine($"ApplyFilter: Sorted {sortedEntries.Count} entries");
            
            System.Diagnostics.Debug.WriteLine("ApplyFilter: Adding entries to collection...");
            foreach (var entry in sortedEntries)
            {
                _timeEntries.Add(entry);
            }
            System.Diagnostics.Debug.WriteLine($"ApplyFilter: Added {_timeEntries.Count} entries to collection");
            System.Diagnostics.Debug.WriteLine("ApplyFilter: Completed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ApplyFilter CAUGHT Exception: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"ApplyFilter Error Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ApplyFilter StackTrace: {ex.StackTrace}");
            // Don't rethrow - just log and show empty list
            _timeEntries.Clear();
        }
    }

    public async Task SaveEntry()
    {
        if (SelectedTask == null)
        {
            await DialogService.DisplayAlertAsync("Fehler", "Bitte waehlen Sie eine Aufgabe aus.", "OK");
            return;
        }

        var entry = SelectedEntry ?? new TimeEntry();
        entry.TaskId = SelectedTask.Id;
        entry.Task = SelectedTask;
        entry.EntryDate = EntryDate;
        entry.StartTime = StartTime;
        entry.DurationInHours = DurationInHours;
        entry.Count = Count;
        entry.CalculatePrice();

        await _databaseService.SaveTimeEntry(entry);
        await LoadData();
        ClearForm();
    }

    public async Task DeleteEntry()
    {
        if (SelectedEntry == null) return;

        var confirm = await DialogService.DisplayConfirmAsync("Bestaetigung",
            "Moechten Sie diesen Eintrag wirklich loeschen?", "Ja", "Nein");

        if (confirm)
        {
            await _databaseService.DeleteTimeEntry(SelectedEntry.Id);
            await LoadData();
            ClearForm();
        }
    }

    public void ClearForm()
    {
        SelectedTask = null;
        EntryDate = DateTime.Now;
        StartTime = new TimeSpan(12, 0, 0);
        DurationInHours = 1;
        Count = 1;
        SelectedEntry = null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

