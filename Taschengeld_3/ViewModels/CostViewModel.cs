using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Taschengeld_3.Models;
using Taschengeld_3.Services;

namespace Taschengeld_3.ViewModels;

public class CostViewModel : INotifyPropertyChanged
{
    private readonly DatabaseService _databaseService;
    private ObservableCollection<CostSummary> _costSummaries = new();
    private int _selectedYear = DateTime.Now.Year;
    private int _selectedMonth = DateTime.Now.Month;
    private int _selectedQuarter = 1;
    private CostViewMode _viewMode = CostViewMode.Monthly;
    private ObservableCollection<TaskItem> _availableTasks = new();
    private TaskItem? _selectedTask;

    public enum CostViewMode
    {
        Weekly,
        Monthly,
        Quarterly
    }

    public List<int> Years { get; } = Enumerable.Range(DateTime.Now.Year - 5, 10).ToList();
    public List<int> Months { get; } = Enumerable.Range(1, 12).ToList();
    public List<int> Quarters { get; } = new List<int> { 1, 2, 3, 4 };
    public List<string> ViewModes { get; } = new List<string> { "Weekly", "Monthly", "Quarterly" };

    public ObservableCollection<CostSummary> CostSummaries
    {
        get => _costSummaries;
        set
        {
            _costSummaries = value;
            OnPropertyChanged();
        }
    }

    public int SelectedYear
    {
        get => _selectedYear;
        set
        {
            _selectedYear = value;
            OnPropertyChanged();
        }
    }

    public int SelectedMonth
    {
        get => _selectedMonth;
        set
        {
            _selectedMonth = value;
            OnPropertyChanged();
        }
    }

    public int SelectedQuarter
    {
        get => _selectedQuarter;
        set
        {
            _selectedQuarter = value;
            OnPropertyChanged();
        }
    }

    public CostViewMode ViewMode
    {
        get => _viewMode;
        set
        {
            _viewMode = value;
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

    public TaskItem? SelectedTask
    {
        get => _selectedTask;
        set
        {
            _selectedTask = value;
            OnPropertyChanged();
        }
    }

    public CostViewModel()
    {
        _databaseService = ServiceHelper.GetService<DatabaseService>()!;
    }

    public async Task LoadData()
    {
        await LoadTasks();
        await LoadCostSummaries();
    }

    private async Task LoadTasks()
    {
        var tasks = await _databaseService.GetAllTasks();
        // Füge "Alle Aufgaben" als erste Option hinzu
        var taskList = new List<TaskItem> 
        { 
            new TaskItem { Id = 0, Name = "Alle Aufgaben" }
        };
        taskList.AddRange(tasks);
        AvailableTasks = new ObservableCollection<TaskItem>(taskList);
        // Setze "Alle Aufgaben" als default
        SelectedTask = AvailableTasks.FirstOrDefault();
    }

    public async Task LoadCostSummaries()
    {
        List<CostSummary> summaries = new();

        switch (ViewMode)
        {
            case CostViewMode.Weekly:
                summaries = await _databaseService.GetWeeklyCostSummariesForMonth(SelectedYear, SelectedMonth);
                break;
            case CostViewMode.Monthly:
                // Wenn ein spezifischer Monat ausgewählt ist, nur diesen laden
                var monthlySummary = await _databaseService.GetMonthlyCostSummary(SelectedYear, SelectedMonth);
                summaries.Add(monthlySummary);
                break;
            case CostViewMode.Quarterly:
                var summary = await _databaseService.GetQuarterlyCostSummary(SelectedYear, SelectedQuarter);
                summaries.Add(summary);
                break;
        }

        // Filter by task if selected (aber nicht wenn "Alle Aufgaben" (Id=0) ausgewählt ist)
        if (SelectedTask?.Id > 0)
        {
            foreach (var summary in summaries)
            {
                summary.TaskCosts = summary.TaskCosts
                    .Where(tc => tc.TaskName == SelectedTask.Name)
                    .ToList();
            }
        }

        CostSummaries = new ObservableCollection<CostSummary>(summaries);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

